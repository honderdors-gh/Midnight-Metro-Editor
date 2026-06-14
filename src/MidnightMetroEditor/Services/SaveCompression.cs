using System.IO.Compression;
using System.Text;
using MidnightMetroEditor.Models;

namespace MidnightMetroEditor.Services;

public static class SaveCompression
{
    public const int MagicV2 = 0x43535932; // 'CYS2'
    public const int CurrentVersion = 3;

    public static byte[] Encode(string json)
    {
        if (string.IsNullOrEmpty(json))
            return Array.Empty<byte>();

        var payload = Encoding.UTF8.GetBytes(json);
        using var output = new MemoryStream();
        using (var writer = new BinaryWriter(output, Encoding.UTF8, leaveOpen: true))
        {
            writer.Write(MagicV2);
            writer.Write(payload.Length);
        }

        using (var gzip = new GZipStream(output, CompressionLevel.Optimal, leaveOpen: true))
            gzip.Write(payload, 0, payload.Length);

        return output.ToArray();
    }

    public static bool TryDecode(byte[] bytes, out string json)
    {
        json = string.Empty;
        if (bytes.Length < 8)
            return false;

        if (BitConverter.ToInt32(bytes, 0) != MagicV2)
            return false;

        var payloadLength = BitConverter.ToInt32(bytes, 4);
        if (payloadLength < 0 || payloadLength > 256 * 1024 * 1024)
            return false;

        using var input = new MemoryStream(bytes, 8, bytes.Length - 8);
        using var gzip = new GZipStream(input, CompressionMode.Decompress);
        using var reader = new BinaryReader(gzip, Encoding.UTF8);
        var payload = reader.ReadBytes(payloadLength);
        json = Encoding.UTF8.GetString(payload);
        return true;
    }

    public static string ReadSaveText(string path)
    {
        var bytes = File.ReadAllBytes(path);
        return TryDecode(bytes, out var json) ? json : Encoding.UTF8.GetString(bytes);
    }

    public static void WriteSave(string path, CitySimSaveFile file, bool createBackup = true)
    {
        if (createBackup && File.Exists(path))
        {
            var backup = path + ".bak";
            File.Copy(path, backup, overwrite: true);
        }

        file.version = CurrentVersion;
        file.savedUtc = DateTime.UtcNow.ToString("o");
        var json = SaveJson.Serialize(file);
        File.WriteAllBytes(path, Encode(json));
    }
}

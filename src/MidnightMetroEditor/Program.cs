namespace MidnightMetroEditor;

using MidnightMetroEditor.Models;
using MidnightMetroEditor.Services;

static class Program
{
    [STAThread]
    static int Main(string[] args)
    {
        if (args.Length >= 2 && args[0] == "--reset-metro")
        {
            var path = args[1];
            try
            {
                MetroSaveRepair.ResetMetroNetwork(path, createBackup: true);
                var json = Services.SaveCompression.ReadSaveText(path);
                Console.WriteLine($"OK: reset metro in {path}");
                Console.WriteLine(MetroSaveRepair.DescribeAfterReset(json));
                return 0;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"FAIL: {ex.Message}");
                return 1;
            }
        }

        if (args.Length >= 2 && args[0] == "--verify")
        {
            var path = args[1];
            try
            {
                var json = Services.SaveCompression.ReadSaveText(path);
                if (MetroSaveGameFormat.IsGameSave(json))
                {
                    var info = MetroSaveVerify.ValidateGameSaveOrThrow(json);
                    var gameFile = MetroSaveJson.Deserialize(json);
                    var merged = MetroSaveJsonMerge.MergeOriginalWithFile(json, gameFile);
                    MetroSaveVerify.ValidateGameSaveOrThrow(merged);
                    Console.WriteLine($"OK: {path} (Midnight Metro game save)");
                    Console.WriteLine(MetroSaveVerify.Describe(info));
                    Console.WriteLine($"  editor schema v{MetroSaveSchema.CurrentSaveVersion}");
                    if (info.Version > MetroSaveSchema.CurrentSaveVersion)
                        Console.WriteLine("  WARN: save version newer than editor — run scripts/sync-save-schema.ps1");
                    return 0;
                }

                var file = Services.SaveJson.Deserialize(json);
                var reencoded = Services.SaveJson.Serialize(file);
                _ = Services.SaveJson.Deserialize(reencoded);
                Console.WriteLine($"OK: {path} (legacy editor schema)");
                Console.WriteLine($"  version={file.version} day={file.session?.day} city={file.session?.cityName}");
                Console.WriteLine($"  residents={file.residents?.rosterId?.Length ?? 0} agents={file.agents.Count} gangs={file.gangs?.gangs.Count ?? 0}");
                return 0;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"FAIL: {ex.Message}");
                return 1;
            }
        }

        ApplicationConfiguration.Initialize();
        Application.ThreadException += (_, e) => ShowFatalError(e.Exception);
        AppDomain.CurrentDomain.UnhandledException += (_, e) =>
        {
            if (e.ExceptionObject is Exception ex)
                ShowFatalError(ex);
        };

        Application.Run(new MainForm());
        return 0;
    }

    static void ShowFatalError(Exception ex)
    {
        MessageBox.Show(
            $"Midnight Metro Save Editor crashed:\n\n{ex.Message}\n\n{ex.StackTrace}",
            "Startup Error",
            MessageBoxButtons.OK,
            MessageBoxIcon.Error);
    }
}

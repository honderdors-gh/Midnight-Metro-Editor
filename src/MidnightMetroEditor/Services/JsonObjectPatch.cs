namespace MidnightMetroEditor.Services;

/// <summary>Brace-aware replacement for top-level JSON object properties (no full re-serialize).</summary>
public static class JsonObjectPatch
{
    public static string ReplaceObject(string json, string key, string replacementObjectJson)
    {
        var needle = $"\"{key}\":";
        var idx = json.IndexOf(needle, StringComparison.Ordinal);
        if (idx < 0)
            throw new InvalidDataException($"Save JSON is missing '{key}'.");

        var start = idx + needle.Length;
        while (start < json.Length && char.IsWhiteSpace(json[start]))
            start++;

        if (start >= json.Length || json[start] != '{')
            throw new InvalidDataException($"Expected object value for '{key}'.");

        var end = FindMatchingBrace(json, start);
        return json[..start] + replacementObjectJson + json[(end + 1)..];
    }

    public static string UpsertSessionInt(string json, string field, int value)
    {
        var sessionNeedle = "\"session\":";
        var sessionIdx = json.IndexOf(sessionNeedle, StringComparison.Ordinal);
        if (sessionIdx < 0)
            throw new InvalidDataException("Save JSON is missing 'session'.");

        var objStart = sessionIdx + sessionNeedle.Length;
        while (objStart < json.Length && char.IsWhiteSpace(json[objStart]))
            objStart++;
        if (json[objStart] != '{')
            throw new InvalidDataException("Expected session object.");

        var objEnd = FindMatchingBrace(json, objStart);
        var sessionBody = json.Substring(objStart + 1, objEnd - objStart - 1);

        var fieldNeedle = $"\"{field}\":";
        var fieldIdx = sessionBody.IndexOf(fieldNeedle, StringComparison.Ordinal);
        string patchedBody;
        if (fieldIdx >= 0)
        {
            var valStart = fieldIdx + fieldNeedle.Length;
            var valEnd = valStart;
            while (valEnd < sessionBody.Length && char.IsDigit(sessionBody[valEnd]))
                valEnd++;
            patchedBody = sessionBody[..valStart] + value + sessionBody[valEnd..];
        }
        else
        {
            var trimmed = sessionBody.TrimEnd();
            var needsComma = trimmed.Length > 0 && !trimmed.EndsWith(',');
            patchedBody = trimmed + (needsComma ? "," : "") + $"\"{field}\":{value}";
        }

        return json[..(objStart + 1)] + patchedBody + json[objEnd..];
    }

    static int FindMatchingBrace(string json, int openBraceIndex)
    {
        var depth = 0;
        var inString = false;
        var escape = false;

        for (var i = openBraceIndex; i < json.Length; i++)
        {
            var c = json[i];
            if (inString)
            {
                if (escape)
                    escape = false;
                else if (c == '\\')
                    escape = true;
                else if (c == '"')
                    inString = false;
                continue;
            }

            if (c == '"')
            {
                inString = true;
                continue;
            }

            if (c == '{')
                depth++;
            else if (c == '}')
            {
                depth--;
                if (depth == 0)
                    return i;
            }
        }

        throw new InvalidDataException("Unbalanced JSON braces while patching save.");
    }
}

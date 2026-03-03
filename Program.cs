using System.Text;

Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

if (args.Length < 2)
{
    PrintUsage();
    return 1;
}

var directory = args[0];
var targetEncodingArg = args[1];

if (!Directory.Exists(directory))
{
    Console.Error.WriteLine($"错误: 目录不存在 -> {directory}");
    return 2;
}

if (!TryParseTargetEncoding(targetEncodingArg, out var targetEncoding, out var targetDisplayName))
{
    Console.Error.WriteLine($"错误: 不支持的目标编码 -> {targetEncodingArg}");
    PrintSupportedEncodings();
    return 3;
}

var options = new EnumerationOptions
{
    RecurseSubdirectories = true,
    IgnoreInaccessible = true,
    AttributesToSkip = FileAttributes.System | FileAttributes.Temporary
};

var total = 0;
var converted = 0;
var skipped = 0;
var failed = 0;

Console.WriteLine($"开始扫描目录: {Path.GetFullPath(directory)}");
Console.WriteLine($"目标编码: {targetDisplayName}");
Console.WriteLine();

foreach (var file in Directory.EnumerateFiles(directory, "*", options))
{
    total++;

    try
    {
        if (!IsTextFileCandidate(file))
        {
            skipped++;
            continue;
        }

        var bytes = File.ReadAllBytes(file);
        if (bytes.Length == 0)
        {
            skipped++;
            continue;
        }

        if (!LooksLikeText(bytes))
        {
            skipped++;
            continue;
        }

        var sourceEncoding = DetectEncoding(bytes);
        var sourceText = sourceEncoding.GetString(bytes);
        var targetBytes = targetEncoding.GetBytes(sourceText);

        if (AreSameBytes(bytes, targetBytes))
        {
            skipped++;
            continue;
        }

        File.WriteAllBytes(file, targetBytes);
        converted++;

        Console.WriteLine($"[已转换] {file}");
        Console.WriteLine($"         {sourceEncoding.WebName} -> {targetDisplayName}");
    }
    catch (Exception ex)
    {
        failed++;
        Console.WriteLine($"[失败]   {file}");
        Console.WriteLine($"         {ex.Message}");
    }
}

Console.WriteLine();
Console.WriteLine("处理完成");
Console.WriteLine($"总文件数: {total}");
Console.WriteLine($"已转换:   {converted}");
Console.WriteLine($"跳过:     {skipped}");
Console.WriteLine($"失败:     {failed}");

return failed > 0 ? 4 : 0;

static bool IsTextFileCandidate(string path)
{
    var ext = Path.GetExtension(path);

    if (string.IsNullOrWhiteSpace(ext))
    {
        return true;
    }

    return KnownTextExtensions.Value.Contains(ext);
}

static bool LooksLikeText(byte[] data)
{
    if (data.Length == 0)
    {
        return false;
    }

    var nullCount = 0;
    var controlCount = 0;

    var max = Math.Min(data.Length, 4096);
    for (var i = 0; i < max; i++)
    {
        var b = data[i];
        if (b == 0)
        {
            nullCount++;
            continue;
        }

        if (b < 0x09)
        {
            controlCount++;
        }
        else if (b is > 0x0D and < 0x20)
        {
            controlCount++;
        }
    }

    return nullCount == 0 && controlCount < max * 0.02;
}

static Encoding DetectEncoding(byte[] data)
{
    if (data.Length >= 3 && data[0] == 0xEF && data[1] == 0xBB && data[2] == 0xBF)
    {
        return new UTF8Encoding(encoderShouldEmitUTF8Identifier: true, throwOnInvalidBytes: true);
    }

    if (data.Length >= 4 && data[0] == 0xFF && data[1] == 0xFE && data[2] == 0x00 && data[3] == 0x00)
    {
        return new UTF32Encoding(bigEndian: false, byteOrderMark: true, throwOnInvalidCharacters: true);
    }

    if (data.Length >= 4 && data[0] == 0x00 && data[1] == 0x00 && data[2] == 0xFE && data[3] == 0xFF)
    {
        return new UTF32Encoding(bigEndian: true, byteOrderMark: true, throwOnInvalidCharacters: true);
    }

    if (data.Length >= 2 && data[0] == 0xFF && data[1] == 0xFE)
    {
        return new UnicodeEncoding(bigEndian: false, byteOrderMark: true, throwOnInvalidBytes: true);
    }

    if (data.Length >= 2 && data[0] == 0xFE && data[1] == 0xFF)
    {
        return new UnicodeEncoding(bigEndian: true, byteOrderMark: true, throwOnInvalidBytes: true);
    }

    if (IsValidUtf8(data))
    {
        return new UTF8Encoding(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true);
    }

    if (LooksLikeUtf16LeWithoutBom(data))
    {
        return new UnicodeEncoding(bigEndian: false, byteOrderMark: false, throwOnInvalidBytes: false);
    }

    if (LooksLikeUtf16BeWithoutBom(data))
    {
        return new UnicodeEncoding(bigEndian: true, byteOrderMark: false, throwOnInvalidBytes: false);
    }

    return Encoding.GetEncoding("GB18030");
}

static bool IsValidUtf8(byte[] data)
{
    try
    {
        _ = new UTF8Encoding(false, true).GetString(data);
        return true;
    }
    catch
    {
        return false;
    }
}

static bool LooksLikeUtf16LeWithoutBom(byte[] data)
{
    var sampleSize = Math.Min(data.Length, 4096);
    var zeroOnOdd = 0;
    var checkedPairs = 0;

    for (var i = 0; i + 1 < sampleSize; i += 2)
    {
        checkedPairs++;
        if (data[i + 1] == 0)
        {
            zeroOnOdd++;
        }
    }

    return checkedPairs > 8 && zeroOnOdd > checkedPairs * 0.6;
}

static bool LooksLikeUtf16BeWithoutBom(byte[] data)
{
    var sampleSize = Math.Min(data.Length, 4096);
    var zeroOnEven = 0;
    var checkedPairs = 0;

    for (var i = 0; i + 1 < sampleSize; i += 2)
    {
        checkedPairs++;
        if (data[i] == 0)
        {
            zeroOnEven++;
        }
    }

    return checkedPairs > 8 && zeroOnEven > checkedPairs * 0.6;
}

static bool AreSameBytes(byte[] left, byte[] right)
{
    if (left.Length != right.Length)
    {
        return false;
    }

    for (var i = 0; i < left.Length; i++)
    {
        if (left[i] != right[i])
        {
            return false;
        }
    }

    return true;
}

static bool TryParseTargetEncoding(string input, out Encoding encoding, out string displayName)
{
    var normalized = input.Trim().ToLowerInvariant();

    switch (normalized)
    {
        case "utf8":
        case "utf-8":
            encoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);
            displayName = "utf-8";
            return true;

        case "utf8bom":
        case "utf-8-bom":
        case "utf8-bom":
            encoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: true);
            displayName = "utf-8-bom";
            return true;

        case "utf16":
        case "utf-16":
        case "utf16le":
        case "utf-16le":
            encoding = new UnicodeEncoding(bigEndian: false, byteOrderMark: true);
            displayName = "utf-16le";
            return true;

        case "utf16be":
        case "utf-16be":
            encoding = new UnicodeEncoding(bigEndian: true, byteOrderMark: true);
            displayName = "utf-16be";
            return true;

        case "utf32":
        case "utf-32":
        case "utf32le":
        case "utf-32le":
            encoding = new UTF32Encoding(bigEndian: false, byteOrderMark: true);
            displayName = "utf-32le";
            return true;

        case "utf32be":
        case "utf-32be":
            encoding = new UTF32Encoding(bigEndian: true, byteOrderMark: true);
            displayName = "utf-32be";
            return true;

        case "gb18030":
        case "gbk":
            encoding = Encoding.GetEncoding("GB18030");
            displayName = "gb18030";
            return true;

        case "ascii":
            encoding = Encoding.ASCII;
            displayName = "ascii";
            return true;

        case "latin1":
        case "iso-8859-1":
            encoding = Encoding.GetEncoding("iso-8859-1");
            displayName = "iso-8859-1";
            return true;

        default:
            try
            {
                encoding = Encoding.GetEncoding(input);
                displayName = encoding.WebName;
                return true;
            }
            catch
            {
                encoding = Encoding.UTF8;
                displayName = string.Empty;
                return false;
            }
    }
}

static void PrintUsage()
{
    Console.WriteLine("用法:");
    Console.WriteLine("  ConverToUTF8 <目录> <目标编码>");
    Console.WriteLine();
    PrintSupportedEncodings();
}

static void PrintSupportedEncodings()
{
    Console.WriteLine("常见目标编码示例:");
    Console.WriteLine("  utf8        (UTF-8 无 BOM)");
    Console.WriteLine("  utf8-bom    (UTF-8 带 BOM)");
    Console.WriteLine("  utf16le");
    Console.WriteLine("  utf16be");
    Console.WriteLine("  utf32le");
    Console.WriteLine("  utf32be");
    Console.WriteLine("  gb18030");
    Console.WriteLine("  ascii");
    Console.WriteLine("  iso-8859-1");
}

static class KnownTextExtensions
{
    public static readonly HashSet<string> Value = new(StringComparer.OrdinalIgnoreCase)
    {
        ".txt", ".csv", ".log", ".md", ".json", ".xml", ".yaml", ".yml",
        ".ini", ".config", ".props", ".targets", ".editorconfig", ".sln",
        ".cs", ".csproj", ".fs", ".vb", ".java", ".kt", ".scala", ".go", ".rs",
        ".c", ".h", ".cpp", ".hpp", ".cc", ".hh", ".m", ".mm",
        ".js", ".jsx", ".ts", ".tsx", ".vue", ".html", ".htm", ".css", ".scss", ".less",
        ".py", ".rb", ".php", ".sh", ".bat", ".ps1", ".cmd", ".sql"
    };
}

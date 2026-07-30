namespace CsvLauncher;

internal sealed record AppOptions(
    string? CsvPath,
    string? FolderId,
    bool ShowHelp);

internal static class AppOptionsParser
{
    public static AppOptions Parse(string[] args)
    {
        string? csvPath = null;
        string? folderId = null;
        var showHelp = false;

        foreach (var arg in args)
        {
            if (arg is "--help" or "-h" or "/?")
            {
                showHelp = true;
                continue;
            }

            if (arg.StartsWith("--folder-id=", StringComparison.OrdinalIgnoreCase))
            {
                folderId = arg["--folder-id=".Length..].Trim();
                continue;
            }

            if (arg.StartsWith("--", StringComparison.Ordinal))
            {
                throw new ArgumentException($"不明な引数です: {arg}", nameof(args));
            }

            if (!string.IsNullOrWhiteSpace(csvPath))
            {
                throw new ArgumentException("CSV ファイルパスは 1 つだけ指定してください。", nameof(args));
            }

            csvPath = arg;
        }

        return new AppOptions(csvPath, folderId, showHelp);
    }

}

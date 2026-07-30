using Google.Apis.Drive.v3;
using DriveFile = Google.Apis.Drive.v3.Data.File;
using Google.Apis.Services;

namespace CsvLauncher;

internal sealed class DriveUploader
{
    private const string ApplicationName = "CsvLauncher";
    private const string EmptySpreadsheetNamePrefix = "New Spreadsheet";
    private const string SpreadsheetMimeType = "application/vnd.google-apps.spreadsheet";
    private const string CsvMimeType = "text/csv";

    private readonly DriveService _drive;

    public DriveUploader(Google.Apis.Auth.OAuth2.UserCredential credential)
    {
        _drive = new DriveService(new BaseClientService.Initializer
        {
            HttpClientInitializer = credential,
            ApplicationName = ApplicationName,
        });
    }

    public async Task<string> CreateSpreadsheetAsync(
        string? csvPath,
        string? folderId,
        CancellationToken cancellationToken)
    {
        DriveFile created;

        if (string.IsNullOrWhiteSpace(csvPath))
        {
            var spreadsheetName = $"{EmptySpreadsheetNamePrefix} {DateTime.Now:yyyyMMdd-HHmmss}";
            var metadata = CreateSpreadsheetMetadata(spreadsheetName, folderId);
            created = await CreateNewSpreadsheet(metadata, cancellationToken);
        }
        else
        {
            var fullCsvPath = ResolveFullCsvPath(csvPath);
            var spreadsheetName = Path.GetFileNameWithoutExtension(csvPath);
            var metadata = CreateSpreadsheetMetadata(spreadsheetName, folderId);
            created = await UploadCsvToDrive(fullCsvPath, metadata, cancellationToken);
        }

        if (created.Id is null)
        {
            throw new InvalidOperationException("スプレッドシート作成後のファイルIDが取得できませんでした。");
        }

        return created.Id;
    }

    private async Task<DriveFile> CreateNewSpreadsheet(DriveFile metadata, CancellationToken cancellationToken)
    {
        var create = _drive.Files.Create(metadata);
        create.Fields = "id";
        return await create.ExecuteAsync(cancellationToken);
    }

    private async Task<DriveFile> UploadCsvToDrive(string fullCsvPath, DriveFile metadata, CancellationToken cancellationToken)
    {
        await using var csvStream = await TextEncodingConverter.OpenUtf8StreamAsync(fullCsvPath, cancellationToken);

        var create = _drive.Files.Create(metadata, csvStream, CsvMimeType);
        create.Fields = "id";

        var uploadResult = await create.UploadAsync(cancellationToken);
        if (uploadResult.Status != Google.Apis.Upload.UploadStatus.Completed)
        {
            throw new InvalidOperationException($"アップロードに失敗しました: {uploadResult.Exception?.Message}");
        }

        var created = create.ResponseBody;
        if (created is null)
        {
            throw new InvalidOperationException("アップロード後のレスポンス本文が取得できませんでした。");
        }

        return created;
    }

    private static string ResolveFullCsvPath(string csvPath)
    {
        var fullCsvPath = Path.GetFullPath(csvPath);
        if (!File.Exists(fullCsvPath))
        {
            throw new FileNotFoundException($"CSV ファイルが見つかりません: {fullCsvPath}", fullCsvPath);
        }

        return fullCsvPath;
    }

    private static DriveFile CreateSpreadsheetMetadata(string spreadsheetName, string? folderId)
    {
        var metadata = new DriveFile
        {
            Name = spreadsheetName,
            MimeType = SpreadsheetMimeType,
        };

        if (!string.IsNullOrWhiteSpace(folderId))
        {
            metadata.Parents = [folderId];
        }

        return metadata;
    }

}

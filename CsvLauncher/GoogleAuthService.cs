using Google.Apis.Auth.OAuth2;
using Google.Apis.Drive.v3;
using Google.Apis.Util.Store;
using System.Text.Json;

namespace CsvLauncher;

internal sealed class GoogleAuthService
{
    private static readonly string[] Scopes = [DriveService.Scope.DriveFile];

    private readonly string _appDataDir;
    private readonly string _tokenPath;

    public GoogleAuthService()
    {
        _appDataDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "csv-launcher");
        _tokenPath = Path.Combine(_appDataDir, "token.json");
    }

    public async Task<UserCredential> GetCredentialAsync(CancellationToken cancellationToken)
    {
        Directory.CreateDirectory(_appDataDir);

        if (string.IsNullOrWhiteSpace(EmbeddedGoogleOAuth.ClientId) ||
            string.IsNullOrWhiteSpace(EmbeddedGoogleOAuth.ClientSecret))
        {
            throw new InvalidOperationException(
                "OAuth client settings are not embedded in this build artifact.");
        }

        var secrets = new ClientSecrets
        {
            ClientId = EmbeddedGoogleOAuth.ClientId,
            ClientSecret = EmbeddedGoogleOAuth.ClientSecret,
        };

        return await GoogleWebAuthorizationBroker.AuthorizeAsync(
            secrets,
            Scopes,
            "user",
            cancellationToken,
            new SingleFileTokenStore(_tokenPath));
    }

    private sealed class SingleFileTokenStore(string tokenPath) : IDataStore
    {
        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            WriteIndented = true,
        };

        public Task StoreAsync<T>(string key, T value)
        {
            var json = JsonSerializer.Serialize(value, JsonOptions);
            return WriteTokenAtomicallyAsync(json);
        }

        public Task DeleteAsync<T>(string key)
        {
            return DeleteTokenFileAsync();
        }

        public async Task<T> GetAsync<T>(string key)
        {
            if (!File.Exists(tokenPath))
            {
                return default!;
            }

            var json = await File.ReadAllTextAsync(tokenPath);
            var value = JsonSerializer.Deserialize<T>(json, JsonOptions);
            if (value is null)
            {
                throw new InvalidDataException($"トークンファイルの読み込みに失敗しました: {tokenPath}");
            }

            return value;
        }

        public Task ClearAsync()
        {
            return DeleteTokenFileAsync();
        }

        private Task DeleteTokenFileAsync()
        {
            if (File.Exists(tokenPath))
            {
                File.Delete(tokenPath);
            }

            return Task.CompletedTask;
        }

        private async Task WriteTokenAtomicallyAsync(string json)
        {
            var tempPath = CreateTempPathInTokenDirectory();
            await using (var stream = new FileStream(tempPath, FileMode.CreateNew, FileAccess.Write, FileShare.None))
            await using (var writer = new StreamWriter(stream))
            {
                await writer.WriteAsync(json);
                await writer.FlushAsync();
            }

            try
            {
                if (File.Exists(tokenPath))
                {
                    File.Replace(tempPath, tokenPath, destinationBackupFileName: null);
                }
                else
                {
                    File.Move(tempPath, tokenPath);
                }
            }
            finally
            {
                if (File.Exists(tempPath))
                {
                    File.Delete(tempPath);
                }
            }
        }

        private string CreateTempPathInTokenDirectory()
        {
            var directory = Path.GetDirectoryName(tokenPath)
                ?? throw new InvalidOperationException($"トークン保存先ディレクトリを特定できません: {tokenPath}");

            return Path.Combine(directory, $"{Path.GetFileName(tokenPath)}.{Path.GetRandomFileName()}.tmp");
        }
    }
}

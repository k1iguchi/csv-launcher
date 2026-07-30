using System.Diagnostics;
using System.Text;
using System.Windows.Forms;

namespace CsvLauncher;

internal static class Program
{
	private const string SpreadsheetUrlTemplate = "https://docs.google.com/spreadsheets/d/{0}/edit";
	private const int ExitCodeSuccess = 0;
	private const int ExitCodeError = 1;
	private const int ExitCodeCanceled = 130;

	private static async Task<int> Main(string[] args)
	{
		Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

		using var cancellationTokenSource = new CancellationTokenSource();
		ConsoleCancelEventHandler cancelHandler = (_, e) =>
		{
			e.Cancel = true;
			cancellationTokenSource.Cancel();
		};
		Console.CancelKeyPress += cancelHandler;

		try
		{
			return await RunAsync(args, cancellationTokenSource.Token);
		}
		catch (OperationCanceledException) when (cancellationTokenSource.IsCancellationRequested)
		{
			return ExitCodeCanceled;
		}
		catch (Exception ex)
		{
			MessageBox.Show($"エラーが発生しました:\n{ex.Message}", "CsvLauncher", MessageBoxButtons.OK, MessageBoxIcon.Error);
			return ExitCodeError;
		}
		finally
		{
			Console.CancelKeyPress -= cancelHandler;
		}
	}

	private static async Task<int> RunAsync(string[] args, CancellationToken cancellationToken)
	{
		var options = AppOptionsParser.Parse(args);

		if (options.ShowHelp)
		{
			PrintHelp();
			return ExitCodeSuccess;
		}

		var authService = new GoogleAuthService();
		var credential = await authService.GetCredentialAsync(cancellationToken);

		var uploader = new DriveUploader(credential);
		var fileId = await uploader.CreateSpreadsheetAsync(
			options.CsvPath,
			options.FolderId,
			cancellationToken);

		OpenSpreadsheetInBrowser(fileId);
		return ExitCodeSuccess;
	}

	private static void OpenSpreadsheetInBrowser(string fileId)
	{
		var url = string.Format(SpreadsheetUrlTemplate, fileId);
		Process.Start(new ProcessStartInfo
		{
			FileName = url,
			UseShellExecute = true,
		});
	}

	private static void PrintHelp()
	{
		Console.WriteLine("CsvLauncher usage:");
		Console.WriteLine("  CsvLauncher.exe [--folder-id=<id>] [<csv-path>]");
		Console.WriteLine("  <csv-path> を省略すると、空の Spreadsheet を新規作成して開きます。");
	}
}

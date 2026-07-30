using System.Text;
using Ude;

namespace CsvLauncher;

internal static class TextEncodingConverter
{
    private const int StreamBufferSize = 81920;
    private const long MaxInMemoryCsvBytes = 64L * 1024 * 1024;
    private static readonly UTF8Encoding Utf8NoBom = new(encoderShouldEmitUTF8Identifier: false);

    public static async Task<MemoryStream> OpenUtf8StreamAsync(string sourcePath, CancellationToken cancellationToken)
    {
        EnsureWithinInMemoryLimit(sourcePath);

        const int probeSize = 64 * 1024;

        var probeBuffer = new byte[probeSize];
        await using (var probeStream = OpenReadStream(sourcePath))
        {
            var totalRead = 0;
            while (totalRead < probeBuffer.Length)
            {
                var read = await probeStream.ReadAsync(
                    probeBuffer.AsMemory(totalRead, probeBuffer.Length - totalRead),
                    cancellationToken);
                if (read == 0)
                {
                    break;
                }

                totalRead += read;
            }

            probeBuffer = probeBuffer[..totalRead];
        }

        var encoding = DetectEncoding(probeBuffer);
        var output = new MemoryStream();

        await using (var inputStream = OpenReadStream(sourcePath))
        using (var reader = new StreamReader(inputStream, encoding, detectEncodingFromByteOrderMarks: true))
        await using (var writer = new StreamWriter(output, Utf8NoBom, bufferSize: StreamBufferSize, leaveOpen: true))
        {
            var charBuffer = new char[StreamBufferSize];
            int charsRead;

            while ((charsRead = await reader.ReadAsync(charBuffer.AsMemory(0, charBuffer.Length), cancellationToken)) > 0)
            {
                await writer.WriteAsync(charBuffer.AsMemory(0, charsRead), cancellationToken);
            }

            await writer.FlushAsync(cancellationToken);
        }

        output.Position = 0;
        return output;
    }

    private static void EnsureWithinInMemoryLimit(string sourcePath)
    {
        var fileSize = new FileInfo(sourcePath).Length;
        if (fileSize > MaxInMemoryCsvBytes)
        {
            throw new InvalidOperationException($"CSV ファイルが大きすぎます ({fileSize} bytes)。上限は {MaxInMemoryCsvBytes} bytes です。");
        }
    }

    private static FileStream OpenReadStream(string sourcePath)
    {
        return new FileStream(
            sourcePath,
            FileMode.Open,
            FileAccess.Read,
            FileShare.Read,
            bufferSize: StreamBufferSize,
            options: FileOptions.Asynchronous);
    }

    // Prefer BOM and fall back to Ude when needed.
    private static Encoding DetectEncoding(byte[] bytes)
    {
        if (bytes.Length >= 3 && bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF)
        {
            return Utf8NoBom;
        }

        if (bytes.Length >= 2)
        {
            if (bytes[0] == 0xFF && bytes[1] == 0xFE)
            {
                return Encoding.Unicode;
            }

            if (bytes[0] == 0xFE && bytes[1] == 0xFF)
            {
                return Encoding.BigEndianUnicode;
            }
        }

        var detector = new CharsetDetector();
        detector.Feed(bytes, 0, bytes.Length);
        detector.DataEnd();

        if (!string.IsNullOrWhiteSpace(detector.Charset))
        {
            try
            {
                return Encoding.GetEncoding(detector.Charset);
            }
            catch (ArgumentException)
            {
                // Fall through to Shift-JIS default.
            }
        }

        return Encoding.GetEncoding("shift_jis");
    }
}

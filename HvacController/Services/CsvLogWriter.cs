namespace HvacController.Services;

public sealed class CsvLogWriter : ICsvLogWriter
{
    private readonly string _baseDirectory;
    private readonly SemaphoreSlim _lock = new(1, 1);

    public CsvLogWriter() : this(
        Path.Combine(Environment.CurrentDirectory, "logs"))
    {
    }

    public CsvLogWriter(string baseDirectory)
    {
        _baseDirectory = baseDirectory;
    }

    public async Task AppendLineAsync(
        string relativePath,
        string header,
        string line,
        CancellationToken cancellationToken)
    {
        Directory.CreateDirectory(_baseDirectory);

        var fullPath = Path.Combine(_baseDirectory, relativePath);

        await _lock.WaitAsync(cancellationToken);

        try
        {
            var shouldWriteHeader =
                !File.Exists(fullPath) ||
                new FileInfo(fullPath).Length == 0;

            await using var stream = new FileStream(
                fullPath,
                FileMode.Append,
                FileAccess.Write,
                FileShare.Read);

            await using var writer = new StreamWriter(stream);

            if (shouldWriteHeader)
            {
                await writer.WriteLineAsync(header);
            }

            await writer.WriteLineAsync(line);
        }
        finally
        {
            _lock.Release();
        }
    }
}

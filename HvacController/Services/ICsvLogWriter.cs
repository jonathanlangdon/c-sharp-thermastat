namespace HvacController.Services;

public interface ICsvLogWriter
{
    Task AppendLineAsync(
        string relativePath,
        string header,
        string line,
        CancellationToken cancellationToken);
}

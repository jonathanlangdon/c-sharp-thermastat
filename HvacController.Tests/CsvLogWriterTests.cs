using HvacController.Services;

namespace HvacController.Tests;

public sealed class CsvLogWriterTests
{
    [Fact]
    public async Task AppendLineAsync_CreatesFileWithHeaderAndLine()
    {
        var directory = CreateTempDirectory();
        var writer = new CsvLogWriter(directory);

        await writer.AppendLineAsync(
            "test.csv",
            "col1,col2",
            "a,b",
            CancellationToken.None);

        var path = Path.Combine(directory, "test.csv");
        var lines = await File.ReadAllLinesAsync(path);

        Assert.Equal(
            new[]
            {
                "col1,col2",
                "a,b"
            },
            lines);
    }

    [Fact]
    public async Task AppendLineAsync_DoesNotRepeatHeader()
    {
        var directory = CreateTempDirectory();
        var writer = new CsvLogWriter(directory);

        await writer.AppendLineAsync(
            "test.csv",
            "col1,col2",
            "a,b",
            CancellationToken.None);

        await writer.AppendLineAsync(
            "test.csv",
            "col1,col2",
            "c,d",
            CancellationToken.None);

        var path = Path.Combine(directory, "test.csv");
        var lines = await File.ReadAllLinesAsync(path);

        Assert.Equal(
            new[]
            {
                "col1,col2",
                "a,b",
                "c,d"
            },
            lines);
    }

    private static string CreateTempDirectory()
    {
        var directory = Path.Combine(
            Path.GetTempPath(),
            $"hvac-controller-tests-{Guid.NewGuid():N}");

        Directory.CreateDirectory(directory);

        return directory;
    }
}

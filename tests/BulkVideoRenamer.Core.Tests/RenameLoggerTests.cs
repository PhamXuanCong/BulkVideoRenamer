namespace BulkVideoRenamer.Core.Tests;

public class RenameLoggerTests
{
    [Fact]
    public void Undo_RevertsLatestRenameBatch()
    {
        using var fixture = new TempFolderFixture();
        fixture.CreateFile("Video [111].mp4");

        var plan = RenameService.BuildPlan(fixture.Path, "#trend");
        RenameService.Execute(plan, fixture.Path);

        Assert.True(File.Exists(Path.Combine(fixture.Path, "Video #trend.mp4")));

        var result = RenameLogger.Undo(fixture.Path);

        Assert.Equal(1, result.SucceededCount);
        Assert.Equal(0, result.ErrorCount);
        Assert.True(File.Exists(Path.Combine(fixture.Path, "Video [111].mp4")));
        Assert.False(File.Exists(Path.Combine(fixture.Path, "Video #trend.mp4")));
    }

    [Fact]
    public void Undo_SecondCallFindsNoLog_Throws()
    {
        using var fixture = new TempFolderFixture();
        fixture.CreateFile("Video [111].mp4");

        var plan = RenameService.BuildPlan(fixture.Path, "#trend");
        RenameService.Execute(plan, fixture.Path);
        RenameLogger.Undo(fixture.Path);

        Assert.Throws<FileNotFoundException>(() => RenameLogger.Undo(fixture.Path));
    }

    [Fact]
    public void FindLatestLog_NoLogs_ReturnsNull()
    {
        using var fixture = new TempFolderFixture();

        Assert.Null(RenameLogger.FindLatestLog(fixture.Path));
    }
}

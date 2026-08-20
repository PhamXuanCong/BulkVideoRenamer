using BulkVideoRenamer.Core.Models;

namespace BulkVideoRenamer.Core.Tests;

public class RenameServiceTests
{
    [Theory]
    [InlineData("#trend")]
    [InlineData("#trend #fyp")]
    public void BuildPlan_StripsId_AndAppendsHashtag(string hashtag)
    {
        using var fixture = new TempFolderFixture();
        fixture.CreateFile("Con meo de thuong [7123456789012345678].mp4");

        var plan = RenameService.BuildPlan(fixture.Path, hashtag);

        var item = Assert.Single(plan);
        Assert.Equal(RenameStatus.Ok, item.Status);
        Assert.Equal($"Con meo de thuong {hashtag}.mp4", item.NewName);
    }

    [Fact]
    public void BuildPlan_NoIdFound_KeepsNameAndFlags()
    {
        using var fixture = new TempFolderFixture();
        fixture.CreateFile("Funny dog compilation.mov");

        var plan = RenameService.BuildPlan(fixture.Path, "#trend");

        var item = Assert.Single(plan);
        Assert.Equal(RenameStatus.NoIdFound, item.Status);
        Assert.Equal("Funny dog compilation #trend.mov", item.NewName);
    }

    [Fact]
    public void BuildHashtagSuffix_RemovesInvalidWindowsChars()
    {
        var result = RenameService.BuildHashtagSuffix("#tre:nd #f*y?p");

        Assert.Equal("#trend #fyp", result);
    }

    [Fact]
    public void BuildHashtagSuffix_EmptyInput_ReturnsEmpty()
    {
        Assert.Equal("", RenameService.BuildHashtagSuffix(""));
        Assert.Equal("", RenameService.BuildHashtagSuffix("   "));
    }

    [Fact]
    public void BuildPlan_CollisionWithinBatch_AddsNumericSuffix()
    {
        using var fixture = new TempFolderFixture();
        fixture.CreateFile("Video [111].mp4");
        fixture.CreateFile("Video [222].mp4");

        var plan = RenameService.BuildPlan(fixture.Path, "");
        var newNames = plan.Select(p => p.NewName).OrderBy(n => n).ToList();

        Assert.Equal(["Video (1).mp4", "Video.mp4"], newNames);
    }

    [Fact]
    public void BuildPlan_CollisionWithExistingFileOnDisk_AddsNumericSuffix()
    {
        using var fixture = new TempFolderFixture();
        fixture.CreateFile("Video [111].mp4");
        fixture.CreateFile("Video.mp4");

        var plan = RenameService.BuildPlan(fixture.Path, "");
        var item = plan.Single(p => p.OriginalName == "Video [111].mp4");

        Assert.Equal("Video (1).mp4", item.NewName);
    }

    [Fact]
    public void BuildPlan_NoOpRename_IsAllowedWithoutSuffix()
    {
        using var fixture = new TempFolderFixture();
        fixture.CreateFile("Video.mp4");

        var plan = RenameService.BuildPlan(fixture.Path, "");
        var item = Assert.Single(plan);

        Assert.Equal("Video.mp4", item.NewName);
    }

    [Fact]
    public void Execute_RenamesFilesOnDisk_AndWritesLog()
    {
        using var fixture = new TempFolderFixture();
        fixture.CreateFile("Video [111].mp4");

        var plan = RenameService.BuildPlan(fixture.Path, "#trend");
        var result = RenameService.Execute(plan, fixture.Path);

        Assert.Equal(1, result.SucceededCount);
        Assert.Equal(0, result.ErrorCount);
        Assert.True(File.Exists(Path.Combine(fixture.Path, "Video #trend.mp4")));
        Assert.False(File.Exists(Path.Combine(fixture.Path, "Video [111].mp4")));

        var logFile = RenameLogger.FindLatestLog(fixture.Path);
        Assert.NotNull(logFile);
    }

    [Fact]
    public void Execute_SkipsWhenNewNameEqualsOriginal()
    {
        using var fixture = new TempFolderFixture();
        fixture.CreateFile("Video.mp4");

        var plan = RenameService.BuildPlan(fixture.Path, "");
        var result = RenameService.Execute(plan, fixture.Path);

        Assert.Equal(1, result.SkippedCount);
        Assert.Equal(0, result.SucceededCount);
    }
}

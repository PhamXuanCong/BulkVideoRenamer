namespace BulkVideoRenamer.Core.Tests;

public class FileScannerTests
{
    [Fact]
    public void ScanVideoFiles_OnlyReturnsSupportedExtensions()
    {
        using var fixture = new TempFolderFixture();
        fixture.CreateFile("clip.mp4");
        fixture.CreateFile("clip.MOV");
        fixture.CreateFile("notes.txt");
        fixture.CreateFile("image.png");

        var files = FileScanner.ScanVideoFiles(fixture.Path);
        var names = files.Select(Path.GetFileName).ToList();

        Assert.Contains("clip.mp4", names);
        Assert.Contains("clip.MOV", names);
        Assert.DoesNotContain("notes.txt", names);
        Assert.DoesNotContain("image.png", names);
    }

    [Fact]
    public void ScanVideoFiles_DoesNotRecurseIntoSubfolders()
    {
        using var fixture = new TempFolderFixture();
        fixture.CreateFile("top.mp4");
        var subDir = Path.Combine(fixture.Path, "sub");
        Directory.CreateDirectory(subDir);
        File.WriteAllText(Path.Combine(subDir, "nested.mp4"), "");

        var files = FileScanner.ScanVideoFiles(fixture.Path);

        Assert.Single(files);
        Assert.Equal("top.mp4", Path.GetFileName(files[0]));
    }

    [Fact]
    public void ScanVideoFiles_MissingFolder_Throws()
    {
        var missing = Path.Combine(Path.GetTempPath(), "bvr-does-not-exist-" + Guid.NewGuid());

        Assert.Throws<DirectoryNotFoundException>(() => FileScanner.ScanVideoFiles(missing));
    }
}

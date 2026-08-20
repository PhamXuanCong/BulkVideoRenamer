namespace BulkVideoRenamer.Core.Tests;

/// <summary>
/// Creates a unique temp folder for a test and deletes it afterward.
/// </summary>
public sealed class TempFolderFixture : IDisposable
{
    public string Path { get; }

    public TempFolderFixture()
    {
        Path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "bvr-tests-" + Guid.NewGuid());
        Directory.CreateDirectory(Path);
    }

    public string CreateFile(string name)
    {
        var path = System.IO.Path.Combine(Path, name);
        File.WriteAllText(path, "");
        return path;
    }

    public void Dispose()
    {
        if (Directory.Exists(Path))
            Directory.Delete(Path, recursive: true);
    }
}

namespace ProjFSSharp;

public class InMemoryVirtualDirectory : VirtualizedDirectory
{
    private record class Item(string Root, List<Item> Children);

    private List<Item> ItemsDirectories { get; } = new();

    public InMemoryVirtualDirectory()
        : base(Path.GetFullPath(Path.GetRandomFileName()))
    {

    }

    protected override IDirectoryEnumerator GetFileSystemEntries(string relativePath)
    {
        List<IProjectedFileInfo> items = new();

        foreach (string directory in Directories)
        {
            items.Add(CreateDirectoryInfo(directory));
        }

        return new SimpleDirectoryEnumerator(items);
    }

    public void AddDirectory(string relativeDirectoryPath)
    {
        Directories.Add(relativeDirectoryPath);
    }

    protected override IProjectedFileInfo? GetFileInfo(string relativePath)
    {
        return Directories.Where(x => x.Equals(relativePath))
            .Select(x => CreateDirectoryInfo(x))
            .FirstOrDefault();
    }

    private IProjectedFileInfo CreateDirectoryInfo(string relativePath)
    {
        return new ProjectedFileInfo(
                    Path.GetFileName(relativePath),
                    GetFullPathInLayer(relativePath),
                    size: 0,
                    isDirectory: true,
                    creationTime: DateTime.Now,
                    lastAccessTime: DateTime.Now,
                    lastWriteTime: DateTime.Now,
                    changeTime: DateTime.Now,
                    attributes: FileAttributes.Directory);
    }
}

namespace ProjFSSharp;

public class InMemoryVirtualDirectory : VirtualizedDirectory
{
    private IList<IProjectedFileInfo> Items { get; } = new List<IProjectedFileInfo>();

    public InMemoryVirtualDirectory()
        : base(Path.GetFullPath(Path.GetRandomFileName()))
    {

    }

    protected override IDirectoryEnumerator GetFileSystemEntries(string relativePath)
    {
        string fullPath = GetFullPathInLayer(relativePath);
        var items = Items.Where(i => Path.GetDirectoryName(i.FullName) == fullPath).ToList();
        return new SimpleDirectoryEnumerator(items);
    }

    public void AddDirectory(string relativePath)
        => AddDirectory(CreateDirectoryInfo(relativePath));

    public void AddDirectory(IProjectedFileInfo directory)
    {
        if (!directory.IsDirectory)
        {
            throw new ArgumentException("The provided IProjectedFileInfo is not a directory.", nameof(directory));
        }
        Items.Add(directory);
    }

    protected override IProjectedFileInfo? GetFileInfo(string relativePath)
    {
        string fullPath = GetFullPathInLayer(relativePath);

        return Items.FirstOrDefault(x => x.FullName == fullPath);
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

// See https://aka.ms/new-console-template for more information
using Microsoft.Windows.ProjFS;
using System.Diagnostics.CodeAnalysis;

namespace ProjFSSharp;

public class DirectoryRequiredCallbacks : BaseRequiredCallbacks
{
    private DirectoryInfo SourceDirectory { get; }

    public DirectoryRequiredCallbacks(VirtualizationInstance virtualizationInstance, DirectoryInfo sourceDirectory)
        : base(virtualizationInstance)
    {
        SourceDirectory = sourceDirectory ?? throw new ArgumentNullException(nameof(sourceDirectory));
    }

    protected override HResult TryCreateDirectoryEnumeration(int commandId, Guid enumerationId, string relativePath, uint triggeringProcessId, string triggeringProcessImageFileName, out IDirectoryEnumeration enumeration)
    {
        enumeration = new SimpleDirectoryEnumeration(
            GetChildItemsInLayer(relativePath)
            .OrderBy(file => file.Name, ProjFSSorter.Instance)
            .ToList());
        return HResult.Ok;
    }

    protected IEnumerable<IProjectedFileInfo> GetChildItemsInLayer(string relativePath)
    {
        string fullPathInLayer = GetFullPathInLayer(relativePath);
        DirectoryInfo dirInfo = new(fullPathInLayer);

        if (!dirInfo.Exists)
        {
            yield break;
        }

        foreach (FileSystemInfo fileSystemInfo in dirInfo.EnumerateFileSystemInfos())
        {
            // We only handle files and directories, not symlinks.
            if ((fileSystemInfo.Attributes & FileAttributes.Directory) == FileAttributes.Directory)
            {
                yield return new ProjectedFileInfo(
                    fileSystemInfo.Name,
                    fileSystemInfo.FullName,
                    size: 0,
                    isDirectory: true,
                    creationTime: fileSystemInfo.CreationTime,
                    lastAccessTime: fileSystemInfo.LastAccessTime,
                    lastWriteTime: fileSystemInfo.LastWriteTime,
                    changeTime: fileSystemInfo.LastWriteTime,
                    attributes: fileSystemInfo.Attributes);
            }
            else if (fileSystemInfo is FileInfo fileInfo)
            {
                yield return new ProjectedFileInfo(
                    fileInfo.Name,
                    fileSystemInfo.FullName,
                    size: fileInfo.Length,
                    isDirectory: false,
                    creationTime: fileSystemInfo.CreationTime,
                    lastAccessTime: fileSystemInfo.LastAccessTime,
                    lastWriteTime: fileSystemInfo.LastWriteTime,
                    changeTime: fileSystemInfo.LastWriteTime,
                    attributes: fileSystemInfo.Attributes);
            }
        }
    }

    protected override IProjectedFileInfo? GetFileInfoInLayer(string relativePath)
    {
        string layerPath = GetFullPathInLayer(relativePath);
        string layerParentPath = Path.GetDirectoryName(layerPath) ?? throw new InvalidOperationException();
        string layerName = Path.GetFileName(relativePath);

        if (FileOrDirectoryExistsInLayer(layerParentPath, layerName, out IProjectedFileInfo? fileInfo))
        {
            return fileInfo;
        }

        return null;
    }

    private static bool FileOrDirectoryExistsInLayer(string layerParentPath, string layerName,
        [NotNullWhen(true)]
    out IProjectedFileInfo? fileInfo)
    {
        fileInfo = null;

        // Check whether the parent directory exists in the layer.
        DirectoryInfo dirInfo = new(layerParentPath);
        if (!dirInfo.Exists)
        {
            return false;
        }

        // Get the FileSystemInfo for the entry in the layer that matches the name, using ProjFS's
        // name matching rules.
        FileSystemInfo? fileSystemInfo =
            dirInfo
            .GetFileSystemInfos()
            .FirstOrDefault(fsInfo => Utils.IsFileNameMatch(fsInfo.Name, layerName));

        if (fileSystemInfo is null)
        {
            return false;
        }

        bool isDirectory = (fileSystemInfo.Attributes & FileAttributes.Directory) == FileAttributes.Directory;

        fileInfo = new ProjectedFileInfo(
            name: fileSystemInfo.Name,
            fullName: fileSystemInfo.FullName,
            size: isDirectory ? 0 : new FileInfo(Path.Combine(layerParentPath, layerName)).Length,
            isDirectory: isDirectory,
            creationTime: fileSystemInfo.CreationTime,
            lastAccessTime: fileSystemInfo.LastAccessTime,
            lastWriteTime: fileSystemInfo.LastWriteTime,
            changeTime: fileSystemInfo.LastWriteTime,
            attributes: fileSystemInfo.Attributes);

        return true;
    }

    protected override string GetFullPathInLayer(string relativePath)
        => Path.Combine(SourceDirectory.FullName, relativePath);

    protected override string? GetFullPathForRootedFileWithReparsePoint(IProjectedFileInfo fileInfo, string? targetPath, DirectoryInfo scratchRoot)
    {
        if (targetPath is null) return null;

        string targetRelativePath = FileSystemApi.TryGetPathRelativeToRoot(SourceDirectory.FullName, targetPath, fileInfo.IsDirectory);
        // GetFullPath is used to get rid of relative path components (such as .\)
        return Path.GetFullPath(Path.Combine(VirtualizationInstance.VirtualizationRootPath.FullName, targetRelativePath));
    }

    protected override bool FileExistsInLayer(string relativePath)
    {
        string layerPath = GetFullPathInLayer(relativePath);
        FileInfo fileInfo = new(layerPath);
        return fileInfo.Exists;
    }

    protected override HResult HydrateFile(string relativePath, uint bufferSize, Func<byte[], uint, bool> tryWriteBytes)
    {
        string layerPath = GetFullPathInLayer(relativePath);
        if (!File.Exists(layerPath))
        {
            return HResult.FileNotFound;
        }

        // Open the file in the layer for read.
        using FileStream fs = new(layerPath, FileMode.Open, FileAccess.Read);
        long remainingDataLength = fs.Length;
        byte[] buffer = new byte[bufferSize];

        while (remainingDataLength > 0)
        {
            // Read from the file into the read buffer.
            int bytesToCopy = (int)Math.Min(remainingDataLength, buffer.Length);
            if (fs.Read(buffer, 0, bytesToCopy) != bytesToCopy)
            {
                return HResult.InternalError;
            }

            // Write the bytes we just read into the scratch.
            if (!tryWriteBytes(buffer, (uint)bytesToCopy))
            {
                return HResult.InternalError;
            }

            remainingDataLength -= bytesToCopy;
        }
        return HResult.Ok;

    }
}
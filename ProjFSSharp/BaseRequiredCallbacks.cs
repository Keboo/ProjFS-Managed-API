using Microsoft.Windows.ProjFS;
using System.Collections.Concurrent;

namespace ProjFSSharp;

public abstract class BaseRequiredCallbacks : IRequiredCallbacks
{
    protected ConcurrentDictionary<Guid, IDirectoryEnumerator> ActiveEnumerations { get; } = new();
    protected VirtualizationInstance VirtualizationInstance { get; }
    public bool IsSymlinkSupportAvailable { get; protected set; }

    protected BaseRequiredCallbacks(VirtualizationInstance virtualizationInstance)
    {
        VirtualizationInstance = virtualizationInstance ?? throw new ArgumentNullException(nameof(virtualizationInstance));
        IsSymlinkSupportAvailable = EnvironmentHelper.IsFullSymlinkSupportAvailable();
    }

    public virtual HResult StartDirectoryEnumerationCallback(
        int commandId,
        Guid enumerationId,
        string relativePath,
        uint triggeringProcessId,
        string triggeringProcessImageFileName)
    {
        HResult result = TryCreateDirectoryEnumeration(
            commandId, 
            enumerationId, 
            relativePath, 
            triggeringProcessId, 
            triggeringProcessImageFileName, 
            out IDirectoryEnumerator? enumeration);

        if (result != HResult.Ok)
        {
            return result;
        }
        
        if (enumeration is null)
        {
            return HResult.InternalError;
        }
        
        if (!ActiveEnumerations.TryAdd(enumerationId, enumeration))
        {
            return HResult.InternalError;
        }
        return HResult.Ok;
    }

    protected abstract HResult TryCreateDirectoryEnumeration(
        int commandId,
        Guid enumerationId,
        string relativePath,
        uint triggeringProcessId,
        string triggeringProcessImageFileName,
        out IDirectoryEnumerator enumeration);

    public virtual HResult GetDirectoryEnumerationCallback(
        int commandId,
        Guid enumerationId,
        string filterFileName,
        bool restartScan,
        IDirectoryEnumerationResults enumResult)
    {
        if (!ActiveEnumerations.TryGetValue(enumerationId, out IDirectoryEnumerator? enumeration))
        {
            return HResult.InternalError;
        }

        if (restartScan)
        {
            // The caller is restarting the enumeration, so we reset our ActiveEnumeration to the
            // first item that matches filterFileName.  This also saves the value of filterFileName
            // into the ActiveEnumeration, overwriting its previous value.
            enumeration.RestartEnumeration(filterFileName);
        }
        else
        {
            // The caller is continuing a previous enumeration, or this is the first enumeration
            // so our ActiveEnumeration is already at the beginning.  TrySaveFilterString()
            // will save filterFileName if it hasn't already been saved (only if the enumeration
            // is restarting do we need to re-save filterFileName).
            //TODO: Handle return
            enumeration.TrySaveFilterString(filterFileName);
        }

        HResult hr = HResult.Ok;
        int numEntriesAdded = 0;
        
        while (hr == HResult.Ok && enumeration.IsCurrentValid)
        {
            IProjectedFileInfo? fileInfo = enumeration.Current;

            if (fileInfo is null)
            {
                //fileInfo is not expected to be null if IsCurrentValid returns true
                return HResult.InternalError;
            }

            if (!TryGetTargetIfReparsePoint(fileInfo, fileInfo.FullName, out string? targetPath))
            {
                return HResult.InternalError;
            }

            // A provider adds entries to the enumeration buffer until it runs out, or until adding
            // an entry fails. If adding an entry fails, the provider remembers the entry it couldn't
            // add. ProjFS will call the GetDirectoryEnumerationCallback again, and the provider
            // must resume adding entries, starting at the last one it tried to add. SimpleProvider
            // remembers the entry it couldn't add simply by not advancing its ActiveEnumeration.
            if (AddFileInfoToEnum(enumResult, fileInfo, targetPath))
            {
                ++numEntriesAdded;
                //TODO: Handle return from MoveNext
                enumeration.MoveNext();
            }
            else
            {
                if (numEntriesAdded == 0)
                {
                    return HResult.InsufficientBuffer;
                }

                break;
            }
        }

        return hr;
    }
    
    public virtual HResult EndDirectoryEnumerationCallback(
        Guid enumerationId)
    {
        if (!ActiveEnumerations.TryRemove(enumerationId, out IDirectoryEnumerator? _))
        {
            return HResult.InternalError;
        }
        return HResult.Ok;
    }

    public virtual HResult GetPlaceholderInfoCallback(
        int commandId,
        string relativePath,
        uint triggeringProcessId,
        string triggeringProcessImageFileName)
    {
        HResult hr;
        IProjectedFileInfo? fileInfo = GetFileInfoInLayer(relativePath);
        if (fileInfo is null)
        {
            hr = HResult.FileNotFound;
        }
        else
        {
            //TODO: This could probably be simplified...
            string layerPath = GetFullPathInLayer(relativePath);
            if (!TryGetTargetIfReparsePoint(fileInfo, layerPath, out string? targetPath))
            {
                hr = HResult.InternalError;
            }
            else
            {
                hr = WritePlaceholderInfo(relativePath, fileInfo, targetPath);
            }
        }

        return hr;
    }

    protected abstract IProjectedFileInfo? GetFileInfoInLayer(string relativePath);
    protected abstract string GetFullPathInLayer(string relativePath);
    

    protected virtual HResult WritePlaceholderInfo(string relativePath, IProjectedFileInfo fileInfo, string? targetPath)
    {
        string directoryName = Path.GetDirectoryName(relativePath) ?? "";
        //if (string.IsNullOrEmpty(directoryName))
        //{
        //    return HResult.InternalError;
        //}
        if (IsSymlinkSupportAvailable)
        {
            return VirtualizationInstance.WritePlaceholderInfo2(
                    relativePath: Path.Combine(directoryName, fileInfo.Name),
                    creationTime: fileInfo.CreationTime,
                    lastAccessTime: fileInfo.LastAccessTime,
                    lastWriteTime: fileInfo.LastWriteTime,
                    changeTime: fileInfo.ChangeTime,
                    fileAttributes: fileInfo.Attributes,
                    endOfFile: fileInfo.Size,
                    isDirectory: fileInfo.IsDirectory,
                    symlinkTargetOrNull: targetPath,
                    contentId: new byte[] { 0 },
                    providerId: new byte[] { 1 });
        }
        else
        {
            return VirtualizationInstance.WritePlaceholderInfo(
                    relativePath: Path.Combine(directoryName, fileInfo.Name),
                    creationTime: fileInfo.CreationTime,
                    lastAccessTime: fileInfo.LastAccessTime,
                    lastWriteTime: fileInfo.LastWriteTime,
                    changeTime: fileInfo.ChangeTime,
                    fileAttributes: fileInfo.Attributes,
                    endOfFile: fileInfo.Size,
                    isDirectory: fileInfo.IsDirectory,
                    contentId: new byte[] { 0 },
                    providerId: new byte[] { 1 });
        }
    }

    protected bool TryGetTargetIfReparsePoint(IProjectedFileInfo fileInfo, string fullPath, out string? targetPath)
    {
        targetPath = null;

        if ((fileInfo.Attributes & FileAttributes.ReparsePoint) != 0 /* TODO: Check for reparse point type */)
        {
            if (!FileSystemApi.TryGetReparsePointTarget(fullPath, out targetPath))
            {
                return false;
            }
            else if (Path.IsPathRooted(targetPath))
            {
                targetPath = GetFullPathForRootedFileWithReparsePoint(fileInfo, targetPath, VirtualizationInstance.VirtualizationRootPath);
                return true;
            }
        }

        return true;
    }

    protected abstract string? GetFullPathForRootedFileWithReparsePoint(IProjectedFileInfo fileInfo, string? targetPath, DirectoryInfo scratchRoot);

    public virtual HResult GetFileDataCallback(
        int commandId,
        string relativePath,
        ulong byteOffset,
        uint length,
        Guid dataStreamId,
        byte[] contentId,
        byte[] providerId,
        uint triggeringProcessId,
        string triggeringProcessImageFileName)
    {
        HResult hr = HResult.Ok;
        if (!FileExistsInLayer(relativePath))
        {
            hr = HResult.FileNotFound;
        }
        else
        {
            // We'll write the file contents to ProjFS no more than 64KB at a time.
            uint desiredBufferSize = Math.Min(64 * 1024, length);
            try
            {
                // We could have used VirtualizationInstance.CreateWriteBuffer(uint), but this 
                // illustrates how to use its more complex overload.  This method gets us a 
                // buffer whose underlying storage is properly aligned for unbuffered I/O.
                using IWriteBuffer writeBuffer = VirtualizationInstance.CreateWriteBuffer(
                    byteOffset,
                    desiredBufferSize,
                    out ulong alignedWriteOffset,
                    out uint alignedBufferSize);
                    // Get the file data out of the layer and write it into ProjFS.
                    hr = HydrateFile(
                        relativePath,
                        alignedBufferSize,
                        (readBuffer, bytesToCopy) =>
                        {
                            // readBuffer contains what HydrateFile() read from the file in the
                            // layer.  Now seek to the beginning of the writeBuffer and copy the
                            // contents of readBuffer into writeBuffer.
                            writeBuffer.Stream.Seek(0, SeekOrigin.Begin);
                            writeBuffer.Stream.Write(readBuffer, 0, (int)bytesToCopy);

                            // Write the data from the writeBuffer into the scratch via ProjFS.
                            HResult writeResult = VirtualizationInstance.WriteFileData(
                                dataStreamId,
                                writeBuffer,
                                alignedWriteOffset,
                                bytesToCopy);

                            if (writeResult != HResult.Ok)
                            {
                                return false;
                            }

                            alignedWriteOffset += bytesToCopy;
                            return true;
                        });

                if (hr != HResult.Ok)
                {
                    return HResult.InternalError;
                }
            }
            catch (OutOfMemoryException)
            {
                hr = HResult.OutOfMemory;
            }
            catch (Exception)
            {
                hr = HResult.InternalError;
            }
        }
        return hr;
    }

    protected abstract bool FileExistsInLayer(string relativePath);

    protected abstract HResult HydrateFile(string relativePath, uint bufferSize, Func<byte[], uint, bool> tryWriteBytes);

    protected bool AddFileInfoToEnum(IDirectoryEnumerationResults enumResult, IProjectedFileInfo fileInfo, string? targetPath)
    {
        if (IsSymlinkSupportAvailable)
        {
            return enumResult.Add(
                fileName: fileInfo.Name,
                fileSize: fileInfo.Size,
                isDirectory: fileInfo.IsDirectory,
                fileAttributes: fileInfo.Attributes,
                creationTime: fileInfo.CreationTime,
                lastAccessTime: fileInfo.LastAccessTime,
                lastWriteTime: fileInfo.LastWriteTime,
                changeTime: fileInfo.ChangeTime,
                symlinkTargetOrNull: targetPath);
        }
        else
        {
            return enumResult.Add(
                fileName: fileInfo.Name,
                fileSize: fileInfo.Size,
                isDirectory: fileInfo.IsDirectory,
                fileAttributes: fileInfo.Attributes,
                creationTime: fileInfo.CreationTime,
                lastAccessTime: fileInfo.LastAccessTime,
                lastWriteTime: fileInfo.LastWriteTime,
                changeTime: fileInfo.ChangeTime);
        }
    }
}

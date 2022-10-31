// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Diagnostics;

namespace ProjFSSharp;

[DebuggerDisplay($"{{{nameof(FullName)}}}")]
public class ProjectedFileInfo : IProjectedFileInfo
{
    public ProjectedFileInfo(
        string name,
        string fullName,
        long size,
        bool isDirectory,
        DateTime creationTime,
        DateTime lastAccessTime,
        DateTime lastWriteTime,
        DateTime changeTime,
        FileAttributes attributes)
    {
        Name = name;
        FullName = fullName;
        Size = isDirectory ? 0 : size;
        IsDirectory = isDirectory;
        CreationTime = creationTime;
        LastAccessTime = lastAccessTime;
        LastWriteTime = lastWriteTime;
        ChangeTime = changeTime;
        // Make sure the directory attribute is stored properly.
        Attributes = isDirectory ? attributes | FileAttributes.Directory : attributes & ~FileAttributes.Directory;
    }

    public ProjectedFileInfo(
        string name,
        string fullName,
        long size,
        bool isDirectory) : this(
            name: name,
            fullName: fullName,
            size: size,
            isDirectory: isDirectory,
            creationTime: DateTime.UtcNow,
            lastAccessTime: DateTime.UtcNow,
            lastWriteTime: DateTime.UtcNow,
            changeTime: DateTime.UtcNow,
            attributes: isDirectory ? FileAttributes.Directory : FileAttributes.Normal)
    { }

    public string Name { get; }
    public string FullName { get; }
    public long Size { get; }
    public bool IsDirectory { get; }
    public DateTime CreationTime { get; }
    public DateTime LastAccessTime { get; }
    public DateTime LastWriteTime { get; }
    public DateTime ChangeTime { get; }
    public FileAttributes Attributes { get; }
}


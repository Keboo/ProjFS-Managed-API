﻿// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

namespace ProjFSSharp;

public interface IProjectedFileInfo
{

    public bool IsDirectory { get; }
    public FileAttributes Attributes { get; }
    string Name { get; }
    string FullName { get; }
    DateTime CreationTime { get; }
    DateTime LastAccessTime { get; }
    DateTime LastWriteTime { get; }
    DateTime ChangeTime { get; }
    long Size { get; }
}


// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using Microsoft.Windows.ProjFS;

namespace ProjFSSharp;

/// <summary>
/// Implements IComparer using <see cref="Utils.FileNameCompare(string, string)"/>.
/// </summary>
internal class ProjFSSorter : Comparer<string>
{
    public static ProjFSSorter Instance { get; } = new ProjFSSorter();

    private ProjFSSorter() { }

    public override int Compare(string? x, string? y) => Utils.FileNameCompare(x, y);
}

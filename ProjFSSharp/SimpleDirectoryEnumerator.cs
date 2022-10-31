// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using Microsoft.Windows.ProjFS;

namespace ProjFSSharp;

public class SimpleDirectoryEnumerator : IDirectoryEnumerator
{
    private IEnumerable<IProjectedFileInfo> FileInfos { get; }
    private IEnumerator<IProjectedFileInfo>? FileInfoEnumerator { get; set; }
    private string? FilterString { get; set; }

    public SimpleDirectoryEnumerator(IEnumerable<IProjectedFileInfo> fileInfos)
    {
        FileInfos = fileInfos ?? throw new ArgumentNullException(nameof(fileInfos));
        ResetEnumerator();
        MoveNext();
    }

    /// <summary>
    /// true if Current refers to an element in the enumeration, false if Current is past the end of the collection
    /// </summary>
    public bool IsCurrentValid { get; private set; }

    public IProjectedFileInfo? Current => FileInfoEnumerator?.Current;

    /// <summary>
    /// Resets the enumerator and advances it to the first ProjectedFileInfo in the enumeration
    /// </summary>
    /// <param name="filter">Filter string to save.  Can be null.</param>
    public void RestartEnumeration(
        string filter)
    {
        ResetEnumerator();
        IsCurrentValid = FileInfoEnumerator?.MoveNext() == true;
        SaveFilter(filter);
    }

    /// <summary>
    /// Advances the enumerator to the next element of the collection (that is being projected).   
    /// If a filter string is set, MoveNext will advance to the next entry that matches the filter.
    /// </summary>
    /// <returns>
    /// true if the enumerator was successfully advanced to the next element; false if the enumerator has passed the end of the collection
    /// </returns>
    public bool MoveNext()
    {
        if (FileInfoEnumerator is { } enumerator)
        {
            IsCurrentValid = enumerator.MoveNext() == true;

            while (IsCurrentValid && IsCurrentHidden())
            {
                IsCurrentValid = enumerator.MoveNext();
            }
            return IsCurrentValid;
        }
        return false;
    }

    /// <summary>
    /// Attempts to save the filter string for this enumeration.  When setting a filter string, if Current is valid
    /// and does not match the specified filter, the enumerator will be advanced until an element is found that
    /// matches the filter (or the end of the collection is reached).
    /// </summary>
    /// <param name="filter">Filter string to save.  Can be null.</param>
    /// <returns> True if the filter string was saved.  False if the filter string was not saved (because a filter string
    /// was previously saved).
    /// </returns>
    /// <remarks>
    /// Per MSDN (https://msdn.microsoft.com/en-us/library/windows/hardware/ff567047(v=vs.85).aspx, the filter string
    /// specified in the first call to ZwQueryDirectoryFile will be used for all subsequent calls for the handle (and
    /// the string specified in subsequent calls should be ignored)
    /// </remarks>
    public bool TrySaveFilterString(
        string? filter)
    {
        if (FilterString is null)
        {
            SaveFilter(filter);
            return true;
        }

        return false;
    }
    
    private static bool FileNameMatchesFilter(
        string? name,
        string? filter)
    {
        if (string.IsNullOrEmpty(filter))
        {
            return true;
        }

        if (filter is "*")
        {
            return true;
        }

        return Utils.IsFileNameMatch(name, filter);
    }

    private void SaveFilter(
        string? filter)
    {
        if (string.IsNullOrEmpty(filter))
        {
            FilterString = string.Empty;
        }
        else
        {
            FilterString = filter;
            if (IsCurrentValid && IsCurrentHidden())
            {
                MoveNext();
            }
        }
    }

    private bool IsCurrentHidden() 
        => !FileNameMatchesFilter(Current?.Name, FilterString);

    private void ResetEnumerator() 
        => FileInfoEnumerator = FileInfos.GetEnumerator();
}



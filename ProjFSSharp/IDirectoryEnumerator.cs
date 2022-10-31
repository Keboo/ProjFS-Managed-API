namespace ProjFSSharp;

public interface IDirectoryEnumerator
{
    bool IsCurrentValid { get; }
    IProjectedFileInfo? Current { get; }

    bool MoveNext();
    void RestartEnumeration(string filterFileName);
    bool TrySaveFilterString(string filterFileName);
}


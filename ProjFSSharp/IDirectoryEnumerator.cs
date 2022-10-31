namespace ProjFSSharp;

public interface IDirectoryEnumerator
{
    public static IDirectoryEnumerator Empty { get; } = new EmptyEnumerator();
    
    bool IsCurrentValid { get; }
    IProjectedFileInfo? Current { get; }

    bool MoveNext();
    void RestartEnumeration(string filterFileName);
    bool TrySaveFilterString(string filterFileName);

    private class EmptyEnumerator : IDirectoryEnumerator
    {
        public bool IsCurrentValid => false;
        public IProjectedFileInfo? Current => null;

        public bool MoveNext() => false;
        public void RestartEnumeration(string filterFileName) { }
        public bool TrySaveFilterString(string filterFileName) => false;
    }
}


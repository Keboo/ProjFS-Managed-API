using Microsoft.Windows.ProjFS;

namespace ProjFSSharp;

public abstract class VirtualizedDirectory : IDisposable
{
    private bool _DisposedValue;
    public string TargetDirectory { get; }

    public DirectoryInfo? VirtualizedRootDirectory => _VirtualizationInstance?.VirtualizationRootPath;

    protected BaseRequiredCallbacks? _Callbacks;
    protected VirtualizationInstance? _VirtualizationInstance;

    public VirtualizedDirectory(string targetDirectory, VirtualizedDirectoryOptions? options = null)
    {
        TargetDirectory = targetDirectory ?? throw new ArgumentNullException(nameof(targetDirectory));
    }


    public void Start()
    {
        List<NotificationMapping> notifications = new();
        string rootName = "";
        notifications.Add(
            new NotificationMapping(
                  NotificationType.FileOpened
                | NotificationType.NewFileCreated
                | NotificationType.FileOverwritten
                | NotificationType.PreDelete
                | NotificationType.PreRename
                | NotificationType.PreCreateHardlink
                | NotificationType.FileRenamed
                | NotificationType.HardlinkCreated
                | NotificationType.FileHandleClosedNoModification
                | NotificationType.FileHandleClosedFileModified
                | NotificationType.FileHandleClosedFileDeleted
                | NotificationType.FilePreConvertToFull,
            rootName)
        );
        
        VirtualizationInstance virtualizationInstance = _VirtualizationInstance = new(
            virtualizationRootPath: TargetDirectory,
            poolThreadCount: 0,
            concurrentThreadCount: 0,
            enableNegativePathCache: false,
            notificationMappings: notifications
        );
        var callbacks = _Callbacks = new CallbackWrapper(this, virtualizationInstance);


        virtualizationInstance.StartVirtualizing(callbacks);
    }

    public void Stop()
    {
        _VirtualizationInstance?.StopVirtualizing();
        _Callbacks = null;
        _VirtualizationInstance = null;
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!_DisposedValue)
        {
            if (disposing)
            {
                Stop();
            }

            // TODO: free unmanaged resources (unmanaged objects) and override finalizer
            // TODO: set large fields to null
            _DisposedValue = true;
        }
    }

    protected abstract IDirectoryEnumerator GetFileSystemEntries(string relativePath);

    protected abstract IProjectedFileInfo? GetFileInfo(string relativePath);

    protected virtual string GetFullPathInLayer(string relativePath) 
        => Path.GetFullPath(relativePath, TargetDirectory);

    // // TODO: override finalizer only if 'Dispose(bool disposing)' has code to free unmanaged resources
    // ~VirtualizedDirectory()
    // {
    //     // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
    //     Dispose(disposing: false);
    // }

    void IDisposable.Dispose()
    {
        // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }

    private class CallbackWrapper : BaseRequiredCallbacks
    {
        public CallbackWrapper(
            VirtualizedDirectory directory,
            VirtualizationInstance virtualizationInstance) 
            : base(virtualizationInstance)
        {
            Directory = directory;
        }

        private VirtualizedDirectory Directory { get; }

        protected override bool FileExistsInLayer(string relativePath)
        {
            throw new NotImplementedException();
        }

        protected override IProjectedFileInfo? GetFileInfoInLayer(string relativePath)
        {
            return Directory.GetFileInfo(relativePath);
        }

        protected override string? GetFullPathForRootedFileWithReparsePoint(IProjectedFileInfo fileInfo, string? targetPath, DirectoryInfo scratchRoot)
        {
            throw new NotImplementedException();
        }

        protected override string GetFullPathInLayer(string relativePath)
        {
            return Directory.GetFullPathInLayer(relativePath);
        }

        protected override HResult HydrateFile(string relativePath, uint bufferSize, Func<byte[], uint, bool> tryWriteBytes)
        {
            throw new NotImplementedException();
        }

        protected override HResult TryCreateDirectoryEnumeration(int commandId, Guid enumerationId, string relativePath, uint triggeringProcessId, string triggeringProcessImageFileName, out IDirectoryEnumerator enumerator)
        {
            enumerator = Directory.GetFileSystemEntries(relativePath);
            return HResult.Ok;
        }
    }
}

public record class VirtualizedDirectoryOptions
{
    
}

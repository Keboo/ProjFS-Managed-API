// See https://aka.ms/new-console-template for more information
using Microsoft.Windows.ProjFS;
using ProjFSSharp;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.CommandLine.IO;

Option<DirectoryInfo> source = new(new[] { "-s", "--source-directory" }, "The source directory to project")
{
    IsRequired = true
};
source.ExistingOnly();

Option<DirectoryInfo> target = new(new[] { "-t", "--target-directory" }, "The target directory to project the source directory into")
{
    IsRequired = true
};
//TODO: NotExistingOnly

RootCommand command = new()
{
    source,
    target
};
command.SetHandler((InvocationContext ctx) =>
{
    DirectoryInfo sourceDirectory = ctx.ParseResult.GetValueForOption(source)!;
    DirectoryInfo targetDirectory = ctx.ParseResult.GetValueForOption(target)!;


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

    VirtualizationInstance virtualizationInstance = new(
        virtualizationRootPath: targetDirectory.FullName,
        poolThreadCount: 0,
        concurrentThreadCount: 0,
        enableNegativePathCache: false,
        notificationMappings: notifications
    );

    DirectoryRequiredCallbacks requiredCallbacks = new(virtualizationInstance, sourceDirectory);
    HResult hr = virtualizationInstance.StartVirtualizing(requiredCallbacks);
    if (hr != HResult.Ok)
    {
        ctx.Console.Error.WriteLine($"Failed to start virtualization instance: {hr}");
        ctx.ExitCode = 1;
        return;
    }
    
    ctx.Console.WriteLine($"Projecting {sourceDirectory.FullName} into {targetDirectory}.");
    ctx.Console.WriteLine("Press Enter to exit.");
    Console.ReadLine();
    
    virtualizationInstance.StopVirtualizing();
});

await command.InvokeAsync(args);

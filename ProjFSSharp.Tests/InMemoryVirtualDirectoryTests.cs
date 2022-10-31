namespace ProjFSSharp.Tests;

public class InMemoryVirtualDirectoryTests
{
    [Fact]
    public void Can_create_test_directory()
    {
        DirectoryInfo? virtualizedDirectory;
        using (var vDir = new InMemoryVirtualDirectory())
        {
            virtualizedDirectory = vDir.VirtualizedRootDirectory;
            Assert.Null(virtualizedDirectory);

            vDir.Start();
            virtualizedDirectory = vDir.VirtualizedRootDirectory;
            Assert.True(virtualizedDirectory!.Exists);
        }
        Assert.False(virtualizedDirectory.Exists);
    }

    [Fact]
    public void Can_simulate_nested_directories()
    {
        using (var vDir = new InMemoryVirtualDirectory())
        {
            vDir.AddDirectory("Foo");
            vDir.Start();

            DirectoryInfo[] directories = vDir.VirtualizedRootDirectory!.GetDirectories("*", SearchOption.AllDirectories);
            Assert.Single(directories);
        }
    }
}



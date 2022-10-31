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

    [Fact]
    public async Task Can_simulate_multiple_nested_directories()
    {
        using (var vDir = new InMemoryVirtualDirectory())
        {
            vDir.AddDirectory("Foo");
            //vDir.AddDirectory("Foo/Bar");
            //vDir.AddDirectory("Foo/Baz");
            vDir.AddDirectory("Baz");
            vDir.Start();

            DirectoryInfo root = vDir.VirtualizedRootDirectory!;


            DirectoryInfo[] directories = root.GetDirectories("*", SearchOption.AllDirectories);
            await Task.Delay(TimeSpan.FromMinutes(5));
            Assert.Equal(2, directories.Length);

            
            Assert.Equal("Baz", directories[0].Name);
            Assert.Equal("Bar", directories[1].Name);
            Assert.Equal("Bar", directories[2].Name);
        }
    }
}



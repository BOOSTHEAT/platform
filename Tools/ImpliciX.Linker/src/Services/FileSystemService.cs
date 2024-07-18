namespace ImpliciX.Linker.Services;

public sealed class FileSystemService : IFileSystemService
{
    public void WriteAllText(string path, string content)
        => File.WriteAllText(path, content);
}
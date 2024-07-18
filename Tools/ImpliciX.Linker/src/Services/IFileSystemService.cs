namespace ImpliciX.Linker.Services;

public interface IFileSystemService
{
    void WriteAllText(string path, string content);
}
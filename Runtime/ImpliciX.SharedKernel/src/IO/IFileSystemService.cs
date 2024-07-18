namespace ImpliciX.SharedKernel.IO
{
    public interface IFileSystemService
    {
        void WriteAllText(string path, string content);
        bool FileExists(string path);
        string ReadAllText(string filePath);
    }
}
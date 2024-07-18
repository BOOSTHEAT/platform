using System.IO;

namespace ImpliciX.SharedKernel.IO
{
    public sealed class FileSystemService : IFileSystemService
    {
        public void WriteAllText(string path, string content) => File.WriteAllText(path, content);
        public bool FileExists(string path) => File.Exists(path);
        public string ReadAllText(string filePath) => File.ReadAllText(filePath);
    }
}
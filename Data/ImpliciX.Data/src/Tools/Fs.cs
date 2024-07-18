using System;
using System.IO;
using ImpliciX.Language.Core;
using Mono.Unix.Native;

namespace ImpliciX.Data
{
    public static class Fs
    {
        public static FileInfo TryCreateSymbolicLink(string source, string symlinkPath)
        {
            Log.Debug($"Try to create symlink {source} --> {symlinkPath}");
            return Syscall.symlink(source, symlinkPath) == 0
                ? new FileInfo(symlinkPath)
                : throw new ApplicationException($"Error during creation of the symlink {symlinkPath} ---> {source}");
        }
        
        public static FilePermissions GetFilePermissions(string filePath)
        {
            var fd = Syscall.open(filePath, OpenFlags.O_RDONLY);
            Syscall.fstat(fd, out var stat);
            return stat.st_mode;
        }

    }
}
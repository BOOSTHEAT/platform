using System;
using ImpliciX.Language.Core;

namespace ImpliciX.Data
{
    public static  class Errors
    {
         public static UpdateError CorruptionError() => 
             new UpdateError($"Corruption error it doesn't match the hash."); 
         
         public static UpdateError TmpCopyError(Uri packageUri) => 
             new UpdateError($"Can't create temp file copy for {packageUri}"); 
         
         public static UpdateError NotFoundManifestError() => 
             new UpdateError($"Manifest not exist in package"); 
         
         public static UpdateError DeserializeManifestError(Exception ex) => 
             new UpdateError($"Can't deserialize manifest: {ex.CascadeMessage()}"); 
         
         public static UpdateError SerializeManifestError(Exception ex) => 
             new UpdateError($"Can't serialize manifest: {ex.CascadeMessage()}"); 
         
         public static UpdateError UpdateManifestError() => 
             new UpdateError($"Can't update manifest"); 
         
         public static UpdateError DecompressFileError(string archiveFullName) => 
             new UpdateError($"Can't unzip  file {archiveFullName}"); 
         
         public static UpdateError DecompressFileError(string archiveFullName, Exception ex) => 
             new UpdateError($"Can't unzip  file {archiveFullName}. Error {ex.Message}"); 
         
         public static UpdateError PackageContentArchiveNotFound(string fileFullName) => 
             new UpdateError($"Can't unzip  manifest {fileFullName}");
    }
    
    public class UpdateError : Error
    {
        public UpdateError(string message) : base(nameof(UpdateError), message)
        {
        }
    }

    public class InvalidSha256Exception : Exception
    {
        public InvalidSha256Exception(string fileHash, string expectedHash) : base($"The actual hash {fileHash} is not equal to the expected hash {expectedHash}.")
        {
        }
    }
   
}
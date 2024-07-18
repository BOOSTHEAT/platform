using System.IO;
using ImpliciX.Data.HotDb.Model;

namespace ImpliciX.Data.HotDb.Hdw;


public class Io: IIo
{
    public string FilePath { get; }

    public Io(string filePath)
    {
        FilePath = filePath;
        Fs = File.Open(filePath, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.Read);
        Writer = new BinaryWriter(Fs);
    }

    public BinaryWriter Writer { get; set; }

    
    public FileStream Fs { get; set; }

    public byte[] ReadAllBytes()
    {
        var bytes = new byte[Fs.Length];
        Fs.Seek(0, SeekOrigin.Begin);
        var _ = Fs.Read(bytes);
        return bytes;
    }

    public void ForceFlush()
    {
        Writer.Flush();
        Fs.Flush(true);
    }

    public void Allocate(long size) => Fs.SetLength(Fs.Length + size);

    public byte[] Read(uint offset, ushort count)
    {
        var bytes = new byte[count];
        Fs.Seek(offset, SeekOrigin.Begin);
        var _ = Fs.Read(bytes);
        return bytes;
    }

    public long Size => Fs.Length;

    public void Flush()
    {
        Writer.Flush();
        Fs.Flush();
    }
    public void Seek(long offset, SeekOrigin origin) => Fs.Seek(offset, origin);
    public void Write(byte value) => Writer.Write(value);

    public uint EndOfFileOffset => (uint)Fs.Length;
    public void Write(byte[] bytes) => Writer.Write(bytes);

    public void Write(ushort value) => Writer.Write(value);

    public void Write(uint value) => Writer.Write(value);

    public void Dispose()
    {
        if(IsDisposed) return;
        Writer.Flush();
        Writer.Dispose();
        Fs.Dispose();
        
        IsDisposed = true;
    }

    private bool IsDisposed { get; set; }
} 

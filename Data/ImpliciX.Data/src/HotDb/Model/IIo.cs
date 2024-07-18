using System;
using System.IO;

namespace ImpliciX.Data.HotDb.Model;

internal interface IIo: IDisposable
{
    string FilePath { get; }
    uint EndOfFileOffset { get; }
    long Size { get; }
    void Flush();
    void Seek(long offset, SeekOrigin origin);
    void Write(byte value);
    void Write(byte[] bytes);
    void Write(ushort value);
    void Write(uint value);
    
    void Allocate(long size);
    byte[] Read(uint offset, ushort count);
    byte[] ReadAllBytes();
    void ForceFlush();
}
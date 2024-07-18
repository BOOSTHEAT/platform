using System;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using ImpliciX.Language.Core;
using static ImpliciX.SharedKernel.SerialApi2.SerialPort.UnsafeNativeMethods;
using static ImpliciX.SharedKernel.SerialApi2.SerialPort.SafeNativeMethods;

namespace ImpliciX.SharedKernel.SerialApi2.SerialPort
{
    public interface IBhSerialPort : IDisposable
    {
        void Open();
        bool IsOpen { get; }
        string Tty { get; set; }
        int ReadTimeout { get; set; }
        int WriteTimeout { get; set; }
        int Read(byte[] bytes, int offset, int length);
        int Write(byte[] bytes, int offset, int length);
        void DiscardInBuffer();
        void DiscardOutBuffer();
    }

    public class BhSerialPort : IBhSerialPort
    {
        public void Open()
        {
            if(!IsOpen)
                Ext.ExecuteNativeFunctions(new Func<int>[]
                {
                    () => serial_open(_portHandle),
                    () => serial_setproperties(_portHandle)
                });
            LogPortProperties();
            void LogPortProperties()
            {
                int baud = 0, databits=0;
                Parity parity = Parity.None;
                StopBits stopBits = StopBits.One;
                var deviceNamePtr = IntPtr.Zero;
                Ext.ExecuteNativeFunctions(new Func<int>[]
                {
                   () => {
                       deviceNamePtr = serial_getdevicename(_portHandle);
                       return 0;
                   },
                   ()=> serial_getbaud(_portHandle, out baud),
                   () => serial_getdatabits(_portHandle, out databits),
                   () => serial_getparity(_portHandle, out parity),
                   () => serial_getstopbits(_portHandle, out stopBits)
                });
                
                Log.Information("Opening serial port {@name} (BaudRate: {@baud}; Databits: {@dataBits}; Parity: {@parity}; Stopbits: {@stopbits})", 
                    Marshal.PtrToStringAnsi(deviceNamePtr), baud, databits, parity, stopBits);
                    
            }
        }

        public bool IsOpen {
            get
            {
                bool isOpen = false;
                Ext.ExecuteNativeFunction(() => serial_isopen(_portHandle, out isOpen));
                return isOpen;
            }
        }
        
        public int Read(byte[] bytes, int offset, int length)
        {
            var readCount = 0;
            SerialReadWriteEvent result = serial_waitforevent(_portHandle, SerialReadWriteEvent.ReadEvent, ReadTimeout);
            switch (result)
            {
                case SerialReadWriteEvent.NoEvent:
                    throw new TimeoutException($"Timeout when reading on port: {Tty}.");
                case SerialReadWriteEvent.Error:
                    Ext.ThrowOnError();
                    break;
                case SerialReadWriteEvent.ReadEvent:
                    unsafe
                    {
                        fixed (byte* p = bytes)
                        {
                            var ptr = (IntPtr)(p+offset);
                            readCount = serial_read(_portHandle, ptr, length); 
                            if(readCount<0) 
                                 Ext.ThrowOnError();
                            Log.Debug("Offset : {@offset}; Length:{@length}; ReadCount:{@readCount}", offset, length, readCount);
                            Log.Debug("Read(nb_bytes: {@readCount}) -> {@str}", readCount, string.Join(",",bytes.Select(o=>((int)o).ToString())));
                        }
                    }
                    break;
            }
            return readCount;
        }

        public int Write(byte[] bytes, int offset, int length)
        {
            SerialReadWriteEvent result = serial_waitforevent(_portHandle, SerialReadWriteEvent.WriteEvent, WriteTimeout);
            var writeCount = 0;
            switch (result)
            {
                case SerialReadWriteEvent.NoEvent:
                    throw new TimeoutException($"Timeout when writing on port: {Tty}.");
                case SerialReadWriteEvent.Error:
                    Ext.ThrowOnError();
                    break;
                case SerialReadWriteEvent.WriteEvent:
                    unsafe
                    {
                        fixed (byte* p = bytes)
                        {
                            var ptr = (IntPtr)(p+offset);
                            writeCount = serial_write(_portHandle, ptr, length); 
                            if(writeCount<0) 
                                Ext.ThrowOnError();
                            Log.Debug("Offset : {@offset}; Length:{@length}; WriteCount:{@writeCount}",offset, length, writeCount);
                        }
                    }
                    break;
            }
            return writeCount;
        }

        public void DiscardInBuffer() => Ext.ExecuteNativeFunction(() => serial_discardinbuffer(_portHandle));

        public void DiscardOutBuffer() => Ext.ExecuteNativeFunction(() => serial_discardoutbuffer(_portHandle));

        public void Close() => Ext.ExecuteNativeFunction(() => serial_close(_portHandle));

        public void Dispose()
        {
            if (IsOpen) 
                Close();
            _portHandle.Dispose();
        }

       public BhSerialPort(string tty, int baud, int data, Parity parity, StopBits stopbits)
        {
            
            Log.Information("Creating serial port {@tty} with v2 serial api.", tty);
            ReadTimeout = 500;
            WriteTimeout = 500;
            _portHandle = serial_init();
            Tty = tty;
            Ext.ExecuteNativeFunctions(new Func<int>[]
            {
                () => serial_setdevicename(_portHandle, tty),
                () => serial_setbaud(_portHandle, baud),
                () => serial_setdatabits(_portHandle, data),
                () => serial_setparity(_portHandle, parity),
                () => serial_setstopbits(_portHandle, stopbits)
            });
            
        }
        
        private readonly SafeSerialHandle _portHandle;

        public string Tty { get; set; }

        public int ReadTimeout { get; set; }
        
        public int WriteTimeout { get; set; }

    }

    internal static class Ext
    {
        public static int ExecuteNativeFunctions(Func<int>[] funcs)
        {
            foreach (var f in funcs)
            {
                ExecuteNativeFunction(f);
            }
            return 0;
        }
        public static int ExecuteNativeFunction(Func<int> f)
        {
            var result = f();
            if (result != 0)
            {
                ThrowOnError();
            }
            return result;
        }
        
        public static void ThrowOnError()
        {
            var (err,msgStr) = LastError();
            switch (err)
            {
                case 1:
                    throw new ArgumentException(msgStr);
                case 2:
                    throw new UnauthorizedAccessException(msgStr);
                case 3:
                    throw new OutOfMemoryException(msgStr);
                case 4:
                    throw new InvalidOperationException($"{msgStr}. Check that the tty is open.");
                case 5:
                    throw new PlatformNotSupportedException(msgStr);
                case 6:
                    throw new IOException(msgStr);
                case 0:
                case 7:
                case 9:
                case 8:
                    throw new ApplicationException(msgStr);
                default:
                    break;
            }
        }

        private static (int no, string msg) LastError()
        {
            var errno = Marshal.GetLastWin32Error();
            var err = netfx_errno(errno);
            var msgPtr = netfx_errstring(errno);
            var msgStr = msgPtr != IntPtr.Zero ? Marshal.PtrToStringAnsi(msgPtr) : "UNDEFINED ERROR MESSAGE";
            return (err,msgStr);
        }
    }
}
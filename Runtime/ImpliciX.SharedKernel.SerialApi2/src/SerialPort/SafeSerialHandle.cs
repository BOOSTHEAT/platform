using System;
using System.Runtime.InteropServices;

namespace ImpliciX.SharedKernel.SerialApi2.SerialPort
{
    internal class SafeSerialHandle : SafeHandle
    {
        public SafeSerialHandle() : base(IntPtr.Zero, true) { }

        public override bool IsInvalid
        {
            get
            {
                return handle.Equals(IntPtr.Zero);
            }
        }

        protected override bool ReleaseHandle()
        {
            UnsafeNativeMethods.serial_terminate(handle);
            return true;
        }
    }
}
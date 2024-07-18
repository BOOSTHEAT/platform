using System.Reflection;
using System.Threading;

namespace ImpliciX.SharedKernel.Tools
{
    public static class ThreadTools
    {
        public static ulong GetCurrentThreadNativeId()
        {
            var p = typeof(Thread).GetProperty("CurrentOSThreadId", BindingFlags.GetProperty | BindingFlags.NonPublic | BindingFlags.Static);
            return p!=null?(ulong)p.GetValue(Thread.CurrentThread):0;
        }
    }
}
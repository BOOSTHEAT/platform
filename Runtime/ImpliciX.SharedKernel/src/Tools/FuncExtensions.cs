using System;

namespace ImpliciX.SharedKernel.Tools
{
    public static class FuncExtensions
    {
        public static Func<T1, T3> Compose<T1, T2, T3>(this Func<T2, T3> f, Func<T1, T2> g) => 
            x => f(g(x));

        public static Func<T1, T3> Pipe<T1, T2, T3>(this Func<T1, T2> f, Func<T2, T3> g) => 
            g.Compose(f);
    }
}
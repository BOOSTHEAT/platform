// ReSharper disable once CheckNamespace

namespace System
{
    public static class ObjectExtension
    {
        public static T AssumeNotNull<T>(this T o, string prefixExceptionMessage = null) where T : class
        {
            var prefixMessage = prefixExceptionMessage ?? $"{prefixExceptionMessage} : ";

            return o ?? throw new NullReferenceException(
                $"{prefixMessage}{typeof(T)} object type must not be null{Environment.NewLine}" +
                $"StackTrace:{Environment.NewLine}" +
                $"{new Diagnostics.StackTrace(true)}");
        }
    }
}
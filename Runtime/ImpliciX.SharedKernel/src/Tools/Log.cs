using ImpliciX.Language.Core;

namespace ImpliciX.SharedKernel.Tools;

public static class LogExtensions
{
    public static T LogDebug<T>(this T @this)
    {
        Log.Debug("Trace object {0} {1}", typeof(T).Name, @this);
        return @this;
    }

    public static T LogDebug<T>(this T @this, string messageTemplate)
    {
        Log.Debug(messageTemplate, @this);
        return @this;
    }

    public static Result<TValue> LogDebugOnSuccess<TValue>(this Result<TValue> @this, string messageTemplate)
        => @this.Tap(_ => { }, whenSuccess: (value) => Log.Debug(messageTemplate, value));

    public static Result<TValue> LogWhenError<TValue>(this Result<TValue> @this, string messageTemplate)
        => @this.Tap(e => Log.Error(messageTemplate, e.Message) , _ => { });
}
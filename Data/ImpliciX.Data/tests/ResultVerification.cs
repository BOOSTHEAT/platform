using System;
using ImpliciX.Language.Core;
using NFluent;

namespace ImpliciX.Data.Tests
{
    public static class ResultVerification
    {
        public static void CheckIsSuccessAnd<T>(this Result<T> result, Action<T> verification)
        {
            Check.That(result.IsSuccess).IsTrue();
            result.Tap(_ => { },verification);
        }

        public static void CheckIsErrorAnd<T>(this Result<T> result, Action<Error> verification)
        {
            Check.That(result.IsError).IsTrue();
            result.Tap(verification,_=>{});
        }
    }
}
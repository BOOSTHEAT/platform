using System;
using ImpliciX.Language.Core;
namespace ImpliciX.SharedKernel.Tests.Core
{
    public class SomeError : Error
    {
        public SomeError(Exception e) : base(nameof(SomeError), e.Message) { }
      
        public SomeError(string message) : base(nameof(SomeError), message){}
    }
    
    public class MyTestError : Error
    {
        public static MyTestError Create(string key, string message)
        {
            return new MyTestError(key, message);
        }

        private MyTestError(string key, string message) : base(key, message)
        {
        }
    }
}
using System;

namespace ImpliciX.SharedKernel.IO
{
    public class EnvironmentService : IEnvironmentService
    {
        public string GetEnvironmentVariable(string variableName) => Environment.GetEnvironmentVariable(variableName);
    }
}
using System;
using System.Collections.Generic;
using System.Linq;
using ImpliciX.Language.Control;
using ImpliciX.Language.Core;
using ImpliciX.Language.Model;

namespace ImpliciX.Control
{
    public class FunctionContext
    {
        private readonly Urn _functionDefinitionUrn;
        private readonly ReadProperty _readFunc;
        private readonly Func<FunctionRun> _createRunner;
        private readonly Urn[] _xUrns;

        private FunctionRun _functionRun;
        private FunctionRun FunctionRun => _functionRun ??= _createRunner();
        

        public FunctionContext(SetWithComputation dslComputation, ReadProperty readFunc)
        {
            _functionDefinitionUrn = dslComputation._funcDefUrn;
            _xUrns = dslComputation._xUrns;
            _readFunc = readFunc;
            _createRunner = dslComputation._funcRef.Runner;
        }

        public Result<float> Compute() =>
            from readResult in ReadProperties(_xUrns)
            let xsr = readResult.Select(x => (((IFloat) x.ModelValue()).ToFloat(), x.At)).ToArray()
            from functionDefinition in GetFunctionDefinition()
            select FunctionRun(functionDefinition, xsr);
        
        public void Reset() => _functionRun = null;

        private Result<FunctionDefinition> GetFunctionDefinition()
        {
            if (_functionDefinitionUrn == constant.parameters.none)
                return Result<FunctionDefinition>.Create(new FunctionDefinition());
            else
                return
                    from readProperty in _readFunc(_functionDefinitionUrn)
                    select (FunctionDefinition) readProperty.ModelValue();
        }

        private Result<IEnumerable<IDataModelValue>> ReadProperties(Urn[] urns)
            => urns.Select(urn => _readFunc(urn)).Traverse();
    }
}
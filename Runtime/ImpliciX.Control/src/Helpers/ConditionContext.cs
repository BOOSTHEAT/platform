using System;
using System.Linq;
using ImpliciX.Language.Control;
using ImpliciX.Language.Model;

namespace ImpliciX.Control.Helpers
{
    public class ConditionContext
    {
        public ConditionDefinition Definition { get; }
        private ReadProperty ReadFunc { get; }

        private Func<IDataModelValue[], bool>[] ConditionDenotations { get; }

        public ConditionContext(ReadProperty readFunc, ConditionDefinition definition)
        {
            ReadFunc = readFunc;
            Definition = definition;
            ConditionDenotations = definition.Parts.Where(c => c.@operator != Op.Any).Select(CreateFunction).ToArray();
        }

        public bool Execute(ConditionDefinition initialConditionDefinition = null)
        {
            initialConditionDefinition ??= Definition;

            if (!ConditionDenotations.Zip(initialConditionDefinition.Parts.Where(c => c.@operator != Op.Any),(First,Second)=>(First,Second))
                .Select(tuple => Apply(tuple.First, tuple.Second.operandsByUrn))
                .All(value => value is true)) return false;

            foreach (var (_, _, operandsByValue) in initialConditionDefinition.Parts.Where(c => c.@operator == Op.Any))
            {
                if (operandsByValue == null) continue;

                var operand1 = (ConditionDefinition) operandsByValue[0];
                var operand2 = (ConditionDefinition) operandsByValue[1];

                var operand1ContainsAny = operand1.ContainsAnyOperator();
                var operand2ContainsAny = operand2.ContainsAnyOperator();

                var operand1Result = operand1ContainsAny ? Execute(operand1) : EvaluateConditionDefinition(operand1);
                var operand2Result = operand2ContainsAny ? Execute(operand2) : EvaluateConditionDefinition(operand2);

                if (!(operand1Result || operand2Result))
                    return false;
            }

            return true;
        }

        private bool EvaluateConditionDefinition(ConditionDefinition conditionDefinition)
        {
            var conditionDenotations = conditionDefinition.Parts.Select(CreateFunction).ToArray();
            return conditionDenotations.Zip(conditionDefinition.Parts,(First,Second)=>(First,Second)).Select(tuple => Apply(tuple.First, tuple.Second.operandsByUrn)).All(value => value is true);
        }


        private bool Apply(Func<IDataModelValue[], bool> denotation, Urn[] operandsByUrn)
        {
            var values = new IDataModelValue[operandsByUrn.Length];
            for (var i = 0; i < operandsByUrn.Length; i++)
            {
                var urn = operandsByUrn[i];
                var result = ReadFunc(urn);
                if (result.IsError)
                {
                    return false;
                }
                values[i] = result.Value;
            }
            return denotation(values);
        }

        private static Func<IDataModelValue[], bool> CreateFunction((Op @operator, Urn[] operandsByUrn, object[] operandsByValue) part)
        {
            bool Condition(IDataModelValue[] props)
            {
                var @operator = part.@operator;
                var operands = new object[props.Length + part.operandsByValue.Length];
                for (var i = 0; i < props.Length; i++)
                {
                    operands[i] = props[i].ModelValue();
                }
                for (var i = 0; i < part.operandsByValue.Length; i++)
                {
                    operands[i + props.Length] = part.operandsByValue[i];
                }
                return Helpers.Conditions.Operator(@operator, operands);
            }
            return Condition;
        }
    }
}
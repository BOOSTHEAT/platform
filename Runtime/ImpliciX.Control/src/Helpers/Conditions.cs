using System;
using System.Linq;
using ImpliciX.Data;
using ImpliciX.Language.Control;
using ImpliciX.Language.Model;

namespace ImpliciX.Control.Helpers
{
    public static class Conditions
    {
        public static bool Operator(Op @operator, object[] operands)
            => @operator switch
            {
                Op.EqWithEpsilon => EqualWithEpsilon(operands),
                Op.EqWithTolerance => EqualWithTolerance(operands),
                Op.Gt=> GreaterThan(operands),
                Op.GtMinusEpsilon => GreaterThanMinusEpsilon(operands),
                Op.GtOrEqMinusEpsilon => GreaterOrEqualThanMinusEpsilon(operands),
                Op.GtOrEqPlusEpsilon => GreaterOrEqualThanPlusEpsilon(operands),
                Op.GtOrEqTo=> GreaterOrEqualTo(operands),
                Op.GtPlusEpsilon => GreaterThanPlusEpsilon(operands),
                Op.InState=> InState(operands),
                Op.Is=> Is(operands),
                Op.IsNot=> IsNot(operands),
                Op.Lt => LowerThan(operands),
                Op.LtMinusEpsilon => LowerThanMinusEpsilon(operands),
                Op.LtOrEqMinusEpsilon => LowerOrEqualThanMinusEpsilon(operands),
                Op.LtOrEqPlusEpsilon => LowerOrEqualThanPlusEpsilon(operands),
                Op.LtOrEqTo=> LowerOrEqualTo(operands),
                Op.LtPlusEpsilon => LowerThanPlusEpsilon(operands),
                Op.NotEqWithTolerance => NotEqualWithTolerance(operands),
                _ => throw new NotSupportedException()
            };

        private static bool InState(object[] operands)
        {
            Debug.PreCondition(()=>operands[0] is EnumSequence && operands[1] is Enum, ()=>$"Operator {Op.InState} wrong argument type");
            return ((EnumSequence) operands[0]).Contains((Enum) operands[1]);
        }

        private static bool IsNot(object[] operands)
        {
            Debug.PreCondition(()=>operands.All(o=>o is Enum),()=>$"Operator {Op.IsNot} is implemented for Enum only" );
            return !((int)operands[0]).Equals((int)operands[1]);
        }

        private static bool Is(object[] operands)
        {
            Debug.PreCondition(()=>operands.All(o=>o is Enum),()=>$"Operator {Op.Is} is implemented for Enum only" );
            return ((int)operands[0]).Equals((int)operands[1]);
        }

        private static bool LowerOrEqualThanPlusEpsilon(object[] operands)
        {
            Debug.PreCondition(()=>operands.All(o=>o is IFloat),()=>$"Operator {Op.LtOrEqPlusEpsilon} is implemented for float only" );
            var operandsf = OperandsToFloats(operands);
            return operandsf[0] <= operandsf[1] + operandsf[2];
        }

        private static bool LowerOrEqualThanMinusEpsilon(object[] operands)
        {
            Debug.PreCondition(()=>operands.All(o=>o is IFloat),()=>$"Operator {Op.LtOrEqMinusEpsilon} is implemented for float only" );
            var operandsf = OperandsToFloats(operands);
            return operandsf[0] <= operandsf[1] - operandsf[2];
        }

        private static bool LowerThanPlusEpsilon(object[] operands)
        {
            Debug.PreCondition(()=>operands.All(o=>o is IFloat),()=>$"Operator {Op.LtPlusEpsilon} is implemented for float only" );
            var operandsf = OperandsToFloats(operands);
            return operandsf[0] < operandsf[1] + operandsf[2];
        }

        private static bool LowerThanMinusEpsilon(object[] operands)
        {
            Debug.PreCondition(()=>operands.All(o=>o is IFloat),()=>$"Operator {Op.LtMinusEpsilon} is implemented for float only" );
            var operandsf = OperandsToFloats(operands);
            return operandsf[0] < operandsf[1] - operandsf[2];
        }

        private static bool GreaterOrEqualThanMinusEpsilon(object[] operands)
        {
            Debug.PreCondition(()=>operands.All(o=>o is IFloat),()=>$"Operator {Op.GtOrEqMinusEpsilon} is implemented for float only" );
            var operandsf = OperandsToFloats(operands);
            return operandsf[0] >= operandsf[1] - operandsf[2];
        }

        private static bool GreaterOrEqualThanPlusEpsilon(object[] operands)
        {
            Debug.PreCondition(()=>operands.All(o=>o is IFloat),()=>$"Operator {Op.GtOrEqPlusEpsilon} is implemented for float only" );
            var operandsf = OperandsToFloats(operands);
            return operandsf[0] >= operandsf[1] + operandsf[2];
        }

        private static bool GreaterThanPlusEpsilon(object[] operands)
        {
            Debug.PreCondition(()=>operands.All(o=>o is IFloat),()=>$"Operator {Op.GtPlusEpsilon} is implemented for float only" );
            var operandsf = OperandsToFloats(operands);
            return operandsf[0] > operandsf[1] + operandsf[2];
        }

        private static bool GreaterThanMinusEpsilon(object[] operands)
        {
            Debug.PreCondition(()=>operands.All(o=>o is IFloat),()=>$"Operator {Op.GtMinusEpsilon} is implemented for float only" );
            var operandsf = OperandsToFloats(operands);
            return operandsf[0] > operandsf[1] - operandsf[2];
        }

        private static bool EqualWithTolerance(object[] operands)
        {
            Debug.PreCondition(()=>operands.All(o=>o is IFloat),()=>$"Operator {Op.EqWithTolerance} is implemented for float only" );
            var operandsf = OperandsToFloats(operands);
            return Math.Abs(operandsf[0] - operandsf[1]) <= Math.Abs(operandsf[0] * operandsf[2]);
        }

        private static bool NotEqualWithTolerance(object[] operands)
        {
            Debug.PreCondition(()=>operands.All(o=>o is IFloat),()=>$"Operator {Op.NotEqWithTolerance} is implemented for float only" );
            var operandsf = OperandsToFloats(operands);
            return Math.Abs(operandsf[0] - operandsf[1]) > Math.Abs(operandsf[0] * operandsf[2]);
        }

        private static bool EqualWithEpsilon(object[] operands)
        {
            Debug.PreCondition(()=>operands.All(o=>o is IFloat),()=>$"Operator {Op.EqWithEpsilon} is implemented for float only" );
            var operandsf = OperandsToFloats(operands);
            return Math.Abs(operandsf[0] - operandsf[1]) <= operandsf[2];
        }

        private static bool LowerThan(object[] operands)
        {
            Debug.PreCondition(()=>operands.All(o=>o is IFloat),()=>$"Operator {Op.Lt} is implemented for float only." );
            var operandsf = OperandsToFloats(operands);
            return operandsf[0] < operandsf[1];
        }

        private static bool GreaterThan(object[] operands)
        {
            Debug.PreCondition(()=>operands.All(o=>o is IFloat),()=>$"Operator {Op.Gt} is implemented for float only" );
            var operandsf = OperandsToFloats(operands);
            return operandsf[0] > operandsf[1];
        }

        private static bool GreaterOrEqualTo(object[] operands)
        {
            Debug.PreCondition(()=>operands.All(o=>o is IFloat),()=>$"Operator {Op.GtOrEqTo} is implemented for float only" );
            var operandsf = OperandsToFloats(operands);
            return operandsf[0] >= operandsf[1];
        }
        
        private static bool LowerOrEqualTo(object[] operands)
        {
            Debug.PreCondition(()=>operands.All(o=>o is IFloat),()=>$"Operator {Op.LtOrEqTo} is implemented for float only" );
            var operandsf = OperandsToFloats(operands);
            return operandsf[0] <= operandsf[1];
        }

        public static float[] OperandsToFloats(object[] operands)
        {
            var operandsToFloats = new float[operands.Length];
            for (var i = 0; i < operands.Length; i++)
            {
                operandsToFloats[i] = ((IFloat) operands[i]).ToFloat();
            }

            return operandsToFloats;
        }
    }
}
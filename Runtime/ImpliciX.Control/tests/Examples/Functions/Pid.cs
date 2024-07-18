using System;
using System.Diagnostics.Contracts;
using System.Linq;
using ImpliciX.Language.Control;
using ImpliciX.Language.Model;

namespace ImpliciX.Control.Tests.Examples.Functions
{
    public class PID
    {
        public static FuncRef Func => new FuncRef(nameof(PID), () => Runner, xUrns=>xUrns.Take(2).ToArray());
        public static FunctionRun Runner
        {
            get
            {
                var pid = new PID();
                return (functionDefinition, xs) => pid.Execute(functionDefinition, xs);
            }
        }
        
        private float integralError;
        private float previousError;
        private TimeSpan previousAt;
        private float currentResult;

        private Start p = null;
        
        public float Execute(FunctionDefinition functionDefinition, (float value, TimeSpan at)[] xs)
        {
            Contract.Assert(xs.Length == 2 || xs.Length == 3, "Pid should have 2 or 3 variables");
            var measure = xs[0];
            var setpoint = xs[1];

            if (p == null)
            {
                p = new Start(functionDefinition, xs);
                previousError = p.ZeroIfTooSmall(measure.value - setpoint.value);
                previousAt = measure.at;
                currentResult = p.Result(0);
                return currentResult;
            }
            
            if (measure.at.CompareTo(previousAt) == 0)
                return currentResult;

            var dt = ComputeDeltaTInSeconds(measure.at);
            var newError = p.ZeroIfTooSmall(measure.value - setpoint.value);
            var proportionalValue = p.Clamp(p.Slope * p.Kp * newError);
            integralError += newError*dt;
            var integralValue = p.Clamp(
                p.Slope * p.Ki * integralError,
                () => integralError = p.Slope * p.ClampingMin / p.Ki,
                () => integralError = p.Slope * p.ClampingMax / p.Ki
            );
            var deriveValue = this.p.Clamp(p.Slope * p.Kd * (newError - previousError)/dt);
            previousError = newError;
            previousAt = measure.at;
            currentResult = p.Result(proportionalValue + integralValue + deriveValue);
            return currentResult;
        }

        private float ComputeDeltaTInSeconds(TimeSpan measureTime)
        {
            return Convert.ToSingle(measureTime.Subtract(previousAt).TotalMilliseconds / 1000);
        }

        public class Start : Parameters
        {
            public Start(FunctionDefinition functionDefinition, (float value, TimeSpan at)[] xs)
            : base(functionDefinition)
            {
                float Biais = (xs.Length == 2) ? functionDefinition.GetValueParam(nameof(Biais)) : Unscaling(xs[2].value);
                YStart = Math.Min(MaxValue, Math.Max(MinValue, Biais));
                ClampingMin = - Math.Max(Math.Abs(MinValue - YStart), Math.Abs(MaxValue - YStart));
                ClampingMax = + Math.Max(Math.Abs(MinValue - YStart), Math.Abs(MaxValue - YStart));
            }

            public float Clamp(float v, Action onMin = null, Action onMax = null)
            {
                if (v < ClampingMin)
                {
                    onMin?.Invoke();
                    return ClampingMin;
                }
                if (v > ClampingMax)
                {
                    onMax?.Invoke();
                    return ClampingMax;
                }
                return v;
            }
            
            public float Result(float shift) => Scaling(Math.Min(MaxValue, Math.Max(MinValue, YStart +  shift)));
            public readonly float YStart;
            public readonly float ClampingMin;
            public readonly float ClampingMax;
        }


        public class Parameters
        {
            public Parameters(FunctionDefinition functionDefinition)
            {
                Kp = functionDefinition.GetValueParam(nameof(Kp));
                Ki = functionDefinition.GetValueParam(nameof(Ki));
                Kd = functionDefinition.GetValueParam(nameof(Kd));
                Zm = functionDefinition.GetValueParam(nameof(Zm));
                MinValue = functionDefinition.GetValueParam(nameof(MinValue));
                MaxValue = functionDefinition.GetValueParam(nameof(MaxValue));
                Slope = functionDefinition.GetValueParam(nameof(Slope));
                (YMin, YMax) = Helpers.ScalingParameters(functionDefinition, MaxValue, MinValue);
            }
            public float Scaling(float x) => Helpers.Scaling(x, MinValue, MaxValue, YMin, YMax);
            public float Unscaling(float y) => Helpers.Unscaling(y, MinValue, MaxValue, YMin, YMax);
            
            public float ZeroIfTooSmall(float f) => (Math.Abs(f) < Zm ? 0 : f);

            public readonly float Kp;
            public readonly float Ki;
            public readonly float Kd;
            public readonly float Zm;
            public readonly float MinValue;
            public readonly float MaxValue;
            public readonly float Slope;   
            public readonly float YMin;   
            public readonly float YMax;
        }

    }
}
using System;
using System.Linq;
using ImpliciX.Control.Tests.TestUtilities;
using ImpliciX.Language.Control;
using ImpliciX.Language.Model;
using NFluent;
using NUnit.Framework;
using static ImpliciX.Language.Control.Condition;
using static ImpliciX.TestsCommon.PropertiesChangedHelper;

namespace ImpliciX.Control.Tests.Fixtures.Helpers
{
    [TestFixture]
    [Ignore("Performance tests")]
    public class ConditionalPerformanceSubsystemTest : SetupSubSystemTests
    {
        [Test]
        public void operands_to_float_perf_test()
        {
            var floats = new object[]
            {
                Percentage.FromFloat(0.99f).Value,
                Temperature.FromFloat(1.1f).Value,
                RotationalSpeed.FromFloat(1.2f).Value,
                Pressure.FromFloat(1.3f).Value
            };
            var activationCount = 10000000;
            var watch = System.Diagnostics.Stopwatch.StartNew();
            for (var i = 0; i < activationCount; i++)
            {
                var operandsToFloats = Control.Helpers.Conditions.OperandsToFloats(floats);
            }

            watch.Stop();
            Console.WriteLine($"Operands to floats {activationCount} loops in {watch.ElapsedMilliseconds}");
            watch = System.Diagnostics.Stopwatch.StartNew();
            for (var i = 0; i < activationCount; i++)
            {
                var operandsToFloats = floats.Cast<IFloat>().Select(o => o.ToFloat()).ToArray();
            }

            watch.Stop();
            Console.WriteLine($"Cast+Select {activationCount} loops in {watch.ElapsedMilliseconds}");
        }

        [Test]
        public void l_test()
        {
            var sut = CreateSut(ConditionExample.State.A, new ConditionExample());
            var changed = CreatePropertyChanged(TimeSpan.Zero, (ConditionExample.x3Urn, Temperature.Create(0f)));

            WithProperties((ConditionExample.x3Urn, Temperature.Create(0f)),
                (ConditionExample.x4Urn, Temperature.Create(0.5f)),
                (ConditionExample.epsilonUrn, Temperature.Create(0.2f)));
            var watch = System.Diagnostics.Stopwatch.StartNew();
            var activationCount = 1000000;
            for (var i = 0; i < activationCount; i++)
            {
                sut.PlayEvents(changed);
            }

            watch.Stop();
            var elapsedMs = watch.ElapsedMilliseconds;
            Console.WriteLine($"{activationCount} sut activations in {elapsedMs}");
            Check.That(sut.CurrentState).IsEqualTo(ConditionExample.State.A);
        }

        public class ConditionExample : SubSystemDefinition<ConditionExample.State>
        {
            public enum State
            {
                A,
                B,
            }

            private static readonly SubSystemNode _subSystemNode =
                new SubSystemNode(nameof(ConditionExample), null);

            public static PropertyUrn<Temperature> x3Urn => PropertyUrn<Temperature>.Build("x3", "measure");
            public static PropertyUrn<Temperature> x4Urn => PropertyUrn<Temperature>.Build("x4", "measure");
            public static PropertyUrn<Temperature> epsilonUrn => PropertyUrn<Temperature>.Build("x5", "epsilon");

            public ConditionExample()
            {
                // @formatter:off
                Subsystem(_subSystemNode)
                    .Initial(State.A)
                    .Define(State.A)
                        .Transitions
                            .When(EqualWithEpsilon(x3Urn,x4Urn, epsilonUrn)).Then(State.B)
                    .Define(State.B);
                // @formatter:on
            }
        }
    }
}
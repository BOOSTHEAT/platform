#nullable disable
using System;
using System.Linq;
using ImpliciX.Data.Factory;
using ImpliciX.Language.Metrics;
using ImpliciX.Language.Metrics.Internals;
using ImpliciX.Language.Model;
using ImpliciX.RuntimeFoundations.Events;
using ImpliciX.RuntimeFoundations.Factory;
using NFluent;
using NUnit.Framework;
using metrics = ImpliciX.Language.Metrics.Metrics;

namespace ImpliciX.CommunicationMetrics.Tests
{
    public class CommunicationCountersTest
    {
        private RootModelNode root { get; set; }
        private Monitoring[] m { get; set; }
        private CommunicationMetricsService sut { get; set; }

        class Monitoring : SubSystemNode
        {
            public DeviceNode slave;
            public AnalyticsCommunicationCountersNode AnalyticSlaveCommunication;

            public Monitoring(ModelNode parent, int idx) : base(idx.ToString(), parent)
            {
                slave = new DeviceNode(nameof(slave), this);
                AnalyticSlaveCommunication = new AnalyticsCommunicationCountersNode(nameof(AnalyticSlaveCommunication), this);
            }
        }

        [SetUp]
        public void Init()
        {
            root = new RootModelNode(nameof(root));
            var modelFactory = new ModelFactory(typeof(fake_analytics_model).Assembly);
            var domainEventFactory = new DomainEventFactory(modelFactory, () => TimeSpan.Zero);
            m = Enumerable.Range(0, 3).Select(i => new Monitoring(root, i)).ToArray();
            var metricsDefinitions = m
                .Select(x => metrics.Metric(x.AnalyticSlaveCommunication).Is.Minutely.DeviceMonitoringOf(x.slave))
                .Select(def => def.Builder.Build<Metric<AnalyticsCommunicationCountersNode>>())
                .ToArray();
            sut = new CommunicationMetricsService(
                metricsDefinitions,
                domainEventFactory);
        }

        private SlaveCommunicationOccured HealthyCommunicationOccured(DeviceNode slave, double time, ushort successes, ushort failures)
            => SlaveCommunicationOccured.CreateHealthy(slave, TimeSpan.FromSeconds(time), new CommunicationDetails(successes, failures));

        private SlaveCommunicationOccured FatalCommunicationOccured(DeviceNode slave, double time, ushort successes, ushort failures)
            => SlaveCommunicationOccured.CreateFatal(slave, TimeSpan.FromSeconds(time), new CommunicationDetails(successes, failures));

        private PropertiesChanged CountersChange(int time,
            params (AnalyticsCommunicationCountersNode analyticSlaveCom, ulong total, ulong failures, ulong fatal)[] counterValues)
        {
            var props = counterValues.SelectMany(c =>
            {
                return new IDataModelValue[]
                {
                    Property<Counter>.Create(c.analyticSlaveCom.request_count, Counter.FromInteger(c.total), TimeSpan.FromSeconds(time)),
                    Property<Counter>.Create(c.analyticSlaveCom.failed_request_count, Counter.FromInteger(c.failures), TimeSpan.FromSeconds(time)),
                    Property<Counter>.Create(c.analyticSlaveCom.fatal_request_count, Counter.FromInteger(c.fatal), TimeSpan.FromSeconds(time))
                };
            }).ToArray();

            return PropertiesChanged.Create(props, TimeSpan.FromSeconds(time));
        }


        [Test]
        public void no_counter_publication_under_one_minute()
        {
            sut.HandleSlaveCommunication(HealthyCommunicationOccured(m[0].slave, 0, 1, 0));
            sut.HandleSlaveCommunication(HealthyCommunicationOccured(m[0].slave, 59, 1, 0));
            var resultingEvents = sut.HandleSystemTicked(SystemTicked.Create(1000, 59));
            Check.That(resultingEvents).IsEmpty();
        }

        [Test]
        public void counter_publication_after_one_minute()
        {
            Check.That(sut.HandleSlaveCommunication(HealthyCommunicationOccured(m[0].slave, 0, 1, 3))).IsEmpty();

            var resultingEvents = sut.HandleSystemTicked(SystemTicked.Create(1000, 60));
            var expectedEvents = CountersChange(60,
                (m[0].AnalyticSlaveCommunication, 4, 3, 0)
            );
            Check.That(resultingEvents).ContainsExactly(expectedEvents);
        }

        [Test]
        public void sum_communications_in_a_one_minute_timespan()
        {
            Check.That(sut.HandleSlaveCommunication(HealthyCommunicationOccured(m[0].slave, 0, 1, 3))).IsEmpty();
            Check.That(sut.HandleSlaveCommunication(HealthyCommunicationOccured(m[0].slave, 5, 3, 2))).IsEmpty();
            Check.That(sut.HandleSlaveCommunication(HealthyCommunicationOccured(m[0].slave, 35, 1, 2))).IsEmpty();

            var resultingEvents = sut.HandleSystemTicked(SystemTicked.Create(1000, 60));
            var expectedEvents = CountersChange(60,
                (m[0].AnalyticSlaveCommunication, 12, 7, 0)
            );
            Check.That(resultingEvents).ContainsExactly(expectedEvents);
        }

        [Test]
        public void sum_communications_in_a_one_minute_timespan_starting_at_any_time()
        {
            Check.That(sut.HandleSlaveCommunication(HealthyCommunicationOccured(m[0].slave, 410, 1, 3))).IsEmpty();
            Check.That(sut.HandleSlaveCommunication(HealthyCommunicationOccured(m[0].slave, 415, 3, 2))).IsEmpty();
            Check.That(sut.HandleSlaveCommunication(HealthyCommunicationOccured(m[0].slave, 445, 1, 2))).IsEmpty();

            var resultingEvents = sut.HandleSystemTicked(SystemTicked.Create(1000, 480));
            var expectedEvents = CountersChange(60,
                (m[0].AnalyticSlaveCommunication, 12, 7, 0)
            );
            Check.That(resultingEvents).ContainsExactly(expectedEvents);
        }

        [Test]
        public void sum_communications_in_consecutive_one_minute_timespans()
        {
            {
                Check.That(sut.HandleSlaveCommunication(HealthyCommunicationOccured(m[0].slave, 0, 1, 3))).IsEmpty();
                Check.That(sut.HandleSlaveCommunication(HealthyCommunicationOccured(m[0].slave, 5, 3, 2))).IsEmpty();
                Check.That(sut.HandleSlaveCommunication(HealthyCommunicationOccured(m[0].slave, 35, 1, 2))).IsEmpty();
                var resultingEvents = sut.HandleSystemTicked(SystemTicked.Create(1000, 60));
                var expectedEvents = CountersChange(60,
                    (m[0].AnalyticSlaveCommunication, 12, 7, 0)
                );
                Check.That(resultingEvents).ContainsExactly(expectedEvents);
            }
            {
                Check.That(sut.HandleSlaveCommunication(HealthyCommunicationOccured(m[0].slave, 75, 4, 1))).IsEmpty();
                Check.That(sut.HandleSlaveCommunication(HealthyCommunicationOccured(m[0].slave, 119, 1, 2))).IsEmpty();

                var resultingEvents = sut.HandleSystemTicked(SystemTicked.Create(1000, 120));
                var expectedEvents = CountersChange(120,
                    (m[0].AnalyticSlaveCommunication, 20, 10, 0)
                );
                Check.That(resultingEvents).ContainsExactly(expectedEvents);
            }
        }

        [Test]
        public void weaved_communications_across_multiple_slaves()
        {
            Check.That(sut.HandleSlaveCommunication(HealthyCommunicationOccured(m[0].slave, 0, 1, 3))).IsEmpty();
            Check.That(sut.HandleSlaveCommunication(HealthyCommunicationOccured(m[1].slave, 0, 2, 6))).IsEmpty();
            Check.That(sut.HandleSlaveCommunication(HealthyCommunicationOccured(m[2].slave, 5, 3, 9))).IsEmpty();
            var resultingEvents = sut.HandleSystemTicked(SystemTicked.Create(1000, 60));
            var expectedEvents = CountersChange(60,
                (m[0].AnalyticSlaveCommunication, 4, 3, 0),
                (m[1].AnalyticSlaveCommunication, 8, 6, 0),
                (m[2].AnalyticSlaveCommunication, 12, 9, 0)
            );
            Check.That(resultingEvents).ContainsExactly(expectedEvents);
        }

        [Test]
        public void count_fatal_errors()
        {
            Check.That(sut.HandleSlaveCommunication(HealthyCommunicationOccured(m[0].slave, 0, 1, 3))).IsEmpty();
            Check.That(sut.HandleSlaveCommunication(FatalCommunicationOccured(m[0].slave, 5, 0, 1))).IsEmpty();
            Check.That(sut.HandleSlaveCommunication(HealthyCommunicationOccured(m[0].slave, 35, 3, 2))).IsEmpty();
            Check.That(sut.HandleSlaveCommunication(FatalCommunicationOccured(m[0].slave, 40, 0, 1))).IsEmpty();
            Check.That(sut.HandleSlaveCommunication(HealthyCommunicationOccured(m[0].slave, 60, 1, 0))).IsEmpty();

            var resultingEvents = sut.HandleSystemTicked(SystemTicked.Create(1000, 60));
            var expectedEvents = CountersChange(60,
                (m[0].AnalyticSlaveCommunication, 12, 7, 2)
            );
            Check.That(resultingEvents).ContainsExactly(expectedEvents);
        }


        [Test]
        public void ignore_slave_when_no_analytics_defined()
        {
            var unknownSlave = new SoftwareDeviceNode("whatever", root);
            Check.That(sut.HandleSlaveCommunication(HealthyCommunicationOccured(unknownSlave, 0, 1, 3))).IsEmpty();
        }
    }
}
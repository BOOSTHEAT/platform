using System;
using System.Collections.Generic;
using ImpliciX.Driver.Common.PropertiesFIlters;
using ImpliciX.Driver.Common.State;
using ImpliciX.Language.Model;
using NFluent;
using NUnit.Framework;
using static ImpliciX.Language.Model.MeasureStatus;

namespace ImpliciX.Driver.Common.Tests.PropertiesFilters
{
    [TestFixture]
    public class PropertiesFiltersTests
    {
        [Test]
        public void on_first_acquisition_should_output_success_properties()
        {
            var status = new List<IDataModelValue>
                {new DataModelValue<MeasureStatus>(PropertyUrn<MeasureStatus>.Build("some","urn"), Success, Time(0))};

            var resultingEvent = StatusPropertiesFilter.Filter(StateKeeper, status);

            Check.That(resultingEvent).ContainsExactly(status);
        }

        [Test]
        public void after_successful_acquisition_on_next_acquisition_should_not_output_success_properties()
        {
            var acquisition1 = new List<IDataModelValue>
                {new DataModelValue<MeasureStatus>(PropertyUrn<MeasureStatus>.Build("some","urn"), Success, Time(0))};
            var acquisition2 = new List<IDataModelValue>
                {new DataModelValue<MeasureStatus>(PropertyUrn<MeasureStatus>.Build("some","urn"), Success, Time(0))};

            StatusPropertiesFilter.Filter(StateKeeper, acquisition1);
            var resultingEvent = StatusPropertiesFilter.Filter(StateKeeper, acquisition2);

            Check.That(resultingEvent).IsEmpty();
        }

        [Test]
        public void after_successful_acquisition_on_next_acquisition_should_output_failed_properties()
        {
            var acquisition1 = new List<IDataModelValue>
                {new DataModelValue<MeasureStatus>(PropertyUrn<MeasureStatus>.Build("some","urn"), Success, Time(0))};
            var acquisition2 = new List<IDataModelValue>
                {new DataModelValue<MeasureStatus>(PropertyUrn<MeasureStatus>.Build("some","urn"), Failure, Time(0))};

            StatusPropertiesFilter.Filter(StateKeeper, acquisition1);
            var resultingEvent = StatusPropertiesFilter.Filter(StateKeeper, acquisition2);

            var expectedValue = new DataModelValue<MeasureStatus>(PropertyUrn<MeasureStatus>.Build("some","urn"), Failure, Time(0));

            Check.That(resultingEvent).ContainsExactly(expectedValue);
        }

        [Test]
        public void after_failed_acquisition_on_next_sucessfull_acquisition_should_output_success_properties()
        {
            var acquisition1 = new List<IDataModelValue>
                {new DataModelValue<MeasureStatus>(PropertyUrn<MeasureStatus>.Build("some","urn"), Failure, Time(0))};
            var acquisition2 = new List<IDataModelValue>
                {new DataModelValue<MeasureStatus>(PropertyUrn<MeasureStatus>.Build("some","urn"), Success, Time(0))};

            StatusPropertiesFilter.Filter(StateKeeper, acquisition1);
            var resultingEvent = StatusPropertiesFilter.Filter(StateKeeper, acquisition2);

            var expectedValue = new DataModelValue<MeasureStatus>(PropertyUrn<MeasureStatus>.Build("some","urn"), Success, Time(0));

            Check.That(resultingEvent).ContainsExactly(expectedValue);
        }

        [Test]
        public void many_urns_tests()
        {
            var acquisition1 = new List<IDataModelValue>
            {
                new DataModelValue<MeasureStatus>(PropertyUrn<MeasureStatus>.Build("some","urn"), Success, Time(0)),
                new DataModelValue<MeasureStatus>(PropertyUrn<MeasureStatus>.Build("some","other","urn"), Success, Time(0))
            };

            var acquisition2 = new List<IDataModelValue>
            {
                new DataModelValue<MeasureStatus>(PropertyUrn<MeasureStatus>.Build("some","urn"), Success, Time(1)),
                new DataModelValue<MeasureStatus>(PropertyUrn<MeasureStatus>.Build("some","other","urn"), Failure, Time(1))
            };

            var acquisition3 = new List<IDataModelValue>
            {
                new DataModelValue<MeasureStatus>(PropertyUrn<MeasureStatus>.Build("some","urn"), Success, Time(2)),
                new DataModelValue<MeasureStatus>(PropertyUrn<MeasureStatus>.Build("some","other","urn"), Success, Time(2))
            };

            StatusPropertiesFilter.Filter(StateKeeper, acquisition1);
            StatusPropertiesFilter.Filter(StateKeeper, acquisition2);
            var resultingEvent = StatusPropertiesFilter.Filter(StateKeeper, acquisition3);

            var expectedValue = new DataModelValue<MeasureStatus>(PropertyUrn<MeasureStatus>.Build("some","other","urn"), Success, Time(0));

            Check.That(resultingEvent).ContainsExactly(expectedValue);
        }
       
        [SetUp]
        public void Setup()
        {
            StateKeeper = new DriverStateKeeper();
          }
     private DriverStateKeeper StateKeeper { get; set; }

        private TimeSpan Time(int n) =>
            TimeSpan.Zero.Add(TimeSpan.FromSeconds(n));
    }
}
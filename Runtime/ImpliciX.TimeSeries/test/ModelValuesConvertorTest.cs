using System;
using System.Collections.Generic;
using ImpliciX.Language.Model;
using NUnit.Framework;

namespace ImpliciX.TimeSeries.Test
{
    [TestFixture]
    public class DataPointsTests
    {
        [Test]
        public void ToDataPoints_WithMetricsOnly_AllMetricValuesConverted()
        {
            var values = new List<IDataModelValue>
            {
                new DataModelValue<MetricValue>(Urn.BuildUrn("my_urn"), MetricValue.FromString("40.0").Value, TimeSpan.FromSeconds(60)),
                new DataModelValue<MetricValue>(Urn.BuildUrn("my_urn"), MetricValue.FromString("40.0").Value, TimeSpan.FromSeconds(60)),
                new DataModelValue<MetricValue>(Urn.BuildUrn("my_urn"), MetricValue.FromString("40.0").Value, TimeSpan.FromSeconds(60)),
            };

            var result = ModelValuesConvertor.ToDataPoints(values, true);
            
            Assert.AreEqual(3, result.Count);
        }
        
        [Test]
        public void ToDataPoints_WithMetricsOnly_NonMetricValuesFiltered()
        {
            var values = new List<IDataModelValue>
            {
                new DataModelValue<MetricValue>(Urn.BuildUrn("my_urn"), MetricValue.FromString("40.0").Value, TimeSpan.FromSeconds(60)),
                new DataModelValue<MetricValue>(Urn.BuildUrn("my_urn"), MetricValue.FromString("40.0").Value, TimeSpan.FromSeconds(60)),
                new DataModelValue<Duration>(Urn.BuildUrn("my_urn"), Duration.FromFloat(20.0f).Value, TimeSpan.FromSeconds(60)),
            };

            var result = ModelValuesConvertor.ToDataPoints(values, true);
            
            Assert.AreEqual(2, result.Count);
        }
        
        [Test]
        public void ToDataPoints_WithoutMetricsOnly_AllValueTypesConverted()
        {
            var values = new List<IDataModelValue>
            {
                new DataModelValue<MetricValue>(Urn.BuildUrn("my_urn"), MetricValue.FromString("40.0").Value, TimeSpan.FromSeconds(60)),
                new DataModelValue<MetricValue>(Urn.BuildUrn("my_urn"), MetricValue.FromString("40.0").Value, TimeSpan.FromSeconds(60)),
                new DataModelValue<Duration>(Urn.BuildUrn("my_urn"), Duration.FromFloat(20.0f).Value, TimeSpan.FromSeconds(60)),
            };

            var result = ModelValuesConvertor.ToDataPoints(values, false);
            
            Assert.AreEqual(3, result.Count);
        }
    }
}
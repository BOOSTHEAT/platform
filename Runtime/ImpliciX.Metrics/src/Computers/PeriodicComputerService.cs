using System;
using System.Linq;
using ImpliciX.Data.HotTimeSeries;
using ImpliciX.Language.Core;
using ImpliciX.SharedKernel.Redis;
using ImpliciX.SharedKernel.Storage;

namespace ImpliciX.Metrics.Computers
{
    /// <summary>
    /// Responsibilities:
    /// - Store and give read/write access to an input stream for an input metric urn, periodStartAt and samplingEndAt
    ///  </summary>
    /// <remarks>
    /// - samplingStartAt : Start of metric observation period
    /// - samplingEndAt : End of metric observation period
    /// </remarks>
    internal sealed class PeriodicComputerService
    {
        private readonly TimeSpan _inputStreamPeriod;
        private readonly TimeSpan _publicationPeriod;
        private readonly ValuesStream _inputValuesStream;
        private readonly ValuesStream _samplingStartAtStream;
        private readonly ValuesStream _samplingEndAtStream;

        public PeriodicComputerService(string streamKey, TimeSpan inputStreamPeriod, TimeSpan publicationPeriod,
            IReadTimeSeries tsReader, IWriteTimeSeries tsWriter, TimeSpan now)
        {
            _inputStreamPeriod = inputStreamPeriod;
            _publicationPeriod = publicationPeriod;

            _inputValuesStream = _inputStreamPeriod == ValuesStreamPeriod.KeepLatestOnly
                ? ValuesStream.Create($"{streamKey}$inputValues", tsReader, tsWriter, ValuesStreamPeriod.KeepLatestOnly)
                : ValuesStream.Create($"{streamKey}$inputValues", tsReader, tsWriter, _inputStreamPeriod, () => GetSamplingStartAt().GetValue());

            _samplingStartAtStream = ValuesStream.Create($"{streamKey}$samplingStartAt", tsReader, tsWriter, ValuesStreamPeriod.KeepLatestOnly);
            _samplingEndAtStream = ValuesStream.Create($"{streamKey}$samplingEndAt", tsReader, tsWriter, ValuesStreamPeriod.KeepLatestOnly);

            var measureStartAt = GetSamplingStartAt();
            if (measureStartAt.IsNone)
                SetSamplingStartAt(now);
        }

        public void AddToInputValues(FloatAt stateItem)
            => _inputValuesStream.Write(stateItem.Value, stateItem.At);

        public Option<TimeSpan> GetSamplingEndAt()
            => _samplingEndAtStream.Last().Match(Option<TimeSpan>.None, data => data.At);

        public Option<TimeSpan> GetSamplingStartAt()
            => _samplingStartAtStream.Last().Match(Option<TimeSpan>.None, data => data.At);

        public Option<FloatAt> GetAt(TimeSpan at)
            => _inputValuesStream.GetAt(at).Match(Option<FloatAt>.None, data => new FloatAt(data.Value, data.At));

        public Option<FloatAt> GetFirstInput()
            => _inputValuesStream.First().Match(Option<FloatAt>.None, data => new FloatAt(data.Value, data.At));

        public Option<FloatAt> GetLastInput(TimeSpan? upTo = null)
            => _inputValuesStream.Last(upTo).Match(Option<FloatAt>.None, data => new FloatAt(data.Value, data.At));

        public Option<FloatAt[]> GetInputsForPublish(TimeSpan publicationAt)
        {
            var upToThatExcludeItemOnPublicationAt = new TimeSpan(publicationAt.Ticks - 1);
            if (upToThatExcludeItemOnPublicationAt < TimeSpan.Zero) return Option<FloatAt[]>.None();

            return _inputValuesStream.All(upToThatExcludeItemOnPublicationAt)
                .Match(
                    Option<FloatAt[]>.None,
                    inputs => inputs.Select(input => new FloatAt(input.Value, input.At)).ToArray()
                );
        }

        public Option<FloatAt[]> GetAllInputs()
            => _inputValuesStream.ReadAllPeriodValues()
                .Match(
                    Option<FloatAt[]>.None,
                    inputs => inputs.Select(input => new FloatAt(input.Value, input.At)).ToArray()
                );

        private void SetSamplingStartAt(TimeSpan time) => _samplingStartAtStream.Write(0, time);
        public void SetSamplingEndAt(TimeSpan time) => _samplingEndAtStream.Write(0, time);
        public void AddNewInputValue(float value, TimeSpan at) => _inputValuesStream.Write(value, at);

        public void SetSamplingStartForNextPublish(TimeSpan publishAt)
        {
            if (!IsPeriodEnding(publishAt)) return;

            var newStart = publishAt - _inputStreamPeriod + _publicationPeriod;
            SetSamplingStartAt(newStart);
        }

        public Option<TimeSpan> GetSamplingStartOnNextPublish(TimeSpan currentPublishAt)
            => GetSamplingStartAt().Match(
                Option<TimeSpan>.None,
                startAt =>
                    currentPublishAt >= startAt + _inputStreamPeriod
                        ? currentPublishAt - _inputStreamPeriod + _publicationPeriod
                        : startAt
            );

        public bool IsPeriodEnding(TimeSpan now)
            => GetSamplingStartAt().Match(
                () => false,
                startAt =>
                {
                    var periodEndAt = startAt + _inputStreamPeriod;
                    return now >= periodEndAt;
                });
    }
}
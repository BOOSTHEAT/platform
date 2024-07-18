using System;
using ImpliciX.Language.Model;
using ImpliciX.RuntimeFoundations.Events;
using ImpliciX.SharedKernel.Bricks;
using ImpliciX.TestsCommon;

namespace ImpliciX.TimeMath.Tests.Steps;

internal class TimeMathServiceTestContext
{
  public TimeMathServiceTestContext(
    TimeMathService timeTimeMathService
  )
  {
    TimeMathService = timeTimeMathService;
  }

  private TimeMathService TimeMathService { get; }

  public DomainEvent[] AdvanceTimeTo(
    float minutes
  )
  {
    return AdvanceTimeTo(TimeSpan.FromMinutes(minutes));
  }

  public DomainEvent[] AdvanceTimeTo(
    TimeSpan at
  )
  {
    var events = TimeMathService.HandleSystemTicked(
      SystemTicked.Create(
        TimeSpan.Zero,
        1000,
        (uint)at.TotalSeconds
      )
    );

    return events;
  }

  public DomainEvent[] ChangeValues(TimeSpan at, params IDataModelValue[] modelValues) =>
    TimeMathService.HandlePropertiesChanged(
      PropertiesChanged.Create(
        modelValues,
        at
      )
    );

  public DomainEvent[] ChangeValue(
    string inputUrn,
    float atInMinutes,
    float value
  )
  {
    return ChangeValue(
      inputUrn,
      TimeSpan.FromMinutes(atInMinutes),
      value
    );
  }

  private DomainEvent[] ChangeValue(
    string inputUrn,
    TimeSpan at,
    float value
  ) =>
    ChangeValues(
      at,
      new[]
      {
        CreateProperty(
          inputUrn,
          value,
          at
        )
      }
    );

  private static IDataModelValue CreateProperty(
    string urn,
    float value,
    TimeSpan at
  )
  {
    return Property<FloatValue>.Create(
      PropertyUrn<FloatValue>.Build(urn),
      new FloatValue(value),
      at
    );
  }
}
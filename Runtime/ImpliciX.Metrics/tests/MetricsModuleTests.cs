using System;
using System.Collections.Generic;
using ImpliciX.Data.HotTimeSeries;
using ImpliciX.Language.Metrics.Internals;
using ImpliciX.Runtime;
using ImpliciX.SharedKernel.Bus;
using ImpliciX.SharedKernel.Clock;
using ImpliciX.SharedKernel.IO;
using ImpliciX.SharedKernel.Redis;
using ImpliciX.SharedKernel.Storage;
using ImpliciX.SharedKernel.Tools;
using Moq;
using NFluent;
using NUnit.Framework;

namespace ImpliciX.Metrics.Tests;

public class MetricsModuleTests
{

    private static IProvideDependency InitProvideDependency(MetricsSettings metricsSettings, IFileSystemService fileSystemService,
        IReadTimeSeries readTimeSeries, IWriteTimeSeries writeTimeSeries)
    {
        var clock = new Mock<IClock>();
        clock.Setup(o => o.Now()).Returns(TimeSpan.Zero);

        var provider = new Mock<IProvideDependency>();
        provider.Setup(o => o.GetService<IClock>()).Returns(clock.Object);
        provider.Setup(o => o.GetService<IFileSystemService>()).Returns(fileSystemService);
        provider.Setup(o => o.GetService<IReadTimeSeries>()).Returns(readTimeSeries);
        provider.Setup(o => o.GetService<IWriteTimeSeries>()).Returns(writeTimeSeries);
        provider.Setup(o => o.GetService<IEventBusWithFirewall>()).Returns(new Mock<IEventBusWithFirewall>().Object);
        provider.Setup(o => o.GetSettings<MetricsSettings>(It.IsAny<string>())).Returns(metricsSettings);

        return provider.Object;
    }
}
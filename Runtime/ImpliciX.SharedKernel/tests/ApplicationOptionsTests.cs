using System;
using System.Collections.Generic;
using ImpliciX.Runtime;
using ImpliciX.SharedKernel.IO;
using Moq;
using NFluent;
using NUnit.Framework;

namespace ImpliciX.SharedKernel.Tests
{

    public class ApplicationOptionsTests
    {
        private static object[] _localStorageNominalCases =
        {
            new object[] { "/var/lib/implicix_app", new Dictionary<string, string>(), "/var/lib/implicix_app" },
            new object[] { "", new Dictionary<string, string> { { "LOCAL_STORAGE", "/home/bh/app" } }, "/home/bh/app" },
            new object[]
            {
                "/var/lib/implicix_app", new Dictionary<string, string> { { "LOCAL_STORAGE", "/home/bh/app" } },
                "/home/bh/app"
            }
        };

        [TestCaseSource(nameof(_localStorageNominalCases))]
        public void GivenILSInEnvAndUnknownInOptions_WhenICreate_ThenLocalStorageIsEqualsToExpected(string envILS,
            IReadOnlyDictionary<string, string> optionsDictionary, string expectedLocalStorage)
        {
            var envService = new Mock<IEnvironmentService>();
            envService.Setup(o => o.GetEnvironmentVariable("IMPLICIX_LOCAL_STORAGE")).Returns(envILS);

            Check.That(new ApplicationOptions(optionsDictionary, envService.Object).LocalStoragePath)
                .IsEqualTo(expectedLocalStorage);
        }

        [Test]
        public void GivenILSEnvAndLSOptionsAreEmpty_WhenICreate_ThenIGetAnException()
        {
            var envService = new Mock<IEnvironmentService>();
            envService.Setup(o => o.GetEnvironmentVariable("IMPLICIX_LOCAL_STORAGE")).Returns("");

            Check.ThatCode(() => new ApplicationOptions(new Dictionary<string, string>(), envService.Object))
                .Throws<InvalidOperationException>()
                .WithMessage(
                    "Local storage path can not be found in 'IMPLICIX_LOCAL_STORAGE' environment variable or 'LOCAL_STORAGE' appSettings Options section");
        }

        [TestCaseSource(nameof(_startModeCases))]
        public void ApplicationStartModeTests(Dictionary<string, string> optionsDict, StartMode expectedStartMode)
        {
            var appOptions = new ApplicationOptions(optionsDict, new Mock<IEnvironmentService>().Object);
            Check.That(appOptions.StartMode).IsEqualTo(expectedStartMode);
        }

        private static object[] _startModeCases =
        {
            new object[] {new Dictionary<string, string>()
            {
                ["LOCAL_STORAGE"] = "/tmp/foo"
            }, StartMode.Safe},
            new object[]
            {
                new Dictionary<string, string>()
                {
                    ["START_MODE"] = "safe",
                    ["LOCAL_STORAGE"] = "/tmp/foo"
                },
                StartMode.Safe
            },
            new object[]
            {
                new Dictionary<string, string>()
                {
                    ["START_MODE"] = "sAfE",
                    ["LOCAL_STORAGE"] = "/tmp/foo"
                },
                StartMode.Safe
            },
            new object[]
            {
                new Dictionary<string, string>()
                {
                    ["START_MODE"] = "failfast",
                    ["LOCAL_STORAGE"] = "/tmp/foo"
                },
                StartMode.FailFast
            },
            new object[]
            {
                new Dictionary<string, string>()
                {
                    ["START_MODE"] = "bar",
                    ["LOCAL_STORAGE"] = "/tmp/foo"
                },
                StartMode.Safe
            },
        };
    }
}
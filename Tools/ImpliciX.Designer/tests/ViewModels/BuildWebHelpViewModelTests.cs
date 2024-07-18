using System.Windows.Input;
using ImpliciX.Designer.ViewModels;
using Moq;
using NFluent;
using NUnit.Framework;
using IConcierge = ImpliciX.DesktopServices.IConcierge;


namespace ImpliciX.Designer.Tests.ViewModels
{
    internal class BuildWebHelpViewModelTests
    {
        // [Test]
        // TODO : Sert uniquement pour la Mise au point
        // public async Task WhenICreateWebHelpCommand()
        // {
        //     var dockerRegistryAuth = new DockerRegistryAuth("pull-images",
        //         "mettre le password du token");
        //
        //     var sut = new BuildWebHelpViewModel(new Concierge(), dockerRegistryAuth);
        //     sut.InputFolderPath = "/home/christophe/WS/Boostheat/ImpliciX.BhTcs/documentation";
        //     sut.OutputFolderPath = "/home/christophe/tmp";
        //
        //     await sut.CreateWebHelpCommand.Execute().ToTask();
        // }

        [TestCase("", "")]
        [TestCase("myInputPath", "")]
        [TestCase("", "myOutputPath")]
        public void GivenSomeRequiredParametersForCreateWebHelpCommandAreUnknown_ThenICanNotExecute(string inputPath, string outputPath)
        {
            var sut = new BuildWebHelpViewModel(new Mock<IConcierge>().Object);
            sut.InputFolderPath = inputPath;
            sut.OutputFolderPath = outputPath;
            Check.That(((ICommand) sut.CreateWebHelpCommand).CanExecute(null)).IsFalse();
        }

        [Test]
        public void GivenAllRequiredParametersForCreateWebHelpCommandAreKnown_ThenICanExecute()
        {
            var sut = new BuildWebHelpViewModel(new Mock<IConcierge>().Object);
            sut.InputFolderPath = "myInputPath";
            sut.OutputFolderPath = "myOutputPath";
            Check.That(((ICommand) sut.CreateWebHelpCommand).CanExecute(null)).IsTrue();
        }
    }
}
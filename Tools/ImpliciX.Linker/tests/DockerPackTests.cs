using NUnit.Framework;
using System.Collections.Generic;
using ImpliciX.Linker.Values;

namespace ImpliciX.Linker.Tests
{
    public class DockerPackTests
    {
        [Test]
        public void GetFileNameAndRevisionForTarget_ValidManifest_ShouldReturnCorrectValues()
        {
            var manifest =
                """{"Device":"Beaver","Revision":"0.2023.7599.28002","Date":"2023-11-13T14:50:41.9233746+00:00","SHA256":"c6b0034fbcceb7dce9962790007237c4d28cc29ff5cf73342e1142c539008505","Content":{"MCU":[],"APPS":[{"Target":"device:app","Revision":"0.2023.7599.28002","FileName":"app.zip"},{"Target":"device:gui","Revision":"0.2023.7599.28002","FileName":"ImpliciX.GUI"}],"BSP":[]}}""";
            var target = "device:app";
            var (fileName, revision) = DockerPack.GetFileNameAndRevisionForTarget(manifest, target);

            Assert.AreEqual("app.zip", fileName);
            Assert.AreEqual("0.2023.7599.28002", revision);
        }

        [Test]
        public void ParseArguments_ValidArguments_ShouldReturnCorrectExecutionContext()
        {
            var args = new Dictionary<string, object>
            {
                ["package"] = "path/to/package",
                ["compose"] = "path/to/composeFile",
                ["output"] = "path/to/outputFile",
                ["run"] = new List<DockerContainer>
                    { new("device:app,implicix_backend,back"), new("device:gui,implicix_gui,gui") },
                ["file"] = new List<string> { "file1", "file2" }
            };

            var context = DockerPack.ParseArguments(args);

            Assert.AreEqual("path/to/package", context.Package);
            Assert.AreEqual("path/to/composeFile", context.ComposeFile);
            Assert.AreEqual("path/to/outputFile", context.OutputFile);
            Assert.IsNotNull(context.Containers);
            Assert.IsNotNull(context.Files);
        }
    }
}
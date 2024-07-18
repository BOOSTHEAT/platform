using NUnit.Framework;
using System.IO;
using System.Text;

namespace ImpliciX.ToQml.Tests
{
    [TestFixture]
    public class CopyrightManagerTests
    {
        private ICopyrightManager _copyrightManager;
        private string _licenceTemplate;

        [SetUp]
        public void Setup()
        {
            const string applicationName = "MyApplication";
            const int year = 2023;
            _copyrightManager = new CopyrightManager(applicationName, year);
            _licenceTemplate = string.Format(CopyrightManager.LicenseTemplate, applicationName, year);
        }

        [Test]
        public void AddCopyrightToStream_WithValidExtension_ShouldAddCopyright()
        {
            const string filename = "sample.qml";
            const string content = "Sample content";
            var expectedContent = _licenceTemplate + content;
            using var stream = new MemoryStream();
            _copyrightManager.AddCopyright(stream, filename);
            var result = Encoding.UTF8.GetString(stream.ToArray()) + content;
            Assert.AreEqual(expectedContent, result);
        }

        [Test]
        public void AddCopyrightToStream_WithInvalidExtension_ShouldNotAddCopyright()
        {

            const string filename = "sample.txt";
            const string content = "Sample content";
            using var stream = new MemoryStream();
            _copyrightManager.AddCopyright(stream, filename);
            var result = Encoding.UTF8.GetString(stream.ToArray()) + content;

            Assert.AreEqual(content, result);
        }

        [Test]
        public void AddCopyrightToString_WithValidExtension_ShouldReturnUpdatedContent()
        {
            const string filename = "sample.qml";
            const string content = "Sample content";
            var expectedContent =  _licenceTemplate + content;
            var result = _copyrightManager.AddCopyright(content, filename);
            Assert.AreEqual(expectedContent, result);
        }

        [Test]
        public void AddCopyrightToString_WithInvalidExtension_ShouldReturnOriginalContent()
        {
            const string filename = "sample.txt";
            const string content = "Sample content";
            var result = _copyrightManager.AddCopyright(content, filename);
            Assert.AreEqual(content, result);
        }
    }
}

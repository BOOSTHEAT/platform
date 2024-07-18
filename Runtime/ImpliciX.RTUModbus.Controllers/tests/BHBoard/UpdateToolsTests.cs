using System.IO;
using System.Linq;
using ImpliciX.RTUModbus.Controllers.Helpers;
using NFluent;
using NUnit.Framework;

namespace ImpliciX.RTUModbus.Controllers.Tests.BHBoard
{
    [TestFixture]
    public class UpdateToolsTests
    {
        [TestCase("BinExamples/Carte_BAS.bin","0B31D7BE",27408u)]
        [TestCase("BinExamples/Carte_HAUT.bin","324D8DE7",22732u)]
        [TestCase("BinExamples/Carte_UE.bin","3B3FB335", 20740u)]
        public void should_compute_crc(string fileName,string expectedCRC,uint expectedSize)
        {
            byte[] bytes = File.ReadAllBytes(fileName);
            var result = UpdateTools.ComputeCrc(bytes);
            Check.That(result.Value.crc).Equals(expectedCRC);
            Check.That(result.Value.size).Equals(expectedSize);
        }

        [Test]
        public void compute_chunks_size_even()
        {
            byte[] bytes = Enumerable.Repeat<byte>(0xca, 500).ToArray();
            var chunks = UpdateTools.ComputeChunks(bytes, 32).ToArray();
            Check.That(chunks[..^1].All(c => c.Registers.Length == 16)).IsTrue();
            Check.That(chunks[^1].Registers.Length == 10).IsTrue();
        }
        
        
        [Test]
        public void compute_chunks_size_odd()
        {
            byte[] bytes = Enumerable.Repeat<byte>(0xca, 500).ToArray();
            var chunks = UpdateTools.ComputeChunks(bytes, 61).ToArray();
            Check.That(chunks[..^1].All(c => c.Registers.Length == 31)).IsTrue();
            Check.That(chunks[^1].Registers.Length == 6).IsTrue();
        }
        
        /*
        [Test]
        public void bug_bootloader()
        {
            byte[] bytes = Enumerable.Repeat<byte>(0xca, 253).ToArray();
            var chunks = UpdateTools.ComputeChunks(bytes, 32).ToArray();
            Check.That(chunks[..^1].All(c => c.Registers.Length == 16)).IsTrue();
            Check.That(chunks[^1].Registers.Length == 15).IsTrue(); // interpreté par le bootloader comme un arret de l'update en cours 
        }
        */
    }
}
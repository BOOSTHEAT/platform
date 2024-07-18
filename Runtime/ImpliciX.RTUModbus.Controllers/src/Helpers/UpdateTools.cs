using System;
using System.Collections.Generic;
using System.Linq;
using ImpliciX.Language.Core;
using ImpliciX.Language.Modbus;

namespace ImpliciX.RTUModbus.Controllers.Helpers
{
    public static class UpdateTools
    {
        //uses ST-Microelectronics algorithm to compute the crc
        //it is important bc. the crc is verified by the mcu bootloader once the update is finished
        //boiler app send the crc in the first frame 
        public static Result<(uint size, string crc)> ComputeCrc(byte[] content)
        {
            var size = (uint)content.Length;
            if (size % 4 != 0) return new Error("FirmwareFile_Content_Error","The input is not 32 bits aligned");

            var sizeInWords = size / 4;

            var poly = 0x04C11DB7u;
            var crctab = Enumerable.Repeat((ulong) 1, 32).ToArray();

            for (var index = 0; index < sizeInWords; index++)
            {
                var buffer = content.Skip((int)index*4).Take(4).ToArray();
                var wdata = BitConverter.ToUInt32(buffer, 0);

                for (var j = 0; j < 32; j++)
                {
                    var crc_temp = (crctab[31] << 31) + (crctab[30] << 30) + (crctab[29] << 29) +
                                   (crctab[28] << 28) + (crctab[27] << 27) + (crctab[26] << 26) + (crctab[25] << 25) +
                                   (crctab[24] << 24) + (crctab[23] << 23) + (crctab[22] << 22) + (crctab[21] << 21) +
                                   (crctab[20] << 20) + (crctab[19] << 19) + (crctab[18] << 18) + (crctab[17] << 17) +
                                   (crctab[16] << 16) + (crctab[15] << 15) + (crctab[14] << 14) + (crctab[13] << 13) +
                                   (crctab[12] << 12) + (crctab[11] << 11) + (crctab[10] << 10) + (crctab[9] << 9) +
                                   (crctab[8] << 8) + (crctab[7] << 7) + (crctab[6] << 6) + (crctab[5] << 5) +
                                   (crctab[4] << 4) + (crctab[3] << 3) + (crctab[2] << 2) + (crctab[1] << 1) + (crctab[0]);

                    crctab[0] = ((wdata >> (31 - j)) & 0x1) ^ crctab[31];

                    for (var i = 1; i < 32; i++)
                    {
                        crctab[i] = (crctab[0] & ((poly >> i) & 0x1)) ^ ((crc_temp >> (i - 1)) & 0x1);
                    }
                }
            }

            var crc = (crctab[31] << 31) + (crctab[30] << 30) + (crctab[29] << 29) +
                      (crctab[28] << 28) + (crctab[27] << 27) + (crctab[26] << 26) + (crctab[25] << 25) +
                      (crctab[24] << 24) + (crctab[23] << 23) + (crctab[22] << 22) + (crctab[21] << 21) +
                      (crctab[20] << 20) + (crctab[19] << 19) + (crctab[18] << 18) + (crctab[17] << 17) +
                      (crctab[16] << 16) + (crctab[15] << 15) + (crctab[14] << 14) + (crctab[13] << 13) +
                      (crctab[12] << 12) + (crctab[11] << 11) + (crctab[10] << 10) + (crctab[9] << 9) +
                      (crctab[8] << 8) + (crctab[7] << 7) + (crctab[6] << 6) + (crctab[5] << 5) + (crctab[4] << 4) +
                      (crctab[3] << 3) + (crctab[2] << 2) + (crctab[1] << 1) + (crctab[0] << 0);

            return (size, crc.ToString("X8"));
        }

        public static Queue<Chunk> ComputeChunks(byte[] contentBytes, int chunkSize = 32)
        {
            var sliceContents = SliceContents().ToArray();
            return new Queue<Chunk>(sliceContents);

            IEnumerable<Chunk> SliceContents()
            {
                var nbSlices = (int) Math.Ceiling(contentBytes.Length / (double) chunkSize);
                var i = 0;
                while (i < nbSlices)
                {
                    var sliceContent = contentBytes.Skip(chunkSize * i).Take(chunkSize).ToArray();
                    var registers = RegistersConverterHelper.ToRegisters(sliceContent);
                    yield return Chunk.Create(registers, i, nbSlices);
                    i++;
                }
            }
        }
    }
}
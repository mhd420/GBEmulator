using GBEmulator.Properties;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GBEmulator.Emulator
{
    public class GameboyCart
    {
        private byte[] romBank0, romBank1;
        private byte[] exRam;

        public GameboyCart()
        {
            romBank0 = new byte[0x4000];
            romBank1 = new byte[0x4000];
            exRam = new byte[0x2000];

            Array.Copy(Resources.tetris, 0, romBank0, 0, 0x3FFF);
            Array.Copy(Resources.tetris, 0x4000, romBank1, 0, 0x3FFF);
        }

        public byte Read(ushort addr)
        {
            switch (addr & 0xF000)
            {
                case 0x0000:
                case 0x1000:
                case 0x2000:
                case 0x3000:
                    return romBank0[addr & 0x3FFF];

                case 0x4000:
                case 0x5000:
                case 0x6000:
                case 0x7000:
                    return romBank1[addr & 0x3FFF];

                case 0xA000:
                case 0xB000:
                    return exRam[addr & 0x1FFF];

                default:
                    return 0;
            }
        }

        public void Write(ushort addr, byte data)
        {
            switch (addr & 0xF000)
            {
                case 0x0000:
                case 0x1000:
                case 0x2000:
                case 0x3000:
                    break;

                case 0x4000:
                case 0x5000:
                case 0x6000:
                case 0x7000:
                    break;

                case 0xA000:
                case 0xB000:
                    exRam[addr & 0x1FFF] = data;
                    break;

                default:
                    break;
            }
        }
    }
}

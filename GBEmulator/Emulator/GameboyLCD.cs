using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GBEmulator.Emulator
{
    public class GameboyLCD
    {
        public byte LY = 0;
        public byte SY, SX, WY, WX = 0;

        public byte Mode = 0;

        public byte[] VRAM = new byte[8192];
        public byte[] OAM = new byte[160];

        public GameboyLCD()
        {

        }

        public void Write(ushort addr, byte data)
        {
            switch (addr & 0xF000)
            {
                case 0x8000:
                case 0x9000:
                    if (Mode != 3)
                        VRAM[addr ^ 0x8000] = data;
                    break;


            }
        }
    }
}

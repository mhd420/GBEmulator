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

        public byte[] VRAM = new byte[4096];
        public byte[] OAM = new byte[160];

        public GameboyLCD()
        {

        }
    }
}

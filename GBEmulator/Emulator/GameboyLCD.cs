using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GBEmulator.Emulator
{
    public class GameboyLCD
    {
        public byte LCY = 0;
        public byte SCY, SCX, WY, WX = 0;

        public byte LCDC, STAT, BGP, OBP0, OBP1;

        public byte Mode = 0;

        public byte[] VRAM = new byte[8192];
        public byte[] OAM = new byte[160];

        
        public GameboyLCD()
        {

        }
    }
}

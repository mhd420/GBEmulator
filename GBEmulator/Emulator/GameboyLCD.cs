using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GBEmulator.Emulator
{
    public class GameboyLCD
    {
        public byte LY, LYC;
        public byte SCY, SCX, WY, WX = 0;

        public byte LCDC, BGP, OBP0, OBP1;

        public byte Mode = 0;

        public byte[] VRAM = new byte[8192];
        public byte[] OAM = new byte[160];

        private byte stat;

        public GameboyLCD()
        {

        }

        public byte STAT
        {
            get
            {
                // bit 0-1 - the current mode
                byte mode = (byte)(Mode & 0x3);
                
                // bit 2 - if ly = lyc : set to 1
                byte lycMatch = (byte)((LY == LYC) ? 0x4 : 0x0);

                return (byte)(stat | lycMatch | mode);
            }
            set { stat = (byte)(value & 0xF8); }
        }

    }
}

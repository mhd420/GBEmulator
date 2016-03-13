using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GBEmulator.Emulator
{
    public class GameboyInput
    {
        public bool Up, Down, Left, Right;
        public bool A, B, Select, Start;

        private byte JOYP;

        public GameboyInput()
        {

        }

        public void Write(byte data)
        {
            JOYP = (byte)(data & 0x30);
        }

        public byte Read()
        {
            byte result = JOYP;

            // if bit 4 is 0, return the state of the dpad
            if ((JOYP & 0x10) == 0x0)
            {
                // if button is pressed it remains low
                result |= (byte)(Right ? 0 : 0x1);
                result |= (byte)(Left ? 0 : 0x2);
                result |= (byte)(Up ? 0 : 0x4);
                result |= (byte)(Down ? 0 : 0x8);
            }

            // if bit 5 is 0, return the state of the buttons
            if ((JOYP & 0x20) == 0x0)
            {
                result |= (byte)(A ? 0 : 0x1);
                result |= (byte)(B ? 0 : 0x2);
                result |= (byte)(Select ? 0 : 0x4);
                result |= (byte)(Start ? 0 : 0x8);
            }
            return result;
        }
    }
}

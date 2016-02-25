using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GBEmulator.Emulator
{
    public class GameboyMMU
    {
        public GameboyMMU()
        {

        }

        public void Clock(int cycles)
        {

        }

        public void Write8(ushort addr, byte data)
        {
            Write(addr, data);
        }

        public void Write16(ushort addr, ushort data)
        {
            Write(addr++, (byte)(data >> 8)); Write(addr, (byte)data);
        }

        public byte Read8(ushort addr)
        {
            return Read(addr);
        }

        public ushort Read16(ushort addr)
        {
            return (ushort)((Read(addr) << 8) + Read(addr++));
        }

        private void Write(ushort addr, byte data)
        {

        }

        private byte Read(ushort addr)
        {
            return 0;
        }
    }
}

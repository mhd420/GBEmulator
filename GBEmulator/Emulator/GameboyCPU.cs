using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GBEmulator.Emulator
{
    [Flags]
    public enum CPUFlags : byte
    {
        Carry = 0x10,
        HalfCarry = 0x20,
        Subtract = 0x40,
        Zero = 0x80,
    }

    public class GameboyCPU
    {
        byte A, B, C, D, E, H, L;
        short SP, PC;

        CPUFlags Flags;

        public short AF
        {
            get { return (short)((A << 8) + F); }
            set
            {
                A = (byte)(value >> 8);
                F = (byte)value;
            }
        }

        public short BC
        {
            get { return (short)((B << 8) + C); }
            set
            {
                B = (byte)(value >> 8);
                C = (byte)value;
            }
        }

        public short DE
        {
            get { return (short)((D << 8) + E); }
            set
            {
                D = (byte)(value >> 8);
                E = (byte)value;
            }
        }

        public short HL
        {
            get { return (short)((H << 8) + L); }
            set
            {
                H = (byte)(value >> 8);
                L = (byte)value;
            }
        }

        public byte F
        {
            get { return (byte)Flags; }
            set { Flags = (CPUFlags)value; }
        }

        public int Step()
        {

        }
        
    }
}

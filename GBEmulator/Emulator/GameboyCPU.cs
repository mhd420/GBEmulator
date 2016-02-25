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
        C = 0x10,
        H = 0x20,
        N = 0x40,
        Z = 0x80,
    }

    public class GameboyCPU
    {

        #region Constants

        const byte INT_VBLANK = 0x1;
        const byte INT_LCDSTAT = 0x2;
        const byte INT_TIMER = 0x4;
        const byte INT_SERIAL = 0x8;
        const byte INT_JOYPAD = 0x10;

        #endregion

        #region Registers

        public byte A, B, C, D, E, H, L;
        public ushort SP, PC;

        public CPUFlags Flags;

        public bool Stop, Halt, IME;
        public byte IE, IF;

        public ushort AF
        {
            get { return (ushort)((A << 8) + F); }
            set
            {
                A = (byte)(value >> 8);
                F = (byte)value;
            }
        }

        public ushort BC
        {
            get { return (ushort)((B << 8) + C); }
            set
            {
                B = (byte)(value >> 8);
                C = (byte)value;
            }
        }

        public ushort DE
        {
            get { return (ushort)((D << 8) + E); }
            set
            {
                D = (byte)(value >> 8);
                E = (byte)value;
            }
        }

        public ushort HL
        {
            get { return (ushort)((H << 8) + L); }
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

        #endregion

        private GameboyMMU mmu;

        private Func<int>[] opCodes;
        private Func<int>[] cbCodes;

        public GameboyCPU(GameboyMMU mmu)
        {
            this.mmu = mmu;

            SP = 0xFFFE;
            PC = 0x0000;

            InitialiseOpcodes();
            InitialiseCBCodes();
        }

        public void Step()
        {
            // Handle interrupts first
            if (IME && ((IE & IF) != 0))
            {
                if (((IE & IF) & INT_VBLANK) != 0)
                {
                    CallISR(0x40);
                    return;
                }
                if (((IE & IF) & INT_LCDSTAT) != 0)
                {
                    CallISR(0x48);
                    return;
                }
                if (((IE & IF) & INT_TIMER) != 0)
                {
                    CallISR(0x50);
                    return;
                }
                if (((IE & IF) & INT_SERIAL) != 0)
                {
                    CallISR(0x58);
                    return;
                }
                if (((IE & IF) & INT_JOYPAD) != 0)
                {
                    CallISR(0x60);
                    return;
                }
            }

            if (!Halt && !Stop)
            {
                byte nextOp = mmu.Read8(PC);
                mmu.Clock(opCodes[nextOp]());
            }
            else
            {
                mmu.Clock(4);
            }
        }

        #region Opcode Map

        private void InitialiseOpcodes()
        {
            opCodes = new Func<int>[0xFF];

            // 0x0x
            opCodes[0x00] = () => { return 4; }; // NOP
            opCodes[0x01] = () => { return 12; }; // LD BC,d16
            opCodes[0x02] = () => { return 8; }; // LD (BC),A
            opCodes[0x03] = () => { return 8; }; // INC BC
            opCodes[0x04] = () => { return 4; }; // INC B
            opCodes[0x05] = () => { return 4; }; // DEC B
            opCodes[0x06] = () => { return 8; }; // LD B,d8
            opCodes[0x07] = () => { return 4; }; // RLCA
            opCodes[0x08] = () => { return 20; }; // LD (a16),SP
            opCodes[0x09] = () => { return 8; }; // ADD HL,BC
            opCodes[0x0A] = () => { return 8; }; // LD A,(BC)
            opCodes[0x0B] = () => { return 8; }; // DEC BC
            opCodes[0x0C] = () => { return 4; }; // INC C
            opCodes[0x0D] = () => { return 4; }; // DEC C
            opCodes[0x0E] = () => { return 8; }; // LD C,d8
            opCodes[0x0F] = () => { return 4; }; // RRCA

            // 0x1x
            opCodes[0x10] = () => { Stop = true; return 4; }; // STOP
            opCodes[0x11] = () => { return 12; }; // LD DE,d16
            opCodes[0x12] = () => { return 8; }; // LD (DE),A
            opCodes[0x13] = () => { return 8; }; // INC DE
            opCodes[0x14] = () => { return 4; }; // INC D
            opCodes[0x15] = () => { return 4; }; // DEC D
            opCodes[0x16] = () => { return 8; }; // LD D,d8
            opCodes[0x17] = () => { return 4; }; // RLA
            opCodes[0x18] = () => { return 12; }; // JR r8
            opCodes[0x19] = () => { return 8; }; // ADD HL,DE
            opCodes[0x1A] = () => { return 8; }; // LD A,(DE)
            opCodes[0x1B] = () => { return 8; }; // DEC DE
            opCodes[0x1C] = () => { return 4; }; // INC E
            opCodes[0x1D] = () => { return 4; }; // DEC E
            opCodes[0x1E] = () => { return 8; }; // LD E,d8
            opCodes[0x1F] = () => { return 4; }; // RRA

            // 0x2x
            opCodes[0x20] = () => { return 12; }; // JR NZ,r8 8 cycles if false
            opCodes[0x21] = () => { return 12; }; // LD HL,d16
            opCodes[0x22] = () => { return 8; }; // LD (HL+),A
            opCodes[0x23] = () => { return 8; }; // INC HL
            opCodes[0x24] = () => { return 4; }; // INC H
            opCodes[0x25] = () => { return 4; }; // DEC H
            opCodes[0x26] = () => { return 8; }; // LD H,d8
            opCodes[0x27] = () => { return 4; }; // DAA
            opCodes[0x28] = () => { return 12; }; // JR Z,r8 8 cycles if false
            opCodes[0x29] = () => { return 8; }; // ADD HL,HL
            opCodes[0x2A] = () => { return 8; }; // LD A,(HL+)
            opCodes[0x2B] = () => { return 8; }; // DEC HL
            opCodes[0x2C] = () => { return 4; }; // INC L
            opCodes[0x2D] = () => { return 4; }; // DEC L
            opCodes[0x2E] = () => { return 8; }; // LD L,d8
            opCodes[0x2F] = () => { return 4; }; // CPL

            // 0x3x
            opCodes[0x30] = () => { return 12; }; // JR NC,r8 8 cycles if false
            opCodes[0x31] = () => { return 12; }; // LD SP,d16
            opCodes[0x32] = () => { return 8; }; // LD (HL-),A
            opCodes[0x33] = () => { return 8; }; // INC SP
            opCodes[0x34] = () => { return 12; }; // INC (HL)
            opCodes[0x35] = () => { return 12; }; // DEC (HL)
            opCodes[0x36] = () => { return 12; }; // LD (HL),d8
            opCodes[0x37] = () => { return 4; }; // SCF
            opCodes[0x38] = () => { return 12; }; // JR C,r8 8 cycles if false
            opCodes[0x39] = () => { return 8; }; // ADD HL,SP
            opCodes[0x3A] = () => { return 8; }; // LD A,(HL-)
            opCodes[0x3B] = () => { return 8; }; // DEC SP
            opCodes[0x3C] = () => { return 4; }; // INC A
            opCodes[0x3D] = () => { return 4; }; // DEC A
            opCodes[0x3E] = () => { return 8; }; // LD A,d8
            opCodes[0x3F] = () => { return 4; }; // CCF

            // 0x4x
            opCodes[0x40] = () => { return 4; }; // LD B,B
            opCodes[0x41] = () => { return 4; }; // LD B,C
            opCodes[0x42] = () => { return 4; }; // LD B,D
            opCodes[0x43] = () => { return 4; }; // LD B,E
            opCodes[0x44] = () => { return 4; }; // LD B,H
            opCodes[0x45] = () => { return 4; }; // LD B,L
            opCodes[0x46] = () => { return 8; }; // LD B,(HL)
            opCodes[0x47] = () => { return 4; }; // LD B,A
            opCodes[0x48] = () => { return 4; }; // LD C,B
            opCodes[0x49] = () => { return 4; }; // LD C,C
            opCodes[0x4A] = () => { return 4; }; // LD C,D 
            opCodes[0x4B] = () => { return 4; }; // LD C,E
            opCodes[0x4C] = () => { return 4; }; // LD C,H
            opCodes[0x4D] = () => { return 4; }; // LD C,L
            opCodes[0x4E] = () => { return 8; }; // LD C,(HL)
            opCodes[0x4F] = () => { return 4; }; // LD C,A

            // 0x5x
            opCodes[0x50] = () => { return 4; }; // LD D,B
            opCodes[0x51] = () => { return 4; }; // LD D,C
            opCodes[0x52] = () => { return 4; }; // LD D,D
            opCodes[0x53] = () => { return 4; }; // LD D,E
            opCodes[0x54] = () => { return 4; }; // LD D,H
            opCodes[0x55] = () => { return 4; }; // LD D,L
            opCodes[0x56] = () => { return 8; }; // LD D,(HL)
            opCodes[0x57] = () => { return 4; }; // LD D,A
            opCodes[0x58] = () => { return 4; }; // LD E,B
            opCodes[0x59] = () => { return 4; }; // LD E,C
            opCodes[0x5A] = () => { return 4; }; // LD E,D 
            opCodes[0x5B] = () => { return 4; }; // LD E,E
            opCodes[0x5C] = () => { return 4; }; // LD E,H
            opCodes[0x5D] = () => { return 4; }; // LD E,L
            opCodes[0x5E] = () => { return 8; }; // LD E,(HL)
            opCodes[0x5F] = () => { return 4; }; // LD E,A

            // 0x6x
            opCodes[0x60] = () => { return 4; }; // LD H,B
            opCodes[0x61] = () => { return 4; }; // LD H,C
            opCodes[0x62] = () => { return 4; }; // LD H,D
            opCodes[0x63] = () => { return 4; }; // LD H,E
            opCodes[0x64] = () => { return 4; }; // LD H,H
            opCodes[0x65] = () => { return 4; }; // LD H,L
            opCodes[0x66] = () => { return 8; }; // LD H,(HL)
            opCodes[0x67] = () => { return 4; }; // LD H,A
            opCodes[0x68] = () => { return 4; }; // LD L,B
            opCodes[0x69] = () => { return 4; }; // LD L,C
            opCodes[0x6A] = () => { return 4; }; // LD L,D 
            opCodes[0x6B] = () => { return 4; }; // LD L,E
            opCodes[0x6C] = () => { return 4; }; // LD L,H
            opCodes[0x6D] = () => { return 4; }; // LD L,L
            opCodes[0x6E] = () => { return 8; }; // LD L,(HL)
            opCodes[0x6F] = () => { return 4; }; // LD L,A

            // 0x7x
            opCodes[0x70] = () => { return 8; }; // LD (HL),B
            opCodes[0x71] = () => { return 8; }; // LD (HL),C
            opCodes[0x72] = () => { return 8; }; // LD (HL),D
            opCodes[0x73] = () => { return 8; }; // LD (HL),E
            opCodes[0x74] = () => { return 8; }; // LD (HL),H
            opCodes[0x75] = () => { return 8; }; // LD (HL),L
            opCodes[0x76] = () => { Halt = true; return 4; }; // HALT
            opCodes[0x77] = () => { return 8; }; // LD (HL),A
            opCodes[0x78] = () => { return 4; }; // LD A,B
            opCodes[0x79] = () => { return 4; }; // LD A,C
            opCodes[0x7A] = () => { return 4; }; // LD A,D 
            opCodes[0x7B] = () => { return 4; }; // LD A,E
            opCodes[0x7C] = () => { return 4; }; // LD A,H
            opCodes[0x7D] = () => { return 4; }; // LD A,L
            opCodes[0x7E] = () => { return 8; }; // LD A,(HL)
            opCodes[0x7F] = () => { return 4; }; // LD A,A

            // 0x8x
            opCodes[0x80] = () => { return 4; }; // ADD A,B
            opCodes[0x81] = () => { return 4; }; // ADD A,C
            opCodes[0x82] = () => { return 4; }; // ADD A,D
            opCodes[0x83] = () => { return 4; }; // ADD A,E
            opCodes[0x84] = () => { return 4; }; // ADD A,H
            opCodes[0x85] = () => { return 4; }; // ADD A,L
            opCodes[0x86] = () => { return 8; }; // ADD A,(HL)
            opCodes[0x87] = () => { return 4; }; // ADD A,A
            opCodes[0x88] = () => { return 4; }; // ADC A,B
            opCodes[0x89] = () => { return 4; }; // ADC A,C
            opCodes[0x8A] = () => { return 4; }; // ADC A,D
            opCodes[0x8B] = () => { return 4; }; // ADC A,E
            opCodes[0x8C] = () => { return 4; }; // ADC A,H
            opCodes[0x8D] = () => { return 4; }; // ADC A,L
            opCodes[0x8E] = () => { return 8; }; // ADC A,(HL)
            opCodes[0x8F] = () => { return 4; }; // ADC A,A

            // 0x9x
            opCodes[0x90] = () => { return 4; }; // SUB B
            opCodes[0x91] = () => { return 4; }; // SUB C
            opCodes[0x92] = () => { return 4; }; // SUB D
            opCodes[0x93] = () => { return 4; }; // SUB E
            opCodes[0x94] = () => { return 4; }; // SUB H
            opCodes[0x95] = () => { return 4; }; // SUB L
            opCodes[0x96] = () => { return 8; }; // SUB (HL)
            opCodes[0x97] = () => { return 4; }; // SUB A
            opCodes[0x98] = () => { return 4; }; // SBC A,B
            opCodes[0x99] = () => { return 4; }; // SBC A,C
            opCodes[0x9A] = () => { return 4; }; // SBC A,D
            opCodes[0x9B] = () => { return 4; }; // SBC A,E
            opCodes[0x9C] = () => { return 4; }; // SBC A,H
            opCodes[0x9D] = () => { return 4; }; // SBC A,L
            opCodes[0x9E] = () => { return 8; }; // SBC A,(HL)
            opCodes[0x9F] = () => { return 4; }; // SBC A,A

            // 0xAx
            opCodes[0xA0] = () => { return 4; }; // AND B
            opCodes[0xA1] = () => { return 4; }; // AND C
            opCodes[0xA2] = () => { return 4; }; // AND D
            opCodes[0xA3] = () => { return 4; }; // AND E
            opCodes[0xA4] = () => { return 4; }; // AND H
            opCodes[0xA5] = () => { return 4; }; // AND L
            opCodes[0xA6] = () => { return 8; }; // AND (HL)
            opCodes[0xA7] = () => { return 4; }; // AND A
            opCodes[0xA8] = () => { return 4; }; // XOR B
            opCodes[0xA9] = () => { return 4; }; // XOR C
            opCodes[0xAA] = () => { return 4; }; // XOR D
            opCodes[0xAB] = () => { return 4; }; // XOR E
            opCodes[0xAC] = () => { return 4; }; // XOR H
            opCodes[0xAD] = () => { return 4; }; // XOR L
            opCodes[0xAE] = () => { return 8; }; // XOR (HL)
            opCodes[0xAF] = () => { return 4; }; // XOR A

            // 0xBx
            opCodes[0xB0] = () => { return 4; }; // OR B
            opCodes[0xB1] = () => { return 4; }; // OR C
            opCodes[0xB2] = () => { return 4; }; // OR D
            opCodes[0xB3] = () => { return 4; }; // OR E
            opCodes[0xB4] = () => { return 4; }; // OR H
            opCodes[0xB5] = () => { return 4; }; // OR L
            opCodes[0xB6] = () => { return 8; }; // OR (HL)
            opCodes[0xB7] = () => { return 4; }; // OR A
            opCodes[0xB8] = () => { return 4; }; // CP B
            opCodes[0xB9] = () => { return 4; }; // CP C
            opCodes[0xBA] = () => { return 4; }; // CP D
            opCodes[0xBB] = () => { return 4; }; // CP E
            opCodes[0xBC] = () => { return 4; }; // CP H
            opCodes[0xBD] = () => { return 4; }; // CP L
            opCodes[0xBE] = () => { return 8; }; // CP (HL)
            opCodes[0xBF] = () => { return 4; }; // CP A

            // 0xCx
            opCodes[0xC0] = () => { return 20; }; // RET NZ 8 cycles if false
            opCodes[0xC1] = () => { return 12; }; // POP BC
            opCodes[0xC2] = () => { return 16; }; // JP NZ,a16 12 cycles if false
            opCodes[0xC3] = () => { return 16; }; // JP a16
            opCodes[0xC4] = () => { return 24; }; // CALL NZ,a16 12 cycles if false
            opCodes[0xC5] = () => { return 16; }; // PUSH BC
            opCodes[0xC6] = () => { return 8; }; // ADD A,d8
            opCodes[0xC7] = () => { return 16; }; // RST 0x00
            opCodes[0xC8] = () => { return 20; }; // RET Z 8 cycles if false
            opCodes[0xC9] = () => { return 16; }; // RET
            opCodes[0xCA] = () => { return 16; }; // JP Z,a16 12 cycles if false
            opCodes[0xCB] = () => { return cbCodes[0x00](); }; // CB Prefix
            opCodes[0xCC] = () => { return 24; }; // CALL Z,a16 12 cycles if false
            opCodes[0xCD] = () => { return 24; }; // CALL a16
            opCodes[0xCE] = () => { return 8; }; // ADC A,d8
            opCodes[0xCF] = () => { return 16; }; // RST 0x08

            // 0xDx
            opCodes[0xD0] = () => { return 20; }; // RET NC 8 cycles if false
            opCodes[0xD1] = () => { return 12; }; // POP DE
            opCodes[0xD2] = () => { return 16; }; // JP NC,a16 12 cycles if false
            // Missing
            opCodes[0xD4] = () => { return 24; }; // CALL NC,a16 12 cycles if false
            opCodes[0xD5] = () => { return 16; }; // PUSH DE
            opCodes[0xD6] = () => { return 8; }; // SUB d8
            opCodes[0xD7] = () => { return 16; }; // RST 0x10
            opCodes[0xD8] = () => { return 20; }; // RET C 8 cycles if false
            opCodes[0xD9] = () => { return 16; }; // RETI
            opCodes[0xDA] = () => { return 16; }; // JP C,a16 12 cycles if false
            // Missing
            opCodes[0xDC] = () => { return 24; }; // CALL C,a16 12 cycles if false
            // Missing
            opCodes[0xDE] = () => { return 8; }; // SBC A,d8
            opCodes[0xDF] = () => { return 16; }; // RST 0x18

            // 0xEx
            opCodes[0xE0] = () => { return 12; }; // LDH (a8),A
            opCodes[0xE1] = () => { return 12; }; // POP HL
            opCodes[0xE2] = () => { return 8; }; // LD (C),A
            // Missing
            // Missing
            opCodes[0xE5] = () => { return 16; }; // PUSH HL
            opCodes[0xE6] = () => { return 8; }; // AND d8
            opCodes[0xE7] = () => { return 16; }; // RST 0x20
            opCodes[0xE8] = () => { return 16; }; // ADD SP,r8
            opCodes[0xE9] = () => { return 4; }; // JP (HL)
            opCodes[0xEA] = () => { return 16; }; // LD (a16),A
            // Missing
            // Missing
            // Missing
            opCodes[0xEE] = () => { return 8; }; // XOR d8
            opCodes[0xEF] = () => { return 16; }; // RST 0x28

            // 0xFx
            opCodes[0xF0] = () => { return 12; }; // LDH A,(a8)
            opCodes[0xF1] = () => { return 12; }; // POP AF
            opCodes[0xF2] = () => { return 8; }; // LD A,(C)
            opCodes[0xF3] = () => { return 4; }; // DI
            // Missing
            opCodes[0xF5] = () => { return 16; }; // PUSH AF
            opCodes[0xF6] = () => { return 8; }; // OR d8
            opCodes[0xF7] = () => { return 16; }; // RST 0x30
            opCodes[0xF8] = () => { return 12; }; // LD HL,SP+r8
            opCodes[0xF9] = () => { return 8; }; // LD SP,HL
            opCodes[0xFA] = () => { return 16; }; // LD A,(a16)
            opCodes[0xFB] = () => { return 4; }; // EI
            // Missing
            // Missing 
            opCodes[0xFE] = () => { return 8; }; // CP d8
            opCodes[0xFF] = () => { return 16; }; // RST 0x38
        }

        private void InitialiseCBCodes()
        {
            cbCodes = new Func<int>[0xFF];

            // 0x0x
            cbCodes[0x00] = () => { return 8; }; // RLC B
            cbCodes[0x01] = () => { return 8; }; // RLC C
            cbCodes[0x02] = () => { return 8; }; // RLC D
            cbCodes[0x03] = () => { return 8; }; // RLC E
            cbCodes[0x04] = () => { return 8; }; // RLC H
            cbCodes[0x05] = () => { return 8; }; // RLC L
            cbCodes[0x06] = () => { return 16; }; // RLC (HL)
            cbCodes[0x07] = () => { return 8; }; // RLC A
            cbCodes[0x08] = () => { return 8; }; // RRC B
            cbCodes[0x09] = () => { return 8; }; // RRC C
            cbCodes[0x0A] = () => { return 8; }; // RRC D
            cbCodes[0x0B] = () => { return 8; }; // RRC E
            cbCodes[0x0C] = () => { return 8; }; // RRC H
            cbCodes[0x0D] = () => { return 8; }; // RRC L
            cbCodes[0x0E] = () => { return 16; }; // RRC (HL)
            cbCodes[0x0F] = () => { return 8; }; // RRC A

            // 0x1x
            cbCodes[0x10] = () => { return 8; }; // RL B
            cbCodes[0x11] = () => { return 8; }; // RL C
            cbCodes[0x12] = () => { return 8; }; // RL D
            cbCodes[0x13] = () => { return 8; }; // RL E
            cbCodes[0x14] = () => { return 8; }; // RL H
            cbCodes[0x15] = () => { return 8; }; // RL L
            cbCodes[0x16] = () => { return 16; }; // RL (HL)
            cbCodes[0x17] = () => { return 8; }; // RL A
            cbCodes[0x18] = () => { return 8; }; // RR B
            cbCodes[0x19] = () => { return 8; }; // RR C
            cbCodes[0x1A] = () => { return 8; }; // RR D
            cbCodes[0x1B] = () => { return 8; }; // RR E
            cbCodes[0x1C] = () => { return 8; }; // RR H
            cbCodes[0x1D] = () => { return 8; }; // RR L
            cbCodes[0x1E] = () => { return 16; }; // RR (HL)
            cbCodes[0x1F] = () => { return 8; }; // RR A

            // 0x2x
            cbCodes[0x20] = () => { return 8; }; // SLA B
            cbCodes[0x21] = () => { return 8; }; // SLA C
            cbCodes[0x22] = () => { return 8; }; // SLA D
            cbCodes[0x23] = () => { return 8; }; // SLA E
            cbCodes[0x24] = () => { return 8; }; // SLA H
            cbCodes[0x25] = () => { return 8; }; // SLA L
            cbCodes[0x26] = () => { return 16; }; // SLA (HL)
            cbCodes[0x27] = () => { return 8; }; // SLA A
            cbCodes[0x28] = () => { return 8; }; // SRA B
            cbCodes[0x29] = () => { return 8; }; // SRA C
            cbCodes[0x2A] = () => { return 8; }; // SRA D
            cbCodes[0x2B] = () => { return 8; }; // SRA E
            cbCodes[0x2C] = () => { return 8; }; // SRA H
            cbCodes[0x2D] = () => { return 8; }; // SRA L
            cbCodes[0x2E] = () => { return 16; }; // SRA (HL)
            cbCodes[0x2F] = () => { return 8; }; // SRA A

            // 0x3x
            cbCodes[0x30] = () => { return 8; }; // SWAP B
            cbCodes[0x31] = () => { return 8; }; // SWAP C
            cbCodes[0x32] = () => { return 8; }; // SWAP D
            cbCodes[0x33] = () => { return 8; }; // SWAP E
            cbCodes[0x34] = () => { return 8; }; // SWAP H
            cbCodes[0x35] = () => { return 8; }; // SWAP L
            cbCodes[0x36] = () => { return 16; }; // SWAP (HL)
            cbCodes[0x37] = () => { return 8; }; // SWAP A
            cbCodes[0x38] = () => { return 8; }; // SRL B
            cbCodes[0x39] = () => { return 8; }; // SRL C
            cbCodes[0x3A] = () => { return 8; }; // SRL D
            cbCodes[0x3B] = () => { return 8; }; // SRL E
            cbCodes[0x3C] = () => { return 8; }; // SRL H
            cbCodes[0x3D] = () => { return 8; }; // SRL L
            cbCodes[0x3E] = () => { return 16; }; // SRL (HL)
            cbCodes[0x3F] = () => { return 8; }; // SRL A

            // 0x4x
            cbCodes[0x40] = () => { return 8; }; // BIT 0,B
            cbCodes[0x41] = () => { return 8; }; // BIT 0,C
            cbCodes[0x42] = () => { return 8; }; // BIT 0,D
            cbCodes[0x43] = () => { return 8; }; // BIT 0,E
            cbCodes[0x44] = () => { return 8; }; // BIT 0,H
            cbCodes[0x45] = () => { return 8; }; // BIT 0,L
            cbCodes[0x46] = () => { return 16; }; // BIT 0,(HL)
            cbCodes[0x47] = () => { return 8; }; // BIT 0,A
            cbCodes[0x48] = () => { return 8; }; // BIT 1,B
            cbCodes[0x49] = () => { return 8; }; // BIT 1,C
            cbCodes[0x4A] = () => { return 8; }; // BIT 1,D
            cbCodes[0x4B] = () => { return 8; }; // BIT 1,E
            cbCodes[0x4C] = () => { return 8; }; // BIT 1,H
            cbCodes[0x4D] = () => { return 8; }; // BIT 1,L
            cbCodes[0x4E] = () => { return 16; }; // BIT 1,(HL)
            cbCodes[0x4F] = () => { return 8; }; // BIT 1,A

            // 0x5x
            cbCodes[0x50] = () => { return 8; }; // BIT 2,B
            cbCodes[0x51] = () => { return 8; }; // BIT 2,C
            cbCodes[0x52] = () => { return 8; }; // BIT 2,D
            cbCodes[0x53] = () => { return 8; }; // BIT 2,E
            cbCodes[0x54] = () => { return 8; }; // BIT 2,H
            cbCodes[0x55] = () => { return 8; }; // BIT 2,L
            cbCodes[0x56] = () => { return 16; }; // BIT 2,(HL)
            cbCodes[0x57] = () => { return 8; }; // BIT 2,A
            cbCodes[0x58] = () => { return 8; }; // BIT 3,B
            cbCodes[0x59] = () => { return 8; }; // BIT 3,C
            cbCodes[0x5A] = () => { return 8; }; // BIT 3,D
            cbCodes[0x5B] = () => { return 8; }; // BIT 3,E
            cbCodes[0x5C] = () => { return 8; }; // BIT 3,H
            cbCodes[0x5D] = () => { return 8; }; // BIT 3,L
            cbCodes[0x5E] = () => { return 16; }; // BIT 3,(HL)
            cbCodes[0x5F] = () => { return 8; }; // BIT 3,A

            // 0x6x
            cbCodes[0x60] = () => { return 8; }; // BIT 4,B
            cbCodes[0x61] = () => { return 8; }; // BIT 4,C
            cbCodes[0x62] = () => { return 8; }; // BIT 4,D
            cbCodes[0x63] = () => { return 8; }; // BIT 4,E
            cbCodes[0x64] = () => { return 8; }; // BIT 4,H
            cbCodes[0x65] = () => { return 8; }; // BIT 4,L
            cbCodes[0x66] = () => { return 16; }; // BIT 4,(HL)
            cbCodes[0x67] = () => { return 8; }; // BIT 4,A
            cbCodes[0x68] = () => { return 8; }; // BIT 5,B
            cbCodes[0x69] = () => { return 8; }; // BIT 5,C
            cbCodes[0x6A] = () => { return 8; }; // BIT 5,D
            cbCodes[0x6B] = () => { return 8; }; // BIT 5,E
            cbCodes[0x6C] = () => { return 8; }; // BIT 5,H
            cbCodes[0x6D] = () => { return 8; }; // BIT 5,L
            cbCodes[0x6E] = () => { return 16; }; // BIT 5,(HL)
            cbCodes[0x6F] = () => { return 8; }; // BIT 5,A

            // 0x7x
            cbCodes[0x70] = () => { return 8; }; // BIT 6,B
            cbCodes[0x71] = () => { return 8; }; // BIT 6,C
            cbCodes[0x72] = () => { return 8; }; // BIT 6,D
            cbCodes[0x73] = () => { return 8; }; // BIT 6,E
            cbCodes[0x74] = () => { return 8; }; // BIT 6,H
            cbCodes[0x75] = () => { return 8; }; // BIT 6,L
            cbCodes[0x76] = () => { return 16; }; // BIT 6,(HL)
            cbCodes[0x77] = () => { return 8; }; // BIT 6,A
            cbCodes[0x78] = () => { return 8; }; // BIT 7,B
            cbCodes[0x79] = () => { return 8; }; // BIT 7,C
            cbCodes[0x7A] = () => { return 8; }; // BIT 7,D
            cbCodes[0x7B] = () => { return 8; }; // BIT 7,E
            cbCodes[0x7C] = () => { return 8; }; // BIT 7,H
            cbCodes[0x7D] = () => { return 8; }; // BIT 7,L
            cbCodes[0x7E] = () => { return 16; }; // BIT 7,(HL)
            cbCodes[0x7F] = () => { return 8; }; // BIT 7,A

            // 0x8x
            cbCodes[0x80] = () => { return 8; }; // RES 0,B
            cbCodes[0x81] = () => { return 8; }; // RES 0,C
            cbCodes[0x82] = () => { return 8; }; // RES 0,D
            cbCodes[0x83] = () => { return 8; }; // RES 0,E
            cbCodes[0x84] = () => { return 8; }; // RES 0,H
            cbCodes[0x85] = () => { return 8; }; // RES 0,L
            cbCodes[0x86] = () => { return 16; }; // RES 0,(HL)
            cbCodes[0x87] = () => { return 8; }; // RES 0,A
            cbCodes[0x88] = () => { return 8; }; // RES 1,B
            cbCodes[0x89] = () => { return 8; }; // RES 1,C
            cbCodes[0x8A] = () => { return 8; }; // RES 1,D
            cbCodes[0x8B] = () => { return 8; }; // RES 1,E
            cbCodes[0x8C] = () => { return 8; }; // RES 1,H
            cbCodes[0x8D] = () => { return 8; }; // RES 1,L
            cbCodes[0x8E] = () => { return 16; }; // RES 1,(HL)
            cbCodes[0x8F] = () => { return 8; }; // RES 1,A

            // 0x9x
            cbCodes[0x90] = () => { return 8; }; // RES 2,B
            cbCodes[0x91] = () => { return 8; }; // RES 2,C
            cbCodes[0x92] = () => { return 8; }; // RES 2,D
            cbCodes[0x93] = () => { return 8; }; // RES 2,E
            cbCodes[0x94] = () => { return 8; }; // RES 2,H
            cbCodes[0x95] = () => { return 8; }; // RES 2,L
            cbCodes[0x96] = () => { return 16; }; // RES 2,(HL)
            cbCodes[0x97] = () => { return 8; }; // RES 2,A
            cbCodes[0x98] = () => { return 8; }; // RES 3,B
            cbCodes[0x99] = () => { return 8; }; // RES 3,C
            cbCodes[0x9A] = () => { return 8; }; // RES 3,D
            cbCodes[0x9B] = () => { return 8; }; // RES 3,E
            cbCodes[0x9C] = () => { return 8; }; // RES 3,H
            cbCodes[0x9D] = () => { return 8; }; // RES 3,L
            cbCodes[0x9E] = () => { return 16; }; // RES 3,(HL)
            cbCodes[0x9F] = () => { return 8; }; // RES 3,A

            // 0xAx
            cbCodes[0xA0] = () => { return 8; }; // RES 4,B
            cbCodes[0xA1] = () => { return 8; }; // RES 4,C
            cbCodes[0xA2] = () => { return 8; }; // RES 4,D
            cbCodes[0xA3] = () => { return 8; }; // RES 4,E
            cbCodes[0xA4] = () => { return 8; }; // RES 4,H
            cbCodes[0xA5] = () => { return 8; }; // RES 4,L
            cbCodes[0xA6] = () => { return 16; }; // RES 4,(HL)
            cbCodes[0xA7] = () => { return 8; }; // RES 4,A
            cbCodes[0xA8] = () => { return 8; }; // RES 5,B
            cbCodes[0xA9] = () => { return 8; }; // RES 5,C
            cbCodes[0xAA] = () => { return 8; }; // RES 5,D
            cbCodes[0xAB] = () => { return 8; }; // RES 5,E
            cbCodes[0xAC] = () => { return 8; }; // RES 5,H
            cbCodes[0xAD] = () => { return 8; }; // RES 5,L
            cbCodes[0xAE] = () => { return 16; }; // RES 5,(HL)
            cbCodes[0xAF] = () => { return 8; }; // RES 5,A

            // 0xBx
            cbCodes[0xB0] = () => { return 8; }; // RES 6,B
            cbCodes[0xB1] = () => { return 8; }; // RES 6,C
            cbCodes[0xB2] = () => { return 8; }; // RES 6,D
            cbCodes[0xB3] = () => { return 8; }; // RES 6,E
            cbCodes[0xB4] = () => { return 8; }; // RES 6,H
            cbCodes[0xB5] = () => { return 8; }; // RES 6,L
            cbCodes[0xB6] = () => { return 16; }; // RES 6,(HL)
            cbCodes[0xB7] = () => { return 8; }; // RES 6,A
            cbCodes[0xB8] = () => { return 8; }; // RES 7,B
            cbCodes[0xB9] = () => { return 8; }; // RES 7,C
            cbCodes[0xBA] = () => { return 8; }; // RES 7,D
            cbCodes[0xBB] = () => { return 8; }; // RES 7,E
            cbCodes[0xBC] = () => { return 8; }; // RES 7,H
            cbCodes[0xBD] = () => { return 8; }; // RES 7,L
            cbCodes[0xBE] = () => { return 16; }; // RES 7,(HL)
            cbCodes[0xBF] = () => { return 8; }; // RES 7,A

            // 0xCx
            cbCodes[0xC0] = () => { return 8; }; // SET 0,B
            cbCodes[0xC1] = () => { return 8; }; // SET 0,C
            cbCodes[0xC2] = () => { return 8; }; // SET 0,D
            cbCodes[0xC3] = () => { return 8; }; // SET 0,E
            cbCodes[0xC4] = () => { return 8; }; // SET 0,H
            cbCodes[0xC5] = () => { return 8; }; // SET 0,L
            cbCodes[0xC6] = () => { return 16; }; // SET 0,(HL)
            cbCodes[0xC7] = () => { return 8; }; // SET 0,A
            cbCodes[0xC8] = () => { return 8; }; // SET 1,B
            cbCodes[0xC9] = () => { return 8; }; // SET 1,C
            cbCodes[0xCA] = () => { return 8; }; // SET 1,D
            cbCodes[0xCB] = () => { return 8; }; // SET 1,E
            cbCodes[0xCC] = () => { return 8; }; // SET 1,H
            cbCodes[0xCD] = () => { return 8; }; // SET 1,L
            cbCodes[0xCE] = () => { return 16; }; // SET 1,(HL)
            cbCodes[0xCF] = () => { return 8; }; // SET 1,A

            // 0xDx
            cbCodes[0xD0] = () => { return 8; }; // SET 2,B
            cbCodes[0xD1] = () => { return 8; }; // SET 2,C
            cbCodes[0xD2] = () => { return 8; }; // SET 2,D
            cbCodes[0xD3] = () => { return 8; }; // SET 2,E
            cbCodes[0xD4] = () => { return 8; }; // SET 2,H
            cbCodes[0xD5] = () => { return 8; }; // SET 2,L
            cbCodes[0xD6] = () => { return 16; }; // SET 2,(HL)
            cbCodes[0xD7] = () => { return 8; }; // SET 2,A
            cbCodes[0xD8] = () => { return 8; }; // SET 3,B
            cbCodes[0xD9] = () => { return 8; }; // SET 3,C
            cbCodes[0xDA] = () => { return 8; }; // SET 3,D
            cbCodes[0xDB] = () => { return 8; }; // SET 3,E
            cbCodes[0xDC] = () => { return 8; }; // SET 3,H
            cbCodes[0xDD] = () => { return 8; }; // SET 3,L
            cbCodes[0xDE] = () => { return 16; }; // SET 3,(HL)
            cbCodes[0xDF] = () => { return 8; }; // SET 3,A

            // 0xEx
            cbCodes[0xE0] = () => { return 8; }; // SET 4,B
            cbCodes[0xE1] = () => { return 8; }; // SET 4,C
            cbCodes[0xE2] = () => { return 8; }; // SET 4,D
            cbCodes[0xE3] = () => { return 8; }; // SET 4,E
            cbCodes[0xE4] = () => { return 8; }; // SET 4,H
            cbCodes[0xE5] = () => { return 8; }; // SET 4,L
            cbCodes[0xE6] = () => { return 16; }; // SET 4,(HL)
            cbCodes[0xE7] = () => { return 8; }; // SET 4,A
            cbCodes[0xE8] = () => { return 8; }; // SET 5,B
            cbCodes[0xE9] = () => { return 8; }; // SET 5,C
            cbCodes[0xEA] = () => { return 8; }; // SET 5,D
            cbCodes[0xEB] = () => { return 8; }; // SET 5,E
            cbCodes[0xEC] = () => { return 8; }; // SET 5,H
            cbCodes[0xED] = () => { return 8; }; // SET 5,L
            cbCodes[0xEE] = () => { return 16; }; // SET 5,(HL)
            cbCodes[0xEF] = () => { return 8; }; // SET 5,A

            // 0xFx
            cbCodes[0xF0] = () => { return 8; }; // SET 6,B
            cbCodes[0xF1] = () => { return 8; }; // SET 6,C
            cbCodes[0xF2] = () => { return 8; }; // SET 6,D
            cbCodes[0xF3] = () => { return 8; }; // SET 6,E
            cbCodes[0xF4] = () => { return 8; }; // SET 6,H
            cbCodes[0xF5] = () => { return 8; }; // SET 6,L
            cbCodes[0xF6] = () => { return 16; }; // SET 6,(HL)
            cbCodes[0xF7] = () => { return 8; }; // SET 6,A
            cbCodes[0xF8] = () => { return 8; }; // SET 7,B
            cbCodes[0xF9] = () => { return 8; }; // SET 7,C
            cbCodes[0xFA] = () => { return 8; }; // SET 7,D
            cbCodes[0xFB] = () => { return 8; }; // SET 7,E
            cbCodes[0xFC] = () => { return 8; }; // SET 7,H
            cbCodes[0xFD] = () => { return 8; }; // SET 7,L
            cbCodes[0xFE] = () => { return 16; }; // SET 7,(HL)
            cbCodes[0xFF] = () => { return 8; }; // SET 7,A
        }

        #endregion

        private void CallISR(byte addr)
        {
            mmu.Clock(8);
            IME = false;

            mmu.Write16(SP, PC);
            mmu.Clock(8);

            PC = addr;
            mmu.Clock(4);
        }

    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GBEmulator.Emulator
{
    public struct CPUFlags
    {
        public bool C, H, N, Z;
    }

    public class GameboyCPU
    {

        #region Constants

        readonly byte INT_VBLANK = 0x1;
        readonly byte INT_LCDSTAT = 0x2;
        readonly byte INT_TIMER = 0x4;
        readonly byte INT_SERIAL = 0x8;
        readonly byte INT_JOYPAD = 0x10;

        #endregion

        #region Registers

        public byte A, B, C, D, E, H, L;
        public ushort SP, PC;

        public CPUFlags Flags;

        public bool Stop, Halt, IME;
        public byte IE, IF;

        public ushort AF
        {
            get { return (ushort)((A << 8) | F); }
            set
            {
                A = (byte)(value >> 8);
                F = (byte)value;
            }
        }

        public ushort BC
        {
            get { return (ushort)((B << 8) | C); }
            set
            {
                B = (byte)(value >> 8);
                C = (byte)value;
            }
        }

        public ushort DE
        {
            get { return (ushort)((D << 8) | E); }
            set
            {
                D = (byte)(value >> 8);
                E = (byte)value;
            }
        }

        public ushort HL
        {
            get { return (ushort)((H << 8) | L); }
            set
            {
                H = (byte)(value >> 8);
                L = (byte)value;
            }
        }

        public byte F
        {
            get
            {
                return (byte)((Flags.C ? 0x10 : 0) | 
                                (Flags.H ? 0x20 : 0) |
                                (Flags.N ? 0x40 : 0) |
                                (Flags.Z ? 0x80 : 0));
            }
            set
            {
                Flags.C = (value & 0x10) != 0;
                Flags.H = (value & 0x20) != 0;
                Flags.N = (value & 0x40) != 0;
                Flags.Z = (value & 0x80) != 0;
            }
        }

        #endregion

        public GameboyMMU MMU;

        /// <summary>
        /// Array of opcode function pointers, each returns number of clocks taken to execute
        /// </summary>
        private Func<int>[] opCodes;

        /// <summary>
        /// Array of CB-prefix opcode function pointers, each returns number of clocks taken to execute
        /// </summary>
        private Func<int>[] cbCodes;

        public GameboyCPU()
        {
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
                    IF &= (byte)(~INT_VBLANK);
                    return;
                }
                if (((IE & IF) & INT_LCDSTAT) != 0)
                {
                    CallISR(0x48);
                    IF &= (byte)(~INT_LCDSTAT);
                    return;
                }
                if (((IE & IF) & INT_TIMER) != 0)
                {
                    CallISR(0x50);
                    IF &= (byte)(~INT_TIMER);
                    return;
                }
                if (((IE & IF) & INT_SERIAL) != 0)
                {
                    CallISR(0x58);
                    IF &= (byte)(~INT_SERIAL);
                    return;
                }
                if (((IE & IF) & INT_JOYPAD) != 0)
                {
                    CallISR(0x60);
                    IF &= (byte)(~INT_JOYPAD);
                    return;
                }
            }

            // If we're Halted, just keep ticking over the clock
            if (Halt)
            {
                MMU.Clock(4);
                return;
            }

            // If we're Stopped, don't do anything after checking interrupts (this is probably not what the real hardware does)
            if (Stop)
                return;

            // Otherwise just fetch and execute next opcode
            byte nextOp = MMU.Read8(PC++);
            MMU.Clock(opCodes[nextOp]());
        }

        #region Opcode Map

        private void InitialiseOpcodes()
        {
            opCodes = new Func<int>[0xFF];

            // 0x0x
            opCodes[0x00] = () => { return 4; }; // NOP
            opCodes[0x01] = () => { BC = MMU.Read16(PC); PC += 2; return 12; }; // LD BC,d16
            opCodes[0x02] = () => { MMU.Write8(BC, A); return 8; }; // LD (BC),A
            opCodes[0x03] = () => { BC++; return 8; }; // INC BC
            opCodes[0x04] = () => { B = Inc8(B); return 4; }; // INC B
            opCodes[0x05] = () => { B = Dec8(B); return 4; }; // DEC B
            opCodes[0x06] = () => { B = MMU.Read8(PC++); return 8; }; // LD B,d8
            opCodes[0x07] = () => { A = Rlc8(A); Flags.Z = false; return 4; }; // RLCA 
            opCodes[0x08] = () => { MMU.Write16(MMU.Read16(PC), SP); PC += 2; return 20; }; // LD (a16),SP
            opCodes[0x09] = () => { HL = Add16(HL, BC); return 8; }; // ADD HL,BC
            opCodes[0x0A] = () => { A = MMU.Read8(BC); return 8; }; // LD A,(BC)
            opCodes[0x0B] = () => { BC--; return 8; }; // DEC BC
            opCodes[0x0C] = () => { C = Inc8(C); return 4; }; // INC C
            opCodes[0x0D] = () => { C = Dec8(C); return 4; }; // DEC C
            opCodes[0x0E] = () => { C = MMU.Read8(PC++); return 8; }; // LD C,d8
            opCodes[0x0F] = () => { A = Rrc8(A); Flags.Z = false; return 4; }; // RRCA

            // 0x1x
            opCodes[0x10] = () => { Stop = true; return 4; }; // STOP
            opCodes[0x11] = () => { DE = MMU.Read16(PC); PC += 2; return 12; }; // LD DE,d16
            opCodes[0x12] = () => { MMU.Write8(DE, A); return 8; }; // LD (DE),A
            opCodes[0x13] = () => { DE++; return 8; }; // INC DE
            opCodes[0x14] = () => { D = Inc8(D); return 4; }; // INC D
            opCodes[0x15] = () => { D = Dec8(D); return 4; }; // DEC D
            opCodes[0x16] = () => { D = MMU.Read8(PC++); return 8; }; // LD D,d8
            opCodes[0x17] = () => { A = Rl8(A); Flags.Z = false; return 4; }; // RLA
            opCodes[0x18] = () => { PC = (ushort)(PC + (sbyte)MMU.Read8(PC)); return 12; }; // JR r8
            opCodes[0x19] = () => { HL = Add16(HL, DE); return 8; }; // ADD HL,DE
            opCodes[0x1A] = () => { A = MMU.Read8(DE); return 8; }; // LD A,(DE)
            opCodes[0x1B] = () => { DE--; return 8; }; // DEC DE
            opCodes[0x1C] = () => { E = Inc8(E); return 4; }; // INC E
            opCodes[0x1D] = () => { E = Dec8(E); return 4; }; // DEC E
            opCodes[0x1E] = () => { E = MMU.Read8(PC++); return 8; }; // LD E,d8
            opCodes[0x1F] = () => { A = Rr8(A); Flags.Z = false; return 4; }; // RRA

            // 0x2x
            opCodes[0x20] = () => { if (Flags.Z) { PC++; return 8; } PC = (ushort)(PC + (sbyte)MMU.Read8(PC)); return 12; }; // JR NZ,r8 8 cycles if false
            opCodes[0x21] = () => { HL = MMU.Read16(PC); PC += 2; return 12; }; // LD HL,d16
            opCodes[0x22] = () => { MMU.Write8(HL++, A); return 8; }; // LD (HL+),A
            opCodes[0x23] = () => { HL++; return 8; }; // INC HL
            opCodes[0x24] = () => { H = Inc8(H); return 4; }; // INC H
            opCodes[0x25] = () => { H = Dec8(H); return 4; }; // DEC H
            opCodes[0x26] = () => { H = MMU.Read8(PC++); return 8; }; // LD H,d8
            opCodes[0x27] = () => { A = Daa(A); return 4; }; // DAA
            opCodes[0x28] = () => { if (!Flags.Z) { PC++; return 8; } PC = (ushort)(PC + (sbyte)MMU.Read8(PC)); return 12; }; // JR Z,r8 8 cycles if false
            opCodes[0x29] = () => { HL = Add16(HL, HL); return 8; }; // ADD HL,HL
            opCodes[0x2A] = () => { A = MMU.Read8(HL++); return 8; }; // LD A,(HL+)
            opCodes[0x2B] = () => { HL--;  return 8; }; // DEC HL
            opCodes[0x2C] = () => { L = Inc8(L); return 4; }; // INC L
            opCodes[0x2D] = () => { L = Dec8(L); return 4; }; // DEC L
            opCodes[0x2E] = () => { L = MMU.Read8(PC++); return 8; }; // LD L,d8
            opCodes[0x2F] = () => { A = (byte)(~A);  return 4; }; // CPL

            // 0x3x
            opCodes[0x30] = () => { if (Flags.C) { PC++; return 8; } PC = (ushort)(PC + (sbyte)MMU.Read8(PC)); return 12; }; // JR NC,r8 8 cycles if false
            opCodes[0x31] = () => { SP = MMU.Read16(PC); PC += 2; return 12; }; // LD SP,d16
            opCodes[0x32] = () => { MMU.Write8(HL--, A); return 8; }; // LD (HL-),A
            opCodes[0x33] = () => { SP++;  return 8; }; // INC SP
            opCodes[0x34] = () => { MMU.Write8(HL, Inc8(MMU.Read8(HL))); return 12; }; // INC (HL)
            opCodes[0x35] = () => { MMU.Write8(HL, Dec8(MMU.Read8(HL))); return 12; }; // DEC (HL)
            opCodes[0x36] = () => { MMU.Write8(HL, MMU.Read8(PC++)); return 12; }; // LD (HL),d8
            opCodes[0x37] = () => { Flags.N = false; Flags.H = false; Flags.C = true; return 4; }; // SCF
            opCodes[0x38] = () => { if (!Flags.C) { PC++; return 8; } PC = (ushort)(PC + (sbyte)MMU.Read8(PC)); return 12; }; // JR C,r8 8 cycles if false
            opCodes[0x39] = () => { HL = Add16(HL, SP); return 8; }; // ADD HL,SP
            opCodes[0x3A] = () => { A = MMU.Read8(HL--); return 8; }; // LD A,(HL-)
            opCodes[0x3B] = () => { SP--; return 8; }; // DEC SP
            opCodes[0x3C] = () => { A = Inc8(A); return 4; }; // INC A
            opCodes[0x3D] = () => { A = Dec8(A); return 4; }; // DEC A
            opCodes[0x3E] = () => { A = MMU.Read8(PC++); return 8; }; // LD A,d8
            opCodes[0x3F] = () => { Flags.N = false; Flags.H = false; Flags.C = !Flags.C; return 4; }; // CCF

            // 0x4x
            opCodes[0x40] = () => { return 4; }; // LD B,B
            opCodes[0x41] = () => { B = C; return 4; }; // LD B,C
            opCodes[0x42] = () => { B = D; return 4; }; // LD B,D
            opCodes[0x43] = () => { B = E; return 4; }; // LD B,E
            opCodes[0x44] = () => { B = H; return 4; }; // LD B,H
            opCodes[0x45] = () => { B = L; return 4; }; // LD B,L
            opCodes[0x46] = () => { B = MMU.Read8(HL); return 8; }; // LD B,(HL)
            opCodes[0x47] = () => { B = A; return 4; }; // LD B,A
            opCodes[0x48] = () => { C = B; return 4; }; // LD C,B
            opCodes[0x49] = () => { return 4; }; // LD C,C
            opCodes[0x4A] = () => { C = D; return 4; }; // LD C,D 
            opCodes[0x4B] = () => { C = E; return 4; }; // LD C,E
            opCodes[0x4C] = () => { C = H; return 4; }; // LD C,H
            opCodes[0x4D] = () => { C = L; return 4; }; // LD C,L
            opCodes[0x4E] = () => { C = MMU.Read8(HL); return 8; }; // LD C,(HL)
            opCodes[0x4F] = () => { C = A; return 4; }; // LD C,A

            // 0x5x
            opCodes[0x50] = () => { D = B; return 4; }; // LD D,B
            opCodes[0x51] = () => { D = C; return 4; }; // LD D,C
            opCodes[0x52] = () => { return 4; }; // LD D,D
            opCodes[0x53] = () => { D = E; return 4; }; // LD D,E
            opCodes[0x54] = () => { D = H; return 4; }; // LD D,H
            opCodes[0x55] = () => { D = L; return 4; }; // LD D,L
            opCodes[0x56] = () => { D = MMU.Read8(HL); return 8; }; // LD D,(HL)
            opCodes[0x57] = () => { D = A; return 4; }; // LD D,A
            opCodes[0x58] = () => { E = B; return 4; }; // LD E,B
            opCodes[0x59] = () => { E = C; return 4; }; // LD E,C
            opCodes[0x5A] = () => { E = D; return 4; }; // LD E,D 
            opCodes[0x5B] = () => { return 4; }; // LD E,E
            opCodes[0x5C] = () => { E = H; return 4; }; // LD E,H
            opCodes[0x5D] = () => { E = L; return 4; }; // LD E,L
            opCodes[0x5E] = () => { E = MMU.Read8(HL); return 8; }; // LD E,(HL)
            opCodes[0x5F] = () => { E = A; return 4; }; // LD E,A

            // 0x6x
            opCodes[0x60] = () => { H = B; return 4; }; // LD H,B
            opCodes[0x61] = () => { H = C; return 4; }; // LD H,C
            opCodes[0x62] = () => { H = D; return 4; }; // LD H,D
            opCodes[0x63] = () => { H = E; return 4; }; // LD H,E
            opCodes[0x64] = () => { return 4; }; // LD H,H
            opCodes[0x65] = () => { H = L; return 4; }; // LD H,L
            opCodes[0x66] = () => { H = MMU.Read8(HL); return 8; }; // LD H,(HL)
            opCodes[0x67] = () => { H = A; return 4; }; // LD H,A
            opCodes[0x68] = () => { L = B; return 4; }; // LD L,B
            opCodes[0x69] = () => { L = C; return 4; }; // LD L,C
            opCodes[0x6A] = () => { L = D; return 4; }; // LD L,D 
            opCodes[0x6B] = () => { L = E; return 4; }; // LD L,E
            opCodes[0x6C] = () => { L = H; return 4; }; // LD L,H
            opCodes[0x6D] = () => { return 4; }; // LD L,L
            opCodes[0x6E] = () => { L = MMU.Read8(HL); return 8; }; // LD L,(HL)
            opCodes[0x6F] = () => { L = A; return 4; }; // LD L,A

            // 0x7x
            opCodes[0x70] = () => { MMU.Write8(HL, B); return 8; }; // LD (HL),B
            opCodes[0x71] = () => { MMU.Write8(HL, C); return 8; }; // LD (HL),C
            opCodes[0x72] = () => { MMU.Write8(HL, D); return 8; }; // LD (HL),D
            opCodes[0x73] = () => { MMU.Write8(HL, E); return 8; }; // LD (HL),E
            opCodes[0x74] = () => { MMU.Write8(HL, H); return 8; }; // LD (HL),H
            opCodes[0x75] = () => { MMU.Write8(HL, L); return 8; }; // LD (HL),L
            opCodes[0x76] = () => { Halt = true; return 4; }; // HALT
            opCodes[0x77] = () => { MMU.Write8(HL, A); return 8; }; // LD (HL),A
            opCodes[0x78] = () => { A = B; return 4; }; // LD A,B
            opCodes[0x79] = () => { A = C; return 4; }; // LD A,C
            opCodes[0x7A] = () => { A = D; return 4; }; // LD A,D 
            opCodes[0x7B] = () => { A = E; return 4; }; // LD A,E
            opCodes[0x7C] = () => { A = H; return 4; }; // LD A,H
            opCodes[0x7D] = () => { A = L; return 4; }; // LD A,L
            opCodes[0x7E] = () => { A = MMU.Read8(HL); return 8; }; // LD A,(HL)
            opCodes[0x7F] = () => { return 4; }; // LD A,A

            // 0x8x
            opCodes[0x80] = () => { A = Add8(A, B); return 4; }; // ADD A,B
            opCodes[0x81] = () => { A = Add8(A, C); return 4; }; // ADD A,C
            opCodes[0x82] = () => { A = Add8(A, D); return 4; }; // ADD A,D
            opCodes[0x83] = () => { A = Add8(A, E); return 4; }; // ADD A,E
            opCodes[0x84] = () => { A = Add8(A, H); return 4; }; // ADD A,H
            opCodes[0x85] = () => { A = Add8(A, L); return 4; }; // ADD A,L
            opCodes[0x86] = () => { A = Add8(A, MMU.Read8(HL)); return 8; }; // ADD A,(HL)
            opCodes[0x87] = () => { A = Add8(A, A); return 4; }; // ADD A,A
            opCodes[0x88] = () => { A = Adc8(A, B); return 4; }; // ADC A,B
            opCodes[0x89] = () => { A = Adc8(A, C); return 4; }; // ADC A,C
            opCodes[0x8A] = () => { A = Adc8(A, D); return 4; }; // ADC A,D
            opCodes[0x8B] = () => { A = Adc8(A, E); return 4; }; // ADC A,E
            opCodes[0x8C] = () => { A = Adc8(A, H); return 4; }; // ADC A,H
            opCodes[0x8D] = () => { A = Adc8(A, L); return 4; }; // ADC A,L
            opCodes[0x8E] = () => { A = Adc8(A, MMU.Read8(HL)); return 8; }; // ADC A,(HL)
            opCodes[0x8F] = () => { A = Adc8(A, A); return 4; }; // ADC A,A

            // 0x9x
            opCodes[0x90] = () => { A = Sub8(A, B); return 4; }; // SUB B
            opCodes[0x91] = () => { A = Sub8(A, C); return 4; }; // SUB C
            opCodes[0x92] = () => { A = Sub8(A, D); return 4; }; // SUB D
            opCodes[0x93] = () => { A = Sub8(A, E); return 4; }; // SUB E
            opCodes[0x94] = () => { A = Sub8(A, H); return 4; }; // SUB H
            opCodes[0x95] = () => { A = Sub8(A, L); return 4; }; // SUB L
            opCodes[0x96] = () => { A = Sub8(A, MMU.Read8(HL)); return 8; }; // SUB (HL)
            opCodes[0x97] = () => { A = Sub8(A, A); return 4; }; // SUB A
            opCodes[0x98] = () => { A = Sbc8(A, B); return 4; }; // SBC A,B
            opCodes[0x99] = () => { A = Sbc8(A, C); return 4; }; // SBC A,C
            opCodes[0x9A] = () => { A = Sbc8(A, D); return 4; }; // SBC A,D
            opCodes[0x9B] = () => { A = Sbc8(A, E); return 4; }; // SBC A,E
            opCodes[0x9C] = () => { A = Sbc8(A, H); return 4; }; // SBC A,H
            opCodes[0x9D] = () => { A = Sbc8(A, L); return 4; }; // SBC A,L
            opCodes[0x9E] = () => { A = Sbc8(A, MMU.Read8(HL)); return 8; }; // SBC A,(HL)
            opCodes[0x9F] = () => { A = Sbc8(A, A); return 4; }; // SBC A,A

            // 0xAx
            opCodes[0xA0] = () => { A = And8(A, B); return 4; }; // AND B
            opCodes[0xA1] = () => { A = And8(A, C); return 4; }; // AND C
            opCodes[0xA2] = () => { A = And8(A, D); return 4; }; // AND D
            opCodes[0xA3] = () => { A = And8(A, E); return 4; }; // AND E
            opCodes[0xA4] = () => { A = And8(A, H); return 4; }; // AND H
            opCodes[0xA5] = () => { A = And8(A, L); return 4; }; // AND L
            opCodes[0xA6] = () => { A = And8(A, MMU.Read8(HL)); return 8; }; // AND (HL)
            opCodes[0xA7] = () => { A = And8(A, A); return 4; }; // AND A
            opCodes[0xA8] = () => { A = Xor8(A, B); return 4; }; // XOR B
            opCodes[0xA9] = () => { A = Xor8(A, C); return 4; }; // XOR C
            opCodes[0xAA] = () => { A = Xor8(A, D); return 4; }; // XOR D
            opCodes[0xAB] = () => { A = Xor8(A, E); return 4; }; // XOR E
            opCodes[0xAC] = () => { A = Xor8(A, H); return 4; }; // XOR H
            opCodes[0xAD] = () => { A = Xor8(A, L); return 4; }; // XOR L
            opCodes[0xAE] = () => { A = Xor8(A, MMU.Read8(HL)); return 8; }; // XOR (HL)
            opCodes[0xAF] = () => { A = Xor8(A, A); return 4; }; // XOR A

            // 0xBx
            opCodes[0xB0] = () => { A = Or8(A, B); return 4; }; // OR B
            opCodes[0xB1] = () => { A = Or8(A, C); return 4; }; // OR C
            opCodes[0xB2] = () => { A = Or8(A, D); return 4; }; // OR D
            opCodes[0xB3] = () => { A = Or8(A, E); return 4; }; // OR E
            opCodes[0xB4] = () => { A = Or8(A, H); return 4; }; // OR H
            opCodes[0xB5] = () => { A = Or8(A, L); return 4; }; // OR L
            opCodes[0xB6] = () => { A = Or8(A, MMU.Read8(HL)); return 8; }; // OR (HL)
            opCodes[0xB7] = () => { A = Or8(A, A); return 4; }; // OR A
            opCodes[0xB8] = () => { Sub8(A, B); return 4; }; // CP B
            opCodes[0xB9] = () => { Sub8(A, C); return 4; }; // CP C
            opCodes[0xBA] = () => { Sub8(A, D); return 4; }; // CP D
            opCodes[0xBB] = () => { Sub8(A, E); return 4; }; // CP E
            opCodes[0xBC] = () => { Sub8(A, H); return 4; }; // CP H
            opCodes[0xBD] = () => { Sub8(A, L); return 4; }; // CP L
            opCodes[0xBE] = () => { Sub8(A, MMU.Read8(HL)); return 8; }; // CP (HL)
            opCodes[0xBF] = () => { Sub8(A, A); return 4; }; // CP A

            // 0xCx
            opCodes[0xC0] = () => { if (Flags.Z) return 8; PC = MMU.Read16(SP); SP += 2; return 20; }; // RET NZ 8 cycles if false
            opCodes[0xC1] = () => { BC = MMU.Read16(SP); SP += 2; return 12; }; // POP BC
            opCodes[0xC2] = () => { if (Flags.Z) { PC += 2; return 12; } PC = MMU.Read16(PC); return 16; }; // JP NZ,a16 12 cycles if false
            opCodes[0xC3] = () => { PC = MMU.Read16(PC); return 16; }; // JP a16
            opCodes[0xC4] = () => { if (Flags.Z) { PC += 2; return 12; } SP -= 2; MMU.Write16(SP, PC); PC = MMU.Read16(PC); return 24; }; // CALL NZ,a16 12 cycles if false
            opCodes[0xC5] = () => { SP -= 2; MMU.Write16(SP, BC); return 16; }; // PUSH BC
            opCodes[0xC6] = () => { A = Add8(A, MMU.Read8(PC++)); return 8; }; // ADD A,d8
            opCodes[0xC7] = () => { SP -= 2; MMU.Write16(SP, PC); PC = 0x0000; return 16; }; // RST 0x00
            opCodes[0xC8] = () => { if (!Flags.Z) return 8; PC = MMU.Read16(SP); SP += 2; return 20; }; // RET Z 8 cycles if false
            opCodes[0xC9] = () => { PC = MMU.Read16(SP); SP += 2; return 16; }; // RET
            opCodes[0xCA] = () => { if (!Flags.Z) { PC += 2; return 12; } PC = MMU.Read16(PC); return 16; }; // JP Z,a16 12 cycles if false
            opCodes[0xCB] = () => { return cbCodes[MMU.Read8(PC++)](); }; // CB Prefix
            opCodes[0xCC] = () => { if (!Flags.Z) { PC += 2; return 12; } SP -= 2; MMU.Write16(SP, PC); PC = MMU.Read16(PC); return 24; }; // CALL Z,a16 12 cycles if false
            opCodes[0xCD] = () => { SP -= 2; MMU.Write16(SP, PC); PC = MMU.Read16(PC); return 24; }; // CALL a16
            opCodes[0xCE] = () => { A = Adc8(A, MMU.Read8(PC++)); return 8; }; // ADC A,d8
            opCodes[0xCF] = () => { SP -= 2; MMU.Write16(SP, PC); PC = 0x0008; return 16; }; // RST 0x08

            // 0xDx
            opCodes[0xD0] = () => { if (Flags.C) return 8; PC = MMU.Read16(SP); SP += 2; return 20; }; // RET NC 8 cycles if false
            opCodes[0xD1] = () => { DE = MMU.Read16(SP); SP += 2; return 12; }; // POP DE
            opCodes[0xD2] = () => { if (Flags.C) { PC += 2; return 12; } PC = MMU.Read16(PC); return 16; }; // JP NC,a16 12 cycles if false
            // Missing
            opCodes[0xD4] = () => { if (Flags.C) { PC += 2; return 12; } SP -= 2; MMU.Write16(SP, PC); PC = MMU.Read16(PC); return 24; }; // CALL NC,a16 12 cycles if false
            opCodes[0xD5] = () => { SP -= 2; MMU.Write16(SP, DE); return 16; }; // PUSH DE
            opCodes[0xD6] = () => { A = Sub8(A, MMU.Read8(PC++)); return 8; }; // SUB d8
            opCodes[0xD7] = () => { SP -= 2; MMU.Write16(SP, PC); PC = 0x0010; return 16; }; // RST 0x10
            opCodes[0xD8] = () => { if (!Flags.C) return 8; PC = MMU.Read16(SP); SP += 2; return 20; }; // RET C 8 cycles if false
            opCodes[0xD9] = () => { PC = MMU.Read16(SP); SP += 2; IME = true; return 16; }; // RETI
            opCodes[0xDA] = () => { if (!Flags.C) { PC += 2; return 12; } PC = MMU.Read16(PC); return 16; }; // JP C,a16 12 cycles if false
            // Missing
            opCodes[0xDC] = () => { if (!Flags.C) { PC += 2; return 12; } SP -= 2; MMU.Write16(SP, PC); PC = MMU.Read16(PC); return 24; }; // CALL C,a16 12 cycles if false
            // Missing
            opCodes[0xDE] = () => { A = Sbc8(A, MMU.Read8(PC++)); return 8; }; // SBC A,d8
            opCodes[0xDF] = () => { SP -= 2; MMU.Write16(SP, PC); PC = 0x0018; return 16; }; // RST 0x18

            // 0xEx
            opCodes[0xE0] = () => { MMU.Write8((ushort)(0xFF00 & MMU.Read8(PC++)), A); return 12; }; // LDH (a8),A
            opCodes[0xE1] = () => { HL = MMU.Read16(SP); SP += 2; return 12; }; // POP HL
            opCodes[0xE2] = () => { MMU.Write8((ushort)(0xFF00 & C), A); return 8; }; // LD (C),A
            // Missing
            // Missing
            opCodes[0xE5] = () => { SP -= 2; MMU.Write16(SP, HL); return 16; }; // PUSH HL
            opCodes[0xE6] = () => { A = And8(A, MMU.Read8(PC++)); return 8; }; // AND d8
            opCodes[0xE7] = () => { SP -= 2; MMU.Write16(SP, PC); PC = 0x0020; return 16; }; // RST 0x20
            opCodes[0xE8] = () => { SP = (ushort)(SP + (sbyte)(MMU.Read8(PC++))); return 16; }; // ADD SP,r8
            opCodes[0xE9] = () => { PC = HL; return 4; }; // JP (HL)
            opCodes[0xEA] = () => { MMU.Write8(MMU.Read16(PC), A); PC += 2; return 16; }; // LD (a16),A
            // Missing
            // Missing
            // Missing
            opCodes[0xEE] = () => { A = Xor8(A, MMU.Read8(PC++)); return 8; }; // XOR d8
            opCodes[0xEF] = () => { SP -= 2; MMU.Write16(SP, PC); PC = 0x0028; return 16; }; // RST 0x28

            // 0xFx
            opCodes[0xF0] = () => { A = MMU.Read8((ushort)(0xFF00 & MMU.Read8(PC++))); return 12; }; // LDH A,(a8)
            opCodes[0xF1] = () => { AF = MMU.Read16(SP); SP += 2; return 12; }; // POP AF
            opCodes[0xF2] = () => { A = MMU.Read8((ushort)(0xFF00 & C)); return 8; }; // LD A,(C)
            opCodes[0xF3] = () => { IME = false; return 4; }; // DI
            // Missing
            opCodes[0xF5] = () => { SP -= 2; MMU.Write16(SP, AF); return 16; }; // PUSH AF
            opCodes[0xF6] = () => { A = Or8(A, MMU.Read8(PC++)); return 8; }; // OR d8
            opCodes[0xF7] = () => { SP -= 2; MMU.Write16(SP, PC); PC = 0x0030; return 16; }; // RST 0x30
            opCodes[0xF8] = () => { HL = (ushort)(SP + (sbyte)(MMU.Read8(PC++))); return 12; }; // LD HL,SP+r8
            opCodes[0xF9] = () => { SP = HL; return 8; }; // LD SP,HL
            opCodes[0xFA] = () => { A = MMU.Read8(MMU.Read16(PC)); PC += 2; return 16; }; // LD A,(a16)
            opCodes[0xFB] = () => { IME = true; return 4; }; // EI
            // Missing
            // Missing 
            opCodes[0xFE] = () => { Sub8(A, MMU.Read8(PC++)); return 8; }; // CP d8
            opCodes[0xFF] = () => { SP -= 2; MMU.Write16(SP, PC); PC = 0x0038; return 16; }; // RST 0x38
        }

        private void InitialiseCBCodes()
        {
            cbCodes = new Func<int>[0xFF];

            // 0x0x
            cbCodes[0x00] = () => { B = Rlc8(B); return 8; }; // RLC B
            cbCodes[0x01] = () => { C = Rlc8(C); return 8; }; // RLC C
            cbCodes[0x02] = () => { D = Rlc8(D); return 8; }; // RLC D
            cbCodes[0x03] = () => { E = Rlc8(E); return 8; }; // RLC E
            cbCodes[0x04] = () => { H = Rlc8(H); return 8; }; // RLC H
            cbCodes[0x05] = () => { L = Rlc8(L); return 8; }; // RLC L
            cbCodes[0x06] = () => { MMU.Write16(HL, Rlc8(MMU.Read8(HL))); return 16; }; // RLC (HL)
            cbCodes[0x07] = () => { A = Rlc8(A); return 8; }; // RLC A
            cbCodes[0x08] = () => { B = Rrc8(B); return 8; }; // RRC B
            cbCodes[0x09] = () => { C = Rrc8(C); return 8; }; // RRC C
            cbCodes[0x0A] = () => { D = Rrc8(D); return 8; }; // RRC D
            cbCodes[0x0B] = () => { E = Rrc8(E); return 8; }; // RRC E
            cbCodes[0x0C] = () => { H = Rrc8(H); return 8; }; // RRC H
            cbCodes[0x0D] = () => { L = Rrc8(L); return 8; }; // RRC L
            cbCodes[0x0E] = () => { MMU.Write16(HL, Rrc8(MMU.Read8(HL))); return 16; }; // RRC (HL)
            cbCodes[0x0F] = () => { A = Rrc8(A); return 8; }; // RRC A

            // 0x1x
            cbCodes[0x10] = () => { B = Rl8(B); return 8; }; // RL B
            cbCodes[0x11] = () => { C = Rl8(C); return 8; }; // RL C
            cbCodes[0x12] = () => { D = Rl8(D); return 8; }; // RL D
            cbCodes[0x13] = () => { E = Rl8(E); return 8; }; // RL E
            cbCodes[0x14] = () => { H = Rl8(H); return 8; }; // RL H
            cbCodes[0x15] = () => { L = Rl8(L); return 8; }; // RL L
            cbCodes[0x16] = () => { MMU.Write16(HL, Rl8(MMU.Read8(HL))); return 16; }; // RL (HL)
            cbCodes[0x17] = () => { A = Rl8(A); return 8; }; // RL A
            cbCodes[0x18] = () => { B = Rr8(B); return 8; }; // RR B
            cbCodes[0x19] = () => { C = Rr8(C); return 8; }; // RR C
            cbCodes[0x1A] = () => { D = Rr8(D); return 8; }; // RR D
            cbCodes[0x1B] = () => { E = Rr8(E); return 8; }; // RR E
            cbCodes[0x1C] = () => { H = Rr8(H); return 8; }; // RR H
            cbCodes[0x1D] = () => { L = Rr8(L); return 8; }; // RR L
            cbCodes[0x1E] = () => { MMU.Write16(HL, Rr8(MMU.Read8(HL))); return 16; }; // RR (HL)
            cbCodes[0x1F] = () => { A = Rr8(A); return 8; }; // RR A

            // 0x2x
            cbCodes[0x20] = () => { B = Sla8(B); return 8; }; // SLA B
            cbCodes[0x21] = () => { C = Sla8(C); return 8; }; // SLA C
            cbCodes[0x22] = () => { D = Sla8(D); return 8; }; // SLA D
            cbCodes[0x23] = () => { E = Sla8(E); return 8; }; // SLA E
            cbCodes[0x24] = () => { H = Sla8(H); return 8; }; // SLA H
            cbCodes[0x25] = () => { L = Sla8(L); return 8; }; // SLA L
            cbCodes[0x26] = () => { MMU.Write16(HL, Sla8(MMU.Read8(HL))); return 16; }; // SLA (HL)
            cbCodes[0x27] = () => { A = Sla8(A); return 8; }; // SLA A
            cbCodes[0x28] = () => { B = Sra8(B); return 8; }; // SRA B
            cbCodes[0x29] = () => { C = Sra8(C); return 8; }; // SRA C
            cbCodes[0x2A] = () => { D = Sra8(D); return 8; }; // SRA D
            cbCodes[0x2B] = () => { E = Sra8(E); return 8; }; // SRA E
            cbCodes[0x2C] = () => { H = Sra8(H); return 8; }; // SRA H
            cbCodes[0x2D] = () => { L = Sra8(L); return 8; }; // SRA L
            cbCodes[0x2E] = () => { MMU.Write16(HL, Sra8(MMU.Read8(HL))); return 16; }; // SRA (HL)
            cbCodes[0x2F] = () => { A = Sra8(A); return 8; }; // SRA A

            // 0x3x
            cbCodes[0x30] = () => { B = Swap8(B); return 8; }; // SWAP B
            cbCodes[0x31] = () => { C = Swap8(C); return 8; }; // SWAP C
            cbCodes[0x32] = () => { D = Swap8(D); return 8; }; // SWAP D
            cbCodes[0x33] = () => { E = Swap8(E); return 8; }; // SWAP E
            cbCodes[0x34] = () => { H = Swap8(H); return 8; }; // SWAP H
            cbCodes[0x35] = () => { L = Swap8(L); return 8; }; // SWAP L
            cbCodes[0x36] = () => { MMU.Write16(HL, Swap8(MMU.Read8(HL))); return 16; }; // SWAP (HL)
            cbCodes[0x37] = () => { A = Swap8(A); return 8; }; // SWAP A
            cbCodes[0x38] = () => { B = Srl8(B); return 8; }; // SRL B
            cbCodes[0x39] = () => { C = Srl8(C); return 8; }; // SRL C
            cbCodes[0x3A] = () => { D = Srl8(D); return 8; }; // SRL D
            cbCodes[0x3B] = () => { E = Srl8(E); return 8; }; // SRL E
            cbCodes[0x3C] = () => { H = Srl8(H); return 8; }; // SRL H
            cbCodes[0x3D] = () => { L = Srl8(L); return 8; }; // SRL L
            cbCodes[0x3E] = () => { MMU.Write16(HL, Srl8(MMU.Read8(HL))); return 16; }; // SRL (HL)
            cbCodes[0x3F] = () => { A = Srl8(A); return 8; }; // SRL A

            // 0x4x
            cbCodes[0x40] = () => { Bit8(B, 0); return 8; }; // BIT 0,B
            cbCodes[0x41] = () => { Bit8(C, 0); return 8; }; // BIT 0,C
            cbCodes[0x42] = () => { Bit8(D, 0); return 8; }; // BIT 0,D
            cbCodes[0x43] = () => { Bit8(E, 0); return 8; }; // BIT 0,E
            cbCodes[0x44] = () => { Bit8(H, 0); return 8; }; // BIT 0,H
            cbCodes[0x45] = () => { Bit8(L, 0); return 8; }; // BIT 0,L
            cbCodes[0x46] = () => { Bit8(MMU.Read8(HL), 0); return 16; }; // BIT 0,(HL)
            cbCodes[0x47] = () => { Bit8(A, 0); return 8; }; // BIT 0,A
            cbCodes[0x48] = () => { Bit8(B, 1); return 8; }; // BIT 1,B
            cbCodes[0x49] = () => { Bit8(C, 1); return 8; }; // BIT 1,C
            cbCodes[0x4A] = () => { Bit8(D, 1); return 8; }; // BIT 1,D
            cbCodes[0x4B] = () => { Bit8(E, 1); return 8; }; // BIT 1,E
            cbCodes[0x4C] = () => { Bit8(H, 1); return 8; }; // BIT 1,H
            cbCodes[0x4D] = () => { Bit8(L, 1); return 8; }; // BIT 1,L
            cbCodes[0x4E] = () => { Bit8(MMU.Read8(HL), 1); return 16; }; // BIT 1,(HL)
            cbCodes[0x4F] = () => { Bit8(A, 1); return 8; }; // BIT 1,A

            // 0x5x
            cbCodes[0x50] = () => { Bit8(B, 2); return 8; }; // BIT 2,B
            cbCodes[0x51] = () => { Bit8(C, 2); return 8; }; // BIT 2,C
            cbCodes[0x52] = () => { Bit8(D, 2); return 8; }; // BIT 2,D
            cbCodes[0x53] = () => { Bit8(E, 2); return 8; }; // BIT 2,E
            cbCodes[0x54] = () => { Bit8(H, 2); return 8; }; // BIT 2,H
            cbCodes[0x55] = () => { Bit8(L, 2); return 8; }; // BIT 2,L
            cbCodes[0x56] = () => { Bit8(MMU.Read8(HL), 2);  return 16; }; // BIT 2,(HL)
            cbCodes[0x57] = () => { Bit8(A, 2); return 8; }; // BIT 2,A
            cbCodes[0x58] = () => { Bit8(B, 3); return 8; }; // BIT 3,B
            cbCodes[0x59] = () => { Bit8(C, 3); return 8; }; // BIT 3,C
            cbCodes[0x5A] = () => { Bit8(D, 3); return 8; }; // BIT 3,D
            cbCodes[0x5B] = () => { Bit8(E, 3); return 8; }; // BIT 3,E
            cbCodes[0x5C] = () => { Bit8(H, 3); return 8; }; // BIT 3,H
            cbCodes[0x5D] = () => { Bit8(L, 3); return 8; }; // BIT 3,L
            cbCodes[0x5E] = () => { Bit8(MMU.Read8(HL), 3);  return 16; }; // BIT 3,(HL)
            cbCodes[0x5F] = () => { Bit8(A, 3); return 8; }; // BIT 3,A

            // 0x6x
            cbCodes[0x60] = () => { Bit8(B, 4); return 8; }; // BIT 4,B
            cbCodes[0x61] = () => { Bit8(C, 4); return 8; }; // BIT 4,C
            cbCodes[0x62] = () => { Bit8(D, 4); return 8; }; // BIT 4,D
            cbCodes[0x63] = () => { Bit8(E, 4); return 8; }; // BIT 4,E
            cbCodes[0x64] = () => { Bit8(H, 4); return 8; }; // BIT 4,H
            cbCodes[0x65] = () => { Bit8(L, 4); return 8; }; // BIT 4,L
            cbCodes[0x66] = () => { Bit8(MMU.Read8(HL), 4);  return 16; }; // BIT 4,(HL)
            cbCodes[0x67] = () => { Bit8(A, 4); return 8; }; // BIT 4,A
            cbCodes[0x68] = () => { Bit8(B, 5); return 8; }; // BIT 5,B
            cbCodes[0x69] = () => { Bit8(C, 5); return 8; }; // BIT 5,C
            cbCodes[0x6A] = () => { Bit8(D, 5); return 8; }; // BIT 5,D
            cbCodes[0x6B] = () => { Bit8(E, 5); return 8; }; // BIT 5,E
            cbCodes[0x6C] = () => { Bit8(H, 5); return 8; }; // BIT 5,H
            cbCodes[0x6D] = () => { Bit8(L, 5); return 8; }; // BIT 5,L
            cbCodes[0x6E] = () => { Bit8(MMU.Read8(HL), 5);  return 16; }; // BIT 5,(HL)
            cbCodes[0x6F] = () => { Bit8(A, 5); return 8; }; // BIT 5,A

            // 0x7x
            cbCodes[0x70] = () => { Bit8(B, 6); return 8; }; // BIT 6,B
            cbCodes[0x71] = () => { Bit8(C, 6); return 8; }; // BIT 6,C
            cbCodes[0x72] = () => { Bit8(D, 6); return 8; }; // BIT 6,D
            cbCodes[0x73] = () => { Bit8(E, 6); return 8; }; // BIT 6,E
            cbCodes[0x74] = () => { Bit8(H, 6); return 8; }; // BIT 6,H
            cbCodes[0x75] = () => { Bit8(L, 6); return 8; }; // BIT 6,L
            cbCodes[0x76] = () => { Bit8(MMU.Read8(HL), 6);  return 16; }; // BIT 6,(HL)
            cbCodes[0x77] = () => { Bit8(A, 6); return 8; }; // BIT 6,A
            cbCodes[0x78] = () => { Bit8(B, 7); return 8; }; // BIT 7,B
            cbCodes[0x79] = () => { Bit8(C, 7); return 8; }; // BIT 7,C
            cbCodes[0x7A] = () => { Bit8(D, 7); return 8; }; // BIT 7,D
            cbCodes[0x7B] = () => { Bit8(E, 7); return 8; }; // BIT 7,E
            cbCodes[0x7C] = () => { Bit8(H, 7); return 8; }; // BIT 7,H
            cbCodes[0x7D] = () => { Bit8(L, 7); return 8; }; // BIT 7,L
            cbCodes[0x7E] = () => { Bit8(MMU.Read8(HL), 7);  return 16; }; // BIT 7,(HL)
            cbCodes[0x7F] = () => { Bit8(A, 7); return 8; }; // BIT 7,A

            // 0x8x
            cbCodes[0x80] = () => { B = Res8(B, 0); return 8; }; // RES 0,B
            cbCodes[0x81] = () => { C = Res8(C, 0); return 8; }; // RES 0,C
            cbCodes[0x82] = () => { D = Res8(D, 0); return 8; }; // RES 0,D
            cbCodes[0x83] = () => { E = Res8(E, 0); return 8; }; // RES 0,E
            cbCodes[0x84] = () => { H = Res8(H, 0); return 8; }; // RES 0,H
            cbCodes[0x85] = () => { L = Res8(L, 0); return 8; }; // RES 0,L
            cbCodes[0x86] = () => { MMU.Write8(HL, Res8(MMU.Read8(HL), 0)); return 16; }; // RES 0,(HL)
            cbCodes[0x87] = () => { A = Res8(A, 0); return 8; }; // RES 0,A
            cbCodes[0x88] = () => { B = Res8(B, 1); return 8; }; // RES 1,B
            cbCodes[0x89] = () => { C = Res8(C, 1); return 8; }; // RES 1,C
            cbCodes[0x8A] = () => { D = Res8(D, 1); return 8; }; // RES 1,D
            cbCodes[0x8B] = () => { E = Res8(E, 1); return 8; }; // RES 1,E
            cbCodes[0x8C] = () => { H = Res8(H, 1); return 8; }; // RES 1,H
            cbCodes[0x8D] = () => { L = Res8(L, 1); return 8; }; // RES 1,L
            cbCodes[0x8E] = () => { MMU.Write8(HL, Res8(MMU.Read8(HL), 1)); return 16; }; // RES 1,(HL)
            cbCodes[0x8F] = () => { A = Res8(A, 1); return 8; }; // RES 1,A

            // 0x9x
            cbCodes[0x90] = () => { B = Res8(B, 2); return 8; }; // RES 2,B
            cbCodes[0x91] = () => { C = Res8(C, 2); return 8; }; // RES 2,C
            cbCodes[0x92] = () => { D = Res8(D, 2); return 8; }; // RES 2,D
            cbCodes[0x93] = () => { E = Res8(E, 2); return 8; }; // RES 2,E
            cbCodes[0x94] = () => { H = Res8(H, 2); return 8; }; // RES 2,H
            cbCodes[0x95] = () => { L = Res8(L, 2); return 8; }; // RES 2,L
            cbCodes[0x96] = () => { MMU.Write8(HL, Res8(MMU.Read8(HL), 2)); return 16; }; // RES 2,(HL)
            cbCodes[0x97] = () => { A = Res8(A, 2); return 8; }; // RES 2,A
            cbCodes[0x98] = () => { B = Res8(B, 3); return 8; }; // RES 3,B
            cbCodes[0x99] = () => { C = Res8(C, 3); return 8; }; // RES 3,C
            cbCodes[0x9A] = () => { D = Res8(D, 3); return 8; }; // RES 3,D
            cbCodes[0x9B] = () => { E = Res8(E, 3); return 8; }; // RES 3,E
            cbCodes[0x9C] = () => { H = Res8(H, 3); return 8; }; // RES 3,H
            cbCodes[0x9D] = () => { L = Res8(L, 3); return 8; }; // RES 3,L
            cbCodes[0x9E] = () => { MMU.Write8(HL, Res8(MMU.Read8(HL), 3)); return 16; }; // RES 3,(HL)
            cbCodes[0x9F] = () => { A = Res8(A, 3); return 8; }; // RES 3,A

            // 0xAx
            cbCodes[0xA0] = () => { B = Res8(B, 4); return 8; }; // RES 4,B
            cbCodes[0xA1] = () => { C = Res8(C, 4); return 8; }; // RES 4,C
            cbCodes[0xA2] = () => { D = Res8(D, 4); return 8; }; // RES 4,D
            cbCodes[0xA3] = () => { E = Res8(E, 4); return 8; }; // RES 4,E
            cbCodes[0xA4] = () => { H = Res8(H, 4); return 8; }; // RES 4,H
            cbCodes[0xA5] = () => { L = Res8(L, 4); return 8; }; // RES 4,L
            cbCodes[0xA6] = () => { MMU.Write8(HL, Res8(MMU.Read8(HL), 4)); return 16; }; // RES 4,(HL)
            cbCodes[0xA7] = () => { A = Res8(A, 4); return 8; }; // RES 4,A
            cbCodes[0xA8] = () => { B = Res8(B, 5); return 8; }; // RES 5,B
            cbCodes[0xA9] = () => { C = Res8(C, 5); return 8; }; // RES 5,C
            cbCodes[0xAA] = () => { D = Res8(D, 5); return 8; }; // RES 5,D
            cbCodes[0xAB] = () => { E = Res8(E, 5); return 8; }; // RES 5,E
            cbCodes[0xAC] = () => { H = Res8(H, 5); return 8; }; // RES 5,H
            cbCodes[0xAD] = () => { L = Res8(L, 5); return 8; }; // RES 5,L
            cbCodes[0xAE] = () => { MMU.Write8(HL, Res8(MMU.Read8(HL), 5)); return 16; }; // RES 5,(HL)
            cbCodes[0xAF] = () => { A = Res8(A, 5); return 8; }; // RES 5,A

            // 0xBx
            cbCodes[0xB0] = () => { B = Res8(B, 6); return 8; }; // RES 6,B
            cbCodes[0xB1] = () => { C = Res8(C, 6); return 8; }; // RES 6,C
            cbCodes[0xB2] = () => { D = Res8(D, 6); return 8; }; // RES 6,D
            cbCodes[0xB3] = () => { E = Res8(E, 6); return 8; }; // RES 6,E
            cbCodes[0xB4] = () => { H = Res8(H, 6); return 8; }; // RES 6,H
            cbCodes[0xB5] = () => { L = Res8(L, 6); return 8; }; // RES 6,L
            cbCodes[0xB6] = () => { MMU.Write8(HL, Res8(MMU.Read8(HL), 6)); return 16; }; // RES 6,(HL)
            cbCodes[0xB7] = () => { A = Res8(A, 6); return 8; }; // RES 6,A
            cbCodes[0xB8] = () => { B = Res8(B, 7); return 8; }; // RES 7,B
            cbCodes[0xB9] = () => { C = Res8(C, 7); return 8; }; // RES 7,C
            cbCodes[0xBA] = () => { D = Res8(D, 7); return 8; }; // RES 7,D
            cbCodes[0xBB] = () => { E = Res8(E, 7); return 8; }; // RES 7,E
            cbCodes[0xBC] = () => { H = Res8(H, 7); return 8; }; // RES 7,H
            cbCodes[0xBD] = () => { L = Res8(L, 7); return 8; }; // RES 7,L
            cbCodes[0xBE] = () => { MMU.Write8(HL, Res8(MMU.Read8(HL), 7)); return 16; }; // RES 7,(HL)
            cbCodes[0xBF] = () => { A = Res8(A, 7); return 8; }; // RES 7,A

            // 0xCx
            cbCodes[0xC0] = () => { B = Set8(B, 0); return 8; }; // SET 0,B
            cbCodes[0xC1] = () => { C = Set8(C, 0); return 8; }; // SET 0,C
            cbCodes[0xC2] = () => { D = Set8(D, 0); return 8; }; // SET 0,D
            cbCodes[0xC3] = () => { E = Set8(E, 0); return 8; }; // SET 0,E
            cbCodes[0xC4] = () => { H = Set8(H, 0); return 8; }; // SET 0,H
            cbCodes[0xC5] = () => { L = Set8(L, 0); return 8; }; // SET 0,L
            cbCodes[0xC6] = () => { MMU.Write8(HL, Set8(MMU.Read8(HL), 0)); return 16; }; // SET 0,(HL)
            cbCodes[0xC7] = () => { A = Set8(A, 0); return 8; }; // SET 0,A
            cbCodes[0xC8] = () => { B = Set8(B, 1); return 8; }; // SET 1,B
            cbCodes[0xC9] = () => { C = Set8(C, 1); return 8; }; // SET 1,C
            cbCodes[0xCA] = () => { D = Set8(D, 1); return 8; }; // SET 1,D
            cbCodes[0xCB] = () => { E = Set8(E, 1); return 8; }; // SET 1,E
            cbCodes[0xCC] = () => { H = Set8(H, 1); return 8; }; // SET 1,H
            cbCodes[0xCD] = () => { L = Set8(L, 1); return 8; }; // SET 1,L
            cbCodes[0xCE] = () => { MMU.Write8(HL, Set8(MMU.Read8(HL), 1)); return 16; }; // SET 1,(HL)
            cbCodes[0xCF] = () => { A = Set8(A, 1); return 8; }; // SET 1,A

            // 0xDx
            cbCodes[0xD0] = () => { B = Set8(B, 2); return 8; }; // SET 2,B
            cbCodes[0xD1] = () => { C = Set8(C, 2); return 8; }; // SET 2,C
            cbCodes[0xD2] = () => { D = Set8(D, 2); return 8; }; // SET 2,D
            cbCodes[0xD3] = () => { E = Set8(E, 2); return 8; }; // SET 2,E
            cbCodes[0xD4] = () => { H = Set8(H, 2); return 8; }; // SET 2,H
            cbCodes[0xD5] = () => { L = Set8(L, 2); return 8; }; // SET 2,L
            cbCodes[0xD6] = () => { MMU.Write8(HL, Set8(MMU.Read8(HL), 2)); return 16; }; // SET 2,(HL)
            cbCodes[0xD7] = () => { A = Set8(A, 2); return 8; }; // SET 2,A
            cbCodes[0xD8] = () => { B = Set8(B, 3); return 8; }; // SET 3,B
            cbCodes[0xD9] = () => { C = Set8(C, 3); return 8; }; // SET 3,C
            cbCodes[0xDA] = () => { D = Set8(D, 3); return 8; }; // SET 3,D
            cbCodes[0xDB] = () => { E = Set8(E, 3); return 8; }; // SET 3,E
            cbCodes[0xDC] = () => { H = Set8(H, 3); return 8; }; // SET 3,H
            cbCodes[0xDD] = () => { L = Set8(L, 3); return 8; }; // SET 3,L
            cbCodes[0xDE] = () => { MMU.Write8(HL, Set8(MMU.Read8(HL), 3)); return 16; }; // SET 3,(HL)
            cbCodes[0xDF] = () => { A = Set8(A, 3); return 8; }; // SET 3,A

            // 0xEx
            cbCodes[0xE0] = () => { B = Set8(B, 4); return 8; }; // SET 4,B
            cbCodes[0xE1] = () => { C = Set8(C, 4); return 8; }; // SET 4,C
            cbCodes[0xE2] = () => { D = Set8(D, 4); return 8; }; // SET 4,D
            cbCodes[0xE3] = () => { E = Set8(E, 4); return 8; }; // SET 4,E
            cbCodes[0xE4] = () => { H = Set8(H, 4); return 8; }; // SET 4,H
            cbCodes[0xE5] = () => { L = Set8(L, 4); return 8; }; // SET 4,L
            cbCodes[0xE6] = () => { MMU.Write8(HL, Set8(MMU.Read8(HL), 4)); return 16; }; // SET 4,(HL)
            cbCodes[0xE7] = () => { A = Set8(A, 4); return 8; }; // SET 4,A
            cbCodes[0xE8] = () => { B = Set8(B, 5); return 8; }; // SET 5,B
            cbCodes[0xE9] = () => { C = Set8(C, 5); return 8; }; // SET 5,C
            cbCodes[0xEA] = () => { D = Set8(D, 5); return 8; }; // SET 5,D
            cbCodes[0xEB] = () => { E = Set8(E, 5); return 8; }; // SET 5,E
            cbCodes[0xEC] = () => { H = Set8(H, 5); return 8; }; // SET 5,H
            cbCodes[0xED] = () => { L = Set8(L, 5); return 8; }; // SET 5,L
            cbCodes[0xEE] = () => { MMU.Write8(HL, Set8(MMU.Read8(HL), 5)); return 16; }; // SET 5,(HL)
            cbCodes[0xEF] = () => { A = Set8(A, 5); return 8; }; // SET 5,A

            // 0xFx
            cbCodes[0xF0] = () => { B = Set8(B, 6); return 8; }; // SET 6,B
            cbCodes[0xF1] = () => { C = Set8(C, 6); return 8; }; // SET 6,C
            cbCodes[0xF2] = () => { D = Set8(D, 6); return 8; }; // SET 6,D
            cbCodes[0xF3] = () => { E = Set8(E, 6); return 8; }; // SET 6,E
            cbCodes[0xF4] = () => { H = Set8(H, 6); return 8; }; // SET 6,H
            cbCodes[0xF5] = () => { L = Set8(L, 6); return 8; }; // SET 6,L
            cbCodes[0xF6] = () => { MMU.Write8(HL, Set8(MMU.Read8(HL), 6)); return 16; }; // SET 6,(HL)
            cbCodes[0xF7] = () => { A = Set8(A, 6); return 8; }; // SET 6,A
            cbCodes[0xF8] = () => { B = Set8(B, 7); return 8; }; // SET 7,B
            cbCodes[0xF9] = () => { C = Set8(C, 7); return 8; }; // SET 7,C
            cbCodes[0xFA] = () => { D = Set8(D, 7); return 8; }; // SET 7,D
            cbCodes[0xFB] = () => { E = Set8(E, 7); return 8; }; // SET 7,E
            cbCodes[0xFC] = () => { H = Set8(H, 7); return 8; }; // SET 7,H
            cbCodes[0xFD] = () => { L = Set8(L, 7); return 8; }; // SET 7,L
            cbCodes[0xFE] = () => { MMU.Write8(HL, Set8(MMU.Read8(HL), 7)); return 16; }; // SET 7,(HL)
            cbCodes[0xFF] = () => { A = Set8(A, 7); return 8; }; // SET 7,A
        }

        #endregion
        
        private byte Add8(byte op1, byte op2)
        {
            byte result = (byte)(op1 + op2);

            Flags.C = ((op1 + op2) & 0x100) != 0;
            Flags.H = (((op1 & 0xF) + (op2 & 0xF)) & 0x10) != 0;
            Flags.N = false;
            Flags.Z = (result == 0);

            return result;
        }

        private ushort Add16(ushort op1, ushort op2)
        {
            ushort result = (ushort)(op1 + op2);

            Flags.C = ((op1 + op2) & 0x10000) != 0;
            Flags.H = ((((op1 >> 8) & 0xF) + ((op2 >> 8) & 0xF)) & 0x10) != 0;
            Flags.N = false;

            return result;
        }
        private byte Adc8(byte op1, byte op2)
        {
            var carry = (Flags.C ? 1 : 0);
            byte result = (byte)(op1 + op2 + carry);

            Flags.C = ((op1 + op2 + carry) & 0x100) != 0;
            Flags.H = (((op1 & 0xF) + (op2 & 0xF) + carry) & 0x10) != 0;
            Flags.N = false;
            Flags.Z = (result == 0);

            return result;
        }

        private byte Sub8(byte op1, byte op2)
        {
            byte result = (byte)(op1 - op2);

            Flags.C = (op1 - op2) < 0;
            Flags.H = ((op1 >> 4) - (op2 >> 4)) < 0;
            Flags.N = true;
            Flags.Z = (result == 0);

            return result;
        }
        private byte Sbc8(byte op1, byte op2)
        {
            var carry = (Flags.C ? 1 : 0);
            byte result = (byte)(op1 - op2 - carry);

            Flags.C = (op1 - op2 - carry) < 0;
            Flags.H = ((op1 >> 4) - ((op2 + carry) >> 4)) < 0;
            Flags.N = true;
            Flags.Z = (result == 0);

            return result;
        }

        private byte Inc8(byte num)
        {
            byte result = num++;

            Flags.H = (((num & 0xF) + 1) & 0x10) != 0;
            Flags.N = false;
            Flags.Z = (result == 0);

            return result;
        }

        private byte Dec8(byte num)
        {
            byte result = num--;

            Flags.H = (num == 0x10);
            Flags.N = true;
            Flags.Z = (result == 0);

            return result;
        }

        private byte And8(byte op1, byte op2)
        {
            byte result = (byte)(op1 & op2);

            Flags.C = false;
            Flags.H = true;
            Flags.N = false;
            Flags.Z = (result == 0);

            return result;
        }
        private byte Xor8(byte op1, byte op2)
        {
            byte result = (byte)(op1 ^ op2);

            Flags.C = false;
            Flags.H = false;
            Flags.N = false;
            Flags.Z = (result == 0);

            return result;
        }

        private byte Or8(byte op1, byte op2)
        {
            byte result = (byte)(op1 | op2);

            Flags.C = false;
            Flags.H = false;
            Flags.N = false;
            Flags.Z = (result == 0);

            return result;
        }
        
        private byte Daa(byte num)
        {
            byte result;
            byte offset = 0;

            if ((Flags.H) || ((num & 0xF) > 0x9))
            {
                offset |= 0x6;
            }

            if ((Flags.C) || ((num & 0xF) > 0x9))
            {
                offset |= 0x60;
            }

            result = (byte)(num + offset);

            Flags.C = (offset & 0x60) != 0;
            Flags.H = false;
            Flags.Z = (result == 0);

            return result;
        }

        private byte Rlc8(byte num)
        {
            var shift = num << 1;
            var carry = num >> 7;
            byte result = (byte)(shift | carry);

            Flags.C = (carry != 0);
            Flags.H = false;
            Flags.N = false;
            Flags.Z = (result == 0);

            return result;
        }

        private byte Rl8(byte num)
        {
            var shift = num << 1;
            var carry = num >> 7;
            byte result = (byte)(shift | (Flags.C ? 0x1 : 0));

            Flags.C = (carry != 0);
            Flags.H = false;
            Flags.N = false;
            Flags.Z = (result == 0);

            return result;
        }

        private byte Rrc8(byte num)
        {
            var shift = num >> 1;
            var carry = num & 1;
            byte result = (byte)(shift | (carry << 7));

            Flags.C = (carry != 0);
            Flags.H = false;
            Flags.N = false;
            Flags.Z = (result == 0);

            return result;
        }

        private byte Rr8(byte num)
        {
            var shift = num >> 1;
            var carry = num & 1;
            byte result = (byte)(shift | (Flags.C ? 0x80 : 0));

            Flags.C = (carry != 0);
            Flags.H = false;
            Flags.N = false;
            Flags.Z = (result == 0);

            return result;
        }

        private byte Sla8(byte num)
        {
            var shift = num << 1;
            var carry = num >> 7;
            byte result = (byte)(shift);

            Flags.C = (carry != 0);
            Flags.H = false;
            Flags.N = false;
            Flags.Z = (result == 0);

            return result;
        }

        private byte Sra8(byte num)
        {
            var shift = num >> 1;
            var carry = num & 1;
            byte result = (byte)(shift | (num & 0x80));

            Flags.C = (carry != 0);
            Flags.H = false;
            Flags.N = false;
            Flags.Z = (result == 0);

            return result;
        }

        private byte Srl8(byte num)
        {
            var shift = num >> 1;
            var carry = num & 1;
            byte result = (byte)(shift);

            Flags.C = (carry != 0);
            Flags.H = false;
            Flags.N = false;
            Flags.Z = (result == 0);

            return result;
        }

        private byte Swap8(byte num)
        {
            return (byte)(((num & 0x0F) << 4) | ((num & 0xF0) >> 4));
        }

        private void Bit8(byte num, byte bit)
        {
            var result = (num >> bit) & 0x1;

            Flags.H = true;
            Flags.N = false;
            Flags.Z = result == 0;
        }

        private byte Set8(byte num, byte bit)
        {
            var result = num | (1 << bit);

            return (byte)result;
        }
        private byte Res8(byte num, byte bit)
        {
            var result = num & (~(1 << bit));

            return (byte)result;
        }

        private void CallISR(byte addr)
        {
            // dealing with an Interrupt resumes a stopped / halted processor
            Halt = false;
            Stop = false;

            IME = false;
            MMU.Clock(8);

            SP -= 2;
            MMU.Write16(SP, PC);
            MMU.Clock(8);

            PC = addr;
            MMU.Clock(4);
        }

    }
}

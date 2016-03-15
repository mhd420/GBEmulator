using GBEmulator.Emulator;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GBEmulator
{
    public class Instruction
    {
        public string Name;
        public int Length;

        public Instruction(string name, int length)
        {
            this.Name = name;
            this.Length = length;
        }

        public override string ToString()
        {
            return Name;
        }
    }

    public class DecompiledInstruction
    {
        public ushort Address { get; set; }
        public byte Data { get; set; }
        public Instruction Instruction { get; set; }

        public DecompiledInstruction(ushort addr, byte data, Instruction instructon = null)
        {
            this.Address = addr;
            this.Data = data;
            this.Instruction = instructon;
        }
    }

    public class Debugger
    {
        public GameboyMMU MMU;
        private Dictionary<byte, Instruction> instructionLookup;

        public Debugger()
        {
            InitialiseLookup();
        }

        public List<DecompiledInstruction> Update(ushort pc)
        {
            var result = new List<DecompiledInstruction>(20);
            var extraBytes = 0;

            for (var addr = pc; addr <= pc + 20; addr++)
            {
                var data = MMU.Read8(addr);
                if (extraBytes == 0)
                {
                    var foundOpcode = instructionLookup[data];
                    extraBytes = foundOpcode.Length - 1;
                    result.Add(new DecompiledInstruction(addr, data, foundOpcode));
                }
                else
                {
                    result.Add(new DecompiledInstruction(addr, data));
                    extraBytes--;
                }
            }

            return result;
        }

        private void InitialiseLookup()
        {
            instructionLookup = new Dictionary<byte, Instruction>
            {
                {0x00, new Instruction("NOP", 1)},
                {0x01, new Instruction("LD BC,d16", 3)},
                {0x02, new Instruction("LD (BC),A", 1)},
                {0x03, new Instruction("INC BC", 1)},
                {0x04, new Instruction("INC B", 1)},
                {0x05, new Instruction("DEC B", 1)},
                {0x06, new Instruction("LD B,d8", 2)},
                {0x07, new Instruction("RCLA", 1)},
                {0x08, new Instruction("LD (a16),SP", 3)},
                {0x09, new Instruction("ADD HL,BC", 1)},
                {0x0A, new Instruction("LD A,(BC)", 1)},
                {0x0B, new Instruction("DEC BC", 1)},
                {0x0C, new Instruction("INC C", 1)},
                {0x0D, new Instruction("DEC C", 1)},
                {0x0E, new Instruction("LD C,d8", 2)},
                {0x0F, new Instruction("RRCA", 1)},

                {0x10, new Instruction("STOP", 2)},
                {0x11, new Instruction("LD DE,d16", 3)},
                {0x12, new Instruction("LD (DE),A", 1)},
                {0x13, new Instruction("INC DE", 1)},
                {0x14, new Instruction("INC D", 1)},
                {0x15, new Instruction("DEC D", 1)},
                {0x16, new Instruction("LD D,d8", 2)},
                {0x17, new Instruction("RLA", 1)},
                {0x18, new Instruction("JR r8", 2)},
                {0x19, new Instruction("ADD HL,DE", 1)},
                {0x1A, new Instruction("LD A,(DE)", 1)},
                {0x1B, new Instruction("DEC DE", 1)},
                {0x1C, new Instruction("INC E", 1)},
                {0x1D, new Instruction("DEC E", 1)},
                {0x1E, new Instruction("LD E,d8", 2)},
                {0x1F, new Instruction("RRA", 1)},

                {0x20, new Instruction("JR NZ,r8", 2)},
                {0x21, new Instruction("LD HL,d16", 3)},
                {0x22, new Instruction("LD (HL+),A", 1)},
                {0x23, new Instruction("INC HL", 1)},
                {0x24, new Instruction("INC H", 1)},
                {0x25, new Instruction("DEC H", 1)},
                {0x26, new Instruction("LD H,d8", 2)},
                {0x27, new Instruction("DAA", 1)},
                {0x28, new Instruction("JR Z,r8", 2)},
                {0x29, new Instruction("ADD HL,HL", 1)},
                {0x2A, new Instruction("LD A,(HL+)", 1)},
                {0x2B, new Instruction("DEC HL", 1)},
                {0x2C, new Instruction("INC L", 1)},
                {0x2D, new Instruction("DEC L", 1)},
                {0x2E, new Instruction("LD L,d8", 2)},
                {0x2F, new Instruction("CPL", 1)},

                {0x30, new Instruction("JR NC,r8", 2)},
                {0x31, new Instruction("LD SP,d16", 3)},
                {0x32, new Instruction("LD (HL-),A", 1)},
                {0x33, new Instruction("INC SP", 1)},
                {0x34, new Instruction("INC (HL)", 1)},
                {0x35, new Instruction("DEC (HL)", 1)},
                {0x36, new Instruction("LD (HL),d8", 2)},
                {0x37, new Instruction("SCF", 1)},
                {0x38, new Instruction("JR C,r8", 2)},
                {0x39, new Instruction("ADD HL,SP", 1)},
                {0x3A, new Instruction("LD A,(HL-)", 1)},
                {0x3B, new Instruction("DEC SP", 1)},
                {0x3C, new Instruction("INC A", 1)},
                {0x3D, new Instruction("DEC A", 1)},
                {0x3E, new Instruction("LD A,d8", 2)},
                {0x3F, new Instruction("CCF", 1)},

                {0x40, new Instruction("LD B,B", 1)},
                {0x41, new Instruction("LD B,C", 1)},
                {0x42, new Instruction("LD B,D", 1)},
                {0x43, new Instruction("LD B,E", 1)},
                {0x44, new Instruction("LD B,H", 1)},
                {0x45, new Instruction("LD B,L", 1)},
                {0x46, new Instruction("LD B,(HL)", 1)},
                {0x47, new Instruction("LD B,A", 1)},
                {0x48, new Instruction("LD C,B", 1)},
                {0x49, new Instruction("LD C,C", 1)},
                {0x4A, new Instruction("LD C,D", 1)},
                {0x4B, new Instruction("LD C,E", 1)},
                {0x4C, new Instruction("LD C,H", 1)},
                {0x4D, new Instruction("LD C,L", 1)},
                {0x4E, new Instruction("LD C,(HL)", 1)},
                {0x4F, new Instruction("LD C,A", 1)},

                {0x50, new Instruction("LD D,B", 1)},
                {0x51, new Instruction("LD D,C", 1)},
                {0x52, new Instruction("LD D,D", 1)},
                {0x53, new Instruction("LD D,E", 1)},
                {0x54, new Instruction("LD D,H", 1)},
                {0x55, new Instruction("LD D,L", 1)},
                {0x56, new Instruction("LD D,(HL)", 1)},
                {0x57, new Instruction("LD D,A", 1)},
                {0x58, new Instruction("LD E,B", 1)},
                {0x59, new Instruction("LD E,C", 1)},
                {0x5A, new Instruction("LD E,D", 1)},
                {0x5B, new Instruction("LD E,E", 1)},
                {0x5C, new Instruction("LD E,H", 1)},
                {0x5D, new Instruction("LD E,L", 1)},
                {0x5E, new Instruction("LD E,(HL)", 1)},
                {0x5F, new Instruction("LD E,A", 1)},

                {0x60, new Instruction("LD H,B", 1)},
                {0x61, new Instruction("LD H,C", 1)},
                {0x62, new Instruction("LD H,D", 1)},
                {0x63, new Instruction("LD H,E", 1)},
                {0x64, new Instruction("LD H,H", 1)},
                {0x65, new Instruction("LD H,L", 1)},
                {0x66, new Instruction("LD H,(HL)", 1)},
                {0x67, new Instruction("LD H,A", 1)},
                {0x68, new Instruction("LD L,B", 1)},
                {0x69, new Instruction("LD L,C", 1)},
                {0x6A, new Instruction("LD L,D", 1)},
                {0x6B, new Instruction("LD L,E", 1)},
                {0x6C, new Instruction("LD L,H", 1)},
                {0x6D, new Instruction("LD L,L", 1)},
                {0x6E, new Instruction("LD L,(HL)", 1)},
                {0x6F, new Instruction("LD L,A", 1)},

                {0x70, new Instruction("LD (HL),B", 1)},
                {0x71, new Instruction("LD (HL),C", 1)},
                {0x72, new Instruction("LD (HL),D", 1)},
                {0x73, new Instruction("LD (HL),E", 1)},
                {0x74, new Instruction("LD (HL),H", 1)},
                {0x75, new Instruction("LD (HL),L", 1)},
                {0x76, new Instruction("HALT", 1)},
                {0x77, new Instruction("LD (HL),A", 1)},
                {0x78, new Instruction("LD A,B", 1)},
                {0x79, new Instruction("LD A,C", 1)},
                {0x7A, new Instruction("LD A,D", 1)},
                {0x7B, new Instruction("LD A,E", 1)},
                {0x7C, new Instruction("LD A,H", 1)},
                {0x7D, new Instruction("LD A,L", 1)},
                {0x7E, new Instruction("LD A,(HL)", 1)},
                {0x7F, new Instruction("LD A,A", 1)},

                {0x80, new Instruction("ADD A,B", 1)},
                {0x81, new Instruction("ADD A,C", 1)},
                {0x82, new Instruction("ADD A,D", 1)},
                {0x83, new Instruction("ADD A,E", 1)},
                {0x84, new Instruction("ADD A,H", 1)},
                {0x85, new Instruction("ADD A,L", 1)},
                {0x86, new Instruction("ADD A,(HL)", 1)},
                {0x87, new Instruction("ADD A,A", 1)},
                {0x88, new Instruction("ADC A,B", 1)},
                {0x89, new Instruction("ADC A,C", 1)},
                {0x8A, new Instruction("ADC A,D", 1)},
                {0x8B, new Instruction("ADC A,E", 1)},
                {0x8C, new Instruction("ADC A,H", 1)},
                {0x8D, new Instruction("ADC A,L", 1)},
                {0x8E, new Instruction("ADC A,(HL)", 1)},
                {0x8F, new Instruction("ADC A,A", 1)},

                {0x90, new Instruction("SUB B", 1)},
                {0x91, new Instruction("SUB C", 1)},
                {0x92, new Instruction("SUB D", 1)},
                {0x93, new Instruction("SUB E", 1)},
                {0x94, new Instruction("SUB H", 1)},
                {0x95, new Instruction("SUB L", 1)},
                {0x96, new Instruction("SUB (HL)", 1)},
                {0x97, new Instruction("SUB A", 1)},
                {0x98, new Instruction("SBC A,B", 1)},
                {0x99, new Instruction("SBC A,C", 1)},
                {0x9A, new Instruction("SBC A,D", 1)},
                {0x9B, new Instruction("SBC A,E", 1)},
                {0x9C, new Instruction("SBC A,H", 1)},
                {0x9D, new Instruction("SBC A,L", 1)},
                {0x9E, new Instruction("SBC A,(HL)", 1)},
                {0x9F, new Instruction("SBC A,A", 1)},

                {0xA0, new Instruction("AND B", 1)},
                {0xA1, new Instruction("AND C", 1)},
                {0xA2, new Instruction("AND D", 1)},
                {0xA3, new Instruction("AND E", 1)},
                {0xA4, new Instruction("AND H", 1)},
                {0xA5, new Instruction("AND L", 1)},
                {0xA6, new Instruction("AND (HL)", 1)},
                {0xA7, new Instruction("AND A", 1)},
                {0xA8, new Instruction("XOR B", 1)},
                {0xA9, new Instruction("XOR C", 1)},
                {0xAA, new Instruction("XOR D", 1)},
                {0xAB, new Instruction("XOR E", 1)},
                {0xAC, new Instruction("XOR H", 1)},
                {0xAD, new Instruction("XOR L", 1)},
                {0xAE, new Instruction("XOR (HL)", 1)},
                {0xAF, new Instruction("XOR A", 1)},

                {0xB0, new Instruction("OR B", 1)},
                {0xB1, new Instruction("OR C", 1)},
                {0xB2, new Instruction("OR D", 1)},
                {0xB3, new Instruction("OR E", 1)},
                {0xB4, new Instruction("OR H", 1)},
                {0xB5, new Instruction("OR L", 1)},
                {0xB6, new Instruction("OR (HL)", 1)},
                {0xB7, new Instruction("OR A", 1)},
                {0xB8, new Instruction("CP B", 1)},
                {0xB9, new Instruction("CP C", 1)},
                {0xBA, new Instruction("CP D", 1)},
                {0xBB, new Instruction("CP E", 1)},
                {0xBC, new Instruction("CP H", 1)},
                {0xBD, new Instruction("CP L", 1)},
                {0xBE, new Instruction("CP (HL)", 1)},
                {0xBF, new Instruction("CP A", 1)},

                {0xC0, new Instruction("RET NZ", 1)},
                {0xC1, new Instruction("POP BC", 1)},
                {0xC2, new Instruction("JP NZ,a16", 3)},
                {0xC3, new Instruction("JP a16", 3)},
                {0xC4, new Instruction("CALL NZ,a16", 3)},
                {0xC5, new Instruction("PUSH BC", 1)},
                {0xC6, new Instruction("ADD A,d8", 2)},
                {0xC7, new Instruction("RST 00h", 1)},
                {0xC8, new Instruction("RET Z", 1)},
                {0xC9, new Instruction("RET", 1)},
                {0xCA, new Instruction("JP Z,a16", 3)},
                {0xCB, new Instruction("CB", 2)},
                {0xCC, new Instruction("CALL Z,a16", 3)},
                {0xCD, new Instruction("CALL a16", 3)},
                {0xCE, new Instruction("ADC A,d8", 2)},
                {0xCF, new Instruction("RST 08h", 1)},

                {0xD0, new Instruction("RET NC", 1)},
                {0xD1, new Instruction("POP DE", 1)},
                {0xD2, new Instruction("JP NC,a16", 3)},
                {0xD3, new Instruction("<undefined>", 1)},
                {0xD4, new Instruction("CALL NC,a16", 3)},
                {0xD5, new Instruction("PUSH DE", 1)},
                {0xD6, new Instruction("SUB d8", 2)},
                {0xD7, new Instruction("RST 10h", 1)},
                {0xD8, new Instruction("RET C", 1)},
                {0xD9, new Instruction("RETI", 1)},
                {0xDA, new Instruction("JP C,a16", 3)},
                {0xDB, new Instruction("<undefined>", 1)},
                {0xDC, new Instruction("CALL C,a16", 3)},
                {0xDD, new Instruction("<undefined>", 1)},
                {0xDE, new Instruction("SBC A,d8", 2)},
                {0xDF, new Instruction("RST 18h", 1)},

                {0xE0, new Instruction("LDH (a8),A", 2)},
                {0xE1, new Instruction("POP HL", 1)},
                {0xE2, new Instruction("LD (C),A", 1)},
                {0xE3, new Instruction("<undefined>", 1)},
                {0xE4, new Instruction("<undefined>", 1)},
                {0xE5, new Instruction("PUSH HL", 1)},
                {0xE6, new Instruction("AND d8", 2)},
                {0xE7, new Instruction("RST 20h", 1)},
                {0xE8, new Instruction("ADD SP,r8", 2)},
                {0xE9, new Instruction("JP (HL)", 1)},
                {0xEA, new Instruction("LD (a16),A", 3)},
                {0xEB, new Instruction("<undefined>", 1)},
                {0xEC, new Instruction("<undefined>", 1)},
                {0xED, new Instruction("<undefined>", 1)},
                {0xEE, new Instruction("XOR d8", 2)},
                {0xEF, new Instruction("RST 28h", 1)},

                {0xF0, new Instruction("LDH A,(a8)", 2)},
                {0xF1, new Instruction("POP AF", 1)},
                {0xF2, new Instruction("LD A,(C)", 1)},
                {0xF3, new Instruction("DI", 1)},
                {0xF4, new Instruction("<undefined>", 1)},
                {0xF5, new Instruction("PUSH AF", 1)},
                {0xF6, new Instruction("OR d8", 2)},
                {0xF7, new Instruction("RST 30h", 1)},
                {0xF8, new Instruction("LD HL,SP+r8", 2)},
                {0xF9, new Instruction("LD SP,HL", 1)},
                {0xFA, new Instruction("LD A,(a16)", 3)},
                {0xFB, new Instruction("EI", 1)},
                {0xFC, new Instruction("<undefined>", 1)},
                {0xFD, new Instruction("<undefined>", 1)},
                {0xFE, new Instruction("CP d8", 2)},
                {0xFF, new Instruction("RST 38h", 1)},
            };
        }
    }
}

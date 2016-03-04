using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GBEmulator.Emulator
{
    public class GameboyMMU
    {
        public GameboyCPU CPU;
        public GameboyLCD LCD;
        public GameboyCart Cart;

        public byte[] WRAM = new byte[8192];
        public byte[] HRAM = new byte[128];

        private bool inBios = true;
        private int dmaCyclesLeft = 0;

        private byte[] gbBios = {
            0x31,0xFE,0xFF,0xAF,0x21,0xFF,0x9F,0x32,0xCB,0x7C,0x20,0xFB,0x21,0x26,0xFF,0x0E,
            0x11,0x3E,0x80,0x32,0xE2,0x0C,0x3E,0xF3,0xE2,0x32,0x3E,0x77,0x77,0x3E,0xFC,0xE0,
            0x47,0x11,0x04,0x01,0x21,0x10,0x80,0x1A,0xCD,0x95,0x00,0xCD,0x96,0x00,0x13,0x7B,
            0xFE,0x34,0x20,0xF3,0x11,0xD8,0x00,0x06,0x08,0x1A,0x13,0x22,0x23,0x05,0x20,0xF9,
            0x3E,0x19,0xEA,0x10,0x99,0x21,0x2F,0x99,0x0E,0x0C,0x3D,0x28,0x08,0x32,0x0D,0x20,
            0xF9,0x2E,0x0F,0x18,0xF3,0x67,0x3E,0x64,0x57,0xE0,0x42,0x3E,0x91,0xE0,0x40,0x04,
            0x1E,0x02,0x0E,0x0C,0xF0,0x44,0xFE,0x90,0x20,0xFA,0x0D,0x20,0xF7,0x1D,0x20,0xF2,
            0x0E,0x13,0x24,0x7C,0x1E,0x83,0xFE,0x62,0x28,0x06,0x1E,0xC1,0xFE,0x64,0x20,0x06,
            0x7B,0xE2,0x0C,0x3E,0x87,0xE2,0xF0,0x42,0x90,0xE0,0x42,0x15,0x20,0xD2,0x05,0x20,
            0x4F,0x16,0x20,0x18,0xCB,0x4F,0x06,0x04,0xC5,0xCB,0x11,0x17,0xC1,0xCB,0x11,0x17,
            0x05,0x20,0xF5,0x22,0x23,0x22,0x23,0xC9,0xCE,0xED,0x66,0x66,0xCC,0x0D,0x00,0x0B,
            0x03,0x73,0x00,0x83,0x00,0x0C,0x00,0x0D,0x00,0x08,0x11,0x1F,0x88,0x89,0x00,0x0E,
            0xDC,0xCC,0x6E,0xE6,0xDD,0xDD,0xD9,0x99,0xBB,0xBB,0x67,0x63,0x6E,0x0E,0xEC,0xCC,
            0xDD,0xDC,0x99,0x9F,0xBB,0xB9,0x33,0x3E,0x3C,0x42,0xB9,0xA5,0xB9,0xA5,0x42,0x3C,
            0x21,0x04,0x01,0x11,0xA8,0x00,0x1A,0x13,0xBE,0x20,0xFE,0x23,0x7D,0xFE,0x34,0x20,
            0xF5,0x06,0x19,0x78,0x86,0x23,0x05,0x20,0xFB,0x86,0x20,0xFE,0x3E,0x01,0xE0,0x50
        };

        public GameboyMMU()
        {

        }

        public void Clock(int cycles)
        {
            if (dmaCyclesLeft > 0)
                dmaCyclesLeft -= cycles;
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
            if ((dmaCyclesLeft > 0) && ((addr & 0xFF80) != 0xFF80))
                return;

            switch (addr & 0xF000)
            {
                case 0x0000: // rom bank 0
                case 0x1000:
                case 0x2000:
                case 0x3000:
                case 0x4000: // rom bank 1
                case 0x5000:
                case 0x6000:
                case 0x7000:
                    Cart.Write(addr, data);
                    break;

                case 0x8000: // vram
                case 0x9000:
                    if (LCD.Mode < 3)
                        LCD.VRAM[addr & 0x7FFF] = data;
                    break;

                case 0xA000: // external ram
                case 0xB000:
                    Cart.Write(addr, data);
                    break;

                case 0xC000: // work ram
                case 0xD000:
                    WRAM[addr & 0x1FFF] = data;
                    break;

                case 0xE000:
                case 0xF000:
                    if (addr == 0xFFFF) // interrupt enable
                    {
                        CPU.IE = data;
                    }
                    else if (addr == 0xFF0F) // interrupt flag
                    {
                        CPU.IF = data;
                    }
                    else if ((addr & 0xFF80) == 0xFF80) // high ram
                    {
                        HRAM[addr & 0x007F] = data;
                    }
                    else if ((addr & 0xFF00) == 0xFE00) // oam ram
                    {
                        var destAddr = addr & 0xFF;
                        
                        if ((LCD.Mode < 2) && (destAddr <= 0x9f))
                            LCD.OAM[destAddr] = data;

                    }
                    else if ((addr & 0xFF00) == 0xFF00) // I/0 ports
                    {
                        switch (addr)
                        {
                            // input
                            case 0xFF00: // joypad
                                break;

                            // timer
                            case 0xFF04: // div - divider
                                break;
                            case 0xFF05: // tima - timer counter
                                break;
                            case 0xFF06: // tma - timer modulo
                                break;
                            case 0xFF07: // tac - timer control
                                break;

                            // lcd
                            case 0xFF40: // lcdc - lcd control
                                LCD.LCDC = data;
                                break;
                            case 0xFF41: // stat - lcd status 
                                LCD.STAT = (byte)(data & 0xF8);
                                break;
                            case 0xFF42: // scy - bg scroll y
                                LCD.SCY = data;
                                break;
                            case 0xFF43: // scx - bg scroll x
                                LCD.SCX = data;
                                break;
                            case 0xFF45: // lyc - ly compare
                                LCD.LCY = data;
                                break;
                            case 0xFF46: // dma - oam dma control
                                var sourceAddr = Math.Min(data, (byte)0xF1) << 8;
                                for (var i = 0; i <= 0x9F; i++)
                                    // copy from specified bytes to oam memory
                                    LCD.OAM[0xFE00 & i] = Read((ushort)(sourceAddr & i));

                                // about 160 usec for dma to finish - need to block access to everythign except hram
                                dmaCyclesLeft = 680;

                                break;
                            case 0xFF47: // bgp - background palette
                                LCD.BGP = data;
                                break;
                            case 0xFF48: // obp0 - object palette 0
                                LCD.OBP0 = data;
                                break;
                            case 0xFF49: // obp1 - object palette 1
                                LCD.OBP1 = data;
                                break;
                            case 0xFF4A: // wy - window y pos
                                LCD.WY = data;
                                break;
                            case 0xFF4B: //wx - window x pos
                                LCD.WX = data;
                                break;

                            // sound

                            // misc
                            case 0xFF50: // disable access to gb boot
                                if ((data & 0x1) == 0x1)
                                    inBios = false;
                                break;
                        }
                    }
                    else
                    {
                        // echo work ram
                        WRAM[addr & 0x1FFF] = data;
                    }
                    break;
            }
        }

        private byte Read(ushort addr)
        {
            // if dma is in progress, you can't access anythign other than high ram
            if ((dmaCyclesLeft > 0) && ((addr & 0xFF80) != 0xFF80))
                return 0xFF;

            return 0;
        }
    }
}

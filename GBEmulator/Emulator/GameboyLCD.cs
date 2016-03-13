using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GBEmulator.Emulator
{
    public class GameboyLCD
    {
        public GameboyCPU CPU;

        public byte[] Screen = new byte[160*144];

        public byte LY, LYC;
        public byte SCY, SCX, WY, WX = 0;

        public byte BGP, OBP0, OBP1;

        public byte Mode;

        public byte[] VRAM = new byte[8192];
        public byte[] OAM = new byte[160];

        public bool FrameReady;

        private byte lcdc, stat;
        private int modeCycle;
        private int lcdTimeout;

        public GameboyLCD()
        {
           
        }

        public byte LCDC
        {
            get
            {
                return lcdc;
            }
            set
            {
                // check if lcd enable has changed
                if ((lcdc & 0x80) != (value & 0x80))
                {
                    if ((value & 0x80) == 0x80)
                    {
                        Mode = 2;
                        modeCycle = 0;
                        LY = 0;
                    }
                    else
                    {
                        Mode = 0;
                        lcdTimeout = 0;
                    }
                }

                lcdc = value;
            }
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

        public void Clock(int cycles)
        {
            // if bit 7 of LCDC is enabled, display is on
            if ((LCDC & 0x80) == 0x80)
            {
                modeCycle += cycles;

                switch (Mode)
                {
                    case 0: // HBLANK
                        if (modeCycle >= 204)
                        {
                            modeCycle -= 204;
                            LY++;
                            // If bit 6 of STAT is enabled, fire interrupt for LYC = LY
                            if ((stat & 0x40) == 0x40)
                                CPU.IF |= (byte)(LY == LYC ? CPU.INT_LCDSTAT : 0);
                            // if Line is 144 we are going to vblank, otherwise draw next line
                            if (LY == 144)
                            {
                                Mode = 1;
                                // If bit 4 of STAT is enabled, fire interrupt for VBLANK
                                if ((stat & 0x10) == 0x10)
                                    CPU.IF |= CPU.INT_LCDSTAT;
                                CPU.IF |= CPU.INT_VBLANK;
                            }
                            else
                            {
                                Mode = 2;
                                // If bit 5 of STAT is enabled, fire interrupt for OAM
                                if ((stat & 0x20) == 0x20)
                                    CPU.IF |= CPU.INT_LCDSTAT;
                            }
                        }
                        break;

                    case 1: // VBLANK
                        if (modeCycle >= 456)
                        {
                            modeCycle -= 456;
                            LY++;
                            // if LY = 154, the vblank is over so go back to drawing
                            if (LY == 154)
                            {
                                LY = 0;
                                Mode = 2;
                                // If bit 5 of STAT is enabled, fire interrupt for OAM
                                if ((stat & 0x20) == 0x20)
                                    CPU.IF |= CPU.INT_LCDSTAT;

                                FrameReady = true;
                            }

                            // If bit 6 of STAT is enabled, fire interrupt on LYC = LY
                            if ((stat & 0x40) == 0x40)
                                CPU.IF |= (byte)(LY == LYC ? CPU.INT_LCDSTAT : 0);
                        }
                        break;

                    case 2: // OAM
                        if (modeCycle >= 80)
                        {
                            modeCycle -= 80;
                            Mode = 3;
                        }
                        break;

                    case 3: // Drawing
                        if (modeCycle >= 172)
                        {
                            DrawLine();

                            modeCycle -= 172;
                            Mode = 0;

                            // If bit 3 of STAT is enabled, fire interrupt for HBLANK
                            if ((stat & 0x8) == 0x8)
                                CPU.IF |= CPU.INT_LCDSTAT;
                        }
                        break;
                }
            }
            else
            {
                lcdTimeout += cycles;

                if (lcdTimeout >= 70224)
                {
                    FrameReady = true;
                    lcdTimeout -= 70224;
                }
            }
        }

        private void DrawLine()
        {

        }
    }
}

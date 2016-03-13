using GBEmulator.Emulator;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace GBEmulator
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private GameboyMMU mmu;
        private GameboyCPU cpu;
        private GameboyLCD lcd;
        private GameboyInput input;
        private GameboyCart cart;

        private WriteableBitmap lcdImage;

        public MainWindow()
        {
            InitializeComponent();

            mmu = new GameboyMMU();
            cpu = new GameboyCPU();
            lcd = new GameboyLCD();
            input = new GameboyInput();
            cart = new GameboyCart();

            mmu.CPU = cpu;
            mmu.Input = input;
            mmu.LCD = lcd;
            mmu.Cart = cart;

            cpu.MMU = mmu;

            lcd.CPU = cpu;

            lcdImage = new WriteableBitmap(160, 144, 96, 96, PixelFormats.Gray8, null);
            Display.Source = lcdImage;
        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            switch (e.Key)
            {
                case Key.Up:
                    input.Up = true;
                    break;
                case Key.Down:
                    input.Down = true;
                    break;
                case Key.Left:
                    input.Left = true;
                    break;
                case Key.Right:
                    input.Right = true;
                    break;
                case Key.Z:
                    input.A = true;
                    break;
                case Key.X:
                    input.B = true;
                    break;
                case Key.A:
                    input.Select = true;
                    break;
                case Key.S:
                    input.Start = true;
                    break;
            }
        }

        private void Window_KeyUp(object sender, KeyEventArgs e)
        {
            switch (e.Key)
            {
                case Key.Up:
                    input.Up = false;
                    break;
                case Key.Down:
                    input.Down = false;
                    break;
                case Key.Left:
                    input.Left = false;
                    break;
                case Key.Right:
                    input.Right = false;
                    break;
                case Key.Z:
                    input.A = false;
                    break;
                case Key.X:
                    input.B = false;
                    break;
                case Key.A:
                    input.Select = false;
                    break;
                case Key.S:
                    input.Start = false;
                    break;
            }
        }

        private void Frame_Click(object sender, RoutedEventArgs e)
        {
            while (!lcd.FrameReady)
            {
                cpu.Step();
            }

            lcdImage.WritePixels(new Int32Rect(0, 0, 160, 144), lcd.Screen, 160, 0);
            lcd.FrameReady = false;

            UpdateRegDebug();
        }

        private void Step_Click(object sender, RoutedEventArgs e)
        {
            cpu.Step();

            if (lcd.FrameReady)
            {
                lcdImage.WritePixels(new Int32Rect(0, 0, 160, 144), lcd.Screen, 1, 0, 0);
                lcd.FrameReady = false;
            }

            UpdateRegDebug();
        }

        private void Run_Click(object sender, RoutedEventArgs e)
        {

        }

        private void UpdateRegDebug()
        {
            RegA.Text = cpu.A.ToString("X2");
            RegB.Text = cpu.B.ToString("X2");
            RegC.Text = cpu.C.ToString("X2");
            RegD.Text = cpu.D.ToString("X2");
            RegE.Text = cpu.E.ToString("X2");
            RegF.Text = cpu.F.ToString("X2");
            RegH.Text = cpu.H.ToString("X2");
            RegL.Text = cpu.L.ToString("X2");
            
            RegSP.Text = cpu.SP.ToString("X4");
            RegPC.Text = cpu.PC.ToString("X4");

            RegFlags.Text = string.Format("{0} {1} {2} {3}",
                cpu.Flags.Z ? "Z" : " ",
                cpu.Flags.N ? "N" : " ",
                cpu.Flags.H ? "H" : " ",
                cpu.Flags.C ? "C" : " ");
        }
    }
}

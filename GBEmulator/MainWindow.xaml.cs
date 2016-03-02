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

        public MainWindow()
        {
            InitializeComponent();

            mmu = new GameboyMMU();
            cpu = new GameboyCPU();

            mmu.CPU = cpu;
            cpu.MMU = mmu;
        }
    }
}

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using XamlAnimatedGif;

namespace BnS_Multitool
{
    /// <summary>
    /// Interaction logic for ProgressControl.xaml
    /// </summary>
    public partial class ProgressControl : UserControl
    {
        public static Grid SadPeepoGrid;
        public static TextBlock progressLbl;

        public ProgressControl()
        {
            InitializeComponent();
            SadPeepoGrid = SadPeepo;
            progressLbl = ProgressLabel;
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            SadPeepo.Visibility = Visibility.Hidden;
        }

        public static void updateProgressLabel(string msg)
        {
            progressLbl.Dispatcher.Invoke(new Action(() =>
            {
                progressLbl.Text = msg;
            }));
        }

        public static void errorSadPeepo(Visibility vis)
        {
            SadPeepoGrid.Dispatcher.Invoke(new Action(() =>
            {
                SadPeepoGrid.Visibility = vis;
            }));
        }
    }
}

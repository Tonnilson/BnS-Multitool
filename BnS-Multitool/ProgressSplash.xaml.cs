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

namespace BnS_Multitool
{
    /// <summary>
    /// Interaction logic for ProgressSplash.xaml
    /// </summary>
    public partial class ProgressSplash : UserControl
    {
        private string progressText;
        private string progressImage;

        private Visibility visibility;
        public new Visibility Visibility
        {
            get { return visibility; }
            set
            {
                visibility = value;
                mainGrid.Dispatcher.BeginInvoke(new Action(() =>
                {
                    mainGrid.Visibility = value;
                }));
            }
        }

        public string ProgressText
        {
            get { return progressText; }
            set
            {
                progressText = value;
                StatusText.Dispatcher.BeginInvoke(new Action(() =>
                {
                    StatusText.Text = progressText;
                }));
            }
        }

        public ProgressSplash()
        {
            InitializeComponent();
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {

        }
    }
}

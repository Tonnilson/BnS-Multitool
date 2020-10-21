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
using System.Windows.Shapes;

namespace BnS_Multitool
{
    /// <summary>
    /// Interaction logic for ErrorPrompt.xaml
    /// </summary>
    public partial class ErrorPrompt : Window
    {
        public ErrorPrompt(string Message, bool good = false)
        {
            InitializeComponent();
            ErrorLabel.Text = Message;

            if (good)
            {
                PromptIcon2.Visibility = Visibility.Visible;
                ErrorLabel.FontSize = 16;
                ErrorLabel.FontWeight = FontWeights.Bold;
            }
            else
                PromptIcon.Visibility = Visibility.Visible;
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}

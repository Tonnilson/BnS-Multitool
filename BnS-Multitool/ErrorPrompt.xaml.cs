using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace BnS_Multitool
{
    /// <summary>
    /// Interaction logic for ErrorPrompt.xaml
    /// </summary>
    public partial class ErrorPrompt : Window
    {
        public string StatusIcon { get; set; }
        public ErrorPrompt(string Message, bool good = false, bool useBoldFont = false)
        {
            // New theme control
            if (SystemConfig.SYS.THEME == 0)
                StatusIcon = good ? "poggies.png" : "Images\\feelsworry.png";
            else
                StatusIcon = good ? "Images\\agon\\agonHappy.png" : "Images\\agon\\agonSob.png";

            InitializeComponent();
            ErrorLabel.Text = Message;

            //Set the owner and center this on the main window.
            this.Owner = MainWindow.mainWindow;
            this.WindowStartupLocation = WindowStartupLocation.CenterOwner;

            if (useBoldFont)
                ErrorLabel.FontWeight = FontWeights.Bold;

            if (good)
            {
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

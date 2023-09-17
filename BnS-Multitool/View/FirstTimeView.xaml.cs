using BnS_Multitool.Messages;
using CommunityToolkit.Mvvm.Messaging;
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

namespace BnS_Multitool.View
{
    /// <summary>
    /// Interaction logic for FirstTimeView.xaml
    /// </summary>
    public partial class FirstTimeView : Window, IRecipient<CloseMessage>
    {
        public FirstTimeView()
        {
            InitializeComponent();
            WeakReferenceMessenger.Default.RegisterAll(this);
        }

        void IRecipient<CloseMessage>.Receive(CloseMessage message) => this.Close();
    }
}

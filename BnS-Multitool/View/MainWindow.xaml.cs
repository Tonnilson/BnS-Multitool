using System.Windows;
using CommunityToolkit.Mvvm.Messaging;

namespace BnS_Multitool.View
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, IRecipient<Messages.ActivateWindowMessage>
    {
        public MainWindow()
        {
            InitializeComponent();
            MouseDown += OnMouseDownDrag;
            WeakReferenceMessenger.Default.RegisterAll(this);
        }

        void IRecipient<Messages.ActivateWindowMessage>.Receive(Messages.ActivateWindowMessage message) =>
            this.Activate();

        private void OnMouseDownDrag(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if(e.LeftButton == System.Windows.Input.MouseButtonState.Pressed)
                DragMove();
        }
    }
}

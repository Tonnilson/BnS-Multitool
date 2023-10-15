using BnS_Multitool.Messages;
using BnS_Multitool.Models;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using System.Diagnostics;

namespace BnS_Multitool.ViewModels
{
    public partial class MainPageViewModel : ObservableObject
    {
        [ObservableProperty]
        private string changeLogSource = "Retreiving...";

        [ObservableProperty]
        private string pingStatus;

        [ObservableProperty]
        private string usersOnlineStatus = "Users Online: Retrieving...";

        public bool IsViewLoaded = false;
        private readonly MultiTool _mt;

        public MainPageViewModel(MultiTool mt)
        {
            _mt = mt;
        }

        [RelayCommand]
        void UILoaded()
        {
            if (_mt.MT_Info != null)
            {
                string changeLog = string.Empty;
                foreach (var entry in _mt.MT_Info.CHANGELOG)
                    changeLog += string.Format("Version: {0}\r{1}\r\r", entry.VERSION, entry.NOTES);

                ChangeLogSource = changeLog;
            }

            //_message.Enqueue(new MessageService.MessagePrompt { Message = "This is a test", IsError = false, UseBold = false });
           //WeakReferenceMessenger.Default.Send(new ShowMessagePrompt(new MessagePrompt { Message = "Test from MainPageViewModel", IsError = true, UseBold = true }));
        }

        [RelayCommand]
        void QuickPlay() => WeakReferenceMessenger.Default.Send(new LaunchMessage(0));

        [RelayCommand]
        void OpenPayPal() => Process.Start(new ProcessStartInfo(@"https://www.paypal.com/cgi-bin/webscr?cmd=_s-xclick&hosted_button_id=DZ8KL2ZDS44JC&source=url") { UseShellExecute = true });

        [RelayCommand]
        void OpenPatreon() => Process.Start(new ProcessStartInfo(@"https://patreon.com/tonnilson") { UseShellExecute = true });
    }
}

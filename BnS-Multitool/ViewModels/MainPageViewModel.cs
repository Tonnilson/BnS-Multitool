using BnS_Multitool.Models;
using BnS_Multitool.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using System;

namespace BnS_Multitool.ViewModels
{
    public partial class MainPageViewModel : ObservableObject
    {
        [ObservableProperty]
        private string changeLogSource = "Hello World";

        [ObservableProperty]
        private string pingStatus;

        [ObservableProperty]
        private string usersOnlineStatus = "Users Online: Retrieving...";

        public bool IsViewLoaded = false;
        private readonly MultiTool _mt;
        private readonly Settings _settings;
        private readonly httpClient _httpClient;
        private readonly ILogger<MainPageViewModel> _logger;

        public MainPageViewModel(MultiTool mt, Settings settings, httpClient http, ILogger<MainPageViewModel> logger)
        {
            _mt = mt;
            _settings = settings;
            _httpClient = http;
            _logger = logger;
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

        private void OnlineUsers_Tick(object? sender)
        {
            try
            {
                var response = _httpClient.DownloadString($"{Properties.Settings.Default.MainServerAddr}usersOnline.php?UID={_settings.System.FINGERPRINT}").GetAwaiter().GetResult();
                int users = int.Parse(response);
                UsersOnlineStatus = $"Users Online: {users:N0}";
            } catch (Exception ex)
            {
                _logger.LogError(ex, "Error with retrieving online users number");
                UsersOnlineStatus = "Users Online: Error";
            }
        }
    }
}

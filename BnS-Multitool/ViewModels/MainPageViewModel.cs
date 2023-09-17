using BnS_Multitool.Extensions;
using BnS_Multitool.Messages;
using BnS_Multitool.Models;
using BnS_Multitool.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Extensions.Logging;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Net.Http;
using System.Net.Sockets;
using System.Threading;

namespace BnS_Multitool.ViewModels
{
    public partial class MainPageViewModel : ObservableObject, IRecipient<UnloadedMainPageVM>
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
        private BackgroundWorker pingWorker = new BackgroundWorker();
        private readonly Timer _onlineUser;
        private readonly MessageService _message;

        public MainPageViewModel(MultiTool mt, Settings settings, httpClient http, ILogger<MainPageViewModel> logger, MessageService ms)
        {
            _mt = mt;
            _settings = settings;
            _httpClient = http;
            _logger = logger;
            _message = ms;
            pingWorker.WorkerSupportsCancellation = true;
            pingWorker.DoWork += PingWorker_DoWork;
            _onlineUser = new Timer(OnlineUsers_Tick, new AutoResetEvent(false), TimeSpan.FromMilliseconds(0), TimeSpan.FromMinutes(10));

            WeakReferenceMessenger.Default.Register(this);
        }

        private void PingWorker_DoWork(object? sender, DoWorkEventArgs e)
        {
            Stopwatch _sw = new Stopwatch();
            Socket _pingSocket;
            int ping = -1;

            while(!pingWorker.CancellationPending)
            {
                if (_settings.System.PING_CHECK)
                {
                    try
                    {
                        _pingSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                        _pingSocket.Blocking = true;
                        _pingSocket.ReceiveTimeout = 1500;
                        _pingSocket.SendTimeout = 1500;
                        _sw.Start();
                        _pingSocket.Connect(_settings.Account.REGION.GetAttribute<GameIPAddressAttribute>().Name, 10100);
                        _sw.Stop();
                        _pingSocket.Close();
                        ping = Convert.ToInt32(_sw.Elapsed.TotalMilliseconds);
                    }
                    catch { ping = -1; }
                    finally { _sw.Reset(); }
                    if (ping >= 0)
                        PingStatus = $"Ping ({_settings.Account.REGION.GetDescription()}): {ping}ms";
                    else
                        PingStatus = "Ping: Error";
                }
                else
                    PingStatus = "Ping: Turned Off";
                Thread.Sleep(1000);
            }
        }

        [RelayCommand]
        void UILoaded()
        {
            GC.Collect();
            if (!pingWorker.IsBusy)
                pingWorker.RunWorkerAsync();

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

        void IRecipient<UnloadedMainPageVM>.Receive(UnloadedMainPageVM message)
        {
            if (pingWorker.IsBusy)
                pingWorker.CancelAsync();
        }
    }
}

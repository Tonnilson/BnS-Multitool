using System;
using System.IO;
using System.Windows;
using BnS_Multitool.Models;
using BnS_Multitool.View;
using BnS_Multitool.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NLog.Extensions.Logging;
using BnS_Multitool.Services;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Reflection;
using CommunityToolkit.Mvvm.Input;

namespace BnS_Multitool
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private readonly IServiceProvider _serviceProvider;

        public App()
        {
            IServiceCollection services = new ServiceCollection();

            // NLog Config
            var logConfig = new NLog.Config.LoggingConfiguration();
            logConfig.AddRule(NLog.LogLevel.Info, NLog.LogLevel.Fatal, new NLog.Targets.ConsoleTarget("logconsole"));
            logConfig.AddRule(NLog.LogLevel.Debug, NLog.LogLevel.Fatal, new NLog.Targets.FileTarget("logfile") { FileName = Path.Combine("logs", $"{DateTime.Now.ToString("yyyy-MM-dd")}.txt") });

            // Add the instances to the serviceCollection
            services.AddSingleton<MainViewModel>().
                AddSingleton<httpClient>().
                AddSingleton<MessageService>().
                AddSingleton<SyncClient>().
                AddSingleton<Settings>().
                AddSingleton<BnS>().
                AddSingleton<Launcher>().
                AddSingleton<LauncherViewModel>().
                AddSingleton<ModsView>().
                AddSingleton<ModsViewModel>().
                AddSingleton<PluginData>().
                AddSingleton<Plugins>().
                AddSingleton<PluginsViewModel>().
                AddSingleton<PatchesView>().
                AddSingleton<PatchesViewModel>().
                AddSingleton<ExtendedOptionsView>().
                AddSingleton<ExtendedOptionsViewModel>().
                AddSingleton<SyncView>().
                AddSingleton<SyncViewModel>().
                AddSingleton<EffectsView>().
                AddSingleton<EffectsViewModel>().
                AddSingleton<MainPage>().
                AddSingleton<XmlModel>().
                AddSingleton<MainPageViewModel>().
                AddSingleton<MultiTool>().
                AddSingleton<NotifyIconService>().
                AddSingleton<InitScreenViewModel>().
                AddSingleton<FirstTimeViewModel>().
                AddSingleton(s => new FirstTimeView()
                {
                    DataContext = s.GetRequiredService<FirstTimeViewModel>()
                }).
                AddSingleton(s => new InitScreen()
                {
                    DataContext = s.GetRequiredService<InitScreenViewModel>()
                }).
                AddSingleton(s => new MainWindow()
                {
                    DataContext = s.GetRequiredService<MainViewModel>()
                }).
                AddLogging(logBuilder =>
                {
                    logBuilder.ClearProviders();
                    logBuilder.SetMinimumLevel(LogLevel.Trace);
                    logBuilder.AddNLog(logConfig);
                });
            _serviceProvider = services.BuildServiceProvider();

            AppDomain.CurrentDomain.UnhandledException += (s, e) =>
                LogUnhandledException((Exception)e.ExceptionObject, "AppDomain.CurrentDomain.UnhandledException");

            DispatcherUnhandledException += (s, e) =>
                LogUnhandledException(e.Exception, "Application.Current.DispatcherUnhandledException");

            TaskScheduler.UnobservedTaskException += (s, e) =>
            {
                LogUnhandledException(e.Exception, "TaskScheduler.UnobservedTaskException");
                e.SetObserved();
            };
        }

        protected override void OnStartup(StartupEventArgs e)
        {
            base.Exit += OnAppExit;

            // Make the tooltip stay on screen till hover is over.
            ToolTipService.ShowDurationProperty.OverrideMetadata(
                typeof(DependencyObject), new FrameworkPropertyMetadata(int.MaxValue));

            MainWindow = _serviceProvider.GetRequiredService<InitScreen>();
            MainWindow.Show();
            Task.Run(async () => await InitApp());
            base.OnStartup(e);

            if (!Directory.Exists("modpolice"))
                Directory.CreateDirectory("modpolice");
        }

        private async Task InitApp()
        {
            // These assembly files are loaded in much later and will cause a stutter so lets load them at start to eliminate that problem
            Assembly.Load("WatsonWebsocket");
            Assembly.Load("System.Net.WebSockets");
            Assembly.Load("System.Net.HttpListener");
            Assembly.Load("System.Reflection.Metadata");
            Assembly.Load("XamlAnimatedGif");
            Assembly.Load("PresentationFramework-SystemData");
            Assembly.Load("PresentationFramework-SystemCore");
            Assembly.Load("Accessibility");
            Assembly.Load("Microsoft.Xaml.Behaviors");
            Assembly.Load("System.Diagnostics.StackTrace");
            Assembly.Load("ToggleSwitch"); // This is subject to be removed later on

            await _serviceProvider.GetRequiredService<InitScreenViewModel>().InitializeAsync();
            await Current.Dispatcher.BeginInvoke(new Action(() =>
            {
                MainWindow.Hide();
                MainWindow = _serviceProvider.GetRequiredService<MainWindow>();
            }));
        }

        private void OnAppExit(object sender, ExitEventArgs e)
        {
            if(_serviceProvider != null)
            {
                var notifyService = _serviceProvider.GetRequiredService<NotifyIconService>();
                if (notifyService != null)
                    notifyService.OnShutdown();
            }
        }

        private void LogUnhandledException(Exception exception, string message)
        {
            var _logger = _serviceProvider.GetRequiredService<ILogger<App>>();
            _logger.LogError(exception, $"Unhandled Exception {message}");
        }
    }
}

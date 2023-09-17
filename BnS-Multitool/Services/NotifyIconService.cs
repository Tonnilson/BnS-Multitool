using BnS_Multitool.Messages;
using CommunityToolkit.Mvvm.Messaging;
using System;
using System.Windows.Forms;

namespace BnS_Multitool.Services
{
    public class NotifyIconService
    {
        private readonly NotifyIcon _notifyIcon;

        public NotifyIconService()
        {
            _notifyIcon = new NotifyIcon
            {
                Visible = false,
                Icon = Properties.Resources.favicon,
                Text = "BnS Multi Tool"
            };

            _notifyIcon.DoubleClick += OnNotifyDoubleClicked;
        }

        private void OnNotifyDoubleClicked(object? sender, EventArgs e)
        {
            _notifyIcon.Visible = false;
            WeakReferenceMessenger.Default.Send(new NotifyIconMessage(false));
        }

        public void OnShutdown() => _notifyIcon.Dispose();

        public void TaskTrayControl(bool isVisible) => _notifyIcon.Visible = isVisible;
    }
}

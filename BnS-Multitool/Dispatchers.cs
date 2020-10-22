using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using ToggleSwitch;

namespace BnS_Multitool
{
    class Dispatchers
    {
        public static void btnIsEnabled(Button button, bool isEnabled) => button.Dispatcher.BeginInvoke(new Action(() => { button.IsEnabled = isEnabled; }));
        public static void labelContent(Label label, string Content) => label.Dispatcher.BeginInvoke(new Action(() => { label.Content = Content; }));
        public static void toggleIsChecked(HorizontalToggleSwitch toggle, bool isChecked) => toggle.Dispatcher.BeginInvoke(new Action(() => { toggle.IsChecked = isChecked; }));
        public static void buttonVisibility(Button button, Visibility vis) => button.Dispatcher.BeginInvoke(new Action(() => { button.Visibility = vis; }));
    }
}

using BnS_Multitool.Extensions;
using BnS_Multitool.Models;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Text.RegularExpressions;
using System.Diagnostics;
using System.Xml.XPath;
using System.Xml.Linq;
using System.Windows;

namespace BnS_Multitool.ViewModels
{
    public partial class ExtendedOptionsViewModel : ObservableObject
    {
        private readonly XmlModel _xmlModel;

        public class EffectsList
        {
            public string Alias { get; set; }
            public EO_UI_Slot UI_Slot { get; set; }
            public EO_Category UI_Category { get; set; }
        }

        public class ConsoleCommands_Entry
        {
            public string command { get; set; } = string.Empty;
        }

        public ExtendedOptionsViewModel(XmlModel xmlModel)
        {
            _xmlModel = xmlModel;
        }

        [ObservableProperty]
        private bool showMainWindow = true;

        [ObservableProperty]
        private bool showEffectMover = false;

        [ObservableProperty]
        private string profileKeyName;

        // I want to die
        [ObservableProperty]
        private bool playerHighEmitter = true;
        [ObservableProperty]
        private bool playerMidEmitter = true;
        [ObservableProperty]
        private bool playerLowEmitter = true;
        [ObservableProperty]
        private bool playerJewelEffect = true;
        [ObservableProperty]
        private bool playerImmuneEffect = true;
        [ObservableProperty]
        private bool playerCharLOD = true;
        [ObservableProperty]
        private bool playerPhysics = true;
        [ObservableProperty]
        private bool playerParticleLight = true;

        [ObservableProperty]
        private bool pcHighEmitter = true;
        [ObservableProperty]
        private bool pcMidEmitter = true;
        [ObservableProperty]
        private bool pcLowEmitter = true;
        [ObservableProperty]
        private bool pcJewelEffect = true;
        [ObservableProperty]
        private bool pcImmuneEffect = true;
        [ObservableProperty]
        private bool pcCharLOD = true;
        [ObservableProperty]
        private bool pcPhysics = true;
        [ObservableProperty]
        private bool pcParticleLight = true;

        [ObservableProperty]
        private bool npcHighEmitter = true;
        [ObservableProperty]
        private bool npcMidEmitter = true;
        [ObservableProperty]
        private bool npcLowEmitter = true;
        [ObservableProperty]
        private bool npcJewelEffect = true;
        [ObservableProperty]
        private bool npcImmuneEffect = true;
        [ObservableProperty]
        private bool npcCharLOD = true;
        [ObservableProperty]
        private bool npcPhysics = true;
        [ObservableProperty]
        private bool npcParticleLight = true;

        [ObservableProperty]
        private bool backHighEmitter = true;
        [ObservableProperty]
        private bool backMidEmitter = true;
        [ObservableProperty]
        private bool backLowEmitter = true;
        [ObservableProperty]
        private bool backParticleLight = true;

        [ObservableProperty]
        private bool showPhantomEffects = true;

        [ObservableProperty]
        private int currentProfileSelection = 0;

        [ObservableProperty]
        private bool shiftKeyChecked = false;

        [ObservableProperty]
        private bool altKeyChecked = false;

        [ObservableProperty]
        private bool ctrlKeyChecked = false;

        [ObservableProperty]
        private string fontScale;

        [ObservableProperty]
        private string fontSpacing;

        [ObservableProperty]
        private bool sgt_hit_enemy = true;
        [ObservableProperty]
        private bool sgt_crithit_enemy = true;
        [ObservableProperty]
        private bool sgt_bighit_enemy = true;

        [ObservableProperty]
        private ObservableCollection<ConsoleCommands_Entry> consoleCommands = new ObservableCollection<ConsoleCommands_Entry>();

        [ObservableProperty]
        private ObservableCollection<EffectsList> effectsCollection = new ObservableCollection<EffectsList>();

        [RelayCommand]
        async void UILoaded()
        {
            if (_xmlModel.extended_options == null)
                await _xmlModel.Load_ExtendedOptions_Async();

            RefreshView();
            await Task.CompletedTask;
        }

        private string[] options = {
            "PlayerHighEmitter", "PlayerMidEmitter", "PlayerLowEmitter", "PlayerJewelEffect", "PlayerImmuneEffect", "PlayerCharLOD", "PlayerPhysics", "PlayerParticleLight",
            "PcHighEmitter", "PcMidEmitter", "PcLowEmitter", "PcJewelEffect", "PcImmuneEffect", "PcCharLOD", "PcPhysics", "PcParticleLight",
            "NpcHighEmitter", "NpcMidEmitter", "NpcLowEmitter", "NpcJewelEffect", "NpcImmuneEffect", "NpcCharLOD", "NpcPhysics", "NpcParticleLight",
            "BackHighEmitter", "BackMidEmitter", "BackLowEmitter", "BackParticleLight"
        };

        [RelayCommand]
        void SaveCurrentProfile()
        {
            var selection = CurrentProfileSelection;

            foreach (var option in options)
            {
                var property = GetType().GetProperty(option);
                if (property == null) continue;
                _xmlModel.extended_options.XPathSelectElement($"config/profile_{selection}/video_options/option[@name='{property.Name}']").Attribute("enable").Value = (bool)property.GetValue(this) ? "1" : "0";
            }

            // Phantom Effects
            _xmlModel.extended_options.XPathSelectElement($"config/profile_{selection}/phantom").Attribute("enable").Value = ShowPhantomEffects ? "1" : "0";

            // Damage font stuff
            var damage_node = _xmlModel.extended_options.XPathSelectElement($"config/profile_{selection}/damage_font");
            damage_node.Attribute("scale").Value = FontScale;
            damage_node.Attribute("wordspacing").Value = FontSpacing;
            damage_node.Attribute("sgt_hit_enemy").Value = Sgt_hit_enemy ? "0" : "1";
            damage_node.Attribute("sgt_crithit_enemy").Value = sgt_crithit_enemy ? "0" : "1"; ;
            damage_node.Attribute("sgt_bighit_enemy").Value = Sgt_bighit_enemy ? "0" : "1";

            // Console commands
            var cmds = _xmlModel.extended_options.XPathSelectElement($"config/profile_{selection}/console_cmds");
            cmds.Descendants().Where(x => true).Remove();
            foreach (var cmd in ConsoleCommands)
                cmds.Add(new XElement("cmd", new XAttribute("run", cmd.command)));

            // hotkey info
            XElement hotkey;
            if (selection == 0)
                hotkey = _xmlModel.extended_options.XPathSelectElement("config/options/option[@name='reloadKey']");
            else
                hotkey = _xmlModel.extended_options.XPathSelectElement($"config/options/option[@name='profile_{selection}']");

            hotkey.Attribute("keyCode").Value = currentProfileKey.ToString("x");
            hotkey.Attribute("bAlt").Value = AltKeyChecked ? "1" : "0";
            hotkey.Attribute("bShift").Value = ShiftKeyChecked ? "1" : "0";
            hotkey.Attribute("bCtrl").Value = CtrlKeyChecked ? "1" : "0";

            _xmlModel.Save(XmlModel.XML.ExtendedOptions);
        }

        private void RefreshView()
        {
            var selection = CurrentProfileSelection;

            if (_xmlModel.extended_options.XPathSelectElement($"config/profile_{selection}") == null)
            {
                var master = _xmlModel.extended_options.XPathSelectElement("config").LastNode;
                master.AddAfterSelf(new XElement($"profile_{selection}",
                    new XElement("console_cmds"),
                    new XElement("video_options"),
                    new XElement("phantom", new XAttribute("enable", "1")),
                    new XElement("damage_font",
                        new XAttribute("scale", "1.6"),
                        new XAttribute("wordspacing", "0"),
                        new XAttribute("sgt_hit_enemy", "0"), // Logic is flipped
                        new XAttribute("sgt_crithit_enemy", "0"), // Logic is flipped
                        new XAttribute("sgt_bighit_enemy", "0") // Logic is flipped
                    )));
            }

            // Setup emitters and stuff... 
            foreach (var option in options)
            {
                // Find the property (variable) by name
                var property = GetType().GetProperty(option);
                if (property == null) continue;
                var node = _xmlModel.extended_options.XPathSelectElement($"config/profile_{selection}/video_options/option[@name='{property.Name}']");

                // Check if the node exists, if not create it otherwise set variable
                if (node == null)
                {
                    var lastNode = _xmlModel.extended_options.XPathSelectElement($"config/profile_{selection}/video_options");
                    lastNode.Add(new XElement("option", new XAttribute("name", property.Name), new XAttribute("enable", "1")));

                    property.SetValue(this, true);
                }
                else
                    property.SetValue(this, node.Attribute("enable").Value == "1" ? true : false);
            }

            // Check if phantom effects is enabled or disabled. Do a null check first to make sure the entry exists if not create it.
            var phantom = _xmlModel.extended_options.XPathSelectElement($"config/profile_{selection}/phantom");
            if (phantom == null)
            {
                _xmlModel.extended_options.XPathSelectElement($"config/profile_{selection}").Add(new XElement("phantom", new XAttribute("enable", "1")));
                ShowPhantomEffects = true;
            }
            else
                ShowPhantomEffects = phantom.Attribute("enable").Value == "1" ? true : false;

            // Get signalinfo stuff (damage font size etc)
            var signalInfo = _xmlModel.extended_options.XPathSelectElement($"config/profile_{selection}/damage_font");
            if (signalInfo == null)
            {
                _xmlModel.extended_options.XPathSelectElement($"config/profile_{selection}").Add(new XElement("damage_font",
                    new XAttribute("scale", "1.6"),
                    new XAttribute("wordspacing", "0"),
                    new XAttribute("sgt_hit_enemy", "0"),
                    new XAttribute("sgt_crithit_enemy", "0"),
                    new XAttribute("sgt_bighit_enemy", "0")
                    ));

                FontScale = "1.6";
                FontSpacing = "0";
                Sgt_hit_enemy = true;
                Sgt_bighit_enemy = true;
                Sgt_crithit_enemy = true;
            }
            else
            {
                FontScale = signalInfo.Attribute("scale").Value;
                FontSpacing = signalInfo.Attribute("wordspacing").Value;

                // Check if attribute exists, if not create new entries
                if (signalInfo.Attribute("sgt_hit_enemy") == null)
                {
                    signalInfo.Add(new XAttribute("sgt_hit_enemy", "0"), new XAttribute("sgt_crithit_enemy", "0"), new XAttribute("sgt_bighit_enemy", "0"));
                    Sgt_bighit_enemy = true;
                    Sgt_crithit_enemy = true;
                    Sgt_hit_enemy = true;
                }
                else
                {
                    // Logic is flipped, 0 means show 1 means disable
                    Sgt_hit_enemy = signalInfo.Attribute("sgt_hit_enemy").Value == "0" ? true : false;
                    Sgt_bighit_enemy = signalInfo.Attribute("sgt_bighit_enemy").Value == "0" ? true : false;
                    Sgt_crithit_enemy = signalInfo.Attribute("sgt_crithit_enemy").Value == "0" ? true : false;
                }
            }

            // Get console commands
            var commands = new List<ConsoleCommands_Entry>();
            foreach (var cmd in _xmlModel.extended_options.XPathSelectElements($"config/profile_{selection}/console_cmds/cmd"))
                commands.Add(new ConsoleCommands_Entry { command = cmd.Attribute("run").Value });

            Application.Current.Dispatcher.BeginInvoke(new Action(() =>
            {
                ConsoleCommands = new ObservableCollection<ConsoleCommands_Entry>(commands);
            }));

            XElement hotkey;
            if (selection == 0)
                hotkey = _xmlModel.extended_options.XPathSelectElement("config/options/option[@name='reloadKey']");
            else
                hotkey = _xmlModel.extended_options.XPathSelectElement($"config/options/option[@name='profile_{selection}']");

            currentProfileKey = int.Parse(hotkey.Attribute("keyCode").Value, System.Globalization.NumberStyles.HexNumber);
            var keyInfo = KeyInterop.KeyFromVirtualKey(currentProfileKey);
            ProfileKeyName = DisplayKeyName(keyInfo.ToString());
            CtrlKeyChecked = hotkey.Attribute("bCtrl").Value == "1" ? true : false;
            AltKeyChecked = hotkey.Attribute("bAlt").Value == "1" ? true : false;
            ShiftKeyChecked = hotkey.Attribute("bShift").Value == "1" ? true : false;

            _xmlModel.Save(XmlModel.XML.ExtendedOptions);
        }

        [RelayCommand]
        void NavigateEffectMover()
        {
            ShowMainWindow = false;
            ShowEffectMover = true;

            if (_xmlModel.extended_options.XPathSelectElement("config/effect_list") == null)
                _xmlModel.extended_options.XPathSelectElement("config").LastNode.AddAfterSelf(new XElement("effect_list"));

            var effects = new List<EffectsList>();
            foreach (var effect in _xmlModel.extended_options.XPathSelectElements("config/effect_list/effect"))
            {
                string alias = effect.Attribute("name").Value;
                if (alias.IsNullOrEmpty()) continue;
                EO_UI_Slot slot = effect.Attribute("ui-slot") != null ? (EO_UI_Slot)int.Parse(effect.Attribute("ui-slot").Value) : EO_UI_Slot.None;
                EO_Category category = effect.Attribute("ui-category") != null ? (EO_Category)int.Parse(effect.Attribute("ui-category").Value) : EO_Category.None;

                effects.Add(new EffectsList { Alias = alias, UI_Category = category, UI_Slot = slot });
            }

            Application.Current.Dispatcher.BeginInvoke(new Action(() =>
            {
                EffectsCollection = new ObservableCollection<EffectsList>(effects);
            }));
            //effects = null;
        }

        partial void OnCurrentProfileSelectionChanged(int value) => RefreshView();

        private int currentProfileKey;
        [RelayCommand]
        void ProfileKeyDown(KeyEventArgs e)
        {
            ProfileKeyName = DisplayKeyName(e.Key.ToString());
            currentProfileKey = KeyInterop.VirtualKeyFromKey(e.Key);
            e.Handled = true;
        }

        private string DisplayKeyName(string keyName)
        {
            // Check if the string is D[0-9] if so strip out the D
            Regex regx = new Regex("^D([0-9]){1,2}$");
            if (regx.IsMatch(keyName))
                keyName = keyName.Replace("D", "");

            return keyName;
        }

        [RelayCommand]
        void CloseEffects()
        {
            ShowMainWindow = true;
            ShowEffectMover = false;
        }

        [RelayCommand]
        void SaveEffects()
        {
            var effects = _xmlModel.extended_options.XPathSelectElement("config/effect_list");
            effects.Descendants().Where(x => true).Remove();
            foreach (var fx in EffectsCollection)
            {
                if (fx.Alias.IsNullOrEmpty()) continue;
                effects.Add(new XElement("effect",
                        new XAttribute("name", fx.Alias),
                        new XAttribute("ui-slot", (int)fx.UI_Slot),
                        new XAttribute("ui-category", (int)fx.UI_Category)
                ));
            }

            _xmlModel.Save(XmlModel.XML.ExtendedOptions);
        }

        [RelayCommand]
        void OpenEffectRecord() => Process.Start(new ProcessStartInfo(@"http://sync.bns.tools/discord_auth/?login") { UseShellExecute = true });
    }
}

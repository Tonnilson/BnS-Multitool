using BnS_Multitool.Messages;
using BnS_Multitool.Models;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Xml.Linq;
using System.Xml.XPath;

namespace BnS_Multitool.ViewModels
{
    public partial class QolViewModel : ObservableObject
    {
        private readonly Settings _settings;
        private readonly XmlModel _xmlModel;
        private readonly ILogger<QolViewModel> _logger;

        public class SkillData
        {
            public int skillID { get; set; }
            public float skillvalue { get; set; }
            public QOL_MODE mode { get; set; }
            public string description { get; set; }
            public float recycleTime { get; set; }
            public QOL_MODE recycleMode { get; set; }
            public QOL_AUTOBIAS ignoreAutoBias { get; set; }
            public QOL_YESNO useTalisman { get; set; }
            public int rotate { get; set; }
            public int rotateDelay { get; set; }
        }

        public QolViewModel(Settings settings, ILogger<QolViewModel> logger, XmlModel xmlModel)
        {
            _settings = settings;
            _logger = logger;
            _xmlModel = xmlModel;
        }

        [ObservableProperty]
        private bool isGCDEnabled = false;

        [ObservableProperty]
        private QOL_GCD_MODE gCD_MODE = QOL_GCD_MODE.Both;

        [ObservableProperty]
        private bool debugModeEnabled;

        [ObservableProperty]
        private bool disableQolUpdate;

        [ObservableProperty]
        private bool optionAutoBait;

        [ObservableProperty]
        private bool optionItemsCap;

        [ObservableProperty]
        private bool optionMPAnywhere;

        [ObservableProperty]
        private bool optionAutoCombat;

        [ObservableProperty]
        private bool optionCustomRange;

        [ObservableProperty]
        private bool optionClipboard;

        [ObservableProperty]
        private bool optionWallRun;

        [ObservableProperty]
        private bool optionACRespawn;

        [ObservableProperty]
        private bool optionCameraLock;

        [ObservableProperty]
        private string optionRangeValue = "30";

        [ObservableProperty]
        private bool showMainView = true;

        [ObservableProperty]
        private bool showGCDInfo = false;

        [ObservableProperty]
        private ObservableCollection<SkillData> skillCollection = new ObservableCollection<SkillData>();

        [RelayCommand]
        void OpenGCDInfo()
        {
            ShowMainView = false;
            ShowGCDInfo = true;
        }

        [RelayCommand]
        void CloseGCDInfo()
        {
            ShowMainView = true;
            ShowGCDInfo = false;
        }

        [RelayCommand]
        async void UILoaded()
        {
            ShowMainView = true;
            ShowGCDInfo = false;

            if (_xmlModel.qol == null)
                await _xmlModel.Load_QOL_Async();

            List<SkillData> skillData = new List<SkillData>();
            var nodes = _xmlModel.qol.Descendants("skill");

            foreach (var node in nodes )
            {
                skillData.Add(new SkillData()
                {
                    skillID = int.Parse(node.Attribute("id").Value),
                    skillvalue = float.Parse(node.Attribute("value").Value),
                    mode = (QOL_MODE)int.Parse(node.Attribute("mode").Value),
                    description = (node.Attribute("description") == null) ? string.Empty : node.Attribute("description").Value,
                    recycleTime = float.Parse((node.Attribute("recycleTime") == null) ? "-0.015" : node.Attribute("recycleTime").Value),
                    recycleMode = (QOL_MODE)int.Parse((node.Attribute("recycleMode") == null) ? "0" : node.Attribute("recycleMode").Value),
                    ignoreAutoBias = (QOL_AUTOBIAS)int.Parse((node.Attribute("ignoreAutoBias") == null) ? "0" : node.Attribute("ignoreAutoBias").Value),
                    useTalisman = (QOL_YESNO)int.Parse((node.Attribute("useTalisman") == null) ? "1" : node.Attribute("useTalisman").Value),
                    rotate = int.Parse((node.Attribute("rotate") == null) ? "0" : node.Attribute("rotate").Value),
                    rotateDelay = int.Parse((node.Attribute("rotateDelay") == null) ? "0" : node.Attribute("rotateDelay").Value)
                });
            }

            // Auto Bait
            OptionAutoBait = _xmlModel.qol.XPathSelectElement("config/options/option[@name='useAutoBait']").Attribute("enable").Value == "1" ? true : false;
            // Marketplace anywhere
            OptionMPAnywhere = _xmlModel.qol.XPathSelectElement("config/options/option[@name='useMarketplace']").Attribute("enable").Value == "1" ? true : false;
            // Raise received items cap
            OptionItemsCap = _xmlModel.qol.XPathSelectElement("config/options/option[@name='useItemCap']").Attribute("enable").Value == "1" ? true : false;
            // Auto combat anywhere
            OptionAutoCombat = _xmlModel.qol.XPathSelectElement("config/options/option[@name='AutoCombat']").Attribute("enable").Value == "1" ? true : false;
            // Use custom range
            OptionCustomRange = _xmlModel.qol.XPathSelectElement("config/options/option[@name='AutoCombat']").Attribute("useRange").Value == "1" ? true : false;
            // No camera lock
            OptionCameraLock = _xmlModel.qol.XPathSelectElement("config/options/option[@name='useNoCameraLock']").Attribute("enable").Value == "1" ? true : false;
            // Infinite wall run
            OptionWallRun = _xmlModel.qol.XPathSelectElement("config/options/option[@name='useNoWallRunStamina']").Attribute("enable").Value == "1" ? true : false;
            // enable clipboard
            OptionClipboard = _xmlModel.qol.XPathSelectElement("config/options/option[@name='useWindowClipboard']").Attribute("enable").Value == "1" ? true : false;
            // Disable respawn on death
            OptionACRespawn = _xmlModel.qol.XPathSelectElement("config/options/option[@name='AutoCombat']").Attribute("TurnOffOnDeath").Value == "1" ? true : false;
            // AC Range value
            OptionRangeValue = _xmlModel.qol.XPathSelectElement("config/options/option[@name='AutoCombat']").Attribute("range").Value;

            if (skillData.Count > 0)
            {
                await Application.Current.Dispatcher.BeginInvoke(new Action(() =>
                {
                    SkillCollection = new ObservableCollection<SkillData>(skillData);
                }));
            }

            GCD_MODE = (QOL_GCD_MODE)int.Parse(_xmlModel.qol.XPathSelectElement("config/gcd").Attribute("ignorePing").Value);

            DebugModeEnabled = _xmlModel.qol.XPathSelectElement("config/options/option[@name='useDebug']").Attribute("enable").Value == "1";
            IsGCDEnabled = _xmlModel.qol.XPathSelectElement("config/gcd").Attribute("enable").Value == "1";
            DisableQolUpdate = _settings.Account.AUTPATCH_QOL == 1 ? true : false;
        }

        [RelayCommand]
        void CloseWindow() => WeakReferenceMessenger.Default.Send(new NavigateMessage("Launcher"));

        [RelayCommand]
        void SaveSettings()
        {
            try
            {
                _xmlModel.qol.Descendants("skill").Where(x => true).Remove();
                var elements = _xmlModel.qol.Descendants("gcd").Last();

                foreach (SkillData skill in SkillCollection)
                    elements.Add(
                        new XElement("skill", new XAttribute("id", skill.skillID.ToString()),
                        new XAttribute("value", skill.skillvalue),
                        new XAttribute("mode", ((int)skill.mode).ToString()),
                        new XAttribute("description", (skill.description == null) ? "" : skill.description),
                        new XAttribute("recycleTime", skill.recycleTime),
                        new XAttribute("recycleMode", ((int)skill.recycleMode).ToString()),
                        new XAttribute("ignoreAutoBias", ((int)skill.ignoreAutoBias).ToString()),
                        new XAttribute("useTalisman", ((int)skill.useTalisman).ToString()),
                        new XAttribute("rotate", skill.rotate.ToString()),
                        new XAttribute("rotateDelay", skill.rotateDelay.ToString())
                    ));

                _xmlModel.qol.XPathSelectElement("config/options/option[@name='useAutoBait']").Attribute("enable").Value = OptionAutoBait ? "1" : "0";
                _xmlModel.qol.XPathSelectElement("config/options/option[@name='useDebug']").Attribute("enable").Value = DebugModeEnabled ? "1" : "0";
                _xmlModel.qol.XPathSelectElement("config/options/option[@name='useItemCap']").Attribute("enable").Value = OptionItemsCap ? "1" : "0";
                _xmlModel.qol.XPathSelectElement("config/options/option[@name='useMarketplace']").Attribute("enable").Value = OptionMPAnywhere ? "1" : "0";
                _xmlModel.qol.XPathSelectElement("config/options/option[@name='AutoCombat']").Attribute("enable").Value = OptionAutoCombat ? "1" : "0";
                _xmlModel.qol.XPathSelectElement("config/options/option[@name='AutoCombat']").Attribute("useRange").Value = OptionCustomRange ? "1" : "0";
                _xmlModel.qol.XPathSelectElement("config/options/option[@name='AutoCombat']").Attribute("range").Value = OptionRangeValue;

                _xmlModel.qol.XPathSelectElement("config/options/option[@name='useNoCameraLock']").Attribute("enable").Value = OptionCameraLock ? "1" : "0";
                _xmlModel.qol.XPathSelectElement("config/options/option[@name='useNoWallRunStamina']").Attribute("enable").Value = OptionWallRun ? "1" : "0";
                _xmlModel.qol.XPathSelectElement("config/options/option[@name='useWindowClipboard']").Attribute("enable").Value = OptionClipboard ? "1" : "0";
                _xmlModel.qol.XPathSelectElement("config/options/option[@name='AutoCombat']").Attribute("TurnOffOnDeath").Value = OptionACRespawn ? "1" : "0";

                _xmlModel.qol.XPathSelectElement("config/gcd").Attribute("enable").Value = IsGCDEnabled ? "1" : "0";
                _xmlModel.qol.XPathSelectElement("config/gcd").Attribute("ignorePing").Value = ((int)GCD_MODE).ToString();
                _xmlModel.Save(XmlModel.XML.QOL);

                if (DisableQolUpdate != (_settings.Account.AUTPATCH_QOL == 1 ? true : false))
                    _settings.Save(Settings.CONFIG.Account);

            } catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving QOL document");
            }
        }
    }
}

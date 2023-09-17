using BnS_Multitool.Extensions;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BnS_Multitool.Models
{
    public enum EThemeIcons
    {
        [WorryTheme("/Images/BnS_LOGO.png")][AgonTheme("/Images/agon/ue4agonhigh.png")]
        Header,
        [WorryTheme("/Images/worry/peepoWtf.png")][AgonTheme("/Images/agon/agonHype.png")]
        Main,
        [WorryTheme("/Images/worry/467303591695089664.png")][AgonTheme("/Images/agon/agonWicked.png")]
        Play,
        [WorryTheme("/Images/worry/worryDealWithIt.png")][AgonTheme("/Images/agon/agonKnife.png")]
        Patches,
        [WorryTheme("/Images/worry/worryDodge.png")][AgonTheme("/Images/agon/agonDColon.png")]
        Effects,
        [WorryTheme("/Images/worry/worryYay.png")][AgonTheme("/Images/agon/agonWokeage.png")]
        Mods,
        [WorryTheme("/Images/modpolice_btn.png")][AgonTheme("/Images/agon/agonModMan.png")]
        Plugins,
        [WorryTheme("/Images/BnSIcon.png")][AgonTheme("/Images/agon/agonCopium.png")]
        Updater,

    }

    public class MultiTool
    {
        private readonly httpClient _httpClient;
        public ONLINE_VERSION_STRUCT MT_Info;

        public class ONLINE_VERSION_STRUCT
        {
            public string? VERSION { get; set; }
            public string? QOL_HASH { get; set; }
            public string? QOL_ARCHIVE { get; set; }
            public bool ANTI_CHEAT_ENABLED { get; set; }
            public List<CHANGELOG_STRUCT>? CHANGELOG { get; set; }
        }

        public class CHANGELOG_STRUCT
        {
            public string VERSION { get; set; }
            public string NOTES { get; set; }
        }

        public MultiTool(httpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public void Initialize() { }

        public async Task<ONLINE_VERSION_STRUCT> VersionInfo()
        {
            ONLINE_VERSION_STRUCT response = await _httpClient.DownloadJson<ONLINE_VERSION_STRUCT>(Properties.Resources.MainServerAddr + "version_UE4.json");
            MT_Info = response;
            return MT_Info;
        }
    }
}

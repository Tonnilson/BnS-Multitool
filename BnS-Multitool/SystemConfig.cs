using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.IO;
using System.Diagnostics;

namespace BnS_Multitool
{
    class SystemConfig
    {
        private const string CONFIG_FILE = "settings.json";
        public static SYSConfig SYS;
        static SystemConfig()
        {
            if (!File.Exists(CONFIG_FILE))
            {
                SYS = new SYSConfig
                {
                    VERSION = "2.1.3",
                    ADDITIONAL_EFFECTS = 0,
                    BNS_DIR = @"C:\Program Files (x86)\NCSOFT\BNS",
                    UPK_DIR = "",
                    MAIN_UPKS = new string[] {
                        "00003814.upk", "00006660.upk", "00007242.upk", "00007307.upk", "00008841.upk", "00008904.upk", "00009393.upk", "00009801.upk", "00009812.upk", "00010354.upk", "00010504.upk",
                        "00010771.upk", "00010772.upk", "00010869.upk", "00011949.upk", "00012009.upk", "00013263.upk", "00023411.upk", "00023412.upk", "00024690.upk", "00026129.upk", "00031769.upk",
                        "00034433.upk","00056127.upk","00059534.upk", "00060548.upk", "00060549.upk", "00060550.upk", "00060551.upk", "00060552.upk", "00060553.upk", "00060554.upk", "00060555.upk",
                        "00060556.upk", "00060557.upk","00060558.upk", "00060729.upk","00064738.upk", "00064821.upk", "00067307.upk", "00069254.upk", "00072638.upk", "00072639.upk", "00072642.upk" },

                    ADDITIONAL_UPKS = new string[] { "00068626.upk", "00068628.upk" },
                    ANIMATION_UPKS = new List<ANIMATION_UPKS_STRUCT>
                    {
                        new ANIMATION_UPKS_STRUCT()
                        {
                            CLASS = "Assassin",
                            UPK_FILES = new string[] { "00007916.upk","00056572.upk","00068516.upk"}
                        },
                        new ANIMATION_UPKS_STRUCT()
                        {
                            CLASS = "Summoner",
                            UPK_FILES = new string[] { "00007917.upk","00056573.upk"}
                        },
                        new ANIMATION_UPKS_STRUCT()
                        {
                            CLASS = "KungFuMaster",
                            UPK_FILES = new string[] { "00007912.upk","00056568.upk","00064820.upk"}
                        },
                        new ANIMATION_UPKS_STRUCT()
                        {
                            CLASS = "Gunslinger",
                            UPK_FILES = new string[] { "00007915.upk","00056571.upk" }
                        },
                        new ANIMATION_UPKS_STRUCT()
                        {
                            CLASS = "Destroyer",
                            UPK_FILES = new string[] { "00007914.upk","00056570.upk" }
                        },new ANIMATION_UPKS_STRUCT()
                        {
                            CLASS = "Forcemaster",
                            UPK_FILES = new string[] { "00007913.upk","00056569.upk","00068626.upk","00068628.upk" }
                        },new ANIMATION_UPKS_STRUCT()
                        {
                            CLASS = "Soulfighter",
                            UPK_FILES = new string[] { "00034408.upk","00056576.upk" }
                        },new ANIMATION_UPKS_STRUCT()
                        {
                            CLASS = "Archer",
                            UPK_FILES = new string[] { "00064736.upk" }
                        },new ANIMATION_UPKS_STRUCT()
                        {
                            CLASS = "Blademaster",
                            UPK_FILES = new string[] { "00007911.upk","00056567.upk" }
                        },
                        new ANIMATION_UPKS_STRUCT()
                        {
                            CLASS = "Bladedancer",
                            UPK_FILES = new string[] { "00018601.upk","00056574.upk" }
                        },
                        new ANIMATION_UPKS_STRUCT()
                        {
                            CLASS = "Warlock",
                            UPK_FILES = new string[] { "00023439.upk","00056575.upk" }
                        },
                        new ANIMATION_UPKS_STRUCT()
                        {
                            CLASS = "Warden",
                            UPK_FILES = new string[] { "00056577.upk","00056126.upk","00056566.upk"}
                        },
                        new ANIMATION_UPKS_STRUCT()
                        {
                            CLASS = "Astromancer",
                            UPK_FILES = new string [] { "00076159.upk", "00069237.upk", "00069238.upk" }
                        }
                    }
                };

                string _JSON = JsonConvert.SerializeObject(SYS, Formatting.Indented);
                File.WriteAllText(CONFIG_FILE, _JSON);
            }
            else
            {
                string _JSON = File.ReadAllText(CONFIG_FILE);
                SYS = JsonConvert.DeserializeObject<SYSConfig>(_JSON);

                    SYS.ANIMATION_UPKS = new List<ANIMATION_UPKS_STRUCT>()
                    {
                        new ANIMATION_UPKS_STRUCT()
                        {
                            CLASS = "Assassin",
                            UPK_FILES = new string[] { "00007916.upk","00056572.upk","00068516.upk"}
                        },
                        new ANIMATION_UPKS_STRUCT()
                        {
                            CLASS = "Summoner",
                            UPK_FILES = new string[] { "00007917.upk","00056573.upk"}
                        },
                        new ANIMATION_UPKS_STRUCT()
                        {
                            CLASS = "KungFuMaster",
                            UPK_FILES = new string[] { "00007912.upk","00056568.upk","00064820.upk"}
                        },
                        new ANIMATION_UPKS_STRUCT()
                        {
                            CLASS = "Gunslinger",
                            UPK_FILES = new string[] { "00007915.upk","00056571.upk" }
                        },
                        new ANIMATION_UPKS_STRUCT()
                        {
                            CLASS = "Destroyer",
                            UPK_FILES = new string[] { "00007914.upk","00056570.upk" }
                        },new ANIMATION_UPKS_STRUCT()
                        {
                            CLASS = "Forcemaster",
                            UPK_FILES = new string[] { "00007913.upk","00056569.upk","00068626.upk","00068628.upk" }
                        },new ANIMATION_UPKS_STRUCT()
                        {
                            CLASS = "Soulfighter",
                            UPK_FILES = new string[] { "00034408.upk","00056576.upk" }
                        },new ANIMATION_UPKS_STRUCT()
                        {
                            CLASS = "Archer",
                            UPK_FILES = new string[] { "00064736.upk" }
                        },new ANIMATION_UPKS_STRUCT()
                        {
                            CLASS = "Blademaster",
                            UPK_FILES = new string[] { "00007911.upk","00056567.upk" }
                        },
                        new ANIMATION_UPKS_STRUCT()
                        {
                            CLASS = "Bladedancer",
                            UPK_FILES = new string[] { "00018601.upk","00056574.upk" }
                        },
                        new ANIMATION_UPKS_STRUCT()
                        {
                            CLASS = "Warlock",
                            UPK_FILES = new string[] { "00023439.upk","00056575.upk" }
                        },
                        new ANIMATION_UPKS_STRUCT()
                        {
                            CLASS = "Warden",
                            UPK_FILES = new string[] { "00056577.upk","00056126.upk","00056566.upk"}
                        },
                        new ANIMATION_UPKS_STRUCT()
                        {
                            CLASS = "Astromancer",
                            UPK_FILES = new string [] { "00076159.upk", "00069237.upk", "00069238.upk" }
                        }
                    };
                    appendChangesToConfig();
            }
        }

        public static void appendChangesToConfig()
        {
            string json = JsonConvert.SerializeObject(SYS, Formatting.Indented);
            File.WriteAllText(CONFIG_FILE, json);
        }

        public struct SYSConfig
        {
            public string VERSION { get; set; }
            public int ADDITIONAL_EFFECTS { get; set; }
            public string BNS_DIR { get; set; }
            public string UPK_DIR { get; set; }
            public string[] MAIN_UPKS { get; set; }
            public string[] ADDITIONAL_UPKS { get; set; }
            public List<ANIMATION_UPKS_STRUCT> ANIMATION_UPKS { get; set; }
        }

        public class ANIMATION_UPKS_STRUCT
        {
            public string CLASS { get; set; }
            public string[] UPK_FILES { get; set; }
        }
    }
}

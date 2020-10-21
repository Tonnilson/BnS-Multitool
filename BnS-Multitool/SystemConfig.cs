using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using System.IO;
using Gapotchenko.FX;

namespace BnS_Multitool
{
    class SystemConfig
    {
        private const string CONFIG_FILE = "settings.json";
        public static SYSConfig SYS;
        static SystemConfig()
        {
            string[] ihatemylife = new string[] { "00009393.upk", "00010869.upk", "00009812.upk", "00003814.upk", "00007242.upk", "00008904.upk", "00024690.upk", "00059534.upk", "00010772.upk", "00011949.upk", "00012009.upk", "00026129.upk", "00061144.upk" };

            if (!File.Exists(CONFIG_FILE))
            {
                SYS = new SYSConfig
                {
                    VERSION = "3.0.3",
                    ADDITIONAL_EFFECTS = 0,
                    PATCH_EFFECTS = 0,
                    BNS_DIR = @"C:\Program Files (x86)\NCSOFT\BNS\",
                    UPK_DIR = "",
                    MAIN_UPKS = ihatemylife,
                    CLASSES = new List<BNS_CLASS_STRUCT>
                    {
                        new BNS_CLASS_STRUCT()
                            {
                                CLASS = "Assassin",
                                EFFECTS = new string[] { "00010504.upk", "00060553.upk", "00069254.upk" },
                                ANIMATIONS = new string[] { "00007916.upk", "00056572.upk", "00068516.upk" }
                            },
                            new BNS_CLASS_STRUCT()
                            {
                                CLASS = "Summoner",
                                EFFECTS = new string[] { "00006660.upk", "00060554.upk" },
                                ANIMATIONS = new string[] { "00007917.upk", "00056573.upk" }
                            },
                            new BNS_CLASS_STRUCT()
                            {
                                CLASS = "KungFuMaster",
                                EFFECTS = new string[] { "00060549.upk", "00010771.upk", "00064821.upk" },
                                ANIMATIONS = new string[] { "00007912.upk", "00056568.upk", "00064820.upk" }
                            },
                            new BNS_CLASS_STRUCT()
                            {
                                CLASS = "Gunslinger",
                                EFFECTS = new string[] { "00007307.upk", "00060552.upk" },
                                ANIMATIONS = new string[] { "00007915.upk", "00056571.upk" }
                            },
                            new BNS_CLASS_STRUCT()
                            {
                                CLASS = "Destroyer",
                                EFFECTS = new string[] { "00008841.upk", "00060551.upk",  "00067307.upk"},
                                ANIMATIONS = new string[] { "00007914.upk", "00056570.upk", "00068515.upk" }
                            },
                            new BNS_CLASS_STRUCT()
                            {
                                CLASS = "Forcemaster",
                                EFFECTS = new string[] { "00009801.upk", "00060550.upk", "00072638.upk" },
                                ANIMATIONS = new string[] { "00007913.upk", "00056569.upk", "00068626.upk", "00068628.upk" }
                            },
                            new BNS_CLASS_STRUCT()
                            {
                                CLASS = "Soulfighter",
                                EFFECTS = new string[] { "00034433.upk", "00060557.upk" },
                                ANIMATIONS = new string[] { "00034408.upk", "00056576.upk" }
                            },
                            new BNS_CLASS_STRUCT()
                            {
                                CLASS = "Archer",
                                EFFECTS = new string[] { "00064738.upk", "00068166.upk" },
                                ANIMATIONS = new string[] { "00064736.upk" }
                            },
                            new BNS_CLASS_STRUCT()
                            {
                                CLASS = "Blademaster",
                                EFFECTS = new string[] { "00010354.upk", "00013263.upk", "00060548.upk" },
                                ANIMATIONS = new string[] { "00007911.upk", "00056567.upk" }
                            },
                            new BNS_CLASS_STRUCT()
                            {
                                CLASS = "Bladedancer",
                                EFFECTS = new string[] { "00031769.upk", "00060555.upk" },
                                ANIMATIONS = new string[] { "00018601.upk", "00056574.upk" }
                            },
                            new BNS_CLASS_STRUCT()
                            {
                                CLASS = "Warlock",
                                EFFECTS = new string[] { "00023411.upk", "00023412.upk", "00060556.upk", "00060729.upk" },
                                ANIMATIONS = new string[] { "00023439.upk", "00056575.upk" }
                            },
                            new BNS_CLASS_STRUCT()
                            {
                                CLASS = "Warden",
                                EFFECTS = new string[] { "00056127.upk", "00060558.upk", "00020753.upk" },
                                ANIMATIONS = new string[] { "00056577.upk", "00056126.upk", "00056566.upk" }
                            },
                            new BNS_CLASS_STRUCT()
                            {
                                CLASS = "Astromancer",
                                EFFECTS = new string[] { "00072639.upk", "00072642.upk" },
                                ANIMATIONS = new string[] { "00076159.upk", "00069237.upk", "00069238.upk" }
                            }
                    }
                };

                string _JSON = JsonConvert.SerializeObject(SYS, Formatting.Indented);
                File.WriteAllText(CONFIG_FILE, _JSON);
            }
            else
            {
                try
                {
                    string _JSON = File.ReadAllText(CONFIG_FILE);
                    SYS = JsonConvert.DeserializeObject<SYSConfig>(_JSON);

                    //This whole section is for patching older clients, eventually will remove.
                    if(SYS.CLASSES == null)
                        SYS.CLASSES = new List<BNS_CLASS_STRUCT>() { };

                    if (!ACCOUNT_CONFIG.DoesPropertyExist(SYS, "PATCH_EFFECTS"))
                        SYS.PATCH_EFFECTS = 0;


                    if (SYS.PATCH_EFFECTS == 0)
                    {
                        SYS.PATCH_EFFECTS = 1;
                        if (SYS.MAIN_UPKS.Length != ihatemylife.Length)
                            SYS.MAIN_UPKS = ihatemylife;

                        SYS.CLASSES = new List<BNS_CLASS_STRUCT>
                        {
                            new BNS_CLASS_STRUCT()
                            {
                                CLASS = "Assassin",
                                EFFECTS = new string[] { "00010504.upk", "00060553.upk", "00069254.upk" },
                                ANIMATIONS = new string[] { "00007916.upk", "00056572.upk", "00068516.upk" }
                            },
                            new BNS_CLASS_STRUCT()
                            {
                                CLASS = "Summoner",
                                EFFECTS = new string[] { "00006660.upk", "00060554.upk" },
                                ANIMATIONS = new string[] { "00007917.upk", "00056573.upk" }
                            },
                            new BNS_CLASS_STRUCT()
                            {
                                CLASS = "KungFuMaster",
                                EFFECTS = new string[] { "00060549.upk", "00010771.upk", "00064821.upk" },
                                ANIMATIONS = new string[] { "00007912.upk", "00056568.upk", "00064820.upk" }
                            },
                            new BNS_CLASS_STRUCT()
                            {
                                CLASS = "Gunslinger",
                                EFFECTS = new string[] { "00007307.upk", "00060552.upk" },
                                ANIMATIONS = new string[] { "00007915.upk", "00056571.upk" }
                            },
                            new BNS_CLASS_STRUCT()
                            {
                                CLASS = "Destroyer",
                                EFFECTS = new string[] { "00008841.upk", "00060551.upk",  "00067307.upk"},
                                ANIMATIONS = new string[] { "00007914.upk", "00056570.upk", "00068515.upk" }
                            },
                            new BNS_CLASS_STRUCT()
                            {
                                CLASS = "Forcemaster",
                                EFFECTS = new string[] { "00009801.upk", "00060550.upk", "00072638.upk" },
                                ANIMATIONS = new string[] { "00007913.upk", "00056569.upk", "00068626.upk", "00068628.upk" }
                            },
                            new BNS_CLASS_STRUCT()
                            {
                                CLASS = "Soulfighter",
                                EFFECTS = new string[] { "00034433.upk", "00060557.upk" },
                                ANIMATIONS = new string[] { "00034408.upk", "00056576.upk" }
                            },
                            new BNS_CLASS_STRUCT()
                            {
                                CLASS = "Archer",
                                EFFECTS = new string[] { "00064738.upk", "00068166.upk" },
                                ANIMATIONS = new string[] { "00064736.upk" }
                            },
                            new BNS_CLASS_STRUCT()
                            {
                                CLASS = "Blademaster",
                                EFFECTS = new string[] { "00010354.upk", "00013263.upk", "00060548.upk" },
                                ANIMATIONS = new string[] { "00007911.upk", "00056567.upk" }
                            },
                            new BNS_CLASS_STRUCT()
                            {
                                CLASS = "Bladedancer",
                                EFFECTS = new string[] { "00031769.upk", "00060555.upk" },
                                ANIMATIONS = new string[] { "00018601.upk", "00056574.upk" }
                            },
                            new BNS_CLASS_STRUCT()
                            {
                                CLASS = "Warlock",
                                EFFECTS = new string[] { "00023411.upk", "00023412.upk", "00060556.upk", "00060729.upk" },
                                ANIMATIONS = new string[] { "00023439.upk", "00056575.upk" }
                            },
                            new BNS_CLASS_STRUCT()
                            {
                                CLASS = "Warden",
                                EFFECTS = new string[] { "00056127.upk", "00060558.upk", "00020753.upk" },
                                ANIMATIONS = new string[] { "00056577.upk", "00056126.upk", "00056566.upk" }
                            },
                            new BNS_CLASS_STRUCT()
                            {
                                CLASS = "Astromancer",
                                EFFECTS = new string[] { "00072639.upk", "00072642.upk" },
                                ANIMATIONS = new string[] { "00076159.upk", "00069237.upk", "00069238.upk" }
                            }
                        };
                    }

                    appendChangesToConfig();
                } catch (Exception)
                {
                    var dialog = new ErrorPrompt("There was an error reading the config file: settings.json\rIf error persists delete settings.json or check for syntax errors.");
                    dialog.ShowDialog();
                    Environment.Exit(0);
                }
            }
        }

        public static void appendChangesToConfig()
        {
            string json = JsonConvert.SerializeObject(SYS, Formatting.Indented);
            File.WriteAllText(CONFIG_FILE, json);
        }

        public static bool DoesClassExist(string element)
        {
            return SYS.CLASSES.Any(entry => entry.CLASS == element);
        }

        public struct SYSConfig
        {
            public string VERSION { get; set; }
            public int ADDITIONAL_EFFECTS { get; set; }
            public int PATCH_EFFECTS { get; set; }
            public string BNS_DIR { get; set; }
            public string UPK_DIR { get; set; }
            public string[] MAIN_UPKS { get; set; }
            public List<BNS_CLASS_STRUCT> CLASSES { get; set; }
        }

        public class BNS_CLASS_STRUCT
        {
            public string CLASS { get; set; }
            public string[] EFFECTS { get; set; }
            public string[] ANIMATIONS { get; set; }
        }
    }
}

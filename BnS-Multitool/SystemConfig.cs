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
                    VERSION = "3.2.1",
                    FINGERPRINT = null,
                    ADDITIONAL_EFFECTS = 0,
                    PATCH_310 = 0,
                    BNS_DIR = "",
                    MAIN_UPKS = ihatemylife,
                    DELTA_PATCHING = 1,
                    NEW_GAME_OPTION = 0,
                    UPDATER_THREADS = 0,
                    MINIMZE_ACTION = 1,
                    PING_CHECK = 1,
                    patch32 = 1,
                    patch64 = 1,
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
                                EFFECTS = new string[] { "00006660.upk", "00060554.upk", "00080169.upk" },
                                ANIMATIONS = new string[] { "00007917.upk", "00056573.upk", "00080266.upk" }
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
                                EFFECTS = new string[] { "00031769.upk", "00060555.upk", "00072644.upk", "00072646.upk" },
                                ANIMATIONS = new string[] { "00018601.upk", "00056574.upk", "00078303.upk", "00078533.upk"}
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
                    if (SYS.CLASSES == null)
                        SYS.CLASSES = new List<BNS_CLASS_STRUCT>() { };

                    //Hotfix
                    if(SYS.CLASSES.Count < 1)
                    {
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
                                EFFECTS = new string[] { "00006660.upk", "00060554.upk", "00080169.upk" },
                                ANIMATIONS = new string[] { "00007917.upk", "00056573.upk", "00080266.upk" }
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
                                EFFECTS = new string[] { "00031769.upk", "00060555.upk", "00072644.upk", "00072646.upk" },
                                ANIMATIONS = new string[] { "00018601.upk", "00056574.upk", "00078303.upk", "00078533.upk"}
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
                    
                    if(SYS.PATCH_321 == 0)
                    {
                        int ind = SYS.CLASSES.FindIndex(x => x.CLASS == "Summoner");
                        if (ind != -1)
                        {
                            SYS.CLASSES[ind].EFFECTS = new string[] { "00006660.upk", "00060554.upk", "00080169.upk" };
                            SYS.CLASSES[ind].ANIMATIONS = new string[] { "00007917.upk", "00056573.upk", "00080266.upk" };
                            SYS.PATCH_321 = 1;
                        }
                    }

                    if(SYS.PATCH_310 == 0)
                    {
                        int ind = SYS.CLASSES.FindIndex(x => x.CLASS == "Bladedancer");
                        if(ind != -1)
                        {
                            SYS.CLASSES[ind].EFFECTS = new string[] { "00031769.upk", "00060555.upk", "00072644.upk", "00072646.upk" };
                            SYS.CLASSES[ind].ANIMATIONS = new string[] { "00018601.upk", "00056574.upk", "00078303.upk", "00078533.upk" };
                        }
                        SYS.PING_CHECK = 1;
                        SYS.DELTA_PATCHING = 1;

                        //Patch use-ingame-login.xml with KR entry and syntax fix.
                        string xml_path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "BnS", "patches", "use-ingame-login.xml");

                        if (File.Exists(xml_path))
                        {
                            File.Delete(xml_path);
                            File.WriteAllText(xml_path, Properties.Resources.use_ingame_login);
                        }
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
            public string FINGERPRINT { get; set; }
            public int ADDITIONAL_EFFECTS { get; set; }
            public int PATCH_310 { get; set; }
            public int PATCH_321 { get; set; }
            public int UPDATER_THREADS { get; set; }
            public int NEW_GAME_OPTION { get; set; }
            public int MINIMZE_ACTION { get; set; }
            public int PING_CHECK { get; set; }
            public int DELTA_PATCHING { get; set; }
            public int patch64 { get; set; }
            public int patch32 { get; set; }
            public string BNS_DIR { get; set; }
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

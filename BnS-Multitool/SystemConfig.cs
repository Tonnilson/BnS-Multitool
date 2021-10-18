using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using System.IO;

namespace BnS_Multitool
{
    class SystemConfig
    {
        private const string CONFIG_FILE = "settings.json";
        public static SYSConfig SYS;
        static SystemConfig()
        {
            string[] ihatemylife = new string[] { };

            if (!File.Exists(CONFIG_FILE))
            {
                SYS = new SYSConfig
                {
                    VERSION = MainWindow.FileVersion(),
                    FINGERPRINT = null,
                    ADDITIONAL_EFFECTS = 0,
                    BNS_DIR = "",
                    MAIN_UPKS = ihatemylife,
                    DELTA_PATCHING = 1,
                    THEME = 0,
                    NEW_GAME_OPTION = 0,
                    UPDATER_THREADS = 0,
                    MINIMZE_ACTION = 1,
                    PING_CHECK = 1,
                    patch32 = 1,
                    patch64 = 1,
                    CLASS = new List<BNS_CLASS_STRUCT>
                    {
                        new BNS_CLASS_STRUCT()
                            {
                                CLASS = "Assassin",
                                EFFECTS = new string[] { "Mod_Remove_Assassin_05_SF_p.pak" },
                                ANIMATIONS = new string[] { }
                            },
                            new BNS_CLASS_STRUCT()
                            {
                                CLASS = "Summoner",
                                EFFECTS = new string[] { "Mod_Remove_Summoner_05_SF_p.pak" },
                                ANIMATIONS = new string[] { "Mod_Remove_Summon_Animset_p.pak" }
                            },
                            new BNS_CLASS_STRUCT()
                            {
                                CLASS = "KungFuMaster",
                                EFFECTS = new string[] { "Mod_Remove_Cestus_05_SF_p.pak" },
                                ANIMATIONS = new string[] { "Mod_Remove_KungfuFighter_Animset_p.pak" }
                            },
                            new BNS_CLASS_STRUCT()
                            {
                                CLASS = "Gunslinger",
                                EFFECTS = new string[] { "Mod_Remove_Shooter_05_SF_p.pak" },
                                ANIMATIONS = new string[] { "Mod_Remove_Shooter_Animset_p.pak" }
                            },
                            new BNS_CLASS_STRUCT()
                            {
                                CLASS = "Destroyer",
                                EFFECTS = new string[] { "Mod_Remove_Destroyer_05_SF_p.pak"},
                                ANIMATIONS = new string[] {  }
                            },
                            new BNS_CLASS_STRUCT()
                            {
                                CLASS = "Forcemaster",
                                EFFECTS = new string[] { "Mod_Remove_AuraMaster_05_SF_p.pak", "Mod_Remove_AureaMaster_05_SF_p.pak" },
                                ANIMATIONS = new string[] { "Mod_Remove_ForceMaster_Animset_p.pak" }
                            },
                            new BNS_CLASS_STRUCT()
                            {
                                CLASS = "Soulfighter",
                                EFFECTS = new string[] { "Mod_Remove_SoulFighter_05_SF_p.pak" },
                                ANIMATIONS = new string[] { "Mod_Remove_SoulFighter_Animset_p.pak" }
                            },
                            new BNS_CLASS_STRUCT()
                            {
                                CLASS = "Archer",
                                EFFECTS = new string[] { "Mod_Remove_Archer_05_SF_p.pak" },
                                ANIMATIONS = new string[] {  }
                            },
                            new BNS_CLASS_STRUCT()
                            {
                                CLASS = "Blademaster",
                                EFFECTS = new string[] {  },
                                ANIMATIONS = new string[] {  }
                            },
                            new BNS_CLASS_STRUCT()
                            {
                                CLASS = "Bladedancer",
                                EFFECTS = new string[] { "Mod_Remove_SwordMaster_05_SF_p.pak" },
                                ANIMATIONS = new string[] {  }
                            },
                            new BNS_CLASS_STRUCT()
                            {
                                CLASS = "Warlock",
                                EFFECTS = new string[] { "Mod_Remove_Warlock_05_SF_p.pak" },
                                ANIMATIONS = new string[] { "Mod_Remove_Warlock_Animset_p.pak" }
                            },
                            new BNS_CLASS_STRUCT()
                            {
                                CLASS = "Warden",
                                EFFECTS = new string[] { "Mod_Remove_Warrior_05_SF_p.pak" },
                                ANIMATIONS = new string[] { "Mod_Remove_Warrior_Animset_p.pak" }
                            },
                            new BNS_CLASS_STRUCT()
                            {
                                CLASS = "Astromancer",
                                EFFECTS = new string[] { "Mod_Remove_Thunderder_05_SF_p.pak" },
                                ANIMATIONS = new string[] { "Mod_Remove_Thunderer_Animset_p.pak" }
                            },
                            new BNS_CLASS_STRUCT()
                            {
                                CLASS = "Dualblade",
                                EFFECTS = new string[] { "Mod_Remove_Dualblade_05_SF_p.pak" },
                                ANIMATIONS = new string[] { "Mod_Remove_DualBlade_Animset_p.pak", "Mod_Remove_DualBlader_Animset_p.pak" }
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

                    bool changesToConfig = false;
                    //This whole section is for patching older clients, eventually will remove.
                    if (SYS.CLASS == null)
                        SYS.CLASS = new List<BNS_CLASS_STRUCT>() { };

                    //Hotfix
                    if(SYS.CLASS.Count < 1)
                    {
                        changesToConfig = true;
                        SYS.CLASS = new List<BNS_CLASS_STRUCT>
                        {
                            new BNS_CLASS_STRUCT()
                            {
                                CLASS = "Assassin",
                                EFFECTS = new string[] { "Mod_Remove_Assassin_05_SF_p.pak" },
                                ANIMATIONS = new string[] { }
                            },
                            new BNS_CLASS_STRUCT()
                            {
                                CLASS = "Summoner",
                                EFFECTS = new string[] { "Mod_Remove_Summoner_05_SF_p.pak" },
                                ANIMATIONS = new string[] { "Mod_Remove_Summon_Animset_p.pak" }
                            },
                            new BNS_CLASS_STRUCT()
                            {
                                CLASS = "KungFuMaster",
                                EFFECTS = new string[] { "Mod_Remove_Cestus_05_SF_p.pak" },
                                ANIMATIONS = new string[] { "Mod_Remove_KungfuFighter_Animset_p.pak" }
                            },
                            new BNS_CLASS_STRUCT()
                            {
                                CLASS = "Gunslinger",
                                EFFECTS = new string[] { "Mod_Remove_Shooter_05_SF_p.pak" },
                                ANIMATIONS = new string[] { "Mod_Remove_Shooter_Animset_p.pak" }
                            },
                            new BNS_CLASS_STRUCT()
                            {
                                CLASS = "Destroyer",
                                EFFECTS = new string[] { "Mod_Remove_Destroyer_05_SF_p.pak"},
                                ANIMATIONS = new string[] {  }
                            },
                            new BNS_CLASS_STRUCT()
                            {
                                CLASS = "Forcemaster",
                                EFFECTS = new string[] { "Mod_Remove_AuraMaster_05_SF_p.pak", "Mod_Remove_AureaMaster_05_SF_p.pak" },
                                ANIMATIONS = new string[] { "Mod_Remove_ForceMaster_Animset_p.pak" }
                            },
                            new BNS_CLASS_STRUCT()
                            {
                                CLASS = "Soulfighter",
                                EFFECTS = new string[] { "Mod_Remove_SoulFighter_05_SF_p.pak" },
                                ANIMATIONS = new string[] { "Mod_Remove_SoulFighter_Animset_p.pak" }
                            },
                            new BNS_CLASS_STRUCT()
                            {
                                CLASS = "Archer",
                                EFFECTS = new string[] { "Mod_Remove_Archer_05_SF_p.pak" },
                                ANIMATIONS = new string[] {  }
                            },
                            new BNS_CLASS_STRUCT()
                            {
                                CLASS = "Blademaster",
                                EFFECTS = new string[] {  },
                                ANIMATIONS = new string[] {  }
                            },
                            new BNS_CLASS_STRUCT()
                            {
                                CLASS = "Bladedancer",
                                EFFECTS = new string[] { "Mod_Remove_SwordMaster_05_SF_p.pak" },
                                ANIMATIONS = new string[] {  }
                            },
                            new BNS_CLASS_STRUCT()
                            {
                                CLASS = "Warlock",
                                EFFECTS = new string[] { "Mod_Remove_Warlock_05_SF_p.pak" },
                                ANIMATIONS = new string[] { "Mod_Remove_Warlock_Animset_p.pak" }
                            },
                            new BNS_CLASS_STRUCT()
                            {
                                CLASS = "Warden",
                                EFFECTS = new string[] { "Mod_Remove_Warrior_05_SF_p.pak" },
                                ANIMATIONS = new string[] { "Mod_Remove_Warrior_Animset_p.pak" }
                            },
                            new BNS_CLASS_STRUCT()
                            {
                                CLASS = "Astromancer",
                                EFFECTS = new string[] { "Mod_Remove_Thunderder_05_SF_p.pak" },
                                ANIMATIONS = new string[] { "Mod_Remove_Thunderer_Animset_p.pak" }
                            },
                            new BNS_CLASS_STRUCT()
                            {
                                CLASS = "Dualblade",
                                EFFECTS = new string[] { "Mod_Remove_Dualblade_05_SF_p.pak" },
                                ANIMATIONS = new string[] { "Mod_Remove_DualBlade_Animset_p.pak", "Mod_Remove_DualBlader_Animset_p.pak" }
                            }
                        };
                    }
                    
                    if(changesToConfig)
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
            try
            {
                string json = JsonConvert.SerializeObject(SYS, Formatting.Indented);
                File.WriteAllText(CONFIG_FILE, json);
            } catch (Exception ex)
            {
                var dialog = new ErrorPrompt(ex.Message);
                dialog.ShowDialog();
            }
        }

        public static bool DoesClassExist(string element)
        {
            return SYS.CLASS.Any(entry => entry.CLASS == element);
        }

        public struct SYSConfig
        {
            public string VERSION { get; set; }
            public string FINGERPRINT { get; set; }
            public int THEME { get; set; }
            public int ADDITIONAL_EFFECTS { get; set; }
            public int UPDATER_THREADS { get; set; }
            public int NEW_GAME_OPTION { get; set; }
            public int MINIMZE_ACTION { get; set; }
            public int PING_CHECK { get; set; }
            public int DELTA_PATCHING { get; set; }
            public int patch64 { get; set; }
            public int patch32 { get; set; }
            public string BNS_DIR { get; set; }
            public string[] MAIN_UPKS { get; set; }
            public List<BNS_CLASS_STRUCT> CLASS { get; set; }
        }

        public class BNS_CLASS_STRUCT
        {
            public string CLASS { get; set; }
            public string[] EFFECTS { get; set; }
            public string[] ANIMATIONS { get; set; }
        }
    }
}
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
            if (!File.Exists(CONFIG_FILE))
            {
                SYS = new SYSConfig
                {
                    VERSION = MainWindow.FileVersion(),
                    FINGERPRINT = null,
                    ADDITIONAL_EFFECTS = 0,
                    BNS_DIR = "",
                    DELTA_PATCHING = 1,
                    THEME = 0,
                    NEW_GAME_OPTION = 0,
                    UPDATER_THREADS = 0,
                    MINIMZE_ACTION = 1,
                    HK_Installed = false,
                    PATCH_413 = true,
                    PING_CHECK = 1,
                    CLASS = new List<BNS_CLASS_STRUCT>
                    {
                        new BNS_CLASS_STRUCT()
                            {
                                CLASS = "Assassin",
                                EFFECTS = new string[] { "Mod_Remove_Assassin_05_SF_p.pak" },
                                ANIMATIONS = new string[] { "Mod_Remove_Assassin_Animset_p.pak" }
                            },
                            new BNS_CLASS_STRUCT()
                            {
                                CLASS = "Summoner",
                                EFFECTS = new string[] { "Mod_Remove_Summoner_05_SF_p.pak" },
                                ANIMATIONS = new string[] { "Mod_Remove_Summoner_Animset_p.pak" }
                            },
                            new BNS_CLASS_STRUCT()
                            {
                                CLASS = "KungFuMaster",
                                EFFECTS = new string[] { "Mod_Remove_KungfuFighter_05_SF_p.pak" },
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
                                ANIMATIONS = new string[] { "Mod_Remove_Destroyer_Animset_p.pak" }
                            },
                            new BNS_CLASS_STRUCT()
                            {
                                CLASS = "Forcemaster",
                                EFFECTS = new string[] { "Mod_Remove_ForceMaster_05_SF_p.pak" },
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
                                ANIMATIONS = new string[] { "Mod_Remove_Archer_Animset_p.pak" }
                            },
                            new BNS_CLASS_STRUCT()
                            {
                                CLASS = "Blademaster",
                                EFFECTS = new string[] { "Mod_Remove_Fencer_05_SF_p.pak" },
                                ANIMATIONS = new string[] { "Mod_Remove_Fencer_Animset_p.pak" }
                            },
                            new BNS_CLASS_STRUCT()
                            {
                                CLASS = "Bladedancer",
                                EFFECTS = new string[] { "Mod_Remove_SwordMaster_05_SF_p.pak" },
                                ANIMATIONS = new string[] { "Mod_Remove_SwordMaster_Animset_p.pak" }
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
                Globals.WriteAllText(CONFIG_FILE, _JSON);
            }
            else
            {
                try
                {
                    string _JSON = File.ReadAllText(CONFIG_FILE);
                    SYS = JsonConvert.DeserializeObject<SYSConfig>(_JSON);

                    bool changesToConfig = false;

                    //This whole section is for patching older clients, sections of code will eventually be removed.
                    if (SYS.CLASS == null)
                        SYS.CLASS = new List<BNS_CLASS_STRUCT>() { };

                    if(!SYS.PATCH_413)
                    {
                        changesToConfig = true;
                        SYS.CLASS = new List<BNS_CLASS_STRUCT>
                        {
                            new BNS_CLASS_STRUCT()
                            {
                                CLASS = "Assassin",
                                EFFECTS = new string[] { "Mod_Remove_Assassin_05_SF_p.pak" },
                                ANIMATIONS = new string[] { "Mod_Remove_Assassin_Animset_p.pak" }
                            },
                            new BNS_CLASS_STRUCT()
                            {
                                CLASS = "Summoner",
                                EFFECTS = new string[] { "Mod_Remove_Summoner_05_SF_p.pak" },
                                ANIMATIONS = new string[] { "Mod_Remove_Summoner_Animset_p.pak" }
                            },
                            new BNS_CLASS_STRUCT()
                            {
                                CLASS = "KungFuMaster",
                                EFFECTS = new string[] { "Mod_Remove_KungfuFighter_05_SF_p.pak" },
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
                                ANIMATIONS = new string[] { "Mod_Remove_Destroyer_Animset_p.pak" }
                            },
                            new BNS_CLASS_STRUCT()
                            {
                                CLASS = "Forcemaster",
                                EFFECTS = new string[] { "Mod_Remove_ForceMaster_05_SF_p.pak" },
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
                                ANIMATIONS = new string[] { "Mod_Remove_Archer_Animset_p.pak" }
                            },
                            new BNS_CLASS_STRUCT()
                            {
                                CLASS = "Blademaster",
                                EFFECTS = new string[] { "Mod_Remove_Fencer_05_SF_p.pak" },
                                ANIMATIONS = new string[] { "Mod_Remove_Fencer_Animset_p.pak" }
                            },
                            new BNS_CLASS_STRUCT()
                            {
                                CLASS = "Bladedancer",
                                EFFECTS = new string[] { "Mod_Remove_SwordMaster_05_SF_p.pak" },
                                ANIMATIONS = new string[] { "Mod_Remove_SwordMaster_Animset_p.pak" }
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
                        Effects.ExtractPakFiles();
                        SYS.PATCH_413 = true;
                    }
                    
                    if(changesToConfig)
                        Save();

                } catch (Exception ex)
                {
                    Logger.log.Error("SYSTEMCONFIG::PATCH\nType: {0}\n{1}", ex.GetType().Name, ex.ToString());
                    var dialog = new ErrorPrompt("There was an error reading or patching settings.json, additional information in logs.\r\rTry deleting settings.json to remove error before starting again, if errors persist submit log.");
                    dialog.ShowDialog();
                    Environment.Exit(0);
                }
            }
        }

        public static void Save()
        {
            try
            {
                string json = JsonConvert.SerializeObject(SYS, Formatting.Indented);
                Globals.WriteAllText(CONFIG_FILE, json);
            } catch (Exception ex)
            {
                Logger.log.Error("SYSTEMCONFIG::Save\nType: {0}\n{1}", ex.GetType().Name, ex.ToString());
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
            public bool HK_Installed { get; set; }
            public int THEME { get; set; }
            public int ADDITIONAL_EFFECTS { get; set; }
            public int UPDATER_THREADS { get; set; }
            public int NEW_GAME_OPTION { get; set; }
            public int MINIMZE_ACTION { get; set; }
            public int PING_CHECK { get; set; }
            public int DELTA_PATCHING { get; set; }
            public string BNS_DIR { get; set; }
            public bool PATCH_413 { get; set; }
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
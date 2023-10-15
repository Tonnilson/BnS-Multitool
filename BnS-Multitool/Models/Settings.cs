using BnS_Multitool.View;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using BnS_Multitool.Functions;
using BnS_Multitool.Extensions;
using System.ComponentModel;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;

namespace BnS_Multitool.Models
{
    [TypeConverter(typeof(EnumDescriptionTypeConverter))]
    public enum EThemes
    {
        [Description("Worry and Pepe")]
        Worry,
        [Description("Agon")]
        Agon
    }

    [TypeConverter(typeof(EnumDescriptionTypeConverter))]
    public enum EStartNewGame
    {
        [Description("Nothing")]
        Nothing,
        [Description("Minimuze Launcher")]
        MinimizeLauncher,
        [Description("Minimize to system tray")]
        MinimizeTray,
        [Description("Close Launcher")]
        CloseLauncher
    }

    [TypeConverter(typeof(EnumDescriptionTypeConverter))]
    public enum EMinimizeWindow
    {
        [Description("Minimize Launcher")]
        Minimize,
        [Description("Minimize to system tray")]
        MinimizeTray
    }

    [TypeConverter(typeof(EnumDescriptionTypeConverter))]
    public enum MemoryCleaner_Timers
    {
        [Description("OFF")]
        [DefaultValue(0)]
        off,
        [DefaultValue(1)]
        [Description("1 Minute")]
        one,
        [DefaultValue(5)]
        [Description("5 Minutes")]
        five,
        [DefaultValue(10)]
        [Description("10 Minutes")]
        ten,
        [DefaultValue(15)]
        [Description("15 Minutes")]
        fifteen,
        [DefaultValue(20)]
        [Description("20 Minutes")]
        twenty,
        [DefaultValue(25)]
        [Description("25 Minutes")]
        twentyfive,
        [DefaultValue(30)]
        [Description("30 Minutes")]
        thirty,
        [DefaultValue(35)]
        [Description("35 Minutes")]
        thirtyfive,
        [DefaultValue(40)]
        [Description("40 Minutes")]
        fourty,
        [DefaultValue(45)]
        [Description("45 Minutes")]
        fourtyfive,
        [DefaultValue(50)]
        [Description("50 Minutes")]
        fithty,
        [DefaultValue(55)]
        [Description("55 Minutes")]
        fithyfive,
        [DefaultValue(60)]
        [Description("60 Minutes")]
        sixty
    }

    public class Settings
    {
        public enum CONFIG
        {
            [Description("settings.json")]
            Settings,
            [Description("accounts.json")]
            Account,
            [Description("sync.json")]
            Sync
        }

        private ILogger<Settings> _logger;
        public SYSConfig System;
        public ACCOUNTS_CONFIG Account;
        public Sync_Config Sync;

        public void Save(CONFIG cfg)
        {
            try
            {
                object config;
                switch (cfg)
                {
                    case CONFIG.Account: config = Account; break;
                    case CONFIG.Sync: config = Sync; break;
                    default:
                        config = System; break;

                }

                string JSON = JsonConvert.SerializeObject(config, Formatting.Indented);
                IO.WriteAllText(cfg.GetDescription(), JSON);
            } catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to save config for {cfg.GetDescription()}");
                // Log
            }
        }

        public async Task SaveAsync(CONFIG cfg)
        {
            try
            {
                object config;
                switch (cfg)
                {
                    case CONFIG.Account: config = Account; break;
                    case CONFIG.Sync: config = Sync; break;
                    default:
                        config = System; break;

                }

                string JSON = JsonConvert.SerializeObject(config, Formatting.Indented);
                await IO.WriteAllTextAsync(cfg.GetDescription(), JSON);
            } catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to save config for {cfg.GetDescription()}");
                // Log
            }
        }

        public string VersionInfo()
        {
            var assemblyInfo = typeof(MainWindow).Assembly.GetName();
            return string.Format("{0}.{1}.{2}", assemblyInfo.Version.Major, assemblyInfo.Version.Minor, assemblyInfo.Version.Build);
        }

        public Settings(ILogger<Settings> logger)
        {
            _logger = logger;
        }

        public void Initialize()
        {
            string Json = string.Empty;
            if (!File.Exists(CONFIG.Settings.GetDescription()))
            {
                System = new SYSConfig
                {
                    VERSION = VersionInfo(),
                    FINGERPRINT = null,
                    BNSPATCH_DIRECTORY = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "BnS"),
                    ADDITIONAL_EFFECTS = 0,
                    AUTO_UPDATE_PLUGINS = false,
                    BNS_DIR = "",
                    DELTA_PATCHING = true,
                    BUILD_RELAY = false,
                    THEME = EThemes.Agon,
                    NEW_GAME_OPTION = EStartNewGame.Nothing,
                    UPDATER_THREADS = 1,
                    DOWNLOADER_THREADS = 1,
                    MINIMZE_ACTION = EMinimizeWindow.MinimizeTray,
                    HK_Installed = false,
                    PING_CHECK = true,
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

                Save(CONFIG.Settings);
            }
            else
            {
                try
                {
                    Json = File.ReadAllText(CONFIG.Settings.GetDescription());
                    System = JsonConvert.DeserializeObject<SYSConfig>(Json);

                    System.CLASS = System.CLASS ?? new List<BNS_CLASS_STRUCT>();
                    System.BNSPATCH_DIRECTORY = System.BNSPATCH_DIRECTORY ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "BnS");

                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to initialize Settings Config\n");
                    Environment.Exit(0);
                }
            }

            if (!File.Exists(CONFIG.Account.GetDescription()))
            {
                Account = new ACCOUNTS_CONFIG
                {
                    USE_ALL_CORES = false,
                    USE_TEXTURE_STREAMING = false,
                    REGION = ERegion.NA,
                    LANGUAGE = ELanguage.EN,
                    AUTPATCH_QOL = false,
                    MEMORY_CLEANER = MemoryCleaner_Timers.off,
                    LAST_USED_ACCOUNT = -1,
                    Saved = new List<BNS_SAVED_ACCOUNTS_STRUCT> { }
                };

                Save(CONFIG.Account);
            }
            else
            {
                try
                {
                    Json = File.ReadAllText(CONFIG.Account.GetDescription());
                    Account = JsonConvert.DeserializeObject<ACCOUNTS_CONFIG>(Json);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to initialize Account Config\n");
                    Environment.Exit(0);
                }
            }

            if (!File.Exists(CONFIG.Sync.GetDescription()))
            {
                Sync = new Sync_Config
                {
                    AUTH_KEY = null,
                    AUTH_REFRESH = null,
                    Synced = new List<Synced_Xmls>()
                };

                Save(CONFIG.Sync);
            } else
            {
                try
                {
                    Json = File.ReadAllText(CONFIG.Sync.GetDescription());
                    Sync = JsonConvert.DeserializeObject<Sync_Config>(Json);
                } catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to initialize Sync Config");
                    Environment.Exit(0);
                }
            }
        }

        public class SYSConfig
        {
            public string VERSION { get; set; }
            public string BNSPATCH_DIRECTORY { get; set; }
            public string? FINGERPRINT { get; set; }
            public bool HK_Installed { get; set; }
            public EThemes THEME { get; set; }
            public int ADDITIONAL_EFFECTS { get; set; }
            public bool AUTO_UPDATE_PLUGINS { get; set; }
            public int UPDATER_THREADS { get; set; } = 1;
            public int DOWNLOADER_THREADS { get; set; } = 1;
            public EStartNewGame NEW_GAME_OPTION { get; set; }
            public EMinimizeWindow MINIMZE_ACTION { get; set; }
            public bool PING_CHECK { get; set; }
            public bool DELTA_PATCHING { get; set; }
            public bool BUILD_RELAY { get; set; }
            public string BNS_DIR { get; set; }
            public List<BNS_CLASS_STRUCT> CLASS { get; set; }
        }

        public class BNS_CLASS_STRUCT
        {
            public string? CLASS { get; set; }
            public string[]? EFFECTS { get; set; }
            public string[]? ANIMATIONS { get; set; }
        }

        public struct ACCOUNTS_CONFIG
        {
            public bool USE_ALL_CORES { get; set; }
            public bool USE_TEXTURE_STREAMING { get; set; }
            public ERegion REGION { get; set; }
            public ELanguage LANGUAGE { get; set; }
            public MemoryCleaner_Timers MEMORY_CLEANER { get; set; }
            public bool AUTPATCH_QOL { get; set; }
            public List<BNS_SAVED_ACCOUNTS_STRUCT> Saved { get; set; }
            public int LAST_USED_ACCOUNT { get; set; }
        }

        public class BNS_SAVED_ACCOUNTS_STRUCT
        {
            public string EMAIL { get; set; }
            public string PASSWORD { get; set; }
            public string PINCODE { get; set; }
            public string PARAMS { get; set; }
            public string ENVARS { get; set; }
        }

        public struct Sync_Config
        {
            public string AUTH_KEY { get; set; }
            public string AUTH_REFRESH { get; set; }
            public DateTime? EXPIRES { get; set; }
            public List<Synced_Xmls> Synced { get; set; }
        }

        public class Synced_Xmls
        {
            public int ID { get; set; }
            public string Username { get; set; }
            public long Discord_id { get; set; }
            public int Version { get; set; }
            public string Name { get; set; }
            public string Type { get; set; }
            public Models.CategoryType Category { get; set; }
            public string Date { get; set; }
            public string Title { get; set; }
            public string Description { get; set; }
            public string Hash { get; set; }
        }
    }
}

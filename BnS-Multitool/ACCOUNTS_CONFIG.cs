using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.IO;
using System.Dynamic;

namespace BnS_Multitool
{
    class ACCOUNT_CONFIG
    {
        public const string CONFIG_FILE = "accounts.json";
        public static ACCOUNTS_CONFIG ACCOUNTS;
        static ACCOUNT_CONFIG()
        {
            if (!File.Exists(CONFIG_FILE))
            {
                ACCOUNTS = new ACCOUNTS_CONFIG
                {
                    FAST_LAUNCH = 0,
                    USE_ALL_CORES = 0,
                    USE_TEXTURE_STREAMING = 0,
                    REGION = 0,
                    CLIENT_BIT = 0,
                    LANGUAGE = 0,
                    TOS_AUTO_BAIT = 0,
                    TOS_RAISE_CAP = 0,
                    TOS_MARKETPLACE = 0,
                    TOS_AUTOCOMBAT = 0,
                    TOS_AUTOCOMBATRANGE = 0,
                    TOS_AUTOCOMBATRANGE_VALUE = 30,
                    MEMORY_CLEANER = 0,
                    Saved = new List<BNS_SAVED_ACCOUNTS_STRUCT> { }
                };

                string _JSON = JsonConvert.SerializeObject(ACCOUNTS, Formatting.Indented);
                File.WriteAllText(CONFIG_FILE, _JSON);
            }
            else
            {
                try
                {
                    string _JSON = File.ReadAllText(CONFIG_FILE);
                    ACCOUNTS = JsonConvert.DeserializeObject<ACCOUNTS_CONFIG>(_JSON);

                    //Check and add property if it doesn't exist, mainly used for updates.

                    if(SystemConfig.SYS.PATCH_310 == 0)
                    {
                        ACCOUNTS.TOS_AUTOCOMBAT = 0;
                        ACCOUNTS.TOS_AUTOCOMBATRANGE = 0;
                        ACCOUNTS.TOS_AUTOCOMBATRANGE_VALUE = 30; //Default is 30m
                        SystemConfig.SYS.PATCH_310 = 1;
                        SystemConfig.appendChangesToConfig();
                    }

                    appendChangesToConfig();
                } catch (Exception)
                {
                    var dialog = new ErrorPrompt("There was an error reading the config file: accounts.json\rIf error persists delete accounts.json or check for syntax errors.");
                    dialog.ShowDialog();
                    Environment.Exit(0);
                }
            }
        }

        public static void appendChangesToConfig()
        {
            string json = JsonConvert.SerializeObject(ACCOUNTS, Formatting.Indented);
            File.WriteAllText(CONFIG_FILE, json);
        }

        public static bool DoesPropertyExist(dynamic settings, string name)
        {
            if (settings is ExpandoObject)
                return ((IDictionary<string, object>)settings).ContainsKey(name);

            return settings.GetType().GetProperty(name) != null;
        }

        public struct ACCOUNTS_CONFIG
        {
            public int FAST_LAUNCH { get; set; }
            public int USE_ALL_CORES { get; set; }
            public int USE_TEXTURE_STREAMING { get; set; }
            public int REGION { get; set; }
            public int CLIENT_BIT { get; set; }
            public int LANGUAGE { get; set; }
            public int TOS_AUTO_BAIT { get; set; }
            public int TOS_RAISE_CAP { get; set; }
            public int TOS_MARKETPLACE { get; set; }
            public int TOS_AUTOCOMBAT { get; set; }
            public int TOS_AUTOCOMBATRANGE { get; set; }
            public int TOS_AUTOCOMBATRANGE_VALUE { get; set; }
            public int MEMORY_CLEANER { get; set; }
            public List<BNS_SAVED_ACCOUNTS_STRUCT> Saved { get; set; }
        }

        public class BNS_SAVED_ACCOUNTS_STRUCT
        {
            public string EMAIL { get; set; }
            public string PASSWORD { get; set; }
            public string PINCODE { get; set; }
        }
    }
}

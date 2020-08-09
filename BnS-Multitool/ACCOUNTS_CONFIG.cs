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
                    Saved = new List<BNS_SAVED_ACCOUNTS_STRUCT> { }
                };

                string _JSON = JsonConvert.SerializeObject(ACCOUNTS, Formatting.Indented);
                File.WriteAllText(CONFIG_FILE, _JSON);
            }
            else
            {
                string _JSON = File.ReadAllText(CONFIG_FILE);
                ACCOUNTS = JsonConvert.DeserializeObject<ACCOUNTS_CONFIG>(_JSON);

                if (!DoesPropertyExist(ACCOUNTS, "USE_ALL_CORES"))
                    ACCOUNTS.USE_ALL_CORES = 0;

                if (!DoesPropertyExist(ACCOUNTS, "USE_TEXTURE_STREAMING"))
                    ACCOUNTS.USE_TEXTURE_STREAMING = 0;

                if (!DoesPropertyExist(ACCOUNTS, "REGION"))
                    ACCOUNTS.REGION = 0;

                if (!DoesPropertyExist(ACCOUNTS, "CLIENT_BIT"))
                    ACCOUNTS.CLIENT_BIT = 0;

                if (!DoesPropertyExist(ACCOUNTS, "LANGUAGE"))
                    ACCOUNTS.LANGUAGE = 0;

                appendChangesToConfig();
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

using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;

namespace BnS_Multitool
{
    public class SyncConfig
    {
        private const string _configFile = "sync.json";
        private static Sync_Config Settings;
        public static string AUTH_KEY
        {
            get { return Settings.AUTH_KEY; }
            set { Settings.AUTH_KEY = value; Save(); }
        }

        public static string AUTH_REFRESH
        {
            get { return Settings.AUTH_REFRESH; }
            set => Settings.AUTH_REFRESH = value;
        }

        public static List<Synced_Xmls> Synced
        {
            get { return Settings.Synced; }
            set => Settings.Synced = value;
        }

        //Constructer
        public SyncConfig()
        {
            if(!File.Exists(_configFile))
            {
                Settings = new Sync_Config
                {
                    AUTH_KEY = null,
                    AUTH_REFRESH = null,
                    Synced = new List<Synced_Xmls>()
                };

                File.WriteAllText(_configFile, JsonConvert.SerializeObject(Settings, Formatting.Indented));
            } else
            {
                try
                {
                    Settings = JsonConvert.DeserializeObject<Sync_Config>(File.ReadAllText(_configFile));
                } catch (Exception)
                {
                    new ErrorPrompt("There was an error reading the sync file: sync.json\rIf error persists delete sync.json").ShowDialog();
                }
            }
        }

        public static void Save()
        {
            try
            {
                Globals.WriteAllText(_configFile, JsonConvert.SerializeObject(Settings, Formatting.Indented));
            } catch (IOException ex)
            {
                new ErrorPrompt(ex.Message).ShowDialog();
            }
        }

        private struct Sync_Config
        {
            public string AUTH_KEY { get; set; }
            public string AUTH_REFRESH { get; set; }
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
            public int Category { get; set; }
            public string Date { get; set; }
            public string Title { get; set; }
            public string Description { get; set; }
            public string Hash { get; set; }
        }
    }
}

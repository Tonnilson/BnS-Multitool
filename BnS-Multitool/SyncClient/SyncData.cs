using System.Collections.Generic;
using System.ComponentModel;
using System.Windows;

namespace BnS_Multitool
{
    public class SyncData
    {
        public class Discord_Refresh
        {
            public string access_token { get; set; }
            public string token_type { get; set; }
            public string refresh_token { get; set; }
        }
        public class SocketReceivedData
        {
            public string type { get; set; }
            public string access_token { get; set; }
            public string refresh_token { get; set; }
        }

        public class XML_VIEW_RESPONSE
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
            public int Downloads { get; set; }
        }

        public class PUBLISH_XML_POST
        {
            public string Title { get; set; }
            public string Hash { get; set; }
            public int Category { get; set; }
            public string Auth_Code { get; set; }
            public string Description { get; set; }
            public string Type { get; set; }
            public string FileName { get; set; }
            public int xml_id { get; set; }
        }

        public class REMOVE_XML_POST
        {
            public string Auth_Code { get; set; }
            public int XML_ID { get; set; }
        }

        public class XMLS_RESPONSE
        {
            public System.Net.HttpStatusCode response { get; set; }
            public string response_msg { get; set; }
            public List<XML_VIEW_RESPONSE> xmls { get; set; }
            public XML_VIEW_RESPONSE xml { get; set; }
        }

        public class XML_View_List
        {
            public string Title { get; set; }
            public string User { get; set; }
            public string Date { get; set; }
            public string Type { get; set; }
            public int ID { get; set; }
            public System.Windows.Media.Brush SyncColor { get; set; }
            public long DiscordID { get; set; }
            public int Category { get; set; }
            public string CategoryText { get; set; }
            public bool Synced { get; set; }
            public int Downloads { get; set; }
            public string Description { get; set; }
            public string SyncButton { get; set; }
            public string FileName { get; set; }
            public Visibility CanRemove { get; set; }
        }
    }
}

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;

namespace BnS_Multitool
{
    public static class EnumHelper
    {
        public static string GetDescription<T>(this T e) where T : IConvertible
        {
            if (e is Enum)
            {
                Type type = e.GetType();
                Array values = System.Enum.GetValues(type);

                foreach (int val in values)
                {
                    if (val == e.ToInt32(CultureInfo.InvariantCulture))
                    {
                        var memInfo = type.GetMember(type.GetEnumName(val));
                        var descriptionAttribute = memInfo[0]
                            .GetCustomAttributes(typeof(DescriptionAttribute), false)
                            .FirstOrDefault() as DescriptionAttribute;

                        if (descriptionAttribute != null)
                        {
                            return descriptionAttribute.Description;
                        }
                    }
                }
            }

            return null; // could also return string.Empty
        }
    }

    public class SyncData
    {
        public enum CategoryType
        {
            [Description("Other")]
            Other = 0,
            [Description("Blademaster")]
            BM = 1,
            [Description("Kung Fu Master")]
            KFM = 2,
            [Description("Forcemaster")]
            FM = 3,
            [Description("Destroyer")]
            DES = 4,
            [Description("Assassin")]
            SIN = 5,
            [Description("Summoner")]
            SUM = 6,
            [Description("Blade dancer")]
            BD = 7,
            [Description("Warlock")]
            WL = 8,
            [Description("Soul Fighter")]
            SF = 9,
            [Description("Gunslinger")]
            GUN = 10,
            [Description("Warden")]
            WAR = 11,
            [Description("Archer")]
            ARC = 12,
            [Description("Astromancer")]
            AST = 13,
            [Description("Dualblade")]
            DB = 14
        }

        public class XML_VIEW_RESPONSE
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

        public class XMLS_RESPONSE
        {
            public System.Net.HttpStatusCode response { get; set; }
            public string response_msg { get; set; }
            public List<XML_VIEW_RESPONSE> xmls { get; set; }
            public XML_VIEW_RESPONSE xml { get; set; }
        }
    }
}

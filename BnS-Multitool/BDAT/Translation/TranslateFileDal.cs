using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Schema;
using BnsDatTool.lib;
using BnS_Multitool.Properties;

namespace BnsDatTool.lib
{
    static class TranslateFileDal
    {
        public static class NodeNames
        {
            public const string Root = "text";
            public const string AutoId = "autoId";
            public const string Alias = "alias";
            public const string Priority = "priority";
            public const string Original = "original";
            public const string Replacement = "replacement";
            public const string Type = "type";
        }

        private static readonly XmlSchemaSet schemaSet;

        static TranslateFileDal()
        {
            schemaSet = new XmlSchemaSet();
            schemaSet.Add("", new XmlTextReader(new StringReader(Resources.translate)));
            schemaSet.Compile();
        }

        public static IEnumerable<TranslatableItem> Open(string path)
        {
            List<TranslatableItem> elements = new List<TranslatableItem>();
            XmlDocument doc = new XmlDocument();
            doc.Load(path);
            XmlNodeList xNodeList = doc.SelectNodes("table/child::node()");

            int autoId = -1;
            string alias = "Null";
            string original = "Null";
            string translate = "Null";
            string type = "nc";
            foreach (XmlNode node in xNodeList)
            {
                if (null != node.Attributes[NodeNames.AutoId])
                    autoId = int.Parse(node.Attributes[NodeNames.AutoId].Value);
                if (null != node.Attributes[NodeNames.Alias])
                    alias = node.Attributes[NodeNames.Alias].Value;
                if (null != node.Attributes[NodeNames.Type])
                    type = node.Attributes[NodeNames.Type].Value;

                foreach (XmlNode childNode in node)
                {
                    switch (childNode.Name)
                    {
                        case NodeNames.Original:
                            original = childNode.InnerText;//.Trim();
                            break;
                        case NodeNames.Replacement:
                            translate = childNode.InnerText;//.Trim();
                            break;
                    }
                }
                elements.Add(new TranslatableItem(autoId, alias, original, translate, type));
            }

            return elements;
        }

        public static void Save(string path, IEnumerable<TranslatableItem> elements)
        {
            XElement texts = new XElement("table");
            //int autoId = 1;
            foreach (TranslatableItem line in elements)
            {
                XElement temp_xml = new XElement(NodeNames.Root,
                    new XAttribute(NodeNames.AutoId, line.AutoId),
                    new XAttribute(NodeNames.Alias, line.Alias),
                    new XAttribute(NodeNames.Priority, line.Priority),
                    new XAttribute(NodeNames.Type, line.Type));

                temp_xml.Add(new XElement(NodeNames.Original, new XCData(line.Original)),
                             new XElement(NodeNames.Replacement, new XCData(line.Translate)));

                texts.Add(temp_xml);

                //autoId++;
            }

            XmlWriterSettings settings = new XmlWriterSettings
            {
                OmitXmlDeclaration = true,
                Encoding = Encoding.UTF8,
                Indent = true
            };

            using (XmlWriter xw = XmlWriter.Create(path, settings))
            {
                texts.Save(xw);
            }
        }

    }
}

using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace BnS_Multitool.Models
{
    public class XmlModel
    {
        public XDocument qol;
        public XDocument extended_options;
        private readonly ILogger<XmlModel> _logger;

        public enum XML
        {
            QOL,
            ExtendedOptions
        }

        public XmlModel(ILogger<XmlModel> logger)
        {
            _logger = logger;
        }

        public async Task InitalizeAsync()
        {
            await Load_QOL_Async();
            await Load_ExtendedOptions_Async();
        }

        public void Save(XML xml)
        {
            if (xml == XML.QOL)
                qol.Save(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "BnS", "multitool_qol.xml"));
            else
                extended_options.Save(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "BnS", "extended_options.xml"));
        }

        public async Task Load_QOL_Async()
        {
            try
            {
                // Check if multitool_qol.xml exists in Documents\BnS if not create it and write our default-template
                if (!File.Exists(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "BnS", "multitool_qol.xml")))
                {
                    _logger.LogInformation("multitool_qol.xml does not exist, creating");
                    using (StreamWriter output = File.CreateText(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "BnS", "multitool_qol.xml")))
                        await output.WriteAsync(Properties.Resources.multitool_qol);
                }

                _logger.LogInformation("Loading multitool_qol.xml from Documents\\BnS");
                // Load XML contents into memory
                qol = XDocument.Load(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "BnS", "multitool_qol.xml"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load QOL XML");
            }
        }

        public async Task Load_ExtendedOptions_Async()
        {
            try
            {
                _logger.LogInformation("Loading extended_options.xml from Documents\\BnS");
                // Check if extended_options.xml exists in Documents\BnS if not create it and write our default-template
                if (!File.Exists(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "BnS", "extended_options.xml")))
                {
                    _logger.LogInformation("extended_options.xml does not exist, writing file");
                    using (StreamWriter output = File.CreateText(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "BnS", "extended_options.xml")))
                        await output.WriteAsync(Properties.Resources.extended_options);
                }

                // Load XML contents into memory
                extended_options = XDocument.Load(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "BnS", "extended_options.xml"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load ExtendedOptions XML");
            }
        }
    }
}

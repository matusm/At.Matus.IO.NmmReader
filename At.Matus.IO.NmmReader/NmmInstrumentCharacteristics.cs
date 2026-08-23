using System;
using System.IO;
using System.Xml.Linq;

namespace At.Matus.IO.NmmReader
{
    public sealed class NmmInstrumentCharacteristics
    {
        private const bool beMerciful = true;
        private const string defaultValue = "---";
        private const string defaultFileName = "NmmInstrumentCharacteristics.xml";
        private static readonly string defaultFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, defaultFileName);

        public NmmInstrumentCharacteristics() : this(defaultFilePath) {}

        public NmmInstrumentCharacteristics(string fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName))
            {
                if (beMerciful) return;
                throw new ArgumentException("A configuration file name is required.", nameof(fileName));
            }

            if (!File.Exists(fileName))
            {
                if (beMerciful) return;
                throw new FileNotFoundException($"The instrument configuration file ({fileName}) was not found.", fileName);
            }
            try
            {
                XDocument document = XDocument.Load(fileName);
                XElement root = document.Element("NmmInstrumentCharacteristics");
                if (root == null)
                {
                    if (beMerciful) return;
                    throw new InvalidDataException("The XML file must contain an NmmInstrumentCharacteristics element.");
                }
                User = GetRequiredValue(root, "User");
                OrganisationLong = GetRequiredValue(root, "OrganisationLong");
                Organisation = GetRequiredValue(root, "Organisation");
                InstrumentManufacturer = GetRequiredValue(root, "InstrumentManufacturer");
                InstrumentModel = GetRequiredValue(root, "InstrumentModel");
                InstrumentSerial = GetRequiredValue(root, "InstrumentSerial");
                InstrumentVersion = GetRequiredValue(root, "InstrumentVersion");
                EnvironmentMode = GetRequiredValue(root, "EnvironmentMode");
            }
            catch (System.Xml.XmlException exception)
            {
                if (beMerciful) return;
                throw new InvalidDataException(
                    "The instrument configuration file contains invalid XML.",
                    exception);
            }
        }

        public string User { get; } = defaultValue;
        public string OrganisationLong { get; } = defaultValue;
        public string Organisation { get; } = defaultValue;
        public string InstrumentManufacturer { get; } = defaultValue;
        public string InstrumentModel { get; } = defaultValue;
        public string InstrumentSerial { get; } = defaultValue;
        public string InstrumentVersion { get; } = defaultValue;
        public string EnvironmentMode { get; } = defaultValue;
        public string InstrumentIdentifier => $"{InstrumentManufacturer} {InstrumentModel} {InstrumentVersion} {InstrumentSerial}";
        public string Institute => $"{OrganisationLong} ({Organisation})";

        private static string GetRequiredValue(XElement root, string elementName)
        {
            XElement element = root.Element(elementName);
            if (element == null || string.IsNullOrWhiteSpace(element.Value))
            {
                if (beMerciful) return defaultValue;
                throw new InvalidDataException($"The required XML element '{elementName}' is missing or empty.");
            }
            return element.Value.Trim();
        }
    }
}

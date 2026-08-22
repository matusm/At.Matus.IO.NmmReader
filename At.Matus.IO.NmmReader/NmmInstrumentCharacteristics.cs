using System;
using System.IO;
using System.Xml.Linq;

namespace At.Matus.IO.NmmReader
{
    public class NmmInstrumentCharacteristics
    {
        public NmmInstrumentCharacteristics() => SetDefaultCharacteristics();

        public NmmInstrumentCharacteristics(string fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName))
                throw new ArgumentException(
                    "A configuration file name is required.",
                    nameof(fileName));
            if (!File.Exists(fileName))
                throw new FileNotFoundException(
                    "The instrument configuration file was not found.",
                    fileName);
            try
            {
                XDocument document = XDocument.Load(fileName);
                XElement root = document.Element("InstrumentSettings");
                if (root == null)
                    throw new InvalidDataException(
                        "The XML file must contain an InstrumentSettings element.");
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
                throw new InvalidDataException(
                    "The instrument configuration file contains invalid XML.",
                    exception);
            }
        }

        public string User { get; private set; }
        public string OrganisationLong { get; private set; }
        public string Organisation { get; private set; }
        public string InstrumentManufacturer { get; private set; }
        public string InstrumentModel { get; private set; }
        public string InstrumentSerial { get; private set; }
        public string InstrumentVersion { get; private set; }
        public string EnvironmentMode { get; private set; }
        public string InstrumentIdentifier => $"{InstrumentManufacturer} {InstrumentModel} {InstrumentVersion} {InstrumentSerial}";
        public string Institute => $"{OrganisationLong} ({Organisation})";

        private static string GetRequiredValue(XElement root, string elementName)
        {
            XElement element = root.Element(elementName);
            if (element == null ||
                string.IsNullOrWhiteSpace(element.Value))
            {
                throw new InvalidDataException($"The required XML element '{elementName}' is missing or empty.");
            }
            return element.Value.Trim();
        }

        private void SetDefaultCharacteristics()
        {
            User = "Michael Matus";
            OrganisationLong = "Bundesamt fuer Eich- und Vermessungswesen";
            Organisation = "BEV";
            InstrumentManufacturer = "SIOS";
            InstrumentModel = "NMM-1";
            InstrumentSerial = "PN11100209";
            InstrumentVersion = "0016";
            EnvironmentMode = "air";
        }
    }
}

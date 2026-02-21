using System;
using System.Collections.Generic;
using System.Text;

namespace At.Matus.IO.NmmReader.scan_mode
{
    public class ScanMetaData
    {
        // geometry parameters
        public long DataMask { get; private set; }
        public int NumberOfProfiles { get; private set; }
        public int NumberOfSpuriousProfiles { get; private set; }
        public int NumberOfDataPoints { get; private set; }
        public int NumberOfGlitchedDataPoints { get; private set; }
        public int NumberOfScans { get; private set; }
        public int NumberOfColumnsInFile { get; private set; }
        public int[] ForwardProfileLengths { get; private set; }
        public int[] BackwardProfileLengths { get; private set; }
        public int SpuriousDataLines { get; private set; }
        // metric parameters
        public double ScanFieldDeltaX { get; private set; }
        public double ScanFieldDeltaY { get; private set; }
        public double ScanFieldDimensionX { get; private set; }
        public double ScanFieldDimensionY { get; private set; }
        public double ScanSpeed { get; private set; }
        public double ScanFieldRotation { get; private set; }
        public double ScanFieldCenterX { get; private set; }
        public double ScanFieldCenterY { get; private set; }
        public double ScanFieldCenterZ { get; private set; }
        public double ScanFieldOriginX { get; private set; }
        public double ScanFieldOriginY { get; private set; }
        public double ScanFieldOriginZ { get; private set; }
        // additional metadata
        public string BaseFileName { get; private set; }
        public int ScanIndex { get; private set; }
        public DateTime CreationDate { get; private set; }
        public TimeSpan ScanDuration { get; private set; }
        public MeasurementProcedure Procedure { get; private set; }
        public ScanDirectionStatus ScanStatus { get; private set; }
        public ScanColumnPredicate[] ColumnPredicates { get; private set; }
        public string SpmTechnique { get; private set; }
        public string ProbeDesignation { get; private set; }
        public List<string> ScanComments { get; private set; }
        // first 3 lines of the "comment field" are taken as the
        // "sample description" according to ISO 28600:2012
        public string SampleIdentifier { get; private set; }
        public string SampleSpecies { get; private set; }
        public string SampleSpecification { get; private set; }
        // environmental data
        public double AirTemperature { get; private set; }
        public double SampleTemperature { get; private set; }
        public double RelativeHumidity { get; private set; }
        public double BarometricPressure { get; private set; }
        public double AirTemperatureDrift { get; private set; }
        public double SampleTemperatureDrift { get; private set; }
        public double RelativeHumidityDrift { get; private set; }
        public double BarometricPressureDrift { get; private set; }
        public double AirTemperatureGradient { get; private set; }
        public int NumberOfAirSamples { get; private set; }
        public string AirSampleSourceText { get; private set; }
        // Instrument characteristics
        public string InstrumentIdentifier { get; private set; }
        public string User { get; private set; }
        public string OrganisationLong { get; private set; }
        public string Organisation { get; private set; }
        public string InstrumentManufacturer { get; private set; }
        public string InstrumentModel { get; private set; }
        public string InstrumentSerial { get; private set; }
        public string InstrumentVersion { get; private set; }
        public string EnvironmentMode { get; private set; }
        public string Institute { get; private set; }

        public void AddDataFrom(object obj)
        {
            if (obj is NmmEnvironmentData)
                FillEnvironmentalData(obj as NmmEnvironmentData);
            if (obj is NmmIndFileParser)
                FillIndexData(obj as NmmIndFileParser);
            if (obj is NmmDescriptionFileParser)
                FillDescriptionData(obj as NmmDescriptionFileParser);
            if (obj is NmmInstrumentCharacteristcs)
                FillInstrumentData(obj as NmmInstrumentCharacteristcs);
            if (obj is NmmFileName)
                FillFileData(obj as NmmFileName);
        }

        public void AddScanFieldCoordinates(double ox, double oy, double oz, double cx, double cy, double cz)
        {
            ScanFieldOriginX = ox;
            ScanFieldOriginY = oy;
            ScanFieldOriginZ = oz;
            ScanFieldCenterX = cx;
            ScanFieldCenterY = cy;
            ScanFieldCenterZ = cz;
        }

        public string ToDebugString()
        {
            StringBuilder sb = new StringBuilder();
            _ = sb.AppendLine(string.Format("InstrumentIdentifier:       {0}", InstrumentIdentifier));
            _ = sb.AppendLine(string.Format("InstrumentManufacturer:     {0}", InstrumentManufacturer));
            _ = sb.AppendLine(string.Format("InstrumentModel:            {0}", InstrumentModel));
            _ = sb.AppendLine(string.Format("InstrumentSerial:           {0}", InstrumentSerial));
            _ = sb.AppendLine(string.Format("InstrumentVersion:          {0}", InstrumentVersion));
            _ = sb.AppendLine(string.Format("Organisation:               {0}", Organisation));
            _ = sb.AppendLine(string.Format("OrganisationLong:           {0}", OrganisationLong));
            _ = sb.AppendLine(string.Format("Institute:                  {0}", Institute));
            _ = sb.AppendLine(string.Format("User:                       {0}", User));
            _ = sb.AppendLine(string.Format("NumberOfProfiles:           {0}", NumberOfProfiles));
            _ = sb.AppendLine(string.Format("NumberOfSpuriousProfiles:   {0}", NumberOfSpuriousProfiles));
            _ = sb.AppendLine(string.Format("NumberOfDataPoints:         {0}", NumberOfDataPoints));
            _ = sb.AppendLine(string.Format("NumberOfGlitchedDataPoints: {0}", NumberOfGlitchedDataPoints));
            _ = sb.AppendLine(string.Format("NumberOfScans:              {0}", NumberOfScans));
            _ = sb.AppendLine(string.Format("SpuriousDataLines:          {0}", SpuriousDataLines));
            _ = sb.AppendLine(string.Format("ScanFieldDeltaX:            {0}", ScanFieldDeltaX));
            _ = sb.AppendLine(string.Format("ScanFieldDeltaY:            {0}", ScanFieldDeltaY));
            _ = sb.AppendLine(string.Format("ScanFieldDimensionX:        {0}", ScanFieldDimensionX));
            _ = sb.AppendLine(string.Format("ScanFieldDimensionY:        {0}", ScanFieldDimensionY));
            _ = sb.AppendLine(string.Format("ScanSpeed:                  {0}", ScanSpeed));
            _ = sb.AppendLine(string.Format("ScanFieldRotation:          {0}", ScanFieldRotation));
            _ = sb.AppendLine(string.Format("SpmTechnique:               {0}", SpmTechnique));
            _ = sb.AppendLine(string.Format("AirTemperature:             {0}", AirTemperature));
            _ = sb.AppendLine(string.Format("SampleTemperature:          {0}", SampleTemperature));
            _ = sb.AppendLine(string.Format("RelativeHumidity:           {0}", RelativeHumidity));
            _ = sb.AppendLine(string.Format("BarometricPressure:         {0}", BarometricPressure));
            _ = sb.AppendLine(string.Format("Environment:                {0}", EnvironmentMode));
            _ = sb.AppendLine(string.Format("AirSampleSourceText:        {0}", AirSampleSourceText));
            _ = sb.AppendLine(string.Format("MeasurementProcedure:       {0}", Procedure));
            _ = sb.AppendLine(string.Format("ScanDirectionStatus:        {0}", ScanStatus));
            _ = sb.AppendLine(string.Format("Filename:                   {0}", BaseFileName));
            _ = sb.AppendLine(string.Format("ScanIndex:                  {0}", ScanIndex));
            _ = sb.AppendLine("----------------");
            foreach (var s in ScanComments)
            {
                _ = sb.AppendLine(string.Format("> {0}", s));
            }
            return sb.ToString();
        }

        private void FillFileData(NmmFileName obj)
        {
            BaseFileName = obj.BaseFileName;
            ScanIndex = obj.ScanIndex;
        }

        private void FillInstrumentData(NmmInstrumentCharacteristcs instrument)
        {
            InstrumentIdentifier = instrument.InstrumentIdentifier;
            User = instrument.User;
            OrganisationLong = instrument.OrganisationLong;
            Organisation = instrument.Organisation;
            InstrumentManufacturer = instrument.InstrumentManufacturer;
            InstrumentModel = instrument.InstrumentModel;
            InstrumentSerial = instrument.InstrumentSerial;
            InstrumentVersion = instrument.InstrumentVersion;
            EnvironmentMode = instrument.EnvironmentMode;
            Institute = instrument.Institute;
        }

        private void FillIndexData(NmmIndFileParser indexFile)
        {
            if (DataMask == 0)
            {
                DataMask = indexFile.DataMask;
                ColumnPredicates = Scan.ColumnPredicatesFor(DataMask);
                NumberOfColumnsInFile = ColumnPredicates.Length;
            }
            // prefer NumberOfProfiles infered from the *.ind file
            NumberOfProfiles = indexFile.NumberOfProfiles;
            // end NumberOfProfiles
            NumberOfSpuriousProfiles = indexFile.SpuriousProfiles;
            NumberOfDataPoints = indexFile.NominalDataPoints;
            NumberOfGlitchedDataPoints = indexFile.DataPointsGlitch;
            SpuriousDataLines = indexFile.SpuriousDataLines;
            ScanStatus = indexFile.ScanStatus;
            CreationDate = indexFile.CreationDate;
            ScanDuration = indexFile.ScanDuration;
            ForwardProfileLengths = indexFile.ForwardProfileLengths.ToArray();
            BackwardProfileLengths = indexFile.BackwardProfileLengths.ToArray();

        }

        private void FillDescriptionData(NmmDescriptionFileParser description)
        {
            // prefer DataMask from the *.dsc file
            DataMask = description.DataMask;
            ColumnPredicates = Scan.ColumnPredicatesFor(DataMask);
            NumberOfColumnsInFile = ColumnPredicates.Length;
            // end DataMask
            if (NumberOfProfiles == 0)
            {
                NumberOfProfiles = description.NumberOfProfiles;
            }
            NumberOfScans = description.NumberOfScans;
            Procedure = description.Procedure;
            ScanFieldDeltaX = description.ScanFieldDeltaX;
            ScanFieldDeltaY = description.ScanFieldDeltaY;
            ScanFieldDimensionX = description.ScanFieldDimensionX;
            ScanFieldDimensionY = description.ScanFieldDimensionY;
            ScanSpeed = description.ScanSpeed;
            ScanFieldRotation = description.ScanFieldRotation;
            SpmTechnique = description.SpmTechnique;
            ScanComments = description.ScanComments;
            ProbeDesignation = description.ProbeDesignation;
            // first 3 lines of the "comment field" are taken as the
            // "sample description" according to ISO 28600:2012
            SampleIdentifier = "(not specified)";
            SampleSpecies = "(not specified)";
            SampleSpecification = "(not specified)";
            if (ScanComments.Count >= 1) SampleIdentifier = ScanComments[0];
            if (ScanComments.Count >= 2) SampleSpecies = ScanComments[1];
            if (ScanComments.Count >= 3) SampleSpecification = ScanComments[2];
        }

        private void FillEnvironmentalData(NmmEnvironmentData environmentSensors)
        {
            AirTemperature = environmentSensors.AirTemperature;
            SampleTemperature = environmentSensors.SampleTemperature;
            RelativeHumidity = environmentSensors.RelativeHumidity;
            BarometricPressure = environmentSensors.BarometricPressure;
            AirTemperatureDrift = environmentSensors.AirTemperatureDrift;
            SampleTemperatureDrift = environmentSensors.SampleTemperatureDrift;
            RelativeHumidityDrift = environmentSensors.RelativeHumidityDrift;
            BarometricPressureDrift = environmentSensors.BarometricPressureDrift;
            AirTemperatureGradient = environmentSensors.AirTemparatureGradient;
            NumberOfAirSamples = environmentSensors.NumberOfAirSamples;
            AirSampleSourceText = environmentSensors.AirSampleSourceText;
        }
    }
}

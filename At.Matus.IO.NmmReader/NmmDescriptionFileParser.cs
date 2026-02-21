using At.Matus.IO.NmmReader._3d_mode;
using At.Matus.IO.NmmReader.scan_mode;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;

namespace At.Matus.IO.NmmReader
{
    public class NmmDescriptionFileParser
    {
        public NmmDescriptionFileParser(NmmFileName fileName)
        {
            LoadDescriptionFile(fileName.GetDscFileName());
            DetermineMeasurementProcedure();
            switch (Procedure)
            {
                case MeasurementProcedure.Unknown:
                case MeasurementProcedure.NoFile:
                    break;
                case MeasurementProcedure.Scan:
                    ParseScanDescription();
                    break;
                case MeasurementProcedure.Point3D:
                    Parse3DPointDescription();
                    break;
                case MeasurementProcedure.Object3D:
                    Parse3DObjectDescription();
                    break;
                default:
                    break;
            }
        }

        // common (scan and 3d) properties
        public MeasurementProcedure Procedure { get; private set; } = MeasurementProcedure.Unknown;
        public string ProbeDesignation { get; private set; }
        // 3d properties
        public string SpecimenIdentifier { get; private set; }
        public double ProbeDiameter { get; private set; }
        public int NumberOfDataPoints { get; private set; }
        // scan properties
        public List<string> ScanComments => scanComments;
        public string SpmTechnique { get; private set; } = "unknown SPM technique";
        public double ScanFieldDeltaX { get; private set; }
        public double ScanFieldDeltaY { get; private set; }
        public double ScanFieldDimensionX { get; private set; }
        public double ScanFieldDimensionY { get; private set; }
        public double ScanFieldRotation { get; private set; }
        public double ScanSpeed { get; private set; }
        public int NumberOfProfiles { get; private set; }
        public int NumberOfScans { get; private set; } = 0;
        public long DataMask { get; private set; }

        public bool ColumnPresent(string columnSymbol)
        {
            if (GetColumnIndexFor(columnSymbol) == -1)
                return false;
            return true;
        }

        private int GetColumnIndexFor(string columnSymbol)
        {
            if (Procedure != MeasurementProcedure.Scan)
                return -1;
            if (metaData.DataMask == 0)
            {
                metaData.AddDataFrom(this);
            }
            for (int i = 0; i < metaData.NumberOfColumnsInFile; i++)
            {
                if (metaData.ColumnPredicates[i].IsOf(columnSymbol)) return i;
            }
            return -1;
        }

        private void LoadDescriptionFile(string fileName)
        {
            try
            {
                string line;
                StreamReader hFile = File.OpenText(fileName);
                while ((line = hFile.ReadLine()) != null)
                    if (!string.IsNullOrWhiteSpace(line))
                        fileContent.Add(line.Trim());
                hFile.Close();
            }
            catch (Exception)
            {
                Procedure = MeasurementProcedure.NoFile;
            }
        }

        private void DetermineMeasurementProcedure()
        {
            if (fileContent.Count == 0) return;
            foreach (string s in fileContent)
            {
                if (s.Contains("NMM Control 3D Point cloud description file"))
                {
                    Procedure = MeasurementProcedure.Point3D;
                    return;
                }
                if (s.Contains("3D Object measurement description file"))
                {
                    Procedure = MeasurementProcedure.Object3D;
                    return;
                }
                if (s.Contains("Scan procedure description file"))
                {
                    Procedure = MeasurementProcedure.Scan;
                    return;
                }
            }
        }

        private void Parse3DObjectDescription()
        {
            string sTemp;
            foreach (string s in fileContent)
            {
                ProbeDesignation = ProbeType3;
                if (s.Contains("Number of data points in file :"))
                {
                    sTemp = s.Replace("Number of data points in file :", " ");
                    NumberOfDataPoints = int.Parse(sTemp, numFormat);
                }
                if (s.Contains("Object name :"))
                {
                    sTemp = s.Replace("Object name :", " ");
                    SpecimenIdentifier = sTemp.Trim();
                }
                if (s.Contains("Measuring object : Plane"))
                {
                    objectType = ObjectType.Plane;
                }
                if (s.Contains("Measuring object : Sphere"))
                {
                    objectType = ObjectType.Sphere;
                }
                if (s.Contains("2. Probe diameter : "))
                {
                    sTemp = s.Replace("2. Probe diameter : ", " ");
                    sTemp = sTemp.Replace("mm", " ");
                    ProbeDiameter = double.Parse(sTemp, numFormat);
                }
                if (s.Contains("Current probe sphere diameter : "))
                {
                    sTemp = s.Replace("Current probe sphere diameter : ", " ");
                    sTemp = sTemp.Replace("mm", " ");
                    ProbeDiameter = double.Parse(sTemp, numFormat);
                }
            }
        }

        private void Parse3DPointDescription()
        {
            // not much one can extract from this type of file
            ProbeDesignation = ProbeType3;
            objectType = ObjectType.Free;
            return;
        }

        private void ParseScanDescription()
        {
            objectType = ObjectType.None;
            // first extract the respective chapter, also for later use
            scanSaveProcedure = ExtractChapterFromFile("1. Save procedure");
            scanProbeSystem = ExtractChapterFromFile("2. Probe system");
            scanFieldParameters = ExtractChapterFromFile("3. Scan field");
            scanComments = ExtractChapterFromFile("5. Additional comments");
            // and now parse the first three chapters
            ParseScanDescriptionForProcedure();
            ParseScanDescriptionForProbe();
            ParseScanDescriptionForFieldParameters();
        }

        private List<string> ExtractChapterFromFile(string chapter)
        {
            List<string> temp = new List<string>();
            bool inChapter = false;
            foreach (string s in fileContent)
            {
                if (inChapter == true)
                {
                    // magic string!
                    if (s.Contains("-----------------------------------------"))
                        return temp;
                    if (!string.IsNullOrWhiteSpace(s))
                        temp.Add(s); // no Trim() needed as fileContent is already trimmed.
                }
                if (s.Contains(chapter)) inChapter = true;
            }
            return temp;
        }

        private void ParseScanDescriptionForProbe()
        {
            SpmTechnique = "unknown SPM technique";
            foreach (string s in scanProbeSystem)
            {
                if (s.Contains("AFM"))
                {
                    SpmTechnique = "NC-AFM";
                    ProbeDesignation = ProbeType1;
                }

                if (s.Contains("Laser Focus"))
                {
                    SpmTechnique = "LFS";
                    ProbeDesignation = ProbeType2;
                }
            }
        }

        private void ParseScanDescriptionForProcedure()
        {
            string sTemp;
            foreach (string s in scanSaveProcedure)
            {
                if (s.Contains("Current data mask       : "))
                {
                    sTemp = s.Replace("Current data mask       : ", " ");
                    DataMask = long.Parse(sTemp, numFormat);
                }
            }
        }

        private void ParseScanDescriptionForFieldParameters()
        {
            string sTemp;
            foreach (string s in scanFieldParameters)
            {
                // read the dx and dy values to check the scan field dimensions.
                // a premature stop of the scan can cause an inconsistency.
                // dsc files of older versions do not contain dy
                if (s.Contains("Distance beetween points  : ")) // sic!
                {
                    sTemp = s.Replace("Distance beetween points  : ", " ");
                    sTemp = sTemp.Replace("[um]", " ");
                    sTemp = sTemp.Replace("[�m]", " ");
                    ScanFieldDeltaX = double.Parse(sTemp, numFormat);
                    ScanFieldDeltaX *= 1.0E-6; // convert to m
                }
                if (s.Contains("Distance beetween point  : ")) // sic!
                {
                    sTemp = s.Replace("Distance beetween point  : ", " ");
                    sTemp = sTemp.Replace("[um]", " ");
                    sTemp = sTemp.Replace("[�m]", " ");
                    ScanFieldDeltaX = double.Parse(sTemp, numFormat);
                    ScanFieldDeltaX *= 1.0E-6; // convert to m
                }
                if (s.Contains("Distance beetween lines   : ")) // sic!
                {
                    sTemp = s.Replace("Distance beetween lines   : ", " ");
                    sTemp = sTemp.Replace("[um]", " ");
                    sTemp = sTemp.Replace("[�m]", " ");
                    ScanFieldDeltaY = double.Parse(sTemp, numFormat);
                    ScanFieldDeltaY *= 1.0E-6; // convert to m
                }
                if (s.Contains("Scan line length :"))
                {
                    sTemp = s.Replace("Scan line length :", " ");
                    sTemp = sTemp.Replace("[um]", " ");
                    sTemp = sTemp.Replace("[�m]", " ");
                    ScanFieldDimensionX = double.Parse(sTemp, numFormat); // might be negative!
                    ScanFieldDimensionX *= 1.0E-6; // convert to m
                }
                if (s.Contains("Scan field width :"))
                {
                    sTemp = s.Replace("Scan field width :", " ");
                    sTemp = sTemp.Replace("[um]", " ");
                    sTemp = sTemp.Replace("[�m]", " ");
                    ScanFieldDimensionY = double.Parse(sTemp, numFormat); // might be negative!
                    ScanFieldDimensionY *= 1.0E-6; // convert to m
                }
                if (s.Contains("Scan angle  :"))
                {
                    sTemp = s.Replace("Scan angle  :", " ");
                    sTemp = sTemp.Replace("rad", " ");
                    ScanFieldRotation = double.Parse(sTemp, numFormat);
                    ScanFieldRotation *= 57.29577951308232087; // convert from rad to °
                }
                if (s.Contains("Scan speed  :"))
                {
                    sTemp = s.Replace("Scan speed  :", " ");
                    sTemp = sTemp.Replace("[um/s]", " ");
                    sTemp = sTemp.Replace("[�m/s]", " ");
                    ScanSpeed = double.Parse(sTemp, numFormat);
                    ScanSpeed *= 1.0e-6; // convert to m/s
                }
                if (s.Contains("Number of lines  :"))
                {
                    sTemp = s.Replace("Number of lines  :", " ");
                    NumberOfProfiles = int.Parse(sTemp, numFormat);
                }
                if (s.Contains("Number of scans  : "))
                {
                    sTemp = s.Replace("Number of scans  : ", " ");
                    NumberOfScans = int.Parse(sTemp, numFormat);
                }
            }
            // dsc files of older versions do not contain dy
            // estimate from field dimension and the profile number
            if (ScanFieldDeltaY == 0)
            {
                if (NumberOfProfiles > 1)
                {
                    ScanFieldDeltaY = Math.Abs(ScanFieldDimensionY / (NumberOfProfiles - 1));
                }
            }
        }

        // for housekeeping
        private ObjectType objectType = ObjectType.None;
        // the file is loaded in this bunch of strings
        private readonly List<string> fileContent = new List<string>();
        // chapters of scan description files
        private List<string> scanSaveProcedure = new List<string>(); // text lines following "1. Save procedure"
        private List<string> scanProbeSystem = new List<string>(); // text lines following "2. Probe system"
        private List<string> scanFieldParameters = new List<string>(); // text lines following "3. Scan field"
        private List<string> scanComments = new List<string>(); // text lines following "5. Additional comments"


        private readonly ScanMetaData metaData = new ScanMetaData();
        private static readonly NumberFormatInfo numFormat = new NumberFormatInfo() { NumberDecimalSeparator = "." };
        private const string ProbeType1 = "DME DualScope DS95";
        private const string ProbeType2 = "SIOS LFS-02";
        private const string ProbeType3 = "Xpress GannenXP";
    }
}

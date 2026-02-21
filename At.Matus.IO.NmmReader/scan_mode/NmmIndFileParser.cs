using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;

namespace At.Matus.IO.NmmReader.scan_mode
{
    /// <summary>
    /// This Class consumes the index files (*.ind) produced during a scan on the SIOS NMM.
    /// The data is provided by properties only, there are no public methods. 
    /// </summary>
    public class NmmIndFileParser
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="T:Bev.IO.NmmReader.NmmIndFileParser"/> class.
        /// </summary>
        /// <param name="fileName">Base name for the data files.</param>
        public NmmIndFileParser(NmmFileName fileName)
        {
            // try to load data from file(s)
            LoadIndexData(fileName.GetIndFileNameForScanIndex(ScanDirection.Forward), ScanDirection.Forward);
            LoadIndexData(fileName.GetIndFileNameForScanIndex(ScanDirection.Backward), ScanDirection.Backward);
            // analyze loaded data
            // also justifies backward profile number if needed.
            AnalyzeDirectionStatus();
            // determine nominal number of data points
            DetermineNominalDataPointNumber();
            // determine profiles with to few data points
            // the largest number is deemed as the nominal length
            FindShortProfiles(NominalDataPoints);
            // Creation date and duration
            EvaluateMeasurementTime();
        }


        public ScanDirectionStatus ScanStatus { get; private set; }
        public int NumberOfProfiles => ForwardProfileLengths.Count();
        public int SpuriousProfiles { get; private set; }
        public int NominalDataPoints { get; private set; }
        public int DataPointsGlitch { get; private set; }
        public long DataMask { get; private set; }
        public DateTime CreationDate { get; private set; }
        public TimeSpan ScanDuration { get; private set; }
        public int[] ProfilsDefectsForward { get; private set; }
        public int[] ProfilsDefectsBackward { get; private set; }
        public List<int> ForwardProfileLengths { get; private set; } = new List<int>();
        public List<int> BackwardProfileLengths { get; private set; } = new List<int>();
        public int SpuriousDataLines { get; private set; } // the number of sporious data lines in the backward scan file

        private void DetermineNominalDataPointNumber()
        {
            NominalDataPoints = 0;
            DataPointsGlitch = 0;
            if (ScanStatus == ScanDirectionStatus.NoData) return;
            if (ScanStatus == ScanDirectionStatus.Unknown) return;
            if (NumberOfProfiles < 1) return;
            NominalDataPoints = ForwardProfileLengths.Max();
            DataPointsGlitch = NominalDataPoints - ForwardProfileLengths.Min();
            if (ScanStatus == ScanDirectionStatus.ForwardAndBackward || ScanStatus == ScanDirectionStatus.ForwardAndBackwardJustified)
            {
                NominalDataPoints = Math.Max(NominalDataPoints, BackwardProfileLengths.Max());
                DataPointsGlitch = Math.Max(DataPointsGlitch, NominalDataPoints - BackwardProfileLengths.Min());
            }
        }

        private void EvaluateMeasurementTime()
        {
            if (forwardTimeStamps.Count == 0)
            {
                ScanDuration = TimeSpan.Zero;
                return;
            }
            forwardTimeStamps.AddRange(backwardTimeStamps);
            CreationDate = forwardTimeStamps.First();
            ScanDuration = forwardTimeStamps.Last() - CreationDate;
        }

        private void FindShortProfiles(int nominalProfileLength)
        {
            if (ForwardProfileLengths.Count() > 0)
            {
                ProfilsDefectsForward = new int[ForwardProfileLengths.Count()];
                for (int i = 0; i < ForwardProfileLengths.Count(); i++)
                {
                    ProfilsDefectsForward[i] = nominalProfileLength - ForwardProfileLengths[i];
                }
            }
            if (BackwardProfileLengths.Count() > 0)
            {
                ProfilsDefectsBackward = new int[BackwardProfileLengths.Count()];
                for (int i = 0; i < BackwardProfileLengths.Count(); i++)
                {
                    ProfilsDefectsBackward[i] = nominalProfileLength - BackwardProfileLengths[i];
                }
            }
        }

        private void AnalyzeDirectionStatus()
        {
            ScanStatus = ScanDirectionStatus.Unknown;
            SpuriousDataLines = 0;
            // no data at all
            if (ForwardProfileLengths.Count == 0)
            {
                ScanStatus = ScanDirectionStatus.NoData;
                return;
            }
            SpuriousProfiles = BackwardProfileLengths.Count - ForwardProfileLengths.Count;
            // backward scan data file invalid or not existing
            if (SpuriousProfiles < 0)
            {
                ScanStatus = ScanDirectionStatus.ForwardOnly;
                if (BackwardProfileLengths.Count != 0)
                    BackwardProfileLengths.Clear();
                SpuriousProfiles = 0;
                return;
            }
            // peculiar NMM file error
            if (SpuriousProfiles > 0)
            {
                ScanStatus = ScanDirectionStatus.ForwardAndBackwardJustified;
                for (int i = 0; i < SpuriousProfiles; i++)
                {
                    SpuriousDataLines += BackwardProfileLengths[i];
                }
                BackwardProfileLengths.RemoveRange(0, SpuriousProfiles);
                backwardTimeStamps.RemoveRange(0, SpuriousProfiles);
                return;
            }
            if (SpuriousProfiles == 0)
            {
                ScanStatus = ScanDirectionStatus.ForwardAndBackward;
                return;
            }
        }

        private void LoadIndexData(string fileName, ScanDirection direction)
        {
            if (!File.Exists(fileName))
            {
                return;
            }
            string line;
            StreamReader hFile = File.OpenText(fileName);
            // file creation date as a first guess, will be overwritten.
            if (direction == ScanDirection.Forward)
            {
                CreationDate = File.GetCreationTime(fileName);
            }
            while ((line = hFile.ReadLine()) != null)
                ParseDataLine(line, direction);
            if (hFile != null) hFile.Close();
        }

        private void ParseDataLine(string line, ScanDirection direction)
        {
            if (string.IsNullOrWhiteSpace(line)) return;
            char[] charSeparators = { ';' }; // fields are separated by semicolons
            string[] tokens = line.Split(charSeparators, StringSplitOptions.RemoveEmptyEntries);
            if (tokens.Length != 5) return;
            DataMask = long.Parse(tokens[2]);   // the data mask is hopefully always the same
            int scanLineLength = int.Parse(tokens[1]);
            DateTime timeStamp = ParseTimeToken(tokens[0]);
            if (direction == ScanDirection.Forward)
            {
                ForwardProfileLengths.Add(scanLineLength);
                forwardTimeStamps.Add(timeStamp);
            }
            if (direction == ScanDirection.Backward)
            {
                BackwardProfileLengths.Add(scanLineLength);
                backwardTimeStamps.Add(timeStamp);
            }
        }

        private DateTime ParseTimeToken(string token)
        {
            token = token.Trim();
            DateTime timeStamp;
            try
            {
                timeStamp = DateTime.ParseExact(token, "dd-MMM-yyyy HH:mm:ss", new CultureInfo("de-DE")); // 01-Jan-2014 13:15:10
                return timeStamp;
            }
            catch (FormatException) { } // fall through
            try
            {
                timeStamp = DateTime.ParseExact(token, "dd-MMM-yyyy HH:mm:ss", new CultureInfo("de-AT")); // 01-Jän-2014 13:15:10
                return timeStamp;
            }
            catch (FormatException) { } // fall through
            try
            {
                timeStamp = DateTime.ParseExact(token, "dd-MMM-yyyy HH:mm:ss", CultureInfo.InvariantCulture); // fallback
                return timeStamp;
            }
            catch (FormatException) { } // fall through
            return DateTime.UtcNow;
        }

        private readonly List<DateTime> forwardTimeStamps = new List<DateTime>();
        private readonly List<DateTime> backwardTimeStamps = new List<DateTime>();

    }
}

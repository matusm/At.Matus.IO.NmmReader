//****************************************************************************************
//
// Class to store the topographic surface scan data of the SIOS NMM.
//
// Usage:
// 1.) create an instance of the ScanMetaData class of the respective data files
// 2.) create an instance of TopographyData (this class) with the
//     ScanMetaData object as parameter
// 3.) populate this object with data line by line using InsertDataLineAt()
// 4.) now profiles can be extracted in any order by ExtractProfile(). 
//     The profiles are identified by their numerical index
// 
// The method InsertDataLineAt() is optimized for useage by a simple file parser.
// Profile index 0 is interpreted as returning all profiles at once by ExtractProfile().
// Hence profiles are enumerated starting at 1
// 
// Author: Michael Matus, 2019-2022
//
//****************************************************************************************

using System;
using System.Linq;

namespace At.Matus.IO.NmmReader.scan_mode
{
    public class TopographyData
    {
        public TopographyData(ScanMetaData scanMetaData)
        {
            this.scanMetaData = scanMetaData;
            NumberOfColumns = scanMetaData.NumberOfColumnsInFile;
            NumberOfProfiles = scanMetaData.NumberOfProfiles;
            NumberOfPointsPerProfile = scanMetaData.NumberOfDataPoints;
            NumberTotalPoints = NumberOfPointsPerProfile * NumberOfProfiles;
            columnNumberOfXYvec = GetColumnIndexFor("XYvec"); // magic string!
            switch (scanMetaData.ScanStatus)
            {
                case ScanDirectionStatus.ForwardOnly:
                    fwdMatrix = new double[NumberOfColumns, NumberTotalPoints];
                    ClearTopographyData(ScanDirection.Forward);
                    bwdMatrix = null;
                    break;
                case ScanDirectionStatus.ForwardAndBackward:
                case ScanDirectionStatus.ForwardAndBackwardJustified:
                    fwdMatrix = new double[NumberOfColumns, NumberTotalPoints];
                    bwdMatrix = new double[NumberOfColumns, NumberTotalPoints];
                    ClearTopographyData(ScanDirection.Forward);
                    ClearTopographyData(ScanDirection.Backward);
                    break;
                case ScanDirectionStatus.Unknown:
                case ScanDirectionStatus.NoData:
                    fwdMatrix = null;
                    bwdMatrix = null;
                    break;
            }
        }

        public int NumberOfProfiles { get; private set; }
        public int NumberTotalPoints { get; private set; }
        public int NumberOfColumns { get; private set; }
        public int NumberOfPointsPerProfile { get; private set; }

        // this is used for Heydemann correction measures
        // the whole field (= all profiles) must be provided at once
        internal void InsertColumnFor(int columnIndex, double[] field, ScanDirection scanDirection)
        {
            if (columnIndex >= NumberOfColumns) return;
            if (columnIndex < 0) return;
            if (field.Length != NumberTotalPoints) return;
            if (scanDirection == ScanDirection.Forward)
            {
                if (fwdMatrix == null) return;
                for (int i = 0; i < field.Length; i++)
                {
                    fwdMatrix[columnIndex, i] = field[i];
                }
            }
            if (scanDirection == ScanDirection.Backward)
            {
                if (bwdMatrix == null) return;
                for (int i = 0; i < field.Length; i++)
                {
                    bwdMatrix[columnIndex, i] = field[i];
                }
            }
        }

        // This is used to populate the matrices line by line (usually during the file reading)
        public void InsertDataLineAt(double[] dataLine, int position, ScanDirection scanDirection)
        {
            // some range checks
            if (position >= NumberTotalPoints) return;
            if (position < 0) return;
            if (dataLine == null) return;
            if (dataLine.Length != NumberOfColumns) return;
            if (scanDirection == ScanDirection.Forward)
            {
                if (fwdMatrix == null) return;
                for (int i = 0; i < NumberOfColumns; i++)
                {
                    fwdMatrix[i, position] = dataLine[i];
                }
                return;
            }
            if (scanDirection == ScanDirection.Backward)
            {
                if (bwdMatrix == null) return;
                for (int i = 0; i < NumberOfColumns; i++)
                {
                    bwdMatrix[i, position] = dataLine[i];
                }
            }
        }

        public double[] ExtractProfile(int column, int profileIndex, TopographyProcessType type)
        {
            // some range checks
            if (column < 0) return InvalidProfile();
            if (column >= NumberOfColumns) return InvalidProfile();
            if (profileIndex < 0) return InvalidProfile();
            if (profileIndex > scanMetaData.NumberOfProfiles) return InvalidProfile(); // starts at 1 !
            if (profileIndex == 0)
                return ProcessTwoProfiles(AllProfiles(column, ScanDirection.Forward), AllProfiles(column, ScanDirection.Backward), type);
            return ProcessTwoProfiles(SingleProfile(column, profileIndex, ScanDirection.Forward), SingleProfile(column, profileIndex, ScanDirection.Backward), type);
        }

        private double[] ProcessTwoProfiles(double[] fwdProfile, double[] bwdProfile, TopographyProcessType type)
        {
            if (fwdProfile.Length != bwdProfile.Length) return null;
            double[] resultProfile = new double[fwdProfile.Length];
            switch (type)
            {
                case TopographyProcessType.None:
                    return InvalidProfile();
                case TopographyProcessType.ForwardOnly:
                    return fwdProfile;
                case TopographyProcessType.BackwardOnly:
                    return bwdProfile;
                case TopographyProcessType.Average:
                    for (int i = 0; i < fwdProfile.Length; i++)
                    {
                        resultProfile[i] = (fwdProfile[i] + bwdProfile[i]) * 0.5;
                    }
                    break;
                case TopographyProcessType.Difference:
                    for (int i = 0; i < fwdProfile.Length; i++)
                    {
                        resultProfile[i] = fwdProfile[i] - bwdProfile[i];
                    }
                    break;
            }
            return resultProfile;
        }

        private double[] SingleProfile(int column, int profileIndex, ScanDirection scanDirection)
        {
            // range checks are performed already
            double[] resultProfile = new double[scanMetaData.NumberOfDataPoints];
            int offset = scanMetaData.NumberOfDataPoints * (profileIndex - 1);
            if (scanDirection == ScanDirection.Forward)
            {
                if (fwdMatrix == null) return InvalidProfile();
                for (int i = 0; i < scanMetaData.NumberOfDataPoints; i++)
                {
                    resultProfile[i] = fwdMatrix[column, i + offset];
                }
            }
            if (scanDirection == ScanDirection.Backward)
            {
                if (bwdMatrix == null) return InvalidProfile();
                for (int i = 0; i < scanMetaData.NumberOfDataPoints; i++)
                {
                    resultProfile[i] = bwdMatrix[column, i + offset];
                }
                // if profile == XYvec than reverse profile
                if (column == columnNumberOfXYvec)
                {
                    Array.Reverse(resultProfile);
                }
            }
            return resultProfile;
        }

        private double[] AllProfiles(int column, ScanDirection scanDirection)
        {
            // range checks are performed already
            double[] resultProfile = new double[NumberTotalPoints];
            if (scanDirection == ScanDirection.Forward)
            {
                if (fwdMatrix == null) return InvalidProfileAll();
                for (int i = 0; i < NumberTotalPoints; i++)
                {
                    resultProfile[i] = fwdMatrix[column, i];
                }
            }
            if (scanDirection == ScanDirection.Backward)
            {
                if (bwdMatrix == null) return InvalidProfileAll();
                for (int i = 0; i < NumberTotalPoints; i++)
                {
                    resultProfile[i] = bwdMatrix[column, i];
                }
                // if profile == XYvec than reverse profile
                if (column == columnNumberOfXYvec)
                {
                    Array.Reverse(resultProfile);
                }
            }
            return resultProfile;
        }

        private void ClearTopographyData(ScanDirection direction)
        {
            double[] invalidDataLine = Enumerable.Repeat(double.NaN, NumberOfColumns).ToArray();
            for (int i = 0; i < NumberTotalPoints; i++)
            {
                InsertDataLineAt(invalidDataLine, i, direction);
            }
        }

        private double[] InvalidProfile() => Enumerable.Repeat(double.NaN, NumberOfPointsPerProfile).ToArray();

        private double[] InvalidProfileAll() => Enumerable.Repeat(double.NaN, NumberTotalPoints).ToArray();

        // an equivalent method is defined also in NmmScanData class!
        // only needed once in the constructor
        private int GetColumnIndexFor(string columnSymbol)
        {
            for (int i = 0; i < scanMetaData.NumberOfColumnsInFile; i++)
            {
                if (scanMetaData.ColumnPredicates[i].IsOf(columnSymbol)) return i;
            }
            return -1;
        }

        private readonly double[,] fwdMatrix;
        private readonly double[,] bwdMatrix;
        private readonly ScanMetaData scanMetaData;
        private readonly int columnNumberOfXYvec;

    }
}

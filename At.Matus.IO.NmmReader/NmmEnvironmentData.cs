using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;

namespace At.Matus.IO.NmmReader
{
    public class NmmEnvironmentData
    {
        private static readonly double referenceTemperature = 20;
        private static readonly double referencePressure = 101300;
        private static readonly double referenceHumidity = 50;
        private static readonly NumberFormatInfo numFormat = new NumberFormatInfo() { NumberDecimalSeparator = "." };

        public NmmEnvironmentData(NmmFileName fileName)
        {
            airTemperatureOrigin = AirTemperatureOrigin.Unknown;
            sampleTemperatureOrigin = SampleTemperatureOrigin.Unknown;
            fileStructure = FileStructure.NoFile;
            // the following analysis on the file structure is a nightmare
            LoadSensorData(fileName.GetPosFileNameForScanIndex(ScanDirection.Forward));
            int numberOfForwardSamples = xTemperatureValues.Count;
            if (numberOfForwardSamples > 0)
                fileStructure = FileStructure.FwdFileOnly;
            LoadSensorData(fileName.GetPosFileNameForScanIndex(ScanDirection.Backward));
            if (xTemperatureValues.Count > numberOfForwardSamples)
            {
                if (fileStructure == FileStructure.FwdFileOnly)
                {
                    fileStructure = FileStructure.FwdBwdFile;
                    if (xTemperatureValues.Count == 2 * numberOfForwardSamples)
                    {
                        fileStructure = FileStructure.ValidFwdBwdFile;
                    }
                }
                else
                {
                    fileStructure = FileStructure.BwdFileOnly;
                }
            }
            AnalyzeSensorOrigin();
        }

        public string AirSampleSourceText => StatusDescription();
        public EnvironmentDataStatus AirSampleSource { get; private set; }
        public int NumberOfAirSamples => xTemperatureValues.Count;
        public double AirTemperature => (XTemperature + YTemperature + ZTemperature) / 3.0;
        public double AirTemperatureDrift => EstimateAirTemperatureRange();
        public double AirTemparatureGradient => EstimateAirTemperatureHomogeneity();
        public double[] AirTemperatureSeries => GetAirTemperatureSeries();
        public double SampleTemperature => GetSampleTemperature();
        public double SampleTemperatureDrift => GetSampleTemperatureRange();
        public double[] SampleTemperatureSeries => UnscrambleValues(sTemperatureValues);
        public double RelativeHumidity => EvaluateMean(humidityValues, referenceHumidity);
        public double RelativeHumidityDrift => EvaluateRange(humidityValues);
        public double[] RelativeHumiditySeries => UnscrambleValues(humidityValues);
        public double BarometricPressure => EvaluateMean(pressureValues, referencePressure);
        public double BarometricPressureDrift => EvaluateRange(pressureValues);
        public double[] BarometricPressureSeries => UnscrambleValues(pressureValues);
        public double XTemperature => EvaluateMean(xTemperatureValues, referenceTemperature);
        public double YTemperature => EvaluateMean(yTemperatureValues, referenceTemperature);
        public double ZTemperature => EvaluateMean(zTemperatureValues, referenceTemperature);

        private void LoadSensorData(string fileName)
        {
            try
            {
                string line;
                StreamReader hFile = File.OpenText(fileName);
                while ((line = hFile.ReadLine()) != null)
                    ParseDataLine(line);
                hFile.Close();
            }
            catch (Exception)
            {
                // file error, ignore
            }
        }

        private void ParseDataLine(string line)
        {
            if (string.IsNullOrWhiteSpace(line))
                return;
            char[] charSeparators = { ' ', '\t' };
            string[] tokens = line.Split(charSeparators, StringSplitOptions.RemoveEmptyEntries);
            try
            {
                switch (tokens.Length)
                {
                    case 5: // 3d file without sample sensor
                        double xt = double.Parse(tokens[0], numFormat);
                        double yt = double.Parse(tokens[1], numFormat);
                        double zt = double.Parse(tokens[2], numFormat);
                        double st = (xt + yt + zt) / 3;
                        xTemperatureValues.Add(xt);
                        yTemperatureValues.Add(yt);
                        zTemperatureValues.Add(zt);
                        sTemperatureValues.Add(st);
                        pressureValues.Add(double.Parse(tokens[3], numFormat));
                        humidityValues.Add(double.Parse(tokens[4], numFormat));
                        airTemperatureOrigin = AirTemperatureOrigin.MeasuredBySensor;
                        sampleTemperatureOrigin = SampleTemperatureOrigin.EstimatedFromAir;
                        break;
                    case 6: // 3d file with sample sensor
                        xTemperatureValues.Add(double.Parse(tokens[0], numFormat));
                        yTemperatureValues.Add(double.Parse(tokens[1], numFormat));
                        zTemperatureValues.Add(double.Parse(tokens[2], numFormat));
                        pressureValues.Add(double.Parse(tokens[3], numFormat));
                        humidityValues.Add(double.Parse(tokens[4], numFormat));
                        sTemperatureValues.Add(double.Parse(tokens[5], numFormat));
                        airTemperatureOrigin = AirTemperatureOrigin.MeasuredBySensor;
                        sampleTemperatureOrigin = SampleTemperatureOrigin.MeasuredBySensor;
                        break;
                    case 8: // scan file without sample sensor
                        xt = double.Parse(tokens[3], numFormat);
                        yt = double.Parse(tokens[4], numFormat);
                        zt = double.Parse(tokens[5], numFormat);
                        st = (xt + yt + zt) / 3;
                        xTemperatureValues.Add(xt);
                        yTemperatureValues.Add(yt);
                        zTemperatureValues.Add(zt);
                        sTemperatureValues.Add(st);
                        pressureValues.Add(double.Parse(tokens[6], numFormat));
                        humidityValues.Add(double.Parse(tokens[7], numFormat));
                        airTemperatureOrigin = AirTemperatureOrigin.MeasuredBySensor;
                        sampleTemperatureOrigin = SampleTemperatureOrigin.EstimatedFromAir;
                        break;
                    case 9: // scan file with sample sensor
                        xTemperatureValues.Add(double.Parse(tokens[3], numFormat));
                        yTemperatureValues.Add(double.Parse(tokens[4], numFormat));
                        zTemperatureValues.Add(double.Parse(tokens[5], numFormat));
                        pressureValues.Add(double.Parse(tokens[6], numFormat));
                        humidityValues.Add(double.Parse(tokens[7], numFormat));
                        sTemperatureValues.Add(double.Parse(tokens[8], numFormat));
                        airTemperatureOrigin = AirTemperatureOrigin.MeasuredBySensor;
                        sampleTemperatureOrigin = SampleTemperatureOrigin.MeasuredBySensor;
                        break;
                    default:
                        return;
                }
            }
            catch (Exception)
            {
                // most probably a System.FormatException occured
                // just ignore
            }
        }

        private double GetSampleTemperature()
        {
            if (sampleTemperatureOrigin == SampleTemperatureOrigin.EstimatedFromAir)
                return AirTemperature;
            return EvaluateMean(sTemperatureValues, referenceTemperature);
        }

        private double GetSampleTemperatureRange()
        {
            if (sampleTemperatureOrigin == SampleTemperatureOrigin.EstimatedFromAir)
                return AirTemperatureDrift;
            return EvaluateRange(sTemperatureValues);
        }

        private double[] GetAirTemperatureSeries()
        {
            var tempX = UnscrambleValues(xTemperatureValues);
            var tempY = UnscrambleValues(yTemperatureValues);
            var tempZ = UnscrambleValues(zTemperatureValues);
            double[] average = new double[tempX.Length];
            for (int i = 0; i < average.Length; i++)
            {
                average[i] = (tempX[i] + tempY[i] + tempZ[i]) / 3.0;
            }
            return average;
        }

        private string StatusDescription()
        {
            switch (AirSampleSource)
            {
                case EnvironmentDataStatus.Unknown:
                    return "This should not happen!";
                case EnvironmentDataStatus.NoDataProvided:
                    return "File(s) not found or invalid, using default values.";
                case EnvironmentDataStatus.DefaultValues:
                    return "File(s) contained default values only (Instrument malfunction).";
                case EnvironmentDataStatus.MeasuredValues:
                    return "All parameters (including sample temperature) recorded.";
                case EnvironmentDataStatus.SampleEstimatedbyAir:
                    return "Air parameters recorded, sample temperature estimated by air temperature.";
                default:
                    return "";
            }
        }

        private void AnalyzeSensorOrigin()
        {
            if (NumberOfAirSamples == 0)
            {
                airTemperatureOrigin = AirTemperatureOrigin.DfaultValues;
                sampleTemperatureOrigin = SampleTemperatureOrigin.DefaultValues;
            }
            else
            {
                if (AirTemperature == 20.0 && AirTemperatureDrift == 0.0)
                    airTemperatureOrigin = AirTemperatureOrigin.DfaultValues;
                if (SampleTemperature == 20.0 && SampleTemperatureDrift == 0.0)
                    sampleTemperatureOrigin = SampleTemperatureOrigin.DefaultValues;
            }
            SubsumizeStatus();
        }

        private void SubsumizeStatus()
        {
            AirSampleSource = EnvironmentDataStatus.Unknown;
            if (NumberOfAirSamples == 0)
            {
                AirSampleSource = EnvironmentDataStatus.NoDataProvided;
                return;
            }
            if (airTemperatureOrigin == AirTemperatureOrigin.MeasuredBySensor)
            {
                switch (sampleTemperatureOrigin)
                {
                    case SampleTemperatureOrigin.MeasuredBySensor:
                        AirSampleSource = EnvironmentDataStatus.MeasuredValues;
                        return;
                    case SampleTemperatureOrigin.EstimatedFromAir:
                        AirSampleSource = EnvironmentDataStatus.SampleEstimatedbyAir;
                        return;
                    default:
                        return;
                }
            }
            if (airTemperatureOrigin == AirTemperatureOrigin.DfaultValues && sampleTemperatureOrigin == SampleTemperatureOrigin.DefaultValues)
            {
                AirSampleSource = EnvironmentDataStatus.DefaultValues;
                return;
            }
        }

        private double EvaluateMean(List<double> values, double defaultValue)
        {
            if (values.Count == 0)
                return defaultValue;
            return values.Average();
        }

        private double EvaluateRange(List<double> values)
        {
            if (values.Count <= 1)
                return 0.0;
            return values.Max() - values.Min();
        }

        private double EstimateAirTemperatureHomogeneity()
        {
            double t1 = XTemperature;
            double t2 = YTemperature;
            double t3 = ZTemperature;
            double tMax = t1;
            if (t2 > tMax) tMax = t2;
            if (t3 > tMax) tMax = t3;
            double tMin = t1;
            if (t2 < tMin) tMin = t2;
            if (t3 < tMin) tMin = t3;
            return tMax - tMin;
        }

        private double EstimateAirTemperatureRange()
        {
            return (EvaluateRange(xTemperatureValues) + EvaluateRange(yTemperatureValues) + EvaluateRange(zTemperatureValues)) / 3.0;
        }

        // when using forward and backward scan data the order of sensor values is scrammbeled
        // this function brings them in chronological order
        private double[] UnscrambleValues(List<double> scrambeldValues)
        {
            double[] temp = scrambeldValues.ToArray();
            if (fileStructure != FileStructure.ValidFwdBwdFile) // TODO what happens with NoFile? FwdBwdFile?
            {
                return temp;
            }
            double[] series = new double[temp.Length];
            for (int i = 0; i < temp.Length; i++)
            {
                if (i % 2 == 0)
                {
                    series[i] = temp[i / 2];
                }
                else
                {
                    series[i] = temp[(temp.Length + i) / 2];
                }
            }
            return series;
        }

        // fields for housekeeping purposes
        private AirTemperatureOrigin airTemperatureOrigin;
        private SampleTemperatureOrigin sampleTemperatureOrigin;
        private FileStructure fileStructure;
        // actual data as provided by file(s)
        private readonly List<double> xTemperatureValues = new List<double>();
        private readonly List<double> yTemperatureValues = new List<double>();
        private readonly List<double> zTemperatureValues = new List<double>();
        private readonly List<double> sTemperatureValues = new List<double>();
        private readonly List<double> humidityValues = new List<double>();
        private readonly List<double> pressureValues = new List<double>();

    }

    internal enum FileStructure
    {
        NoFile,             // no *.pos file was read (or data found).
        FwdFileOnly,        // only a single *.pos file was read. From a forward scan or 3D-measurement. 
        BwdFileOnly,        // only a single *b.pos file was read. From a backward scan. Unusual.
        FwdBwdFile,         // both files from a scan read, but not of equal size.
        ValidFwdBwdFile     // both files from a scan read, of equal size and thus valid.
    }

    internal enum AirTemperatureOrigin
    {
        Unknown,
        MeasuredBySensor,
        DfaultValues
    }

    internal enum SampleTemperatureOrigin
    {
        Unknown,
        MeasuredBySensor,
        EstimatedFromAir,
        DefaultValues
    }

}

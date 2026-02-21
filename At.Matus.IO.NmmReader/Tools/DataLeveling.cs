using System.Linq;

namespace At.Matus.IO.NmmReader.Tools
{
    public class DataLeveling
    {
        public DataLeveling(double[] rawData, int numPoints, int numProfiles)
        {
            this.rawData = rawData;
            this.numPoints = numPoints;
            this.numProfiles = numProfiles;
            Mode = ReferenceTo.None;
        }

        public DataLeveling(double[] rawData, int numPoints) : this(rawData, numPoints, 1) { }

        // misc
        public ReferenceTo Mode { get; private set; }
        public string LevelModeDescription => ModeToString(Mode);
        // z
        public double BiasValue { get; set; }
        public double MaximumValue => rawData.Max();
        public double MinimumValue => rawData.Min();
        public double AverageValue => rawData.Average();
        public double CentralValue => (MaximumValue + MinimumValue) / 2.0;
        // x
        public double FirstValue => rawData[0];
        public double LastValue => rawData.Last();
        public double CenterValue => rawData[rawData.Length / 2];

        public double[] LevelData(ReferenceTo mode)
        {
            slopeX = 0.0;
            slopeY = 0.0;
            intercept = 0.0;
            switch (GetDataType())
            {
                case DataType.Unknown:
                    Mode = ReferenceTo.None;
                    return rawData;
                case DataType.Profile:
                    Mode = mode;
                    return LevelProfileData(mode);
                case DataType.Raster:
                    Mode = mode;
                    return LevelRasterData(mode);
                default:
                    Mode = ReferenceTo.None;
                    return rawData;
            }
        }

        private double[] LevelProfileData(ReferenceTo mode)
        {
            double[] leveledData = new double[rawData.Length];
            // calculate mode dependend parameters
            switch (mode)
            {
                case ReferenceTo.None:
                    break;
                case ReferenceTo.First:
                    intercept = FirstValue;
                    break;
                case ReferenceTo.Last:
                    intercept = LastValue;
                    break;
                case ReferenceTo.Center:
                    intercept = CenterValue;
                    break;
                case ReferenceTo.Minimum:
                    intercept = MinimumValue;
                    break;
                case ReferenceTo.Maximum:
                    intercept = MaximumValue;
                    break;
                case ReferenceTo.Average:
                    intercept = AverageValue;
                    break;
                case ReferenceTo.Central:
                    intercept = CentralValue;
                    break;
                case ReferenceTo.Bias:
                    intercept = BiasValue;
                    break;
                case ReferenceTo.Line:
                case ReferenceTo.LinePositive:
                    intercept = FirstValue;
                    slopeX = (LastValue - FirstValue) / (rawData.Length - 1);
                    break;
                case ReferenceTo.Lsq:
                case ReferenceTo.LsqPositive:
                    FitLsqLine();
                    break;
            }
            // now level the data
            for (int i = 0; i < rawData.Length; i++)
            {
                leveledData[i] = sign * (rawData[i] - (intercept + (slopeX * (double)i)));
            }
            // check if positive values were asked for
            if (mode == ReferenceTo.LinePositive || mode == ReferenceTo.LsqPositive)
            {
                double min = leveledData.Min();
                for (int i = 0; i < leveledData.Length; i++)
                    leveledData[i] = leveledData[i] - min;
            }
            return leveledData;
        }

        private double[] LevelRasterData(ReferenceTo mode)
        {
            double[] leveledData = new double[rawData.Length];
            // calculate mode dependend parameters
            switch (mode)
            {
                case ReferenceTo.None:
                    break;
                case ReferenceTo.First:
                    intercept = FirstValue;
                    break;
                case ReferenceTo.Last:
                    intercept = LastValue;
                    break;
                case ReferenceTo.Center:
                    intercept = CenterValue;
                    break;
                case ReferenceTo.Minimum:
                    intercept = MinimumValue;
                    break;
                case ReferenceTo.Maximum:
                    intercept = MaximumValue;
                    break;
                case ReferenceTo.Average:
                    intercept = AverageValue;
                    break;
                case ReferenceTo.Central:
                    intercept = CentralValue;
                    break;
                case ReferenceTo.Bias:
                    intercept = BiasValue;
                    break;
                case ReferenceTo.Line:
                case ReferenceTo.LinePositive:
                    intercept = FirstValue;
                    slopeX = (rawData[numPoints - 1] - FirstValue) / (numPoints - 1);
                    slopeY = (rawData[rawData.Length - numPoints] - FirstValue) / (numProfiles - 1);
                    break;
                case ReferenceTo.Lsq:
                case ReferenceTo.LsqPositive:
                    FitLsqPlane();
                    break;
            }
            // now level the data
            for (int i = 0; i < numPoints; i++)
                for (int j = 0; j < numProfiles; j++)
                    leveledData[i + j * numPoints] = sign * (rawData[i + (j * numPoints)] - (intercept + (slopeX * (double)i) + (slopeY * (double)j)));
            // check if positive values were asked for
            if (mode == ReferenceTo.LinePositive || mode == ReferenceTo.LsqPositive)
            {
                double min = leveledData.Min();
                for (int i = 0; i < leveledData.Length; i++)
                    leveledData[i] = leveledData[i] - min;
            }
            return leveledData;
        }

        // Fits a least square line to raster data
        // works with equidistant spacing only !
        private void FitLsqLine()
        {
            int n = rawData.Length;
            double[] x = new double[n];
            double[] xx = new double[n];
            double[] xy = new double[n];
            for (int i = 0; i < n; i++)
            {
                x[i] = (double)i;
                xx[i] = (double)i * (double)i;
                xy[i] = rawData[i] * (double)i;
            }
            double sigmaX = x.Sum();
            double sigmaY = rawData.Sum();
            double sigmaXX = xx.Sum();
            double sigmaXY = xy.Sum();
            slopeX = ((n * sigmaXY) - (sigmaX * sigmaY)) / ((n * sigmaXX) - (sigmaX * sigmaX));
            intercept = (sigmaY - (slopeX * sigmaX)) / (double)n;
            // The algorithm used is a straightforward implementation.
            // taking into account the arithmetic series of the x-array, a more efficient calculation would be possible
            // TODO find efficient implementation!
            //slopeX = (12.0 * sigmaXY - 6.0 * (n - 1.0) * rawData.Sum()) / (n * (n * n + 1.0));
            //intercept = AverageValue - 0.5 * slopeX * (n * (n - 1.0));
        }

        // Fits a least square plane to raster data
        // works only with rectangular ordered data with equidistant spacing
        // spacing in x and y can be different, however
        // implementation according to EUNA 15178 ENC eq. (9.7)
        private void FitLsqPlane()
        {
            double u = 0.0, v = 0.0, w = 0.0;
            int M = numPoints;
            int N = numProfiles;
            // the casting from int to double is essential - M⁴ 
            double fM = (double)M;
            double fN = (double)N;
            // u
            for (int l = 0; l < N; l++)
                for (int k = 0; k < M; k++)
                    u += k * rawData[k + l * M];
            // v
            for (int l = 0; l < N; l++)
                for (int k = 0; k < M; k++)
                    v += l * rawData[k + l * M];
            // w
            w = rawData.Sum();
            // (9-7)
            double a = ((7 * fM * fN + fM + fN - 5) * w - 6 * u * (fN + 1) - 6 * v * (fM + 1)) / (fM * fN * (fM + 1) * (fN + 1));
            double b = (12 * u - 6 * w * (fM - 1)) / (fM * fN * (fM - 1) * (fM + 1));
            double c = (12 * v - 6 * w * (fN - 1)) / (fM * fN * (fN - 1) * (fN + 1));
            // dx und dy = 1 !
            // translate parameter names
            intercept = a;
            slopeX = b;
            slopeY = c;
        }

        private string ModeToString(ReferenceTo mode)
        {
            switch (GetDataType())
            {
                case DataType.Unknown:
                    return "Data not leveled";
                case DataType.Profile:
                    return ModeProfileToString(mode);
                case DataType.Raster:
                    return ModeRasterToString(mode);
                default:
                    return "Data not leveled"; ;
            }

        }

        private string ModeProfileToString(ReferenceTo mode)
        {
            switch (mode)
            {
                case ReferenceTo.None:
                    return "Profile not referenced";
                case ReferenceTo.Average:
                    return $"Profile referenced to avarage height value ({intercept})";
                case ReferenceTo.Bias:
                    return $"Profile referenced to user supplied value ({BiasValue})";
                case ReferenceTo.Center:
                    return $"Profile referenced to central value of trace ({intercept})";
                case ReferenceTo.Central:
                    return $"Profile referenced to mid height value ({intercept})";
                case ReferenceTo.First:
                    return $"Profile referenced to first value of trace ({intercept})";
                case ReferenceTo.Last:
                    return $"Profile referenced to last value of trace ({intercept})";
                case ReferenceTo.Maximum:
                    return $"Profile referenced to maximum height value ({intercept})";
                case ReferenceTo.Minimum:
                    return $"Profile referenced to minimum height value ({intercept})";
                case ReferenceTo.Line:
                    return "Profile leveled to line connecting boundary points";
                case ReferenceTo.Lsq:
                    return "Profile leveled to least square line";
                case ReferenceTo.LinePositive:
                    return "Profile leveled parallel to line connecting boundary points, always positive";
                case ReferenceTo.LsqPositive:
                    return "Profile leveled parallel to least square line, always positive";
                default:
                    return "this should not happen!";
            }
        }

        private string ModeRasterToString(ReferenceTo mode)
        {
            switch (mode)
            {
                case ReferenceTo.None:
                    return "Surface not referenced";
                case ReferenceTo.Average:
                    return $"Surface referenced to avarage height value ({intercept})";
                case ReferenceTo.Bias:
                    return $"Surface referenced to user supplied value ({BiasValue})";
                case ReferenceTo.Center:
                    return $"Surface referenced to central value of array ({intercept})";
                case ReferenceTo.Central:
                    return $"Surface referenced to mid height value ({intercept})";
                case ReferenceTo.First:
                    return $"Surface referenced to first value of array ({intercept})";
                case ReferenceTo.Last:
                    return $"Surface referenced to last value of array ({intercept})";
                case ReferenceTo.Maximum:
                    return $"Surface referenced to maximum height value ({intercept})";
                case ReferenceTo.Minimum:
                    return $"Surface referenced to minimum height value ({intercept})";
                case ReferenceTo.Line:
                    return "Three point surface leveling";
                case ReferenceTo.Lsq:
                    return "Surface leveled to least square plane";
                case ReferenceTo.LinePositive:
                    return "Three point surface leveling, always positive";
                case ReferenceTo.LsqPositive:
                    return "Surface leveled parallel to least square plane, always positive";
                default:
                    return "this should not happen!";
            }
        }

        private DataType GetDataType()
        {
            if (rawData.Length != numPoints * numProfiles) return DataType.Unknown;
            if (numPoints <= 0) return DataType.Unknown;
            if (numProfiles <= 0) return DataType.Unknown;
            if (numProfiles == 1) return DataType.Profile;
            return DataType.Raster;
        }

        // private fields
        private readonly double[] rawData;
        private readonly int numPoints;
        private readonly int numProfiles;
        // fields for leveling methods
        private double intercept; // constant part to be subtracted
        private double slopeX; // X-length dependend part to be subtracted
        private double slopeY; // Y-length dependend part to be subtracted
        private readonly double sign = 1.0; // sign factor (+1/-1); currently not used

    }

    // some handy enums

    public enum ReferenceTo
    {
        None,          // do not change the height data
        Minimum,       // reference height data to minimal z-value
        Maximum,       // reference height data to maximal z-value
        Average,       // reference height data to arithmetic mean z-value
        Central,       // reference height data to mid of span z-value
        Bias,          // reference height data to user defined bias z-value
        First,         // reference height data to first value
        Last,          // reference height data to last value
        Center,        // reference height data to center value
        Line,          // subtract linear line (first to last point) / three point plane
        Lsq,           // subtract least square linear line / plane
        LsqPositive,   // subtract least square linear line / plane and shift to positve values
        LinePositive   // subtract linear line (first to last point) / three point plane and shift to positve values
    }

    public enum DataType
    {
        Unknown,    // neither a single profile nor raster data
        Profile,    // data represents a single profile
        Raster      // data represents a recangular raster
    }


}

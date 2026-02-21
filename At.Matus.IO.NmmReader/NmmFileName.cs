using System.IO;

namespace At.Matus.IO.NmmReader
{
    public class NmmFileName
    {
        public NmmFileName(string path)
        {
            BasePath = Path.ChangeExtension(path, null);
        }

        public string BaseFileName => Path.GetFileNameWithoutExtension(BasePath);
        public string BasePath { get; }
        public int ScanIndex { get; private set; } = 0;

        // setting index to 0 is used for a single scan without special filenames
        public void SetScanIndex(int index)
        {
            if (index < 0) index = 0;
            ScanIndex = index;
        }

        public string GetDatFileNameForScanIndex(ScanDirection direction) => GetFileName(direction, "dat", ScanIndex);

        public string GetPosFileNameForScanIndex(ScanDirection direction) => GetFileName(direction, "pos", ScanIndex);

        public string GetIndFileNameForScanIndex(ScanDirection direction) => GetFileName(direction, "ind", ScanIndex);

        public string GetDscFileNameForScanIndex(ScanDirection direction) => GetFileName(direction, "dsc", ScanIndex);

        // overloads without parameters return the forward scan files
        public string GetDatFileName() => GetFreeFileName("dat");

        public string GetPosFileName() => GetFreeFileName("pos");

        public string GetIndFileName() => GetFreeFileName("ind");

        public string GetDscFileName() => GetFreeFileName("dsc");

        // 3D-files have neither a direction nor multiple scans
        public string Get3DFileName() => GetFreeFileName("3d");

        public string GetTrgFileName() => GetFreeFileName("trg");

        // this may be used to generate names for output files.
        public string GetFreeFileName(string extension) => GetFileName(ScanDirection.Forward, extension, 0);

        public string GetFreeFileNameWithIndex(string extension) => GetFileName(ScanDirection.Forward, extension, ScanIndex);

        private string GetFileName(ScanDirection direction, string extension, int index)
        {
            switch (direction)
            {
                case ScanDirection.Forward:
                    return GetFwdFileName(extension, index);
                case ScanDirection.Backward:
                    return GetBwdFileName(extension, index);
                default:
                    return string.Empty;
            }
        }

        private string GetFwdFileName(string extension, int index)
        {
            if (index > 0)
            {
                return Path.ChangeExtension(BasePath + $"_{index}", extension);
            }
            return Path.ChangeExtension(BasePath, extension);
        }

        private string GetBwdFileName(string extension, int index)
        {
            if (index > 0)
            {
                return Path.ChangeExtension(BasePath + $"b_{index}", extension);
            }
            return Path.ChangeExtension(BasePath + "b", extension);
        }

        public override string ToString()
        {
            if (ScanIndex == 0)
                return $"[NmmFileName: {BaseFileName}]";
            return $"[NmmFileName: {BaseFileName} for scan: {ScanIndex}]";
        }
    }
}

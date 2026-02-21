using System.Collections.Generic;

namespace At.Matus.IO.NmmReader.scan_mode
{
    /// <summary>
    /// A convienient container for some methods used to evaluate NMM scan files.
    /// </summary>
    public static class Scan
    {

        /// <summary>
        /// Interpretes the provided data mask for the number of columns in the data file.
        /// </summary>
        /// <param name="dataMask">The data mask.</param>
        /// <returns>The expected number of columns in the data file.</returns>
        public static int NumberOfColumnsFor(long dataMask) => ColumnPredicatesFor(dataMask).Length;

        /// <summary>
        /// Interpretes the provided data mask for short and long titles of columns in the data file.
        /// </summary>
        /// <param name="dataMask">The data mask.</param>
        /// <returns>The array of <c>ScanColumnPredicate</c>.</returns>
        public static ScanColumnPredicate[] ColumnPredicatesFor(long dataMask)
        {
            // create a list of the predicates
            List<ScanColumnPredicate> columnsPredicates = new List<ScanColumnPredicate>();
            // some bools to implement logic not inside the data mask
            bool columnLX = false;
            bool columnLY = false;
            bool columnLZ = false;
            bool columnAZ = false;
            // split the data mask
            // the order of the if cases is essential!
            // The data files columnes are (hopefully) in the same order.
            if ((dataMask & 0x1) != 0)
            {
                columnsPredicates.Add(new ScanColumnPredicate("LX", "X length", "m"));
                columnLX = true;
            }
            if ((dataMask & 0x2) != 0)
            {
                columnsPredicates.Add(new ScanColumnPredicate("LY", "Y length", "m"));
                columnLY = true;
            }
            if ((dataMask & 0x4) != 0)
            {
                columnsPredicates.Add(new ScanColumnPredicate("LZ", "Z length", "m"));
                columnLZ = true;
            }
            if ((dataMask & 0x8) != 0)
                columnsPredicates.Add(new ScanColumnPredicate("WX", "X angle", "n"));
            if ((dataMask & 0x10) != 0)
                columnsPredicates.Add(new ScanColumnPredicate("WY", "Y angle", "n"));
            if ((dataMask & 0x20) != 0)
                columnsPredicates.Add(new ScanColumnPredicate("WZ", "Z angle", "n"));
            if ((dataMask & 0x40) != 0)
                columnsPredicates.Add(new ScanColumnPredicate("AX", "Auxiliary input 1", "n"));
            if ((dataMask & 0x80) != 0)
            {
                // The *.dsc file sometimes masks the AY channel
                // columnsPredicates.Add(new ScanColumnPredicate("AY", "Auxiliary input 2", "n"));
            }
            if ((dataMask & 0x100) != 0)
            {
                columnsPredicates.Add(new ScanColumnPredicate("AZ", "Probe values", "m"));
                columnAZ = true;
            }
            if ((dataMask & 0x200) != 0)
                columnsPredicates.Add(new ScanColumnPredicate("TX", "Air temperature X", "oC"));
            if ((dataMask & 0x400) != 0)
                columnsPredicates.Add(new ScanColumnPredicate("TY", "Air temperature Y", "oC"));
            if ((dataMask & 0x800) != 0)
                columnsPredicates.Add(new ScanColumnPredicate("TZ", "Air temperature Z", "oC"));
            if ((dataMask & 0x1000) != 0)
                columnsPredicates.Add(new ScanColumnPredicate("LD", "Air pressure", "Pa"));
            if ((dataMask & 0x2000) != 0)
                columnsPredicates.Add(new ScanColumnPredicate("LF", "Relative humidity", "%"));
            if ((dataMask & 0x4000) != 0)
                columnsPredicates.Add(new ScanColumnPredicate("?0", "??? 0", "n"));
            if ((dataMask & 0x8000) != 0)
                columnsPredicates.Add(new ScanColumnPredicate("?1", "??? 1", "n"));
            if ((dataMask & 0x10000) != 0)
                columnsPredicates.Add(new ScanColumnPredicate("?2", "??? 2", "n"));
            if ((dataMask & 0x20000) != 0)
                columnsPredicates.Add(new ScanColumnPredicate("F0", "Interferometer signal XS", "n"));
            if ((dataMask & 0x40000) != 0)
                columnsPredicates.Add(new ScanColumnPredicate("F1", "Interferometer signal XC", "n"));
            if ((dataMask & 0x80000) != 0)
                columnsPredicates.Add(new ScanColumnPredicate("F2", "Interferometer signal YS", "n"));
            if ((dataMask & 0x100000) != 0)
                columnsPredicates.Add(new ScanColumnPredicate("F3", "Interferometer signal YC", "n"));
            if ((dataMask & 0x200000) != 0)
                columnsPredicates.Add(new ScanColumnPredicate("F4", "Interferometer signal ZS", "n"));
            if ((dataMask & 0x400000) != 0)
                columnsPredicates.Add(new ScanColumnPredicate("F5", "Interferometer signal ZC", "n"));
            if ((dataMask & 0x800000) != 0)
                columnsPredicates.Add(new ScanColumnPredicate("AZ0", "Probe input 1", "n"));
            if ((dataMask & 0x1000000) != 0)
                columnsPredicates.Add(new ScanColumnPredicate("AZ1", "Probe input 2", "n"));
            // the following columns are not mapped by the data mask
            // but anyway present in the *.dat file 
            if (columnLZ && columnAZ)
                columnsPredicates.Add(new ScanColumnPredicate("-LZ+AZ", "Surface height", "m"));
            if (columnLX && columnLY)
                columnsPredicates.Add(new ScanColumnPredicate("XYvec", "XY motion vector", "m"));
            // return as array
            return columnsPredicates.ToArray();
        }
    }
}

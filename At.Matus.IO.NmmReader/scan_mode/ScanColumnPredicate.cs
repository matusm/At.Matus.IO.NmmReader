//****************************************************************************************
//
// Class to hold the title and corresponding measurement unit for the data columns
// of NNM scan files.
//
// The constructor is the only way to set the three properties.
// 
// Author: Michael Matus, 2019-2022
//
//****************************************************************************************

using System;

namespace At.Matus.IO.NmmReader.scan_mode
{
    public class ScanColumnPredicate
    {

        public ScanColumnPredicate(string colSymbol, string title, string unit)
        {
            ColumnSymbol = colSymbol.Trim();
            ColumnTitle = title.Trim();
            UnitSymbol = unit.Trim();
        }

        public string ColumnSymbol { get; }
        public string ColumnTitle { get; }
        public string UnitSymbol { get; }

        public bool IsOf(string columnSymbol)
        {
            if (string.Equals(columnSymbol, "height", StringComparison.OrdinalIgnoreCase))
                columnSymbol = "-LZ+AZ";
            if (string.Equals(columnSymbol, "AZ-LZ", StringComparison.OrdinalIgnoreCase))
                columnSymbol = "-LZ+AZ";
            if (string.Equals(columnSymbol, "XY", StringComparison.OrdinalIgnoreCase))
                columnSymbol = "XYvec";
            return string.Equals(ColumnSymbol, columnSymbol.Trim(), StringComparison.OrdinalIgnoreCase);
        }

        public override string ToString() => $"[ScanColumnPredicate: ColumnSymbol={ColumnSymbol}, ColumnTitle={ColumnTitle}, UnitSymbol={UnitSymbol}]";

    }

}

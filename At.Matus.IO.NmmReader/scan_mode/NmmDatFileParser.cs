//****************************************************************************************
//
// Class to read the topographic surface scan data of the SIOS NMM.
// Data is read line per line from the two *.dat files.
//
// Usage:
// 1.) create instance of NmmDatFileParser (this class) with a NmmFileName object as parameter.
// 2.) call NextForwardDataLine() to query a single data line from the forward *.dat file.
//     call NextBackwardDataLine() to query a single data line from the backward *b.dat file.
//     Both methods return null at end of file. 
// 3.) after all data is consumed, call Close();
//
// 
// Author: Michael Matus, 2019-2022
//
//****************************************************************************************

using System;
using System.Globalization;
using System.IO;

namespace At.Matus.IO.NmmReader.scan_mode
{
    public class NmmDatFileParser
    {
        static readonly NumberFormatInfo numFormat = new NumberFormatInfo() { NumberDecimalSeparator = "." };

        public NmmDatFileParser(NmmFileName fileName)
        {
            // try to open file(s)
            hForwardFile = OpenFileForLoading(fileName.GetDatFileNameForScanIndex(ScanDirection.Forward));
            hBackwardFile = OpenFileForLoading(fileName.GetDatFileNameForScanIndex(ScanDirection.Backward));
        }

        public bool BackwardFilePresent => hBackwardFile != null;
        public bool ForwardFilePresent => hForwardFile != null;

        public void Close()
        {
            if (ForwardFilePresent) hForwardFile.Close();
            if (BackwardFilePresent) hBackwardFile.Close();
        }

        public double[] NextForwardDataLine() => ExtractNextDataLine(hForwardFile);

        public double[] NextBackwardDataLine() => ExtractNextDataLine(hBackwardFile);

        private StreamReader OpenFileForLoading(string fileName)
        {
            try
            {
                return File.OpenText(fileName);
            }
            catch (Exception)
            {
                return null;
            }
        }

        private double[] ExtractNextDataLine(StreamReader hFile)
        {
            if (hFile == null)
            {
                return null;
            }
            string datLine = hFile.ReadLine();
            if (string.IsNullOrWhiteSpace(datLine))
            {
                return null;
            }
            char[] charSeparators = { ' ' };
            string[] sToken = datLine.Split(charSeparators, StringSplitOptions.RemoveEmptyEntries);
            double[] dataLine = new double[sToken.Length];
            for (int i = 0; i < sToken.Length; i++)
                dataLine[i] = double.Parse(sToken[i], numFormat);
            return dataLine;
        }

        private readonly StreamReader hForwardFile;
        private readonly StreamReader hBackwardFile;
    }
}

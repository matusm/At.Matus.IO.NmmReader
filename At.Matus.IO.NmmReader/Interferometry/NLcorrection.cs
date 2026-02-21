using System;

namespace At.Matus.IO.NmmReader.Interferometry
{
    public class NLcorrection
    {
        public CorrectionStatus Status { get; private set; } = CorrectionStatus.Unknown;
        public double[] CorrectedData { get; }
        public double CorrectionSpan => CorrectionSpan2thOrder + CorrectionSpan4thOrder;
        public double CorrectionSpan2thOrder { get; private set; } = 0.0;
        public double CorrectionSpan4thOrder { get; private set; } = 0.0;

        // The Dai correction is performed in any case with the provided correction value
        public NLcorrection(double[] rawData, double[] sinValues, double[] cosValues, double empiricalCorrection)
        {
            Status = CheckInput(rawData, sinValues, cosValues);
            if (Status == CorrectionStatus.Uncorrected)
            {
                Quad[] signal = CombineSignals(sinValues, cosValues);
                NLcorrectionHeydemann heydemann = new NLcorrectionHeydemann(rawData, signal);
                NLcorrectionDai gaoliang = new NLcorrectionDai(heydemann.CorrectedData, heydemann.CorrectedQuadratureValues, empiricalCorrection);
                CorrectedData = gaoliang.CorrectedData;
                CorrectionSpan2thOrder = heydemann.CorrectionSpan;
                CorrectionSpan4thOrder = gaoliang.CorrectionSpan;
                Status = heydemann.Status;
            }
            else
            {
                Array.Copy(rawData, CorrectedData, rawData.Length);
            }
        }

        // The Dai correction is performed with an estimated value or with the default value, depending on the Heydemann success
        public NLcorrection(double[] rawData, double[] sinValues, double[] cosValues)
        {
            Status = CheckInput(rawData, sinValues, cosValues);
            if (Status == CorrectionStatus.Uncorrected)
            {
                Quad[] signal = CombineSignals(sinValues, cosValues);
                NLcorrectionHeydemann heydemann = new NLcorrectionHeydemann(rawData, signal);
                NLcorrectionDai gaoliang;
                if (heydemann.Status == CorrectionStatus.Corrected)
                {
                    gaoliang = new NLcorrectionDai(heydemann.CorrectedData, heydemann.CorrectedQuadratureValues);
                }
                else
                {
                    gaoliang = new NLcorrectionDai(heydemann.CorrectedData, heydemann.CorrectedQuadratureValues, NLconstants.defaultDaiCorrection);
                }
                CorrectedData = gaoliang.CorrectedData;
                CorrectionSpan2thOrder = heydemann.CorrectionSpan;
                CorrectionSpan4thOrder = gaoliang.CorrectionSpan;
                Status = heydemann.Status;
            }
            else
            {
                Array.Copy(rawData, CorrectedData, rawData.Length);
            }
        }

        private CorrectionStatus CheckInput(double[] rawData, double[] sinValues, double[] cosValues)
        {
            if (sinValues.Length != cosValues.Length)
                return CorrectionStatus.UncorrectedInconsitentData;
            if (sinValues.Length != rawData.Length)
                return CorrectionStatus.UncorrectedInconsitentData;
            return CorrectionStatus.Uncorrected;
        }

        private Quad[] CombineSignals(double[] sinValues, double[] cosValues)
        {
            Quad[] quad = new Quad[sinValues.Length];
            for (int i = 0; i < quad.Length; i++)
            {
                quad[i] = new Quad(sinValues[i], cosValues[i]);
            }
            return quad;
        }
    }
}

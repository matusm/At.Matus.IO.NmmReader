//*******************************************************************************************
//
// Class to compensate specific periodic errors in signals of homodyne laser interferometers
// 
// To have an ideal circular Lissajous trajectory, the interferometer signals (sin, cos)
// should have zero offset, identical amplitudes, and 90° phase difference.
// Deviations from the ideal case leads to an eliptical Lissajous trajectory which can
// be corrected by this class.
//
// The algorithm fits an ellipse in the sin/cos data.
// For this MathNet.Numerics.LinearAlgebra is used.
// The parameters of the fitted ellipse are accessible as properties.
//
//
// Usage:
// 1.) create an instance of NLcorrectionHeydemann with three arrays as parameters:
//     - rawData: the actual length values in m to be corrected
//     - sinValues: the respective sin-signal of the interferometer
//     - cosValues: the respective cos-signal of the interferometer
// 2.) consume the corrected data via the CorrectedData property
// 
// There are no user accessible methods for this class.
// However some properties (getters) provide details on the correction status,
// most important is CorrectionSpan (in m) the maximum correction applied,
// and Status which gives information if there was an correction at all.
// Backtransformed interferometer signals are accessible, too. 
// in cases where there is no correction possible, CorrectionData just equals rawData.
// 
// The whole calculation is performed in the constructor only.
// 
// Author: Michael Matus, 2020-2022
//
//*******************************************************************************************

using MathNet.Numerics.LinearAlgebra;
using System;
using System.Linq;

namespace At.Matus.IO.NmmReader.Interferometry
{
    public class NLcorrectionHeydemann
    {
        public CorrectionStatus Status { get; private set; } = CorrectionStatus.Unknown;
        public double CorrectionSpan { get; private set; } = 0.0;
        public double[] CorrectedData { get; private set; }
        public Quad[] CorrectedQuadratureValues { get; private set; }
        // the 5 parameters characterizing an ellipse in the plane
        // the init values will result in a 0-correction
        public double OffsetX { get; private set; } = 0.0;
        public double OffsetY { get; private set; } = 0.0;
        public double Phase { get; private set; } = 0.0;
        public double Amplitude { get; private set; } = 1.0;
        public double AmplitudeRelation { get; private set; } = 1.0;

        public NLcorrectionHeydemann(double[] rawData, Quad[] signal)
        {
            if (rawData.Max() - rawData.Min() >= NLconstants.lambda2)
            {
                PerformCorrection(rawData, signal);
            }
            else
            {
                Status = CorrectionStatus.UncorrectedRangeTooSmall;
            }
        }

        public NLcorrectionHeydemann(Quad[] signal)
        {
            double[] dummyData = new double[signal.Length];
            for (int i = 0; i < dummyData.Length; i++)
            {
                dummyData[i] = 0;
            }
            PerformCorrection(dummyData, signal);
        }

        private void PerformCorrection(double[] rawData, Quad[] signal)
        {
            Status = CorrectionStatus.Uncorrected;
            CorrectedData = new double[rawData.Length];
            CorrectedQuadratureValues = new Quad[rawData.Length];
            Array.Copy(rawData, CorrectedData, rawData.Length);
            Array.Copy(signal, CorrectedQuadratureValues, signal.Length);
            if (CorrectedQuadratureValues.Length < 5) // need more than 5 data points to perform fit
            {
                Status = CorrectionStatus.UncorrectedTooFewData;
                return;
            }
            FitEllipse(signal);
            // now the ellipse parameters are valid
            double deviation;
            double maxDeviation = double.MinValue;
            double minDeviation = double.MaxValue;
            for (int i = 0; i < rawData.Length; i++)
            {
                deviation = HeydemannDeviationForPoint(signal[i].Sin, signal[i].Cos);
                CorrectedData[i] = rawData[i] - deviation; // ATTENTION: the sign is valid only for rawData = -LZ+AZ !
                if (deviation > maxDeviation) maxDeviation = deviation;
                if (deviation < minDeviation) minDeviation = deviation;
                // write corrected quadrature signals
                CorrectedQuadratureValues[i] = new Quad(SinCor(signal[i].Sin, signal[i].Cos), CosCor(signal[i].Sin, signal[i].Cos));
            }
            CorrectionSpan = maxDeviation - minDeviation;
            Status = CorrectionStatus.Corrected;
        }

        private void FitEllipse(Quad[] signal)
        {
            var M = Matrix<double>.Build;
            var V = Vector<double>.Build;
            double[,] matMtemp = new double[5, signal.Length];
            // M=[ks.*ks,kc.*kc,ks.*kc,ks,kc]
            for (int i = 0; i < signal.Length; i++)
            {
                matMtemp[0, i] = signal[i].Sin * signal[i].Sin;
                matMtemp[1, i] = signal[i].Cos * signal[i].Cos;
                matMtemp[2, i] = signal[i].Sin * signal[i].Cos;
                matMtemp[3, i] = signal[i].Sin;
                matMtemp[4, i] = signal[i].Cos;
            }
            var matM = M.DenseOfArray(matMtemp);
            // P=inv(M'*M)
            var matQ = matM * matM.Transpose();
            var matP = matQ.Inverse();
            // s=P*M'*X;
            var matX = V.Dense(signal.Length, 1.0);
            // var matS = matP * matM * matX;
            var matT = matM * matX;
            var matS = matP * matT;
            double sA = matS[0];
            double sB = matS[1];
            double sC = matS[2];
            double sD = matS[3];
            double sE = matS[4];
            // % Phase deviation
            // alpha=asin((C/(4*A*B)^0.5));
            // % alphagrd=alpha*180/pi;
            Phase = Math.Asin(sC / Math.Sqrt(4.0 * sA * sB));
            // % Amplitude relation r
            // r=(B/A)^0.5;
            AmplitudeRelation = Math.Sqrt(sB / sA);
            // % Offset x
            // p=(2*B*D-E*C)/(C^2-4*A*B);
            OffsetX = (2.0 * sB * sD - sE * sC) / (sC * sC - 4.0 * sA * sB);
            // % Offset y
            // q=(2*A*E-D*C)/(C^2-4*A*B);
            OffsetY = (2.0 * sA * sE - sD * sC) / (sC * sC - 4.0 * sA * sB);
            // % Amplitude R
            // R=((p^2+r^2*q^2+2*r*p*q*sin(alpha))/cos(alpha)^2+1/(A*cos(alpha)^2))^0.5;
            double x1 = (OffsetX * OffsetX + AmplitudeRelation * AmplitudeRelation * OffsetY * OffsetY + 2.0 * AmplitudeRelation * OffsetX * OffsetY * Math.Sin(Phase)) / (Math.Cos(Phase) * Math.Cos(Phase));
            double x2 = 1.0 / (sA * Math.Cos(Phase) * Math.Cos(Phase));
            Amplitude = Math.Sqrt(x1 + x2);
        }

        private double HeydemannDeviationForPoint(double sin, double cos)
        {
            double sinc = SinCor(sin, cos);
            double cosc = CosCor(sin, cos);
            double deviation = (NLconstants.lambda2 / (2.0 * Math.PI)) * (Math.Atan2(cos, sin) - Math.Atan2(cosc, sinc));
            if (deviation > 300e-9) deviation -= NLconstants.lambda2;
            if (deviation < -300e-9) deviation += NLconstants.lambda2;
            return deviation;
        }

        private double SinCor(double sin, double cos) => sin - OffsetX;

        private double CosCor(double sin, double cos) => SinCor(sin, cos) * Math.Sin(Phase) + AmplitudeRelation * (cos - OffsetY) / Math.Cos(Phase);
    }
}

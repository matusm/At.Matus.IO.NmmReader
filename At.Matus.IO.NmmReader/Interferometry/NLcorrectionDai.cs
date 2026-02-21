//*******************************************************************************************
//
// Class to compensate specific periodic errors in signals of homodyne laser interferometers
// 
// To have an ideal circular Lissajous trajectory, the interferometer signals (sin, cos)
// should have zero offset, identical amplitudes, and 90° phase difference.
// Deviations from the ideal case leads to an eliptical Lissajous trajectory which can
// be corrected by the NLcorrectionHeydemann (2th-order nonlinearity).
//
// In the NMM-1 additional nonlinearity contributions are present (4th-order).
// This specific nonlinearity is corrected by this class.
//
//
// Usage:
// 1.) create an instance of NLcorrectionDai with three arrays as parameters:
//     - rawData: the actual length values in m to be corrected
//     - sinValues: the respective sin-signal of the interferometer
//     - cosValues: the respective cos-signal of the interferometer
// 2.) consume the corrected data via the CorrectedData property
// 
// A constructor with an additional parameter (empirical correction amplitude)
// is also provided.
//
// There are no user accessible methods for this class.
// However some properties (getters) provide details on the correction status,
// most important is CorrectionSpan (in m) the maximum correction applied,
// and Status which gives information if there was an correction at all.
// in cases where there is no correction possible, CorrectionData just equals rawData.
// 
// The whole calculation is performed in the constructor only.
// 
// Author: Michael Matus, 2023
//
//*******************************************************************************************

using System;

namespace At.Matus.IO.NmmReader.Interferometry
{
    public class NLcorrectionDai
    {
        public CorrectionStatus Status { get; private set; } = CorrectionStatus.Unknown;
        public double CorrectionAmplitude { get; private set; }
        public double CorrectionSpan => CorrectionAmplitude * 2;
        public double RelativeSquashing { get; private set; }
        public double[] CorrectedData { get; private set; }
        public Quad[] QuadratureValues { get; }
        public Quad[] CorrectedQuadratureValues { get; private set; }

        public NLcorrectionDai(double[] rawData, Quad[] signal)
        {
            QuadratureValues = new Quad[signal.Length];
            Array.Copy(signal, QuadratureValues, signal.Length);
            CorrectionAmplitude = EstimateCorrectionAmplitude();
            PerformCorrection(rawData);
        }

        public NLcorrectionDai(double[] rawData, Quad[] signal, double empiricalCorrection)
        {
            QuadratureValues = new Quad[signal.Length];
            Array.Copy(signal, QuadratureValues, signal.Length);
            CorrectionAmplitude = empiricalCorrection;
            PerformCorrection(rawData);
        }

        private void PerformCorrection(double[] rawData)
        {
            CorrectedData = new double[rawData.Length];
            CorrectedQuadratureValues = new Quad[QuadratureValues.Length];
            Array.Copy(rawData, CorrectedData, rawData.Length);
            for (int i = 0; i < rawData.Length; i++)
            {
                CorrectedData[i] += GetLengthCorrection(QuadratureValues[i]);
                CorrectedQuadratureValues[i] = CorrectQuadValue(QuadratureValues[i]);
            }
            Status = CorrectionStatus.Corrected;
        }

        private double GetLengthCorrection(Quad quad) => -CorrectionAmplitude * Math.Sin(4 * quad.Phi);

        private double EstimateCircleSquashing()
        {
            StatisticPod.StatisticPod allRadii = new StatisticPod.StatisticPod();
            StatisticPod.StatisticPod axisRadii = new StatisticPod.StatisticPod();
            StatisticPod.StatisticPod medianRadii = new StatisticPod.StatisticPod();
            for (int i = 0; i < QuadratureValues.Length; i++)
            {
                Quad q = QuadratureValues[i];
                double r = q.Radius;
                double phi = q.PhiDeg;
                allRadii.Update(r);
                if (IsNearToAxis(phi)) axisRadii.Update(r);
                if (IsNearToMedian(phi)) medianRadii.Update(r);
            }
            absoluteDeviation = medianRadii.AverageValue - axisRadii.AverageValue;
            RelativeSquashing = absoluteDeviation / allRadii.AverageValue;
            return RelativeSquashing;
        }

        private double EstimateCorrectionAmplitude() => NLconstants.empiricalNLfactor * EstimateCircleSquashing();

        private bool IsNearToAxis(double phi)
        {
            if (IsNear(phi, 0)) return true;
            if (IsNear(phi, 90)) return true;
            if (IsNear(phi, 180)) return true;
            if (IsNear(phi, -90)) return true;
            if (IsNear(phi, -180)) return true;
            return false;
        }

        private bool IsNearToMedian(double phi)
        {
            if (IsNear(phi, 45)) return true;
            if (IsNear(phi, 135)) return true;
            if (IsNear(phi, -45)) return true;
            if (IsNear(phi, -135)) return true;
            return false;
        }

        private bool IsNear(double phi, double target)
        {
            const double eps = 2;
            if (Math.Abs(target - phi) < eps) return true;
            return false;
        }

        private Quad CorrectQuadValue(Quad quad)
        {
            double deltaR = DeltaRadius(quad);
            return new Quad(quad.Radius + deltaR, quad.Phi, AngleUnit.Radian);
        }

        private double DeltaRadius(Quad quad) => 0.5 * absoluteDeviation * Math.Cos(4 * quad.Phi);

        private double absoluteDeviation;

    }

}

namespace At.Matus.IO.NmmReader.Interferometry
{
    public enum CorrectionStatus
    {
        Unknown,
        Uncorrected,
        UncorrectedInconsitentData,
        UncorrectedTooFewData,
        UncorrectedRangeTooSmall,
        Corrected
    }
}

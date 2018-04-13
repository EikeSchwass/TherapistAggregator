namespace TherapistAggregator
{
    public struct AggregateProgressReport
    {
        public ProgressReport ProgressReport { get; }
        public ITherapistAggregator TherapistAggregator { get; }

        public AggregateProgressReport(ProgressReport progressReport, ITherapistAggregator therapistAggregator)
        {
            ProgressReport = progressReport;
            TherapistAggregator = therapistAggregator;
        }
    }
}
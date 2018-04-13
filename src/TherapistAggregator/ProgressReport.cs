namespace TherapistAggregator
{
    public struct ProgressReport
    {
        /// <summary>
        /// Value from 0 to 1
        /// </summary>
        public double Progress { get; }
        public string Message { get; }

        public ProgressReport(string message, double progress)
        {
            Message = message;
            Progress = progress;
        }
    }
}
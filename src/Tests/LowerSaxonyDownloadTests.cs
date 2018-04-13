using System;
using System.Diagnostics;
using System.Linq;
using NUnit.Framework;
using TherapistAggregator;
using TherapistsLowerSaxony;

namespace Tests
{
    [TestFixture]
    public class LowerSaxonyDownloadTests
    {
        [Test]
        public void DownloadTherapistsDebugTest()
        {
            var lowerSaxonyAggregator = new LowerSaxonyAggregator();
            var progress = new Progress<ProgressReport>();
            progress.ProgressChanged += (o, e) => Debug.WriteLine($"{e.Message} ({e.Progress:P})");
            var therapists = lowerSaxonyAggregator.DownloadTherapistsAsync(null, new WebHelper(), progress).Result.ToList();

        }
    }
}
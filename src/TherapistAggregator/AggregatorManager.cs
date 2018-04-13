using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Core;

namespace TherapistAggregator
{
    public class AggregatorManager
    {
        public IReadOnlyCollection<ITherapistAggregator> TherapistAggregators { get; }

        public AggregatorManager(AggregatorLoader aggregatorLoader)
        {
            var aggregators = aggregatorLoader.LoadAggregators();
            TherapistAggregators = new ReadOnlyCollection<ITherapistAggregator>(aggregators);
        }

        public async Task<IEnumerable<Therapist>> LoadAllTherapistsAsync(IAddressToGpsConverter addressToGpsConverter, WebHelper webHelper, IProgress<AggregateProgressReport> progress)
        {
            var therapists = await Task.WhenAll(TherapistAggregators.Select((aggregator, i) => Task.Run(() =>
                                                                                                        {
                                                                                                            var localProgress = new Progress<ProgressReport>();
                                                                                                            localProgress.ProgressChanged += (o, e) => progress.Report(new AggregateProgressReport(e, aggregator));
                                                                                                            return aggregator.DownloadTherapistsAsync(null, webHelper, localProgress);
                                                                                                        })));
            return therapists.SelectMany(t => t);
        }
    }
}
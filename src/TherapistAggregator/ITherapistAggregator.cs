using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Core;

namespace TherapistAggregator
{
    public interface ITherapistAggregator
    {
        StateName GetStateName();
        IEnumerable<Therapist> DownloadTherapists(IAddressToGpsConverter addressToGpsConverter, WebHelper webHelper, IProgress<ProgressReport> progress);
    }
}
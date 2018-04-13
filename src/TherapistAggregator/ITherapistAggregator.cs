using System;
using System.Threading.Tasks;
using Core;

namespace TherapistAggregator
{
    public interface ITherapistAggregator
    {
        StateName GetStateName();
        Task<Therapist[]> DownloadTherapistsAsync(IAddressToGpsConverter addressToGpsConverter, WebHelper webHelper, IProgress<ProgressReport> progress);
    }
}
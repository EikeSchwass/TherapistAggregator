using Core;

namespace TherapistAggregator
{
    public interface IAddressToGpsConverter
    {
        GPSLocation ConvertAddress(Address address);
    }
}
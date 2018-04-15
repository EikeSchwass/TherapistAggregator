using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Core;
using Moq;
using NUnit.Framework;
using TherapistAggregator;
using TherapistsLowerSaxony;

namespace Tests
{
    [TestFixture]
    public class TherapistAggregatorTests
    {
        private static IList<ITherapistAggregator> GetITherapistAggregators()
        {
            return new AggregatorLoader().LoadAggregators();
        }

        [Test, TestCaseSource(nameof(GetITherapistAggregators))]
        public void AnyTest(ITherapistAggregator aggregator)
        {
            var mock = new Mock<IAddressToGpsConverter>();
            mock.Setup(a => a.ConvertAddress(It.IsAny<Address>())).Returns(new GPSLocation(1, 1));
            var therapists =
                aggregator.DownloadTherapists(mock.Object, new WebHelper(), new Progress<ProgressReport>());
            Assert.True(therapists.Any());
        }

        [Test, TestCaseSource(nameof(GetITherapistAggregators))]
        public void NameSetTest(ITherapistAggregator aggregator)
        {
            var mock = new Mock<IAddressToGpsConverter>();
            mock.Setup(a => a.ConvertAddress(It.IsAny<Address>())).Returns(new GPSLocation(1, 1));
            var therapists =
                aggregator.DownloadTherapists(mock.Object, new WebHelper(), new Progress<ProgressReport>());
            bool condition = therapists.Take(5).Skip(5).Take(5).Skip(5).Take(5).All(t =>
                !string.IsNullOrWhiteSpace(t.FamilyName) && !string.IsNullOrWhiteSpace(t.Name));
            Assert.True(condition);
        }

        [Test, TestCaseSource(nameof(GetITherapistAggregators))]
        public void GenderSetTest(ITherapistAggregator aggregator)
        {
            var mock = new Mock<IAddressToGpsConverter>();
            mock.Setup(a => a.ConvertAddress(It.IsAny<Address>())).Returns(new GPSLocation(1, 1));
            var therapists =
                aggregator.DownloadTherapists(mock.Object, new WebHelper(), new Progress<ProgressReport>());
            bool condition = therapists.Take(5).Skip(5).Take(5).Skip(5).Take(5).All(t => t.Gender != Gender.Unknown);
            Assert.True(condition);
        }

        [Test, TestCaseSource(nameof(GetITherapistAggregators))]
        public void WebsiteSetTest(ITherapistAggregator aggregator)
        {
            var mock = new Mock<IAddressToGpsConverter>();
            mock.Setup(a => a.ConvertAddress(It.IsAny<Address>())).Returns(new GPSLocation(1, 1));
            var therapists =
                aggregator.DownloadTherapists(mock.Object, new WebHelper(), new Progress<ProgressReport>());
            bool condition = therapists.Take(5).Skip(5).Take(5).Skip(5).Take(5).All(t => !string.IsNullOrWhiteSpace(t.KVWebsite));
            Assert.True(condition);
        }

        [Test, TestCaseSource(nameof(GetITherapistAggregators))]
        public void LanguagesFormatTest(ITherapistAggregator aggregator)
        {
            var mock = new Mock<IAddressToGpsConverter>();
            mock.Setup(a => a.ConvertAddress(It.IsAny<Address>())).Returns(new GPSLocation(1, 1));
            var therapists =
                aggregator.DownloadTherapists(mock.Object, new WebHelper(), new Progress<ProgressReport>());
            bool condition = therapists.Take(5).Skip(5).Take(5).Skip(5).Take(5).All(t => t.Languages.All(l => !string.IsNullOrWhiteSpace(l)));
            Assert.True(condition);
        }
    }
}
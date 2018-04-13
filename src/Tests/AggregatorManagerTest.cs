using System.Collections.Generic;
using System.Linq;
using Core;
using NUnit.Framework;
using TherapistAggregator;

namespace Tests
{
    [TestFixture]
    public class AggregatorManagerTests
    {
        [Test]
        public void LoadAggregatorsNotEmptyTest()
        {
            var aggregatorManager = new AggregatorManager(new AggregatorLoader());
            Assert.True(aggregatorManager.TherapistAggregators.Any());
        }

        [Test]
        public void LoadAggregatorsStatesUniqueTest()
        {
            var aggregatorManager = new AggregatorManager(new AggregatorLoader());
            List<StateName> names = new List<StateName>();
            foreach (var therapistAggregator in aggregatorManager.TherapistAggregators)
            {
                var stateName = therapistAggregator.GetStateName();
                if(names.Contains(stateName))
                    Assert.Fail($"{stateName} is already provided");
                names.Add(stateName);
            }
        }
    }
}

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Core;
using TherapistAggregator;

namespace TherapistsLowerSaxony
{
    public class LowerSaxonyAggregator : ITherapistAggregator
    {
        public StateName GetStateName() => StateName.LowerSaxony;

        private const string SearchLink = "http://www.arztauskunft-niedersachsen.de/arztsuche/extSearchAction.action";

        public IEnumerable<Therapist> DownloadTherapists(IAddressToGpsConverter addressToGpsConverter, WebHelper webHelper, IProgress<ProgressReport> progress)
        {
            var searchPages = CreateSearches(webHelper).ToList();
            int totalPageCount = searchPages.Sum(s => s.GetPageCount());
            int currentPageCount = 0;
            var parallelOptions = new ParallelOptions { MaxDegreeOfParallelism = 128 };
            foreach (var searchPage in searchPages)
            {
                var currentPage = searchPage;
                do
                {
                    var therapists = Task.WhenAll(currentPage.GetLinksToTherapistSites().Select(async link => new TherapistPage(await webHelper.DoHttpGetAsync(link), link).GetTherapist())).Result;
                    foreach (var therapist in therapists)
                    {
                        foreach (var therapistOffice in therapist.Offices)
                        {
                            therapistOffice.Location = addressToGpsConverter.ConvertAddress(therapistOffice.Address);
                        }
                        yield return therapist;
                    }

                    currentPageCount++;
                    progress.Report(new ProgressReport($"Page #{currentPageCount}", currentPageCount * 1.0 / totalPageCount));

                    if (currentPage.HasNextButton())
                    {
                        currentPage = currentPage.GetNextSearchPage(webHelper);
                    }
                    else
                        break;
                }
                while (true);
            }
        }

        private IEnumerable<SearchPage> CreateSearches(WebHelper webHelper)
        {
            int[] fachgebiete = { 380, 1350, 2200, 2300, 55, 4200, 65, 4350 };
            var values = fachgebiete.Select(id => new Dictionary<string, string>
            {
                {"geschlecht", "b"},
                {"name", ""},
                {"strasse", ""},
                {"plz", ""},
                {"ort", ""},
                {"__checkbox_kasse", "true"},
                {"__checkbox_privat", "true"},
                {"fachgebiet", id.ToString()},
                {"dmp", "-1"},
                {"kenntnisse", "-1"},
                {"wochentag", "-1"},
                {"von", "-1"},
                {"bis", "-1"},
                {"fremdsprache", "-1"},
            });
            foreach (var dictionary in values)
            {
                var htmlDocument = webHelper.DoHttpPostAsync(SearchLink, new FormUrlEncodedContent(dictionary)).Result;
                var searchPage = new SearchPage(htmlDocument);
                yield return searchPage;
            }
        }

        private static Therapist[] MergeTherapists(Therapist[] therapists)
        {
            List<Therapist> result = new List<Therapist>();
            var singleTherapsists = therapists.GroupBy(t => t.ID).Select(g => g.First()).ToList();
            result.AddRange(singleTherapsists);

            Debug.Assert(result.Count == therapists.Select(t => t.ID).Distinct().Count());

            return result.ToArray();
        }
        private static Therapist[] MergeCategoryAndLocation(Therapist[] therapistsWithLocation, Therapist[] therapistsWithCategories)
        {
            List<Therapist> result = new List<Therapist>();

            foreach (var therapistsWithCategory in therapistsWithCategories)
            {
                long id = therapistsWithCategory.ID;
                var fittingTherapists = therapistsWithLocation.Where(t => t.ID == id).ToArray();
                if (!fittingTherapists.Any())
                    throw new InvalidOperationException("Mismatch");
                foreach (var office in therapistsWithCategory.Offices)
                {
                    foreach (var fittingTherapist in fittingTherapists)
                    {
                        var fittingOffice = fittingTherapist.Offices.FirstOrDefault(o => Equals(o.Address, office.Address));
                        if (fittingOffice != null)
                        {
                            office.Location = fittingOffice.Location;
                            break;
                        }
                    }
                }
                result.Add(therapistsWithCategory);
            }
            return result.ToArray();
        }
    }
}

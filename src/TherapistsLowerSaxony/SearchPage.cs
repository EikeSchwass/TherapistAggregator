using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using HtmlAgilityPack;
using TherapistAggregator;

namespace TherapistsLowerSaxony
{
    internal class SearchPage
    {
        private const string SiteChange = "http://www.arztauskunft-niedersachsen.de/arztsuche/changeSite.action";

        private HtmlDocument HtmlDocument { get; }

        internal SearchPage(HtmlDocument htmlDocument)
        {
            HtmlDocument = htmlDocument;
        }


        internal IEnumerable<string> GetLinksToTherapistSites()
        {
            List<string> list = new List<string>();
            foreach (HtmlNode resultContainer in HtmlDocument.DocumentNode.Descendants().ByAttribute("class", "resultContainer"))
                foreach (var linkNode in resultContainer.Descendants("a"))
                {
                    string link = linkNode.GetAttributeValue("href", "");
                    if (link.Contains("arztId="))
                        list.Add("http://www.arztauskunft-niedersachsen.de" + link);
                }

            return list;
        }

        internal SearchPage GetNextSearchPage(WebHelper webHelper)
        {
            if (!HasNextButton())
                throw new InvalidOperationException("No next page");

            var inputNodes = HtmlDocument.DocumentNode.Descendants("div").ByAttribute("class", "nextButton").First()
                                        .Descendants("input").ToList();

            var nextSiteIndex = GetPageIndex() + 1;
            var ex = (from node in inputNodes
                      where node.GetAttributeValue("name", "") == "ex"
                      let exString = node.GetAttributeValue("value", "")
                      where !string.IsNullOrWhiteSpace(exString)
                      select exString).Single();

            var values = new Dictionary<string, string>
            {
                {"seite", nextSiteIndex.ToString()},
                {"name", ""},
                {"adresseOrt", ""},
                {"gebiet", ""},
                {"ex", ex}
            };
            var htmlDocument = webHelper.DoHttpPostAsync(SiteChange, new FormUrlEncodedContent(values)).Result;
            var nextPage = new SearchPage(htmlDocument);
            return nextPage;
        }

        internal int GetPageIndex()
        {
            var siteValue = HtmlDocument.DocumentNode.Descendants("input").Single(n => n.Id == "aktSeite").GetAttributeValue("value", "-1");
            int siteIndex = Convert.ToInt32(siteValue);
            if (siteIndex < 0)
                throw new FormatException("The value for site index is wrong");
            return siteIndex;
        }

        internal bool HasNextButton()
        {
            var nextButton = HtmlDocument.DocumentNode.Descendants().First(n => n.GetAttributeValue("class", "") == "nextButton");
            var innerText = nextButton.InnerText;
            return !string.IsNullOrWhiteSpace(innerText);
        }
        public int GetPageCount()
        {
            var siteInfoDiv = HtmlDocument.DocumentNode.Descendants("div").ByAttribute("class","siteInfo").First();
            var innerText = siteInfoDiv.Descendants("div").Last().InnerText;
            innerText = innerText.Replace("von", "");
            int pageCount = Convert.ToInt32(innerText.Trim());
            if (pageCount < 0)
                throw new FormatException("The value for page count is wrong");
            return pageCount;
        }
    }
}
using System;
using System.Collections.Generic;
using System.Linq;
using Core;
using HtmlAgilityPack;
using TherapistAggregator;
using static System.Diagnostics.Debug;

namespace TherapistsLowerSaxony
{
    public class TherapistPage
    {
        private HtmlDocument HtmlDocument { get; }
        private long ID { get; }

        public TherapistPage(HtmlDocument htmlDocument, string link)
        {
            HtmlDocument = htmlDocument;
            ID = Convert.ToInt64(link.Substring(link.LastIndexOf('=') + 1));
        }

        public Therapist GetTherapist()
        {
            Therapist therapist = new Therapist();
            therapist.Qualifications.AddRange(ExtractAbilities());
            therapist.Languages.AddRange(ExtractLanguages());
            therapist.Gender = ExtractGender();
            therapist.Title = ExtractTitle();
            var name = ExtractName();
            therapist.Name = name.name;
            therapist.FamilyName = name.familyName;
            therapist.Offices.AddRange(ExtractOffices());
            therapist.ID = ID;
            therapist.KVNWebsite = $"http://www.arztauskunft-niedersachsen.de/arztsuche/detailAction.action?arztId={ID}";
            return therapist;
        }

        private HtmlNode[] GetContentDetailContainer()
        {
            var contentNode = HtmlDocument.DocumentNode.Descendants("div").ById("Content").Single();
            var overviewHtmlNode = contentNode.ChildNodes.ByAttribute("class", "detailContainer").First();
            return overviewHtmlNode.Descendants("div").ByAttribute("class", "detailContainer").ToArray();
        }

        private IEnumerable<Qualification> ExtractAbilities()
        {
            var qualificationsNode = GetContentDetailContainer().ElementAt(2);
            var nonEmptyParapgraphs = qualificationsNode.Descendants("p").Where(p => !string.IsNullOrWhiteSpace(p.InnerText));
            var lines = nonEmptyParapgraphs.SelectMany(p => p.Descendants("span")).Select(p => p.InnerText.TrimWhiteSpace()).ToArray();

            var fachgebiete = new List<string>();
            var zusatzbezeichnungen = new List<string>();
            var besondereKenntnisse = new List<string>();

            var currentList = fachgebiete;

            foreach (var line in lines)
            {
                if (string.Equals(line, "fachgebiet:", StringComparison.InvariantCultureIgnoreCase))
                {
                    currentList = fachgebiete;
                    continue;
                }

                if (string.Equals(line, "zusatzbezeichnung:", StringComparison.InvariantCultureIgnoreCase))
                {
                    currentList = zusatzbezeichnungen;
                    continue;
                }

                if (string.Equals(line, "BesondereKenntnisse:", StringComparison.InvariantCultureIgnoreCase))
                {
                    currentList = besondereKenntnisse;
                    continue;
                }

                currentList.Add(line.Trim());
            }

            yield return new Qualification { Category = "kenntnisse", Content = besondereKenntnisse };
            yield return new Qualification { Category = "zusatzbezeichnungen", Content = zusatzbezeichnungen };
            yield return new Qualification { Category = "fachgebiete", Content = fachgebiete };
        }

        private IEnumerable<string> ExtractLanguages()
        {
            var languageNode = GetContentDetailContainer()[1];
            var nonEmptyParapgraphs = languageNode.Descendants("p").Where(p => !string.IsNullOrWhiteSpace(p.InnerText));
            var lines = nonEmptyParapgraphs.SelectMany(p => p.Descendants("span")).Select(p => p.InnerText.TrimWhiteSpace()).Skip(1).ToArray();
            return lines;
        }

        private Gender ExtractGender()
        {
            var nameNode = GetContentDetailContainer()[0];
            var nonEmptyParapgraphs = nameNode.Descendants("p").Where(p => !string.IsNullOrWhiteSpace(p.InnerText));
            var lines = nonEmptyParapgraphs.SelectMany(p => p.Descendants("span")).Select(p => p.InnerText.TrimWhiteSpace()).ToArray();
            switch (lines.First().Trim().ToLower())
            {
                case "frau": return Gender.Female;
                case "herr": return Gender.Male;
                default: throw new ArgumentException(lines.First());
            }
        }

        private string ExtractTitle()
        {
            var nameNode = GetContentDetailContainer()[0];
            var nonEmptyParapgraphs = nameNode.Descendants("p").Where(p => !string.IsNullOrWhiteSpace(p.InnerText));
            var lines = nonEmptyParapgraphs.SelectMany(p => p.Descendants("span")).Select(p => p.InnerText.TrimWhiteSpace()).Skip(1).ToArray();
            if (lines.Length == 2)
                return lines.First().Trim();
            return "";
        }

        private (string name, string familyName) ExtractName()
        {
            var nameNode = GetContentDetailContainer()[0];
            var nonEmptyParapgraphs = nameNode.Descendants("p").Where(p => !string.IsNullOrWhiteSpace(p.InnerText));
            var lines = nonEmptyParapgraphs.SelectMany(p => p.Descendants("span")).Select(p => p.InnerText.TrimWhiteSpace()).Skip(1).ToArray();
            string nameLine = lines.Last().Trim();
            string familyName = nameLine.Split(' ').Last().Trim();
            string name = nameLine.Substring(0, nameLine.Length - familyName.Length).Trim();
            return (name, familyName);
        }

        private IEnumerable<Office> ExtractOffices()
        {
            var contentNode = HtmlDocument.DocumentNode.Descendants("div").ById("Content").Single();
            var officeNodes = (from officeNode in contentNode.ChildNodes
                               where officeNode.Attributes["class"]?.Value == "detailContainer" && officeNode.InnerText.TrimWhiteSpace().Length > 0
                               select officeNode).Skip(1).ToArray();
            return officeNodes.Select(ParseOffice);
        }

        private Office ParseOffice(HtmlNode officeNode)
        {
            var children = officeNode.ChildNodes.Where(n => n.Attributes["class"]?.Value == "detailContainer").ToArray();
            var addressNode = children[0];
            var officeContactNode = children[1];
            var officeHoursNode = children[2];
            var officeContactHoursNode = children[3];

            var office = new Office();

            ParseAddress(office, addressNode);
            ParseContact(office, officeContactNode);
            ParseOfficeHours(office, officeHoursNode);
            ParseOfficeContactHours(office, officeContactHoursNode);


            return office;
        }

        private void ParseOfficeContactHours(Office office, HtmlNode officeContactHoursNode)
        {
            var htmlNodes = officeContactHoursNode.GetNonEmptyChildren().Where(n => n.Name == "table").ToArray();
            if (!htmlNodes.Any())
                return;
            Assert(htmlNodes.Length == 1);
            var children = htmlNodes.Single().GetNonEmptyChildren().ToArray();
            var firstLine = "TelefonischeErreichbarkeit:".TrimWhiteSpace();
            Assert(children.First().GetDecodedInnerText().TrimWhiteSpace() == firstLine);
            Assert(children.All(c => c.Name != "tbody"));
            Assert(children.All(c => c.Name == "tr"));
            foreach (var telefoneNode in children.Skip(1))
            {
                var tds = telefoneNode.GetNonEmptyChildren().ToArray();
                Assert(tds.Length == 2);
                var telefoneNumber = tds.First().GetDecodedInnerText().TrimWhiteSpace();
                var officeHours = new List<OfficeHour>();
                office.ContactTimes.Add(new ContactTime
                {
                    TelefoneNumber = new TelefoneNumber { Number = telefoneNumber, Type = TelefoneNumber.TelefoneNumberType.Telefon },
                    OfficeHours = officeHours
                });
                var parsedTimes = ParseContactTimes(tds.Last());
                officeHours.AddRange(parsedTimes);
            }

        }

        private string[] SplitByNewLine(string source)
        {
            return source.Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries).Where(s => !string.IsNullOrWhiteSpace(s)).ToArray();
        }

        private IEnumerable<OfficeHour> ParseContactTimes(HtmlNode td)
        {
            var tableRows = SplitByNewLine(td.GetDecodedInnerText()).Select(s => s.TrimWhiteSpace()).ToArray();
            if (!tableRows.Any())
                yield break;

            DayOfWeek? currentDayOfWeek = null;

            foreach (var tableRow in tableRows)
            {
                var testDay = ParseDayOfWeek(tableRow);
                if (testDay != null)
                {
                    currentDayOfWeek = testDay;
                }
                else if (tableRow.StartsWithNumber() && currentDayOfWeek != null)
                {
                    var fromTo = tableRow.Split('-').Select(s => s.TrimWhiteSpace()).ToArray();
                    Assert(fromTo.Length == 2);
                    var from = fromTo[0].Split(':').Select(s => s.TrimWhiteSpace()).ToArray();
                    var to = fromTo[1].Split(':').Select(s => s.TrimWhiteSpace()).ToArray();
                    var fromHours = from[0];
                    var fromMinutes = from[1];
                    var toHours = to[0];
                    var toMinutes = to[1];
                    var fromTime = new DateTime(1970, 1, 1, Convert.ToInt32(fromHours), Convert.ToInt32(fromMinutes), 0);
                    var toTime = new DateTime(1970, 1, 1, Convert.ToInt32(toHours), Convert.ToInt32(toMinutes), 0);
                    yield return new OfficeHour { DayOfWeek = currentDayOfWeek.Value, From = fromTime, To = toTime };
                }
                else
                {
                    throw new InvalidOperationException(tableRow);
                }
            }

        }

        private void ParseOfficeHours(Office office, HtmlNode officeHoursNode)
        {
            var tableRows = SplitByNewLine(officeHoursNode.GetDecodedInnerText()).Select(s => s.TrimWhiteSpace()).ToArray();
            if (!tableRows.Any())
                return;

            Assert(tableRows.First() == "Sprechzeiten:");

            DayOfWeek? currentDayOfWeek = null;

            foreach (var tableRow in tableRows.Skip(1))
            {
                var testDay = ParseDayOfWeek(tableRow);
                if (testDay != null)
                {
                    currentDayOfWeek = testDay;
                }
                else if (tableRow.StartsWithNumber() && currentDayOfWeek != null)
                {
                    var fromTo = tableRow.Split('-').Select(s => s.TrimWhiteSpace()).ToArray();
                    Assert(fromTo.Length == 2);
                    var from = fromTo[0].Split(':').Select(s => s.TrimWhiteSpace()).ToArray();
                    var to = fromTo[1].Split(':').Select(s => s.TrimWhiteSpace()).ToArray();
                    var fromHours = from[0];
                    var fromMinutes = from[1];
                    var toHours = to[0];
                    var toMinutes = to[1];
                    var fromTime = new DateTime(1970, 1, 1, Convert.ToInt32(fromHours), Convert.ToInt32(fromMinutes), 0);
                    var toTime = new DateTime(1970, 1, 1, Convert.ToInt32(toHours), Convert.ToInt32(toMinutes), 0);
                    office.OfficeHours.Add(new OfficeHour { DayOfWeek = currentDayOfWeek.Value, From = fromTime, To = toTime });
                }
                else
                {
                    WriteLine(tableRow);
                }
            }

        }

        private void ParseContact(Office office, HtmlNode officeContactNode)
        {
            var children = officeContactNode.ChildNodes.Where(n => n.HasInnerText()).ToArray();
            foreach (var child in children)
            {
                var entry = child.Descendants("span").Select(n => n.GetDecodedInnerText().TrimWhiteSpace()).ToArray();
                Assert(entry.Length >= 2);
                TelefoneNumber.TelefoneNumberType type = GetContactType(entry[0]);
                foreach (var contactRow in entry.Skip(1))
                {
                    var telefoneNumber = new TelefoneNumber
                    {
                        Number = contactRow,
                        Type = type
                    };
                    office.TelefoneNumbers.Add(telefoneNumber);
                }
            }
        }

        private static DayOfWeek? ParseDayOfWeek(string innerText)
        {
            switch (innerText.ToLower())
            {
                case "montag": return DayOfWeek.Monday;
                case "dienstag": return DayOfWeek.Tuesday;
                case "mittwoch": return DayOfWeek.Wednesday;
                case "donnerstag": return DayOfWeek.Thursday;
                case "freitag": return DayOfWeek.Friday;
                case "samstag": return DayOfWeek.Saturday;
                case "sonntag": return DayOfWeek.Sunday;
            }
            return null;
        }

        private TelefoneNumber.TelefoneNumberType GetContactType(string s)
        {
            switch (s)
            {
                case "Telefon:":
                    return TelefoneNumber.TelefoneNumberType.Telefon;
                case "Mobil:":
                    return TelefoneNumber.TelefoneNumberType.Mobil;
                case "Fax:":
                    return TelefoneNumber.TelefoneNumberType.Fax;
                case "Webseite:":
                    return TelefoneNumber.TelefoneNumberType.Webseite;
                default: throw new ArgumentException(nameof(s));
            }
        }

        private void ParseAddress(Office office, HtmlNode addressNode)
        {
            var children = addressNode.ChildNodes.Where(n => n.HasInnerText()).ToArray();

            if (children.Count(n => n.HasInnerText()) < 2)
            {
                WriteLine($"Invalid office address format: {addressNode.InnerText.HtmlDecode()}");
                return;
            }

            var officeName = children[children.Length - 2].InnerText.HtmlDecode().Trim();
            office.Name = officeName;
            addressNode = children.Skip(1).LastOrDefault(n => n.HasInnerText());

            if (addressNode == null || !addressNode.HasInnerText())
                throw new FormatException($"Invalid office address format: {addressNode.GetDecodedInnerText()}");

            var addressLines = addressNode.Descendants("span").Select(n => n.GetDecodedInnerText().TrimWhiteSpace()).ToArray();
            Assert(addressLines.Length == 2);

            office.Address.Street = addressLines[0];
            office.Address.City = addressLines[1];
        }
    }
}
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using HtmlAgilityPack;

namespace TherapistAggregator
{
    public static class ExtensionMethods
    {
        public static DirectoryInfo GetParent(this DirectoryInfo directoryInfo) => Directory.GetParent(directoryInfo.FullName);

        public static string TrimWhiteSpace(this string s)
        {
            string result = s.Replace("\t", "").Replace("\n", "").Replace("\r", "").Trim();
            result = WebUtility.HtmlDecode(result);
            return result;
        }

        public static string HtmlDecode(this string s)
        {
            string result = WebUtility.HtmlDecode(s);
            return result;
        }

        public static IEnumerable<HtmlNode> GetNonEmptyChildren(this HtmlNode node)
        {
            return node.ChildNodes.Where(n => n.HasInnerText());
        }

        public static bool HasInnerText(this HtmlNode htmlNode)
        {
            return !string.IsNullOrWhiteSpace(htmlNode.GetDecodedInnerText());
        }

        public static string GetDecodedInnerText(this HtmlNode htmlNode) => HtmlEntity.DeEntitize(htmlNode.InnerText);


        public static bool StartsWithNumber(this string s)
        {
            return s.Any() && int.TryParse(s.First() + "", out var _);
        }
    }
}
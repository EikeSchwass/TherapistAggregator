using System.Collections.Generic;
using System.Linq;
using HtmlAgilityPack;

namespace TherapistAggregator
{
    public static class HtmlExtensions
    {
        public static IEnumerable<HtmlNode> ByAttribute(this IEnumerable<HtmlNode> node, string attributeName, string attributeValue)
        {
            return node.Where(n => n.GetAttributeValue(attributeName, attributeValue + "1") == attributeValue);
        }

        public static IEnumerable<HtmlNode> ById(this IEnumerable<HtmlNode> node, string id)
        {
            return node.Where(n => n.Id == id);
        }
    }
}
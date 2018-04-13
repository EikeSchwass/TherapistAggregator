using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using HtmlAgilityPack;

namespace TherapistAggregator
{
    public class WebHelper
    {
        private static HttpClient Client { get; } = new HttpClient();

        public async Task<HtmlDocument> DoHttpPostAsync(string url, HttpContent content)
        {
            var responseMessage = await Client.PostAsync(url, content);
            if (responseMessage.StatusCode != HttpStatusCode.OK)
                throw new HttpRequestException($"Status code was {responseMessage.StatusCode}");
            return await LoadHtmlDocumentFromResponseAsync(responseMessage);
        }

        public async Task<HtmlDocument> DoHttpDeleteAsnc(string url)
        {
            var responseMessage = await Client.DeleteAsync(url);
            if (responseMessage.StatusCode != HttpStatusCode.OK)
                throw new HttpRequestException($"Status code was {responseMessage.StatusCode}");
            return await LoadHtmlDocumentFromResponseAsync(responseMessage);
        }

        public async Task<HtmlDocument> DoHttpPutAsync(string url, HttpContent content)
        {
            var responseMessage = await Client.PutAsync(url, content);
            if (responseMessage.StatusCode != HttpStatusCode.OK)
                throw new HttpRequestException($"Status code was {responseMessage.StatusCode}");
            return await LoadHtmlDocumentFromResponseAsync(responseMessage);
        }

        public async Task<HtmlDocument> DoHttpGetAsync(string url)
        {
            var responseMessage = await Client.GetAsync(url);
            if (responseMessage.StatusCode != HttpStatusCode.OK)
                throw new HttpRequestException($"Status code was {responseMessage.StatusCode}");
            return await LoadHtmlDocumentFromResponseAsync(responseMessage);
        }

        private async Task<HtmlDocument> LoadHtmlDocumentFromResponseAsync(HttpResponseMessage responseMessage)
        {
            var htmlDocument = new HtmlDocument();
            var resultData = await responseMessage.Content.ReadAsByteArrayAsync();
            using (var ms = new MemoryStream(resultData))
            {
                ms.Position = 0;
                htmlDocument.Load(ms, Encoding.GetEncoding("iso-8859-1"));
                return htmlDocument;
            }
        }
    }
}
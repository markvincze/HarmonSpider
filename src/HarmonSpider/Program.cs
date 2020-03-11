using HtmlAgilityPack;
using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

namespace HarmonSpider
{
    class Program
    {
        private const string cookie = "FILL";
        private static readonly HttpClient client = new HttpClient();

        static async Task Main(string[] args)
        {
            client.DefaultRequestHeaders.Add("Cookie", cookie);
            var page = 1;

            while (true)
            {
                Console.WriteLine("Processing page {0}", page);
                var response = await client.GetAsync($"https://www.harmontown.com/category/podcasts/page/{page}");

                if(response.StatusCode == HttpStatusCode.NotFound)
                {
                    Console.WriteLine("All episodes have been downloaded.");
                }

                response.EnsureSuccessStatusCode();

                HtmlDocument doc = new HtmlDocument();
                doc.Load(await response.Content.ReadAsStreamAsync());

                //< h2 class="entry-title">
                //    <a href = "https://www.harmontown.com/2019/07/episode-its-not-called-show-friends/" title="Permalink to: &quot;Episode: It’s Not Called Show-Friends&quot;">Episode: It’s Not Called Show-Friends</a>
                //</h2>
                var episodePageLinks = doc.DocumentNode
                    .SelectNodes("//h2/a")
                    .Where(a => a.InnerText.Contains("Video"))
                    .Select(a => a.Attributes["href"].Value)
                    .ToList();

                foreach (var link in episodePageLinks)
                {
                    var episodePageResponse = await client.GetAsync(link);

                    episodePageResponse.EnsureSuccessStatusCode();

                    HtmlDocument episodeDoc = new HtmlDocument();
                    episodeDoc.Load(await episodePageResponse.Content.ReadAsStreamAsync());

                    var downloadLink = episodeDoc.DocumentNode
                        .SelectNodes("//center/a")
                        .FirstOrDefault(a => a.InnerText.StartsWith("Download") && a.Attributes["href"].Value.EndsWith("mp4"))
                        ?.Attributes["href"]?.Value;

                    Console.WriteLine("Downloading {0}...", downloadLink);

                    var fileResponse = await client.GetAsync(downloadLink, HttpCompletionOption.ResponseHeadersRead);

                    fileResponse.EnsureSuccessStatusCode();

                    var fileName = downloadLink.Substring(downloadLink.LastIndexOf('/') + 1);
                    using(var hs = await fileResponse.Content.ReadAsStreamAsync())
                    using(var fs = new FileStream($"C:\\Temp\\Harmontown\\{fileName}", FileMode.Create))
                    {
                        await hs.CopyToAsync(fs);
                    }

                    Console.WriteLine("Download done!");
                }

                page++;
            }
        }
    }
}

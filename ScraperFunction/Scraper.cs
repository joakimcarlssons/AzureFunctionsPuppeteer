using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using PuppeteerSharp;
using System;
using AngleSharp;
using System.Numerics;

namespace ScraperFunction
{
    public static class Scraper
    {
        [FunctionName("Scraper")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = null)] HttpRequest req,
            ILogger log)
        {
            log.LogInformation("C# HTTP trigger function processed a request.");

            var browserFetcher = new BrowserFetcher(new BrowserFetcherOptions()
            {
                Path = Path.GetTempPath()
            });

            await browserFetcher.DownloadAsync(BrowserFetcher.DefaultChromiumRevision);
            var browser = await Puppeteer.LaunchAsync(new LaunchOptions
            {
                Headless = true,
                ExecutablePath = browserFetcher.RevisionInfo(BrowserFetcher.DefaultChromiumRevision.ToString()).ExecutablePath
            });

            using var page = await browser.NewPageAsync();
            await page.SetUserAgentAsync("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/107.0.0.0 Safari/537.36");
            await page.GoToAsync("http://fplstatistics.com/Home/IndexAndroid");
            await page.WaitForNetworkIdleAsync();
            var content = await page.GetContentAsync();

            var context = BrowsingContext.New(Configuration.Default);
            var document = await context.OpenAsync(req => req.Content(content));

            var players = document.QuerySelector("#myDataTable")?.QuerySelector("tbody")?.QuerySelectorAll("tr");

            var props = players[0].QuerySelectorAll("td");

            return new OkObjectResult($"{props[1].QuerySelector("span")?.InnerHtml} -- {props[2].InnerHtml} -- {props[^2].InnerHtml}");
        }
    }
}

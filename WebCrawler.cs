namespace PurpleLemonPhotography.WebsiteOptimizer
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;

    using HtmlAgilityPack;

    using PurpleLemonPhotography.WebsiteOptimizer.SiteProcessors;

    public class WebCrawler
    {

        private readonly IPreCrawlProcessor[] preCrawlProcessors;
        private readonly IPageProcessor[] pageProcessors;
        private readonly IPostCrawlProcessor[] postCrawlProcessors;

        private readonly Logger logger;

        private readonly Dictionary<Uri, string> pages;

        private readonly Dictionary<Uri, string> resources = new Dictionary<Uri, string>();

        private readonly Uri siteRoot;

        public WebCrawler(string url, string outputFolder, bool debugMessages, IPreCrawlProcessor[] preCrawlProcessors, IPageProcessor[] pageProcessors, IPostCrawlProcessor[] postCrawlProcessors)
        {
            this.Url = new Uri(url, UriKind.Absolute);
            this.OutputFolder = new Uri(new Uri(Environment.CurrentDirectory + Path.DirectorySeparatorChar), outputFolder + Path.DirectorySeparatorChar);
            this.logger = new Logger(debugMessages);
            this.preCrawlProcessors = preCrawlProcessors;
            this.pageProcessors = pageProcessors;
            this.postCrawlProcessors = postCrawlProcessors;

            this.logger.LogDebugMessage("Output Folder is {0}", this.OutputFolder.LocalPath);

            this.siteRoot = new Uri(this.Url.GetLeftPart(UriPartial.Authority));
            this.pages = new Dictionary<Uri, string> { { this.Url, null } };
        }

        public Uri OutputFolder { get; private set; }

        public Uri Url { get; private set; }

        public void ProcessWebsite()
        {
            foreach (var processor in this.preCrawlProcessors)
            {
                processor.BeforeCrawl(this.OutputFolder, this.siteRoot, this.pages, this.resources, this.logger);
            }

            var uncrawledPages = this.GetUncrawledPages();
            do
            {
                foreach (var uncrawledPage in uncrawledPages.ToArray())
                {
                    this.ProcessPage(uncrawledPage);
                }

            } 
            while ((uncrawledPages = this.GetUncrawledPages()).Any());

            foreach (var processor in this.postCrawlProcessors)
            {
                processor.AfterCrawl(this.OutputFolder, this.siteRoot, this.pages, this.resources, this.logger);
            }

            foreach (var page in this.pages)
            {
                this.logger.LogMessage("{0}: {1}", page.Key, page.Value);
            }

            foreach (var resource in this.resources.OrderBy(r => Path.GetExtension(r.Key.LocalPath)))
            {
                this.logger.LogMessage("{0}: {1}", this.OutputFolder.MakeRelativeUri(resource.Key), resource.Value);
            }
        }

        private void ProcessPage(Uri pageUrl)
        {
            try
            {
                var webHelper = new HtmlWeb();
                var pageDocument = webHelper.Load(pageUrl.AbsoluteUri);

                this.UpdateLinks(pageUrl, pageDocument);
                foreach (var processor in this.pageProcessors)
                {
                    processor.ProcessPage(this.OutputFolder, this.siteRoot, this.pages, this.resources, pageUrl, pageDocument, this.logger);
                }
            }
            catch (Exception exc)
            {
                this.pages[pageUrl] = "Unhandled exception: " + exc.Message;
                return;
            }

            this.pages[pageUrl] = "Success!";
        }

        private void UpdateLinks(Uri pageUrl, HtmlDocument pageDocument)
        {
            var links = pageDocument.DocumentNode.Descendants("a")
                .Select(a => a.GetAttributeValue("href", null))
                .Where(href => href != null);

            var absoluteLinks = links
                .Select(href => new Uri(pageUrl, href))
                .Where(uri => this.siteRoot.IsBaseOf(uri));

            var newLinks = absoluteLinks.Except(this.pages.Keys);

            foreach (var newLink in newLinks)
            {
                this.pages.Add(newLink, null);
            }
        }

        private IEnumerable<Uri> GetUncrawledPages()
        {
            return this.pages.Where(p => p.Value == null).Select(p => p.Key);
        }
    }
}

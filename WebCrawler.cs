﻿namespace PurpleLemonPhotography.WebsiteOptimizer
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Net;
    using System.Text.RegularExpressions;

    using HtmlAgilityPack;

    using PurpleLemonPhotography.WebsiteOptimizer.SiteProcessors;

    public class WebCrawler
    {
        private const string PageFileName = "index.htm";

        private readonly IPreCrawlProcessor[] preCrawlProcessors;
        private readonly IPageProcessor[] pageProcessors;
        private readonly IPostCrawlProcessor[] postCrawlProcessors;

        private readonly Logger logger;

        private readonly Dictionary<Uri, string> pages;

        private readonly Dictionary<Uri, string> resources = new Dictionary<Uri, string>();

        private readonly Uri siteRoot;

        private static readonly Regex DoctypeRegex = new Regex(@"^<!\s*DOCTYPE", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        private static readonly Regex StartsWithIEConditionalCommentRegex = new Regex(@"^<!--\[if [^\]]*\]>", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        private static readonly Regex EndsWithIEConditionalCommentRegex = new Regex(@"<!\[endif\]-->$", RegexOptions.IgnoreCase | RegexOptions.Compiled);

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

                OptimizeDocument(pageDocument);
                this.SaveToPageDisk(pageUrl, pageDocument);
                this.UpdateLinks(pageUrl, pageDocument);
                this.GetResources(pageUrl, pageDocument);
            }
            catch (Exception exc)
            {
                this.pages[pageUrl] = "Unhandled exception: " + exc.Message;
                return;
            }

            this.pages[pageUrl] = "Success!";
        }

        private static void OptimizeDocument(HtmlDocument pageDocument)
        {
            pageDocument.OptionOutputOptimizeAttributeValues = true;
            foreach (var textNode in (from node in pageDocument.DocumentNode.Descendants()
                                       where node.NodeType == HtmlNodeType.Text
                                       select node).ToArray())
            {
                if (string.IsNullOrWhiteSpace(textNode.InnerText))
                {
                    textNode.Remove();
                }

                textNode.InnerHtml = textNode.InnerHtml.Trim();
            }

            foreach (var commentNode in (from node in pageDocument.DocumentNode.Descendants()
                                         where node.NodeType == HtmlNodeType.Comment
                                         select node).ToArray())
            {
                if (!DoctypeRegex.IsMatch(commentNode.InnerHtml) && 
                    !StartsWithIEConditionalCommentRegex.IsMatch(commentNode.InnerHtml) && 
                    !EndsWithIEConditionalCommentRegex.IsMatch(commentNode.InnerHtml))
                {
                    commentNode.Remove();
                }
            }
        }

        private void GetResources(Uri pageUrl, HtmlDocument pageDocument)
        {
            var linkUrls = pageDocument.DocumentNode.Descendants("link").Select(link => link.GetAttributeValue("href", null));
            var resourcesUrls = pageDocument.DocumentNode.Descendants("script").Union(pageDocument.DocumentNode.Descendants("img"))
                .Select(elem => elem.GetAttributeValue("src", null)).Union(linkUrls)
                .Where(url => url != null)
                .Select(url => new Uri(pageUrl, url))
                .Select(uri => new
            {
                ResourceUrl = uri,
                FilePath = new Uri(this.OutputFolder, uri.LocalPath.Substring(1))
            })
            .Where(resource => !this.resources.ContainsKey(resource.FilePath) && this.siteRoot.IsBaseOf(resource.ResourceUrl));

            using (var webClient = new WebClient())
            {
                foreach (var resourceUrls in resourcesUrls)
                {
                    var localResourcePath = resourceUrls.FilePath.LocalPath;
                    this.logger.LogDebugMessage("Saving resource {0} to {1}", pageUrl, localResourcePath);

                    string errorMessage;
                    if (!this.VerifyDirectoryExists(Path.GetDirectoryName(localResourcePath), out errorMessage))
                    {
                        this.pages[pageUrl] = errorMessage;
                        continue;
                    }

                    webClient.DownloadFile(resourceUrls.ResourceUrl, localResourcePath);

                    this.resources.Add(resourceUrls.FilePath, null);

                    if (Path.GetExtension(localResourcePath).Equals(".js", StringComparison.OrdinalIgnoreCase))
                    {
                        var externsFileName = GetExternsFilePath(localResourcePath);
                        try
                        {
                            webClient.DownloadFile(GetExternsFileUrl(resourceUrls.ResourceUrl), externsFileName);
                        }
                        catch (WebException)
                        {
                            using (File.Create(externsFileName))
                            {
                            }
                        }
                    }
                }
            }
        }

        private static string GetExternsFileUrl(Uri resourceUrl)
        {
            return resourceUrl.AbsoluteUri.Substring(0, resourceUrl.AbsoluteUri.Length - 3) + ".externs.js";
        }

        private static string GetExternsFilePath(string filePath)
        {
            return Path.Combine(Environment.CurrentDirectory, Path.GetFileNameWithoutExtension(filePath) + ".externs.js");
        }

        private void SaveToPageDisk(Uri pageUrl, HtmlDocument pageDocument)
        {
            var relativeFilePath = Path.Combine(pageUrl.LocalPath.Substring(1), PageFileName);
            var filename = new Uri(this.OutputFolder, relativeFilePath).LocalPath;
            this.logger.LogDebugMessage("Saving page {0} to {1}", pageUrl, filename);

            string errorMessage;
            if (!this.VerifyDirectoryExists(Path.GetDirectoryName(filename), out errorMessage))
            {
                this.pages[pageUrl] = errorMessage;
                return;
            }

            pageDocument.Save(filename);
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

        private bool VerifyDirectoryExists(string directoryName, out string errorMessage)
        {
            errorMessage = null;
            if (Directory.Exists(directoryName))
            {
                return true;
            }
            
            try
            {
                Directory.CreateDirectory(directoryName);
                return true;
            }
            catch (IOException exc)
            {
                errorMessage = "IOException trying to create directory: " + exc.Message;
                this.logger.LogDebugMessage(errorMessage);
                return false;
            }
        }

        private IEnumerable<Uri> GetUncrawledPages()
        {
            return this.pages.Where(p => p.Value == null).Select(p => p.Key);
        }
    }
}

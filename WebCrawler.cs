namespace PurpleLemonPhotography.WebsiteOptimizer
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Net;

    using HtmlAgilityPack;

    public class WebCrawler
    {
        private const string PageFileName = "index.htm";

        private readonly Dictionary<Uri, string> pages;

        private readonly Dictionary<Uri, string> resources = new Dictionary<Uri, string>();

        private readonly Uri siteRoot;

        public WebCrawler(string url, string outputFolder, bool debugMessages)
        {
            this.Url = new Uri(url, UriKind.Absolute);
            this.OutputFolder = new Uri(new Uri(Environment.CurrentDirectory + Path.DirectorySeparatorChar), outputFolder + Path.DirectorySeparatorChar);
            this.DebugMessages = debugMessages;

            this.LogMessage("Output Folder is {0}", this.OutputFolder.LocalPath);

            this.siteRoot = new Uri(this.Url.GetLeftPart(UriPartial.Authority));
            this.pages = new Dictionary<Uri, string> { { this.Url, null } };
        }

        public bool DebugMessages { get; private set; }

        public Uri OutputFolder { get; private set; }

        public Uri Url { get; private set; }

        public void ProcessWebsite()
        {
            try
            {
                Directory.Delete(this.OutputFolder.LocalPath, true);
            }
            catch (IOException exc)
            {
                this.LogMessage("Error clearing output folder: {0}", exc.Message);
            }

            var uncrawledPages = this.GetUncrawledPages();
            do
            {
                foreach (var uncrawledPage in uncrawledPages.ToList())
                {
                    this.ProcessPage(uncrawledPage);
                }

            } 
            while ((uncrawledPages = this.GetUncrawledPages()).Any());

            OptimizeResources();

            foreach (var page in this.pages)
            {
                Console.WriteLine("{0}: {1}", page.Key, page.Value);
            }

            foreach (var resource in this.resources.OrderBy(r => Path.GetExtension(r.Key.LocalPath)))
            {
                Console.WriteLine("{0}: {1}", this.OutputFolder.MakeRelativeUri(resource.Key), resource.Value);
            }
        }

        private void OptimizeResources()
        {
            foreach (var resource in this.resources.Keys.ToArray())
            {
                var extension = Path.GetExtension(resource.LocalPath);
                switch (extension.ToUpperInvariant())
                {
                    case ".CSS":
                        break;
                    case ".JS":
                        OptimizeJavaScript(resource);
                        break;
                    case ".PNG":
                        break;
                    default:
                        this.resources[resource] = "No support for optimizing " + extension;
                        break;
                }
            }
        }

        private void OptimizeJavaScript(Uri resource)
        {
            this.LogMessage("Optimizing " + this.OutputFolder.MakeRelativeUri(resource));
            var externsFileName = GetExternsFilePath(resource.LocalPath);

// ReSharper disable PossibleNullReferenceException
            Process.Start(
                new ProcessStartInfo(
                    fileName: "java",
                    arguments:
                        string.Format(
                            @"-jar libs\compiler.jar --js=""{0}"" --js_output_file=""{0}"" --warning_level VERBOSE --externs=""{1}""",
                            resource.LocalPath,
                            externsFileName))
                    {
                        WorkingDirectory = Environment.CurrentDirectory,
                        UseShellExecute = !this.DebugMessages,
                        RedirectStandardOutput = this.DebugMessages
                    }).WaitForExit();

// ReSharper restore PossibleNullReferenceException
            File.Delete(externsFileName);
        }

        private void ProcessPage(Uri pageUrl)
        {
            try
            {
                var webHelper = new HtmlWeb();
                var pageDocument = webHelper.Load(pageUrl.AbsoluteUri);
                pageDocument.OptionOutputAsXml = true; // TODO: Determine doctype

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
                    this.LogMessage("Saving resource {0} to {1}", pageUrl, localResourcePath);

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
            this.LogMessage("Saving page {0} to {1}", pageUrl, filename);

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
                this.LogMessage(errorMessage);
                return false;
            }
        }

        private IEnumerable<Uri> GetUncrawledPages()
        {
            return this.pages.Where(p => p.Value == null).Select(p => p.Key);
        }

        private void LogMessage(string format, params object[] arg)
        {
            if (!this.DebugMessages)
            {
                return;
            }

            Console.WriteLine("****************************************");
            Console.WriteLine(format, arg);
            Console.WriteLine();
        }
    }
}

namespace PurpleLemonPhotography.WebsiteOptimizer.SiteProcessors
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Net;

    using HtmlAgilityPack;

    public class FileOutputProcessor : IPreCrawlProcessor, IPageProcessor
    {
        private const string PageFileName = "index.htm";

        public void BeforeCrawl(Uri outputFolder, Logger logger)
        {
            try
            {
                Directory.Delete(outputFolder.LocalPath, true);
            }
            catch (IOException exc)
            {
                logger.LogDebugMessage("Error clearing output folder: {0}", exc.Message);
            }
        }

        public void ProcessPage(Uri outputFolder, Func<Uri, bool> isLocalUrl, Dictionary<Uri, string> pages, Dictionary<Uri, string> resources, Uri pageUrl, HtmlDocument pageDocument, Logger logger)
        {
            this.SavePageFile(outputFolder, pages, pageUrl, pageDocument, logger);
            this.GetResources(outputFolder, isLocalUrl, pages, resources, pageUrl, pageDocument, logger);
        }

        private void SavePageFile(Uri outputFolder, Dictionary<Uri, string> pages, Uri pageUrl, HtmlDocument pageDocument, Logger logger)
        {
            var relativeFilePath = Path.Combine(pageUrl.LocalPath.Substring(1), PageFileName);
            var filename = new Uri(outputFolder, relativeFilePath).LocalPath;
            logger.LogDebugMessage("Saving page {0} to {1}", pageUrl, filename);

            string errorMessage;
            if (!VerifyDirectoryExists(Path.GetDirectoryName(filename), logger, out errorMessage))
            {
                pages[pageUrl] = errorMessage;
                return;
            }

            pageDocument.Save(filename);
        }

        private void GetResources(Uri outputFolder, Func<Uri, bool> isLocalUrl, Dictionary<Uri, string> pages, Dictionary<Uri, string> resources, Uri pageUrl, HtmlDocument pageDocument, Logger logger)
        {
            var resourcesUrls = pageDocument.GetResourcesUrls(outputFolder, isLocalUrl, resources, pageUrl);

            using (var webClient = new WebClient())
            {
                foreach (var resourceUrls in resourcesUrls)
                {
                    var localResourcePath = resourceUrls.FilePath.LocalPath;
                    logger.LogDebugMessage("Saving resource {0} to {1}", pageUrl, localResourcePath);

                    string errorMessage;
                    if (!VerifyDirectoryExists(Path.GetDirectoryName(localResourcePath), logger, out errorMessage))
                    {
                        pages[pageUrl] = errorMessage;
                        continue;
                    }

                    webClient.DownloadFile(resourceUrls.ResourceUrl, localResourcePath);

                    resources.Add(resourceUrls.FilePath, null);
                }
            }
        }

        private static bool VerifyDirectoryExists(string directoryName, Logger logger, out string errorMessage)
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
                logger.LogDebugMessage(errorMessage);
                return false;
            }
        }
    }
}
namespace PurpleLemonPhotography.WebsiteOptimizer.SiteProcessors
{
    using System;
    using System.Collections.Generic;
    using System.IO;

    using HtmlAgilityPack;

    public class FileOutputProcessor : IPreCrawlProcessor, IPageProcessor
    {
        public void BeforeCrawl(Uri outputFolder, Uri siteRoot, Dictionary<Uri, string> pages, Dictionary<Uri, string> resources, Logger logger)
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

        public void ProcessPage(Uri outputFolder, Uri siteRoot, Dictionary<Uri, string> pages, Dictionary<Uri, string> resources, Uri pageUrl, HtmlDocument pageDocument, Logger logger)
        {
            throw new NotImplementedException();
        }
    }
}
namespace PurpleLemonPhotography.WebsiteOptimizer.SiteProcessors
{
    using System;
    using System.Collections.Generic;

    using HtmlAgilityPack;

    public class ImageOptimizationProcessor : IPageProcessor
    {
        public void ProcessPage(Uri outputFolder, Func<Uri, bool> isLocalUrl, Dictionary<Uri, string> pages, Dictionary<Uri, string> resources, Uri pageUrl, HtmlDocument pageDocument, Logger logger)
        {
            logger.LogDebugMessage("Image optimization not yet implemented");
        }
    }
}
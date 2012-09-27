namespace PurpleLemonPhotography.WebsiteOptimizer.SiteProcessors
{
    using System;
    using System.Collections.Generic;

    using HtmlAgilityPack;

    public class ImageOptimizationProcessor : IPageProcessor
    {
        public void ProcessPage(Uri outputFolder, Uri siteRoot, Dictionary<Uri, string> pages, Dictionary<Uri, string> resources, Uri pageUrl, HtmlDocument pageDocument, Logger logger)
        {
            throw new NotImplementedException();
        }
    }
}
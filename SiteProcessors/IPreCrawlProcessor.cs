namespace PurpleLemonPhotography.WebsiteOptimizer.SiteProcessors
{
    using System;
    using System.Collections.Generic;

    public interface IPreCrawlProcessor
    {
        void BeforeCrawl(Uri outputFolder, Uri siteRoot, Dictionary<Uri, string> pages, Dictionary<Uri, string> resources, Logger logger);
    }
}
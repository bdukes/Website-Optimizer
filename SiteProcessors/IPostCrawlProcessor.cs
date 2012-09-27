namespace PurpleLemonPhotography.WebsiteOptimizer.SiteProcessors
{
    using System;
    using System.Collections.Generic;

    public interface IPostCrawlProcessor
    {
        void AfterCrawl(Uri outputFolder, Dictionary<Uri, string> pages, Dictionary<Uri, string> resources, Logger logger);
    }
}
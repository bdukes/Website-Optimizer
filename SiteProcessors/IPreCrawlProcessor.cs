namespace PurpleLemonPhotography.WebsiteOptimizer.SiteProcessors
{
    using System;

    public interface IPreCrawlProcessor
    {
        void BeforeCrawl(Uri outputFolder, Logger logger);
    }
}
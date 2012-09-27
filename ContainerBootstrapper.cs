namespace PurpleLemonPhotography.WebsiteOptimizer
{
    using PurpleLemonPhotography.WebsiteOptimizer.SiteProcessors;

    using StructureMap;

    public static class ContainerBootstrapper
    {
        public static void BootstrapStructureMap(string url, string outputFolder, bool debugMessages)
        {
            ObjectFactory.Initialize(x =>
                {
                    x.Scan(
                        scanner =>
                            {
                                scanner.AssembliesFromApplicationBaseDirectory();

                                scanner.AddAllTypesOf<IPreCrawlProcessor>();
                                scanner.AddAllTypesOf<IPostCrawlProcessor>();
                                scanner.AddAllTypesOf<IPageProcessor>();

                                scanner.WithDefaultConventions();
                            });

                    x.ForConcreteType<WebCrawler>().Configure.Ctor<string>("url").Is(url)
                                                             .Ctor<string>("outputFolder").Is(outputFolder)
                                                             .Ctor<bool>("debugMessages").Is(debugMessages);
                });
        }
    }
}
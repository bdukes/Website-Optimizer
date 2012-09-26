namespace PurpleLemonPhotography.WebsiteOptimizer
{
    using PurpleLemonPhotography.WebsiteOptimizer.SiteProcessors;

    using StructureMap;

    public class ContainerBootstrapper
    {
        public static void BootstrapStructureMap()
        {
        }

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

                    x.ForConcreteType<WebCrawler>().Configure.Ctor<string>("url").Is(url);
                    x.ForConcreteType<WebCrawler>().Configure.Ctor<string>("outputFolder").Is(outputFolder);
                    x.ForConcreteType<WebCrawler>().Configure.Ctor<bool>("debugMessages").Is(debugMessages);
                });
        }
    }
}
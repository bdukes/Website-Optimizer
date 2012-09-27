namespace PurpleLemonPhotography.WebsiteOptimizer
{
    using System;

    using StructureMap;

    public class Program
    {
        private static void Main(string[] args)
        {
            try
            {
                var webCrawler = ProcessArguments(args);

                if (webCrawler != null)
                {
                    webCrawler.ProcessWebsite();

                    ////Console.WriteLine("Launching site");

                    ////StartWebserver(webCrawler.OutputFolder);
                    ////BrowseToSite();
                }
            }
            catch (Exception exc)
            {
                Console.WriteLine("Unhandled Exception: {0}", exc.Message);
                Console.WriteLine("Stack Trace: {0}", exc.StackTrace);
            }

            Console.WriteLine("All Done!");
        }

        ////private static void BrowseToSite()
        ////{
        ////    Process.Start(
        ////        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), @"Mozilla Firefox\firefox.exe"),
        ////        "http://localhost:8080/");
        ////}

        ////private static void StartWebserver(Uri outputFolder)
        ////{
        ////    Process.Start(
        ////        Path.Combine(
        ////            Environment.GetFolderPath(Environment.SpecialFolder.CommonProgramFilesX86),
        ////            @"microsoft shared\DevServer\10.0\Webdev.WebServer40.exe"),
        ////        string.Format("/port:8080 /path:\"{0}\"", outputFolder.LocalPath));
        ////}

        private static WebCrawler ProcessArguments(string[] args)
        {
            if (args == null || args.Length < 3 || args[1] != "-o" || (args.Length == 4 && args[3] != "-v"))
            {
                Console.WriteLine("Usage: WebsiteOptimizer ErrorUrl -o OutputFolder [-v]");
                return null;
            }

            ContainerBootstrapper.BootstrapStructureMap(url: args[0], outputFolder: args[2], debugMessages: args.Length == 4);
            return ObjectFactory.GetInstance<WebCrawler>();
        }
    }
}

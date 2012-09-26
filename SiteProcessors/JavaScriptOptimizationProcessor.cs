namespace PurpleLemonPhotography.WebsiteOptimizer.SiteProcessors
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;

    public class JavaScriptOptimizationProcessor : IPostCrawlProcessor
    {
        public void AfterCrawl(Uri outputFolder, Uri siteRoot, Dictionary<Uri, string> pages, Dictionary<Uri, string> resources, Logger logger)
        {
            foreach (var resource in resources.Keys.ToArray().Where(resource => Path.GetExtension(resource.LocalPath).Equals(".JS", StringComparison.OrdinalIgnoreCase)))
            {
                logger.LogDebugMessage("Optimizing " + outputFolder.MakeRelativeUri(resource));
                var externsFileName = GetExternsFilePath(resource.LocalPath);

                // ReSharper disable PossibleNullReferenceException
                Process.Start(
                    new ProcessStartInfo(
                        fileName: "java",
                        arguments:
                            string.Format(
                                @"-jar libs\compiler.jar --js=""{0}"" --js_output_file=""{0}"" --warning_level VERBOSE --externs=""{1}""",
                                resource.LocalPath,
                                externsFileName))
                    {
                        WorkingDirectory = Environment.CurrentDirectory,
                        UseShellExecute = !logger.InDebugMode,
                        RedirectStandardOutput = logger.InDebugMode,
                    }).WaitForExit();

                // ReSharper restore PossibleNullReferenceException
                File.Delete(externsFileName);
            }
        }

        private static string GetExternsFilePath(string filePath)
        {
            return Path.Combine(Environment.CurrentDirectory, Path.GetFileNameWithoutExtension(filePath) + ".externs.js");
        }
    }
}
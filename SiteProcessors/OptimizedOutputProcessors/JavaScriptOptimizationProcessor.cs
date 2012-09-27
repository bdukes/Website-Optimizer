namespace PurpleLemonPhotography.WebsiteOptimizer.SiteProcessors
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Net;

    using HtmlAgilityPack;

    public class JavaScriptOptimizationProcessor : IPageProcessor, IPostCrawlProcessor
    {
        public void ProcessPage(Uri outputFolder, Uri siteRoot, Dictionary<Uri, string> pages, Dictionary<Uri, string> resources, Uri pageUrl, HtmlDocument pageDocument, Logger logger)
        {
            var resourcesUrls = pageDocument.GetResourcesUrls(outputFolder, siteRoot, resources, pageUrl);

            using (var webClient = new WebClient())
            {
                foreach (var resourceUrls in resourcesUrls)
                {
                    var localResourcePath = resourceUrls.FilePath.LocalPath;
                    if (!Path.GetExtension(localResourcePath).Equals(".js", StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    string errorMessage;
                    if (!VerifyDirectoryExists(Path.GetDirectoryName(localResourcePath), logger, out errorMessage))
                    {
                        continue;
                    }

                    var externsFileName = GetExternsFilePath(localResourcePath);
                    try
                    {
                        webClient.DownloadFile(GetExternsFileUrl(resourceUrls.ResourceUrl), externsFileName);
                    }
                    catch (WebException)
                    {
                        using (File.Create(externsFileName))
                        {
                        }
                    }
                }
            }
        }

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

        private static string GetExternsFileUrl(Uri resourceUrl)
        {
            return resourceUrl.AbsoluteUri.Substring(0, resourceUrl.AbsoluteUri.Length - 3) + ".externs.js";
        }

        private static string GetExternsFilePath(string filePath)
        {
            return Path.Combine(Environment.CurrentDirectory, Path.GetFileNameWithoutExtension(filePath) + ".externs.js");
        }

        private static bool VerifyDirectoryExists(string directoryName, Logger logger, out string errorMessage)
        {
            errorMessage = null;
            if (Directory.Exists(directoryName))
            {
                return true;
            }

            try
            {
                Directory.CreateDirectory(directoryName);
                return true;
            }
            catch (IOException exc)
            {
                errorMessage = "IOException trying to create directory: " + exc.Message;
                logger.LogDebugMessage(errorMessage);
                return false;
            }
        }
    }
}
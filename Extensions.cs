namespace PurpleLemonPhotography.WebsiteOptimizer
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using HtmlAgilityPack;

    using PurpleLemonPhotography.WebsiteOptimizer.SiteProcessors;

    public static class Extensions
    {
        public static IEnumerable<ResourceFile> GetResourcesUrls(this HtmlDocument pageDocument, Uri outputFolder, Uri siteRoot, Dictionary<Uri, string> resources, Uri pageUrl)
        {
            return pageDocument.DocumentNode.Descendants("script")
                .Union(pageDocument.DocumentNode.Descendants("img"))
                .Select(elem => elem.GetAttributeValue("src", null))
                .Union(pageDocument.DocumentNode.Descendants("link").Select(link => link.GetAttributeValue("href", null)))
                .Where(url => url != null)
                .Select(url => new Uri(pageUrl, url))
                .Select(uri => new ResourceFile(uri, new Uri(outputFolder, uri.LocalPath.Substring(1))))
                .Where(resource => !resources.ContainsKey(resource.FilePath) && siteRoot.IsBaseOf(resource.ResourceUrl));
        }
    }
}
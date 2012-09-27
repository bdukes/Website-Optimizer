namespace PurpleLemonPhotography.WebsiteOptimizer.SiteProcessors
{
    using System;

    public class ResourceFile
    {
        private readonly Uri resourceUrl;
        private readonly Uri filePath;

        public ResourceFile(Uri resourceUrl, Uri filePath)
        {
            this.resourceUrl = resourceUrl;
            this.filePath = filePath;
        }

        public Uri ResourceUrl
        {
            get { return this.resourceUrl; }
        }

        public Uri FilePath
        {
            get { return this.filePath; }
        }
    }
}
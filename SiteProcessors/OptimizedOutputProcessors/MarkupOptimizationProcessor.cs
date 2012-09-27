namespace PurpleLemonPhotography.WebsiteOptimizer.SiteProcessors
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text.RegularExpressions;

    using HtmlAgilityPack;

    public class MarkupOptimizationProcessor : IPageProcessor
    {
        private static readonly Regex DoctypeRegex = new Regex(@"^<!\s*DOCTYPE", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        private static readonly Regex StartsWithIEConditionalCommentRegex = new Regex(@"^<!--\[if [^\]]*\]>", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        private static readonly Regex EndsWithIEConditionalCommentRegex = new Regex(@"<!\[endif\]-->$", RegexOptions.IgnoreCase | RegexOptions.Compiled);

        public void ProcessPage(Uri outputFolder, Uri siteRoot, Dictionary<Uri, string> pages, Dictionary<Uri, string> resources, Uri pageUrl, HtmlDocument pageDocument, Logger logger)
        {
            pageDocument.OptionOutputOptimizeAttributeValues = true;
            foreach (var textNode in (from node in pageDocument.DocumentNode.Descendants()
                                      where node.NodeType == HtmlNodeType.Text
                                      select node).ToArray())
            {
                if (string.IsNullOrWhiteSpace(textNode.InnerText))
                {
                    textNode.Remove();
                }

                textNode.InnerHtml = textNode.InnerHtml.Trim();
            }

            foreach (var commentNode in (from node in pageDocument.DocumentNode.Descendants()
                                         where node.NodeType == HtmlNodeType.Comment
                                         select node).ToArray())
            {
                if (!DoctypeRegex.IsMatch(commentNode.InnerHtml) &&
                    !StartsWithIEConditionalCommentRegex.IsMatch(commentNode.InnerHtml) &&
                    !EndsWithIEConditionalCommentRegex.IsMatch(commentNode.InnerHtml))
                {
                    commentNode.Remove();
                }
            }
        }
    }
}
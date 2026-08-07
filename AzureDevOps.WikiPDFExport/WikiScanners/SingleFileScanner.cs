using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.TeamFoundation.TestManagement.WebApi;
using Microsoft.TeamFoundation.Wiki.WebApi;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace azuredevops_export_wiki
{
    internal class SingleFileScanner : WikiOptionFilesScanner
    {
        private readonly string singleFilePath;

        public SingleFileScanner(string filePath, string wikiPath, Options options, ILogger logger)
            : base(wikiPath, options, logger) 
        {
            this.singleFilePath = filePath;
        }

        public override IList<MarkdownFile> Scan()
        {
            //handle the case that we have a .order file
            var allFiles = base.Scan(); 

            //handle the case that we have a single file only by providing a .md file in the -s parameter
            if (singleFilePath.EndsWith(".md", true, System.Globalization.CultureInfo.InvariantCulture))
            {
                var fileInfo = new FileInfo(singleFilePath);
                if (!fileInfo.Exists)
                {
                    var repairedPath = AzureDevopsToMarkdownConverter.TryRepairUtf8Mojibake(singleFilePath);
                    if (!string.IsNullOrEmpty(repairedPath))
                    {
                        fileInfo = new FileInfo(repairedPath);
                    }
                }
                if (!fileInfo.Exists)
                {
                    throw new FileNotFoundException($"The specified Markdown file was not found: {singleFilePath}", singleFilePath);
                }
                var file  = new MarkdownFile(fileInfo, "", 0, Path.GetRelativePath(wikiPath, fileInfo.FullName));
                return new List<MarkdownFile>() { file };
            }

            var current = allFiles.FirstOrDefault(m => m.FileRelativePath.Contains(singleFilePath));
            var idx = allFiles.IndexOf(current);

            if (current is null || idx == -1)
            {
                logger.Log($"Single-File [-s] {singleFilePath} specified not found" + singleFilePath, LogLevel.Error);
                throw new ArgumentException($"{singleFilePath} not found");
            }
            var result = new List<MarkdownFile>() { current };

            if (allFiles.Count > idx)
            {
                foreach (var item in allFiles.Skip(idx + 1))
                {
                    if (item.Level > current.Level)
                    {
                        result.Add(item);
                    }
                    else
                        break;
                }
            }

            return result;
        }

    }
}

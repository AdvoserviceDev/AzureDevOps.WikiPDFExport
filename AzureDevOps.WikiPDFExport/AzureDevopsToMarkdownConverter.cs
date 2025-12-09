using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace azuredevops_export_wiki
{
    /// <summary>
    /// Provides methods to convert Azure DevOps markdown to standard markdown.
    /// </summary>
    class AzureDevopsToMarkdownConverter
    {
        // Regex pattern to find all headlines without a space between # and the headline, excluding numbers after # (workItems).  
        // Format: #headline
        private const string HeadlinePattern = @"^(#+)(?!\d+|$|#)([^\s])(.*)$";

        /// <summary>
        /// Preprocesses the markdown content to ensure consistency by:
        /// - Converting Azure DevOps-specific syntax to standard markdown.
        /// - Adding a space between # and the headline title to ensure Azure DevOps renders headlines correctly in standard Markdown. Exclude WorkItems
        /// - Adding <br> tag after linebreak of Azure DevOps markdown to break line in standard markdown.
        /// </summary>
        /// <param name="markdown">The markdown content to preprocess.</param>
        /// <returns>The processed markdown content.</returns>
        public static string ConvertAzureDevopsToStandardMarkdown(string markdown)
        {
            // 1. Add a space after # for headlines matched by HeadlinePattern (ignores WorkItems).
            string processedText = Regex.Replace(markdown, HeadlinePattern, "$1 $2$3", RegexOptions.Multiline);

            // 2. Insert <br> tag in all lines that contain a line break, are not headlines, do not contain headlines, and are not inside tables.
            // Initialization
            string[] lines = processedText.Split('\n');
            List<string> processedLines = new List<string>();
            bool isInCodeBlock = false;
            bool isInTable = false;

            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i].TrimEnd();
                string nextLine = i < lines.Length - 1 ? lines[i + 1].TrimEnd() : "";

                // Checks if the line is inside a code block.
                if (line.Trim().StartsWith("```"))
                {
                    processedLines.Add(line); // Adds the line with no <br> Tag
                    isInCodeBlock = !isInCodeBlock;
                    continue; // Exits the loop to prevent adding a <br> tag to the last line of a code block
                }

                // Checks if the line is inside a table
                // If the line starts with "|", it sets isInTable to true
                if (line.StartsWith("|"))
                {
                    if (isInTable == false)
                    {
                        // Check if the previous line in processedLines has content (potential caption)
                        // Skip empty lines and look for the last non-empty line
                        int lastNonEmptyIndex = processedLines.Count - 1;
                        while (lastNonEmptyIndex >= 0 && string.IsNullOrWhiteSpace(processedLines[lastNonEmptyIndex]))
                        {
                            lastNonEmptyIndex--;
                        }
                        
                        if (lastNonEmptyIndex >= 0)
                        {
                            string lastLine = processedLines[lastNonEmptyIndex];
                            // Get the text without <br> tag for checking if it's a headline
                            string lastLineWithoutBr = lastLine.Replace("<br>", "").Trim();
                            
                            // Only wrap as caption if the previous line is NOT a headline
                            // Headlines start with # after trimming and need to stay as-is for proper rendering
                            if (!lastLineWithoutBr.StartsWith("#") && !lastLine.Contains("<span class=\"table-with-caption\">"))
                            {
                                // Wrap the caption with the class
                                processedLines[lastNonEmptyIndex] = $"<span class=\"table-with-caption\">{lastLine}</span>";
                            }
                        }
                        processedLines.Add(""); // Inserts an empty line before the start of a table to ensure that tables without a preceding empty line are also recognized as tables.
                    }
                    isInTable = true;
                }
                // If the line no longer starts with "|", and is not empty or whitespace, it sets isInTable to false
                else if (isInTable && !line.StartsWith("|") && !string.IsNullOrWhiteSpace(line))
                {
                    isInTable = false;
                }

                // Determine if we should add a <br> tag
                bool shouldAddBreak =
                    !isInCodeBlock && // code blocks should not get line breaks because they would break the code block structure
                    !isInTable && // tables should not get line breaks because they would break the table structure
                    !string.IsNullOrWhiteSpace(line) && // empty lines should not get line breaks to avoid to large empty space
                    !line.Contains("[TOC]") && // table of content should not get line breaks because to avoid broken br tags
                    !(nextLine.StartsWith("|") && !isInTable); // don't add <br> before table captions

                if (shouldAddBreak)
                {
                    line += "<br>";
                }

                processedLines.Add(line);
            }

            return string.Join("\n", processedLines);
        }
    }
}
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
                    // Detect and mark a table caption if text is directly above the table (no empty line)
                    HandleTableStart(ref isInTable, processedLines);
                }
                else if (isInTable && !line.StartsWith("|") && !string.IsNullOrWhiteSpace(line))
                {
                    isInTable = false;
                }

                // Add <br> tags for Azure DevOps line break compatibility
                if (ShouldAddLineBreak(line, nextLine, isInCodeBlock, isInTable))
                {
                    line += "<br>";
                }

                processedLines.Add(line);
            }

            return string.Join("\n", processedLines);
        }

        /// <summary>
        /// Wraps text directly above tables with table-caption span to prevent page breaks with css-code.
        /// Adds empty line before table for proper recognition.
        /// </summary>
        private static void HandleTableStart(ref bool isInTable, List<string> processedLines)
        {
            if (isInTable == false)
            {
                // Find last non-empty line (potential table caption)
                int lastNonEmptyIndex = FindLastNonEmptyLineIndex(processedLines);


                // Only if there is no empty line between text and table, the text is wrapped as table caption
                if (lastNonEmptyIndex >= 0)
                {
                    WrapTableCaptionIfNeeded(processedLines, lastNonEmptyIndex);
                }

                // Insert empty line for table recognition
                processedLines.Add("");
            }
            isInTable = true;
        }

        // Finds the last non-empty line to detect a potential table caption
        private static int FindLastNonEmptyLineIndex(List<string> processedLines)
        {
            int lastNonEmptyIndex = processedLines.Count - 1;
            while (lastNonEmptyIndex >= 0 && string.IsNullOrWhiteSpace(processedLines[lastNonEmptyIndex]))
            {
                lastNonEmptyIndex--;
            }
            return lastNonEmptyIndex;
        }

        /// <summary>
        /// Wraps non-headline text with table-caption class for CSS styling.
        /// </summary>
        private static void WrapTableCaptionIfNeeded(List<string> processedLines, int lineIndex)
        {
            string lastLine = processedLines[lineIndex];
            // Remove <br> tag to accurately identify headlines
            string lastLineWithoutBr = lastLine.Replace("<br>", "").Trim();

            // Only wrap non-headline text that doesn't already have the class
            if (!lastLineWithoutBr.StartsWith("#") && !lastLine.Contains("<span class=\"table-caption\">"))
            {
                processedLines[lineIndex] = $"<span class=\"table-caption\">{lastLine}</span>";
            }
        }

        /// <summary>
        /// Determines if a <br> tag should be added for Azure DevOps markdown compatibility.
        /// </summary>
        private static bool ShouldAddLineBreak(string line, string nextLine, bool isInCodeBlock, bool isInTable)
        {
            return !isInCodeBlock && // Code blocks would break
                   !isInTable && // Table structure would break
                   !string.IsNullOrWhiteSpace(line) && // Avoid excessive spacing
                   !line.Contains("[TOC]") && // TOC would break
                   !(nextLine.StartsWith("|") && !isInTable); // Don't break table captions
        }
    }
}
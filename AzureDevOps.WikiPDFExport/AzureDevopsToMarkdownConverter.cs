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
        public static (string, bool) ConvertAzureDevopsToStandardMarkdown(string markdown, bool hasH2NoAsTopLevelHeadline)
        {
            // 1. Add a space after # for headlines matched by HeadlinePattern (ignores WorkItems).
            string processedText = Regex.Replace(markdown, HeadlinePattern, "$1 $2$3", RegexOptions.Multiline);

            // 2. Insert <br> tag in all lines that contain a line break, are not headlines, do not contain headlines, and are not inside tables.
            // Initialization
            string[] lines = processedText.Split('\n');
            List<string> processedLines = new List<string>();
            bool isInCodeBlock = false;
            bool isInTable = false;
            bool isTocPrint = false;

            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i].TrimEnd();
                string nextLine = i < lines.Length - 1 ? lines[i + 1].TrimEnd() : "";

                if (line.Contains("[TOC]"))
                {
                    isTocPrint = true;
                }

                // Checks if the line is inside a code block.
                if (line.Trim().StartsWith("```"))
                {
                    processedLines.Add(line); // Adds the line with no <br> Tag
                    isInCodeBlock = !isInCodeBlock;
                    continue; // Exits the loop to prevent adding a <br> tag to the last line of a code block
                }

                // Checks if the line is inside a table
                // If the line starts and ends with "|", it sets isInTable to true
                if (line.StartsWith("|") && line.EndsWith("|"))
                {
                    isInTable = true;
                }
                // If the line no longer starts or ends with "|", and is not empty or whitespace, it sets isInTable to false
                else if (isInTable && (!line.StartsWith("|") || !line.EndsWith("|")) && !string.IsNullOrWhiteSpace(line))
                {
                    isInTable = false;
                }

                // Determine if we should add a <br> tag
                bool shouldAddBreak =
                    !isInCodeBlock && // code blocks should not get line breaks because they would break the code block structure
                    !isInTable && // tables should not get line breaks because they would break the table structure
                    !string.IsNullOrWhiteSpace(line) && // empty lines should not get line breaks to avoid to large empty space
                    !line.Contains("[TOC]"); // table of content should not get line breaks because to avoid broken br tags

                if (shouldAddBreak)
                {
                    line += "<br>";
                }

                // Page-Break logic in headlines
                if (shouldAddBreak && IsHeadline(line) && !isTocPrint)
                {
                    int headlineLevel = GetHeadlineLevel(line);
                    
                    // Add pagebreak if headline is not after another h2 headline
                    if(headlineLevel == 1 && hasH2NoAsTopLevelHeadline)
                    {
                        processedLines.Add("<div style='page-break-before: always;'></div>");
                        processedLines.Add("");
                        hasH2NoAsTopLevelHeadline = false;
                    }

                    else if (headlineLevel == 1)
                    {
                        hasH2NoAsTopLevelHeadline = false;
                    }

                    else
                    {
                        hasH2NoAsTopLevelHeadline = true;
                    }
                }
                
                processedLines.Add(line);
            }

            return (string.Join("\n", processedLines), hasH2NoAsTopLevelHeadline);
        }

        /// <summary>
        /// Checks if a line is a headline (starts with #).
        /// </summary>
        /// <param name="line">The line to check.</param>
        /// <returns>True if the line is a headline, false otherwise.</returns>
        private static bool IsHeadline(string line)
        {
            string trimmedLine = line.Trim();
            return trimmedLine.StartsWith("#") && !Regex.IsMatch(trimmedLine, @"^#+\d"); // Exclude WorkItems
        }

        /// <summary>
        /// Gets the headline level (number of # characters) from a headline.
        /// </summary>
        /// <param name="line">The headline line.</param>
        /// <returns>The headline level (1-6), or 0 if not a valid headline.</returns>
        private static int GetHeadlineLevel(string line)
        {
            if (!IsHeadline(line))
                return 0;

            string trimmedLine = line.Trim();
            int level = 0;

            for (int i = 0; i < trimmedLine.Length && i < 6; i++)
            {
                if (trimmedLine[i] == '#')
                    level++;
                else
                    break;
            }

            return level;
        }
    }
}
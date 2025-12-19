using System.Collections.Generic;

namespace azuredevops_export_wiki
{
    /// <summary>
    /// Processes markdown by adding HTML tags for better PDF rendering.
    /// This class adds HTML-specific enhancements that are not part of standard markdown conversion.
    /// </summary>
    class MarkdownHtmlProcessor
    {
        /// <summary>
        /// Inserts HTML table captions to prevent page breaks between table titles and tables.
        /// Wraps text directly above tables with span tags for CSS styling.
        /// </summary>
        /// <param name="markdown">The markdown content to process.</param>
        /// <returns>The processed markdown content with HTML table caption tags.</returns>
        public static string InsertTableCaptions(string markdown)
        {
            string[] lines = markdown.Split('\n');
            List<string> processedLines = new List<string>();
            bool isInTable = false;

            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i].TrimEnd();

                // Detect table start
                if (line.StartsWith("|"))
                {
                    if (!isInTable)
                    {
                        // Check if previous line should be a table caption
                        WrapTableCaptionIfDirectlyAbove(processedLines);
                        isInTable = true;
                    }
                }
                else if (isInTable && !line.StartsWith("|") && !string.IsNullOrWhiteSpace(line))
                {
                    // End of table
                    isInTable = false;
                }

                processedLines.Add(line);
            }

            return string.Join("\n", processedLines);
        }

        /// <summary>
        /// Wraps the line directly above a table with table-caption span to prevent page breaks.
        /// </summary>
        /// <param name="lines">The list of lines to check and modify.</param>
        private static void WrapTableCaptionIfDirectlyAbove(List<string> lines)
        {
            if (lines.Count == 0)
                return;

            int captionLineIndex = lines.Count - 1;
            string captionLine = lines[captionLineIndex];

            // Skip auto-inserted empty line before table and check the actual text line above it
            if (string.IsNullOrWhiteSpace(captionLine) && lines.Count > 1)
            {
                captionLineIndex = lines.Count - 2;
                captionLine = lines[captionLineIndex];
            }

            // Only wrap if the line is not empty and doesn't already have the caption tag
            if (string.IsNullOrWhiteSpace(captionLine))
                return;

            if (captionLine.Contains("<span class=\"table-caption\">"))
                return;

            // Skip headlines because wrapping them with HTML would break markdown rendering
            string trimmedLine = captionLine.TrimStart();
            if (trimmedLine.StartsWith("#"))
                return;

            // Wrap the line with table-caption span for CSS styling
            lines[captionLineIndex] = $"<span class=\"table-caption\">{captionLine}</span>";
        }
    }
}

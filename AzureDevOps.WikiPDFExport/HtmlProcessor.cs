using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace azuredevops_export_wiki
{
    /// <summary>
    /// Processes HTML output by adding tags for better PDF rendering.
    /// This class adds HTML-specific enhancements.
    /// </summary>
    class HtmlProcessor
    {
        /// <summary>
        /// Wraps text directly above HTML tables with span tags for CSS styling to prevent page breaks.
        /// </summary>
        /// <param name="html">The HTML content to process.</param>
        /// <returns>The processed HTML content with table caption spans.</returns>
        public static string InsertTableCaptions(string html)
        {
            string[] lines = html.Split('\n');
            List<string> processedLines = new List<string>();

            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i];

                // Detect HTML table start
                if (line.TrimStart().StartsWith("<table"))
                {
                    // Check if previous line should be a table caption
                    WrapTableCaptionIfDirectlyAbove(processedLines);
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

            // Skip empty lines and find the actual text line above the table
            while (captionLineIndex >= 0 && string.IsNullOrWhiteSpace(captionLine))
            {
                captionLineIndex--;
                if (captionLineIndex >= 0)
                    captionLine = lines[captionLineIndex];
            }

            // No valid caption line found
            if (captionLineIndex < 0 || string.IsNullOrWhiteSpace(captionLine))
                return;

            // Only wrap if the line doesn't already have the caption tag
            if (captionLine.Contains("<span class=\"table-caption\">"))
                return;

            // Skip if line is already a heading tag (h1-h6)
            string trimmedLine = captionLine.TrimStart();
            if (Regex.IsMatch(trimmedLine, @"^<h[1-6]"))
                return;

            // Skip if line is already wrapped in other block elements that shouldn't be nested
            if (Regex.IsMatch(trimmedLine, @"^<(div|table|ul|ol|pre|blockquote)"))
                return;

            // Wrap the line with table-caption span for CSS styling
            lines[captionLineIndex] = $"<span class=\"table-caption\">{captionLine}</span>";
        }
    }
}

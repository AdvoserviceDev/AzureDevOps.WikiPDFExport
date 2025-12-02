using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace azuredevops_export_wiki
{
    /// <summary>
    /// Provides basic German hyphenation by inserting soft hyphens (&shy;) into long words.
    /// This ensures that Chrome's PDF renderer can properly break long German compound words.
    /// </summary>
    internal class GermanHyphenation
    {
        private static NHunspell.Hyphen hyphenator;

        public static string InsertSoftHyphens(string html)
        {
            if (hyphenator == null)
            {
                string dictPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "dictionaries", "hyph_de_DE.dic");
                hyphenator = new NHunspell.Hyphen(dictPath);
            }
            
            // Protect <style> and <script> blocks
            var blocks = new List<string>();
            html = Regex.Replace(html, @"<(style|script)[^>]*>.*?</\1>", m => { blocks.Add(m.Value); return $"___P{blocks.Count - 1}___"; }, RegexOptions.Singleline | RegexOptions.IgnoreCase);
            
            // Split by tags and hyphenate text content only
            var result = new StringBuilder();
            foreach (var part in Regex.Split(html, @"(<[^>]+>)"))
            {
                if (part.StartsWith("<") || part.StartsWith("___P"))
                    result.Append(part);
                else
                    result.Append(Regex.Replace(part, @"\b([a-zA-ZäöüÄÖÜß]{8,})\b", m => hyphenator.Hyphenate(m.Value)?.HyphenatedWord?.Replace("=", "&shy;") ?? m.Value));
            }
            
            // Restore protected blocks
            html = result.ToString();
            for (int i = 0; i < blocks.Count; i++)
                html = html.Replace($"___P{i}___", blocks[i]);
            
            return html;
        }
    }
}

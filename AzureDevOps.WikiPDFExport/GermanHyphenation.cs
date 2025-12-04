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
        private NHunspell.Hyphen hyphenator;

        public string InsertSoftHyphens(string html)
        {
            if (hyphenator == null)
            {
                string exePath = System.Reflection.Assembly.GetEntryAssembly()?.Location;
                string exeDir = exePath != null ? System.IO.Path.GetDirectoryName(exePath) : null;
                // Fallback auf BaseDirectory, falls exeDir null ist
                if (exeDir == null)
                {
                    exeDir = AppDomain.CurrentDomain.BaseDirectory;
                }
                string dictPath = System.IO.Path.Combine(exeDir, "dictionaries", "hyph_de_DE.dic");
                try
                {
                    hyphenator = new NHunspell.Hyphen(dictPath);
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine($"[GermanHyphenation] Could not load dictionary file '{dictPath}': {ex.Message}");
                    return html;
                }
            }
            
            // Protect <style> and <script> blocks because they have text between their tags that must stay as-is
            List<string> blocks = new List<string>();
            html = Regex.Replace(html, @"<(style|script)[^>]*>.*?</\1>", m => { blocks.Add(m.Value); return $"___P{blocks.Count - 1}___"; }, RegexOptions.Singleline | RegexOptions.IgnoreCase);
            
            // Split by tags and hyphenate text content only
            StringBuilder result = new StringBuilder();
            foreach (string part in Regex.Split(html, @"(<[^>]+>)"))
            {
                if (part.StartsWith("<") || part.StartsWith("___P"))
                    result.Append(part);
                else
                    // Hyphenate words with 3 or more letters
                    result.Append(Regex.Replace(part, @"\b([a-zA-ZäöüÄÖÜß]{3,})\b", m => hyphenator.Hyphenate(m.Value)?.HyphenatedWord?.Replace("=", "&shy;") ?? m.Value));
            }
            
            // Restore protected blocks
            html = result.ToString();
            for (int i = 0; i < blocks.Count; i++)
                html = html.Replace($"___P{i}___", blocks[i]);
            
            return html;
        }
    }
}

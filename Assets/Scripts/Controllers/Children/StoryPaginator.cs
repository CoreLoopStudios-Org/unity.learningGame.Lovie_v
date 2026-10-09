using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;

namespace UI
{
    /// <summary>
    /// Turns a story's content payload into fixed-width pages, split on sentence
    /// boundaries where possible. Pure text logic — no scene or UI dependencies.
    /// </summary>
    public static class StoryPaginator
    {
        [Serializable]
        private class StoryContentPayload
        {
            public string content;
        }

        // Story text rides inside the contentPayload JSON string ("content": "...").
        public static string ExtractContent(string contentPayload)
        {
            if (string.IsNullOrEmpty(contentPayload)) return string.Empty;

            try
            {
                return JsonUtility.FromJson<StoryContentPayload>(contentPayload)?.content ?? string.Empty;
            }
            catch
            {
                return contentPayload;
            }
        }

        public static string[] BuildPages(string content, int charsPerPage)
        {
            if (string.IsNullOrWhiteSpace(content)) return Array.Empty<string>();

            List<string> pageList = new List<string>();
            StringBuilder page = new StringBuilder();

            // Sentences keep kids' reading natural; a page is a few sentences up to charsPerPage.
            foreach (string sentence in Regex.Split(content.Trim(), @"(?<=[.!?])\s+"))
            {
                if (string.IsNullOrWhiteSpace(sentence)) continue;

                if (page.Length > 0 && page.Length + sentence.Length + 1 > charsPerPage)
                {
                    pageList.Add(page.ToString().Trim());
                    page.Clear();
                }

                if (page.Length > 0)
                {
                    page.Append(' ');
                }

                page.Append(sentence.Trim());

                // Single sentence longer than a page is split by words so it still fits.
                while (page.Length > charsPerPage)
                {
                    int cut = page.ToString().LastIndexOf(' ', Math.Min(charsPerPage, page.Length - 1));
                    if (cut <= 0) break;

                    pageList.Add(page.ToString(0, cut).Trim());
                    page.Remove(0, cut + 1);
                }
            }

            if (page.Length > 0)
            {
                pageList.Add(page.ToString().Trim());
            }

            return pageList.ToArray();
        }
    }
}

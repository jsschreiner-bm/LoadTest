﻿using System.Xml;
using System.Xml.Linq;

namespace LoadTest.Services;

public static class UrlsRetriever
{
    private static readonly HttpClient _httpClient = new();

    /// <summary>
    /// Get URLs from a local file or local/remote sitemap.
    /// </summary>
    public static async Task<string[]> GetUrlsAsync(string path)
    {
        return File.Exists(path) && !path.EndsWith(".xml")
            ? await GetUrlsFromUrlListFileAsync(path)
            : await GetUrlsFromSitemapUrlAsync(path);
    }

    /// <summary>
    /// Get URLs and save them to a local file.
    /// </summary>
    public static async Task<int> SaveUrlsAsync(string path, string outputPath)
    {
        var urls = await GetUrlsAsync(path);

        Console.WriteLine($"Writing URLs to {outputPath}.");
        await File.WriteAllLinesAsync(outputPath, urls);
        return 0;
    }

    private static async Task<string[]> GetUrlsFromUrlListFileAsync(string filePath)
    {
        Console.WriteLine("Getting URLs from file.");
        return (await File.ReadAllLinesAsync(filePath))
            .Distinct()
            .ToArray();
    }

    private static async Task<string[]> GetUrlsFromSitemapUrlAsync(string sitemapUrl)
    {
        Console.WriteLine("Getting URLs from sitemap.");

        var urls = await GetUrlsFromSitemapRecursiveAsync(sitemapUrl);

        return urls
            .Distinct()
            .ToArray();
    }

    private static async Task<List<string>> GetUrlsFromSitemapRecursiveAsync(string sitemapUrl)
    {
        try
        {
            var xml = await GetSitemapXmlAsync(sitemapUrl);

            var urls = new List<string>();

            var urlSet = xml.DescendantsAndSelf()
                .FirstOrDefault(x => x.Name.LocalName == "urlset");

            if (urlSet is not null)
            {
                var locs = xml.Descendants()
                    .Where(x => x.Name.LocalName == "loc")
                    .Select(x => x.Value);

                urls.AddRange(locs);

                var alts = xml.Descendants()
                    .Where(x => x.Name.LocalName == "link" && x.Attribute("rel")?.Value == "alternate" && !string.IsNullOrWhiteSpace(x.Attribute("href")?.Value))
                    .Select(x => x.Attribute("href")!.Value);

                urls.AddRange(alts);
            }

            var childSitemapUrls = xml
                .DescendantsAndSelf()
                .Where(x => x.Name.LocalName == "sitemap")
                .SelectMany(sitemap => sitemap.Descendants()
                    .Where(loc => loc.Name.LocalName == "loc")
                    .Select(x => x.Value));

            foreach (var childSitemapUrl in childSitemapUrls)
            {
                Console.WriteLine($"Following child sitemap at {childSitemapUrl}.");
                urls.AddRange(await GetUrlsFromSitemapRecursiveAsync(childSitemapUrl));
            }

            return urls;
        }
        catch (HttpRequestException ex)
        {
            Console.WriteLine($"Error retrieving sitemap at {sitemapUrl} (Status Code: {ex.StatusCode}).");

            return new();
        }
        catch (XmlException ex)
        {
            Console.WriteLine($"Error parsing XML of sitemap at {sitemapUrl} (Exception: {ex.Message}).");

            return new();
        }
    }

    /// <summary>
    /// Can get XML from a file or URL.
    /// </summary>
    /// <param name="sitemapUrl"></param>
    private static async Task<XElement> GetSitemapXmlAsync(string sitemapUrl)
    {
        var xmlString = File.Exists(sitemapUrl) ?
            await File.ReadAllTextAsync(sitemapUrl) :
            await _httpClient.GetStringAsync(sitemapUrl);

        return XElement.Parse(xmlString);
    }
}

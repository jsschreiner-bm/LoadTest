namespace LoadTest.Test;
using LoadTest.Helpers;

public class UriHelpersTests
{
    [Theory]
    // Add root slash
    [InlineData("https://example.com", "https://example.com/")]
    // Relative Protocol
    [InlineData("//example.com", "https://example.com/")]
    // Https upgrade
    [InlineData("http://example.com/", "https://example.com/")]
    // Lowercase domain
    [InlineData("https://EXAMPLE.com/", "https://example.com/")]
    // Lowercase path
    [InlineData("https://example.com/TEST/TEST/", "https://example.com/test/test")]
    // Relative URL
    [InlineData("/", "https://example.com/")]
    [InlineData("/TEST/TEST/", "https://example.com/test/test")]
    // Special characters are not encoded
    [InlineData("/TE-&= ST", "https://example.com/te-&= st")]
    // Fragments and queries are removed
    [InlineData("/TEST/?q=test#something", "https://example.com/test")]
    // Empty returns null
    [InlineData("", null)]
    public void GetNormalizedUri(string url, string expectedNormalizedUrl)
    {
        Assert.Equal(expectedNormalizedUrl, url.GetNormalizedUri("EXAMPLE.com", [])?.ToString());
    }
}

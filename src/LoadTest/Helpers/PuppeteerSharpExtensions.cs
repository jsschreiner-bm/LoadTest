
using PuppeteerSharp;

namespace LoadTest.Helpers;

public static class PuppeteerSharpExtensions
{
    public static void EnsureSuccessStatusCode(this IResponse response)
    {
        // Mimic HttpResponseMessage.EnsureSuccessStatusCode()
        if (!(((int)response.Status >= 200) && ((int)response.Status <= 299)))
        {
            throw new HttpRequestException($"Response status code does not indicate success: {(int)response.Status} ({response.Status}).");
        }
    }
}

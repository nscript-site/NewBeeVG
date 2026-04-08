using System.Text;
using System.Text.Json;

namespace NewBeeVG.Internal;

internal class NBMlxTTS
{
    public static byte[]? SynthesizeFoo(string serverUrl, string apiKey, string text, string voice, string lang, string instructs = "")
    {
        var baseUrl = serverUrl;

        instructs = instructs.Trim();

        using var httpClient = new HttpClient();
        httpClient.DefaultRequestHeaders.Add("ApiKey", apiKey);

        var payload = new
        {
            text = text,
            voice = voice,
            lang_code = lang,
            instructs = instructs
        };

        var json = JsonSerializer.Serialize(payload);
        using var content = new StringContent(json, Encoding.UTF8, "application/json");

        using var response = httpClient.PostAsync($"{baseUrl}/v1/tts", content).GetAwaiter().GetResult();

        if (!response.IsSuccessStatusCode)
        {
            var errorText = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
            Console.WriteLine($"Request failed: {(int)response.StatusCode} {response.StatusCode}");
            throw new Exception(errorText);
        }

        var audioBytes = response.Content.ReadAsByteArrayAsync().GetAwaiter().GetResult();
        return audioBytes;
    }
}

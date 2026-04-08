using System;
using System.Collections.Generic;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace NewBeeVG.Internal;

internal class QWenTTS
{
    const string ApiKey_Aliyun_BaiLian = "@Aliyun_BaiLian_ApiKey";

    private const string Endpoint = "https://dashscope.aliyuncs.com/api/v1/services/aigc/multimodal-generation/generation";
    private readonly HttpClient _http;
    private readonly string _apiKey;

    public QWenTTS(HttpClient? httpClient = null)
    {
        _apiKey = Methods.GetApiKey(ApiKey_Aliyun_BaiLian);

        _http = httpClient ?? new HttpClient();
        if (_http.DefaultRequestHeaders.Authorization == null)
            _http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);

        _http.DefaultRequestHeaders.Accept.Clear();
        _http.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
    }

    /// <summary>
    /// Call the Dashscope TTS generation endpoint and deserialize the response into QWenTTSApiResponse.
    /// </summary>
    public async Task<QWenTTSApiResponse> GenerateAsync(string text, string voice = "Cherry", string language = "Chinese", string instructions = "", string model = "qwen3-tts-instruct-flash", CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(text)) throw new ArgumentException("text must not be empty.", nameof(text));

        instructions = instructions.Trim();

        var payload = new
        {
            model = model,
            input = new
            {
                text = text,
                voice = voice,
                language_type = language,
                optimize_instructions = !String.IsNullOrEmpty(instructions),
                instructions = instructions
            }
        };

        var json = JsonSerializer.Serialize(payload);
        using var content = new StringContent(json, Encoding.UTF8, "application/json");

        using var resp = await _http.PostAsync(Endpoint, content, ct).ConfigureAwait(false);
        var body = await resp.Content.ReadAsStringAsync(ct).ConfigureAwait(false);

        var options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
        };

        QWenTTSApiResponse? apiResp;
        try
        {
            apiResp = JsonSerializer.Deserialize<QWenTTSApiResponse>(body, options);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Failed to deserialize QWenTTS response JSON.", ex);
        }

        if (apiResp == null)
            throw new InvalidOperationException("Response JSON was empty or could not be parsed.");

        // Attach HTTP-level info for callers
        apiResp.HttpStatus = resp.StatusCode;
        apiResp.RawBody = body;

        if(apiResp.HttpStatus != System.Net.HttpStatusCode.OK)
        {
            var msg = apiResp.Message;
            if (msg != null)
                throw new Exception(msg);
        }

        return apiResp;
    }

    /// <summary>
    /// Download audio from the response's audio.url into the specified file path.
    /// Returns the path when completed.
    /// </summary>
    public async Task<string> DownloadAudioAsync(QWenTTSApiResponse response, string outputFilePath, CancellationToken ct = default)
    {
        if (response?.Output?.Audio == null) throw new ArgumentException("Response does not contain audio information.", nameof(response));
        var url = response.Output.Audio.Url;
        if (string.IsNullOrWhiteSpace(url)) throw new InvalidOperationException("Audio URL is empty.");

        using var audioResp = await _http.GetAsync(url, ct).ConfigureAwait(false);
        audioResp.EnsureSuccessStatusCode();

        await using var fs = File.Create(outputFilePath);
        await audioResp.Content.CopyToAsync(fs, ct).ConfigureAwait(false);
        await fs.FlushAsync(ct).ConfigureAwait(false);

        return outputFilePath;
    }

    // --- Response DTOs (prefixed with QWenTTS) ---
    public class QWenTTSApiResponse
    {
        [JsonPropertyName("status_code")]
        public int StatusCode { get; set; }

        [JsonPropertyName("request_id")]
        public string? RequestId { get; set; }

        [JsonPropertyName("code")]
        public string? Code { get; set; }

        [JsonPropertyName("message")]
        public string? Message { get; set; }

        [JsonPropertyName("output")]
        public QWenTTSOutput? Output { get; set; }

        [JsonPropertyName("usage")]
        public QWenTTSUsage? Usage { get; set; }

        // convenience fields (not from JSON)
        [JsonIgnore]
        public System.Net.HttpStatusCode HttpStatus { get; set; }

        [JsonIgnore]
        public string? RawBody { get; set; }
    }

    public class QWenTTSOutput
    {
        [JsonPropertyName("text")]
        public string? Text { get; set; }

        [JsonPropertyName("finish_reason")]
        public string? FinishReason { get; set; }

        [JsonPropertyName("choices")]
        public object? Choices { get; set; }

        [JsonPropertyName("audio")]
        public QWenTTSAudio? Audio { get; set; }
    }

    public class QWenTTSAudio
    {
        [JsonPropertyName("data")]
        public string? Data { get; set; }

        [JsonPropertyName("url")]
        public string? Url { get; set; }

        [JsonPropertyName("id")]
        public string? Id { get; set; }

        [JsonPropertyName("expires_at")]
        public long ExpiresAt { get; set; }
    }

    public class QWenTTSUsage
    {
        [JsonPropertyName("input_tokens")]
        public int InputTokens { get; set; }

        [JsonPropertyName("output_tokens")]
        public int OutputTokens { get; set; }

        [JsonPropertyName("characters")]
        public int Characters { get; set; }

        [JsonPropertyName("input_tokens_details")]
        public object? InputTokensDetails { get; set; }

        [JsonPropertyName("output_tokens_details")]
        public object? OutputTokensDetails { get; set; }

        [JsonPropertyName("total_tokens")]
        public int TotalTokens { get; set; }
    }
}

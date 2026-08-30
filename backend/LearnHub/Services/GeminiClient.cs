using System.Net.Http.Json;
using LearnHub.Models.DTOs.Chatbot;

namespace LearnHub.Services
{
    public interface IGeminiClient
    {
        Task<string> GenerateReplyAsync(string systemInstruction, IReadOnlyList<ChatMessageDto> history, string message);
    }

    public class GeminiClient : IGeminiClient
    {
        private const string Model = "gemini-flash-latest";
        private readonly HttpClient _httpClient;

        public GeminiClient(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<string> GenerateReplyAsync(string systemInstruction, IReadOnlyList<ChatMessageDto> history, string message)
        {
            var contents = history
                .Select(h => new { role = h.Role, parts = new[] { new { text = h.Content } } })
                .ToList();
            contents.Add(new { role = "user", parts = new[] { new { text = message } } });

            var body = new
            {
                systemInstruction = new { parts = new[] { new { text = systemInstruction } } },
                contents,
            };

            var response = await _httpClient.PostAsJsonAsync($"v1beta/models/{Model}:generateContent", body);
            if (!response.IsSuccessStatusCode)
                throw new ApiException("The AI tutor is temporarily unavailable.", 502);

            var result = await response.Content.ReadFromJsonAsync<GeminiResponse>();
            var reply = result?.Candidates?.FirstOrDefault()?.Content?.Parts?.FirstOrDefault()?.Text;
            if (string.IsNullOrWhiteSpace(reply))
                throw new ApiException("The AI tutor is temporarily unavailable.", 502);

            return reply;
        }

        private class GeminiResponse
        {
            public List<Candidate>? Candidates { get; set; }
        }

        private class Candidate
        {
            public ContentPart? Content { get; set; }
        }

        private class ContentPart
        {
            public List<TextPart>? Parts { get; set; }
        }

        private class TextPart
        {
            public string? Text { get; set; }
        }
    }
}

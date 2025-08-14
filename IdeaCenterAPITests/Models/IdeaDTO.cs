namespace IdeaCenterAPITests.Models
{
    internal class IdeaDTO
    {
        [System.Text.Json.Serialization.JsonPropertyName("title")]
        public string Title { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("description")]
        public string Description { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("url")]
        public string? Url { get; set; }
    }
}

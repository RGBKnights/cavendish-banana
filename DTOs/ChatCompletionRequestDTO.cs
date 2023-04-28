using Newtonsoft.Json;
using OpenAI.GPT3.ObjectModels.RequestModels;
using System.Collections.Generic;

namespace CavendishBanana.DTO
{
    public class ChatCompletionRequestDTO
    {
        [JsonProperty("user")]
        public string? User { get; set; }

        [JsonProperty("history")]
        public List<ChatMessageDTO> Messages { get; set; }
    }
}

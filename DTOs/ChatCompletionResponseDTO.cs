using Newtonsoft.Json;
using OpenAI.GPT3.ObjectModels.RequestModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace CavendishBanana.DTO
{
    public class ChatCompletionResponseDTO
    {
        [JsonProperty("message")]
        public ChatMessageDTO Message { get; set; }
    }
}

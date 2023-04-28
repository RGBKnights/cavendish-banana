using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace CavendishBanana.DTO
{
    public class TranscriptionResponseDTO
    {
        [JsonProperty("transcription")]
        public string Transcription { get; set; }
    }
}

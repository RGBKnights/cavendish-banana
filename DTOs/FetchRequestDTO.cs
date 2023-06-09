﻿using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace CavendishBanana.DTO
{
    public class FetchRequestDTO
    {
        [JsonProperty("user")]
        public string User { get; set; }

        [JsonProperty("url")]
        public string Url { get; set; }
    }
}

using Newtonsoft.Json;

namespace GreeControl
{
    internal class RequestPackInfo
    {
        [JsonProperty("t")]
        public string Type { get; set; }

        [JsonProperty("uid", NullValueHandling = NullValueHandling.Ignore)]
        public int? UID { get; set; }

        [JsonProperty("sub")]
        public string MAC { get; set; }
    }
}

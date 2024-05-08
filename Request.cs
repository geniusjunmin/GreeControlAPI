using Newtonsoft.Json;

namespace GreeControl
{
    internal class PackInfo
    {
        [JsonProperty("t")]
        public string Type { get; set; }

        [JsonProperty("cid")]
        public string ClientId { get; set; }
    }
    internal class DevRequest : PackInfo
    {
        [JsonProperty("i")]
        public int I { get; set; }

        [JsonProperty("tcid")]
        public string TargetClientId { get; set; }

        [JsonProperty("uid")]
        public int UID { get; set; }

        [JsonProperty("pack")]
        public string Pack { get; set; }

        public static DevRequest Create(string targetClientId, string pack, int i = 0)
        {
            return new DevRequest()
            {
                ClientId = "app",
                Type = "pack",
                I = i,
                TargetClientId = targetClientId,
                Pack = pack,
                UID = 0
            };
        }
    }
}

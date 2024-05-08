using Newtonsoft.Json;

namespace GreeControlAPI
{
    internal class DeviceStatusRequestPack
    {

        [JsonProperty("t")]
        public string Type { get; set; }

        [JsonProperty("uid", NullValueHandling = NullValueHandling.Ignore)]
        public int? UID { get; set; }

        [JsonProperty("mac")]
        public string MAC { get; set; }


        [JsonProperty("cols")]
        public List<string> Columns { get; set; }

        public static DeviceStatusRequestPack Create(string clientId, List<string> columns)
        {
            return new DeviceStatusRequestPack()
            {
                Type = "status",
                MAC = clientId,
                Columns = columns,
                UID = null
            };
        }
    }
}

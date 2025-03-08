using System.Text.Json.Serialization;

namespace TorCSClient.Relays
{
    /**
     * Most of the fields are completely not necessary to load into memory
     * And yet we will [JsonIgnore] only effective_family and alleged_family due to how much memory they take
     **/
    [Serializable]
    internal struct RelayList
    {
        [JsonPropertyName("version")]
        public string Version { get; set; }

        [JsonPropertyName("build_revision")]
        public string BuildRevision { get; set; }

        [JsonPropertyName("relays_published")]
        public string RelaysPublishDate { get; set; }

        [JsonPropertyName("bridges_published")]
        public string BridgesPublishDate { get; set; }

        [JsonPropertyName("relays")]
        public Relay[] Relays { get; set; }

        [JsonIgnore]
        [JsonPropertyName("bridges")]
        public Bridge[] Bridges { get; set; }
    }
}

using System.Text.Json.Serialization;

namespace TorCSClient.Relays
{
    [Serializable]
    internal struct Bridge
    {
        [JsonPropertyName("nickname")]
        public string Nickname { get; set; }

        [JsonPropertyName("hashed_fingerprint")]
        public string HashedFingerprint { get; set; }

        [JsonPropertyName("or_addresses")]
        public string[] Addresses { get; set; }

        [JsonPropertyName("last_seen")]
        public string LastSeen { get; set; }

        [JsonPropertyName("first_seen")]
        public string FirstSeen { get; set; }

        [JsonPropertyName("last_restarted")]
        public string LastRestarted { get; set; }

        [JsonPropertyName("running")]
        public bool Running { get; set; }

        [JsonPropertyName("flags")]
        public string[] Flags { get; set; }

        [JsonPropertyName("advertised_bandwidth")]
        public ulong AdvertisedBandwidth { get; set; }

        [JsonPropertyName("contact")]
        public string Contact { get; set; }

        [JsonPropertyName("platform")]
        public string Platform { get; set; }

        [JsonPropertyName("version")]
        public string Version { get; set; }

        [JsonPropertyName("version_status")]
        public string VersionStatus { get; set; }

        [JsonPropertyName("recommended_version")]
        public bool IsRecommendedVersion { get; set; }

        [JsonPropertyName("transports")]
        public string[] Transports { get; set; }

        [JsonPropertyName("bridgedb_distributor")]
        public string BridgedbDistributor { get; set; }

        [JsonPropertyName("blocklist")]
        public string[] Blocklist { get; set; }
    }
}

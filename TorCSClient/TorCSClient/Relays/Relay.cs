using System.Text.Json.Serialization;

namespace TorCSClient.Relays
{
    [Serializable]
    internal struct Relay
    {
        [JsonIgnore]
        [JsonPropertyName("nickname")]
        public string Nickname { get; set; }

        [JsonPropertyName("fingerprint")]
        public string Fingerprint { get; set; }

        [JsonPropertyName("or_addresses")]
        public string[] Addresses { get; set; }

        [JsonIgnore]
        [JsonPropertyName("last_seen")]
        public string LastSeen { get; set; }

        [JsonPropertyName("last_changed_address_or_port")]
        public string LastChangedAddressOrPort { get; set; }

        [JsonIgnore]
        [JsonPropertyName("first_seen")]
        public string FirstSeen { get; set; }

        [JsonIgnore]
        [JsonPropertyName("last_restarted")]
        public string LastRestarted { get; set; }

        [JsonPropertyName("running")]
        public bool Running { get; set; }

        [JsonPropertyName("flags")]
        public string[] Flags { get; set; }

        [JsonPropertyName("country")]
        public string Country { get; set; }

        [JsonPropertyName("country_name")]
        public string CountryName { get; set; }

        
        //[JsonIgnore]
        [JsonPropertyName("as")]
        public string AS { get; set; }

        //[JsonIgnore]
        [JsonPropertyName("as_name")]
        public string ASName { get; set; }

        [JsonIgnore]
        [JsonPropertyName("consensus_weight")]
        public ulong ConsesusWeight { get; set; }

        [JsonIgnore]
        [JsonPropertyName("bandwidth_rate")]
        public ulong BandwidthRate { get; set; }

        [JsonIgnore]
        [JsonPropertyName("bandwidth_burst")]
        public ulong BandwidthBurst { get; set; }

        [JsonIgnore]
        [JsonPropertyName("observed_bandwidth")]
        public ulong ObservedBandwidth { get; set; }

        [JsonIgnore]
        [JsonPropertyName("advertised_bandwidth")]
        public ulong AdvertisedBandwidth { get; set; }

        [JsonIgnore]
        [JsonPropertyName("exit_policy")]
        public string[] ExitPolicy { get; set; }

        [JsonIgnore]
        [JsonPropertyName("exit_policy_summary")]
        public RelayExitPolicySummary ExitPolicySummary { get; set; }

        [JsonIgnore]
        [JsonPropertyName("contact")]
        public string Contact { get; set; }

        [JsonIgnore]
        [JsonPropertyName("platform")]
        public string Platform { get; set; }

        [JsonIgnore]
        [JsonPropertyName("version")]
        public string Version { get; set; }

        [JsonIgnore]
        [JsonPropertyName("version_status")]
        public string VersionStatus { get; set; }

        [JsonIgnore]
        [JsonPropertyName("effective_family")]
        public string[] EffectiveFamily { get; set; }

        [JsonIgnore]
        [JsonPropertyName("alleged_family")]
        public string[] AllegedFamily { get; set; }

        [JsonIgnore]
        [JsonPropertyName("consensus_weight_fraction")]
        public double ConsesusWeightFraction { get; set; }

        [JsonIgnore]
        [JsonPropertyName("guard_probability")]
        public double GuardProbability { get; set; }

        [JsonIgnore]
        [JsonPropertyName("middle_probability")]
        public double MiddleProbability { get; set; }

        [JsonIgnore]
        [JsonPropertyName("exit_probability")]
        public double ExitProbability { get; set; }

        [JsonIgnore]
        [JsonPropertyName("recommended_version")]
        public bool IsRecommendedVersion { get; set; }

        [JsonIgnore]
        [JsonPropertyName("measured")]
        public bool Measured { get; set; }

        // Converts to bridge string
        public override string ToString()
        {
            return Addresses.First().ToString() + " " + Fingerprint;
        }
    }
}

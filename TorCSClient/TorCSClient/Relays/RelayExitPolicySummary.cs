using System.Text.Json.Serialization;

namespace TorCSClient.Relays
{
    [Serializable]
    internal struct RelayExitPolicySummary
    {

        [JsonPropertyName("accept")]
        public string[] Accept { get; set; }


        [JsonPropertyName("reject")]
        public string[] Reject { get; set; }

    }
}

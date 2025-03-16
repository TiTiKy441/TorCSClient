using System.Text.Json.Serialization;

namespace TorCSClient.Network.ProxiFyre
{
    [Serializable]
    internal struct ProxiFyreConfig
    {

        [JsonPropertyName("logLevel")]
        public string LogLevel { get; set; }

        [JsonPropertyName("proxies")]
        public ProxiFyreProxyInformation[] Proxies { get; set; }

    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace TorCSClient.Network.ProxiFyre
{
    [Serializable]
    internal struct ProxiFyreProxyInformation
    {

        [JsonPropertyName("appNames")]
        public string[] AppNames { get; set; }

        [JsonPropertyName("socks5ProxyEndpoint")]
        public string ProxyEndpoint { get; set; }

        [JsonIgnore]
        [JsonPropertyName("username")]
        public string Username { get; set; }

        [JsonIgnore]
        [JsonPropertyName("password")]
        public string Password { get; set; }

        [JsonPropertyName("supportedProtocols")]
        public string[] Protocols { get; set; }

    }
}

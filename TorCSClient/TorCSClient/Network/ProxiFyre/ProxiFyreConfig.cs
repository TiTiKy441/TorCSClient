using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

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

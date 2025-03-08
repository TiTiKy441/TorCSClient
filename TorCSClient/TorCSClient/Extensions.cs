using System.Net;
using TorCSClient.Relays;

namespace TorCSClient
{
    internal static class Extensions
    {
        public static void Shuffle<T>(this Random rng, List<T> array)
        {
            int n = array.Count;
            while (n > 1)
            {
                int k = rng.Next(n--);
                (array[k], array[n]) = (array[n], array[k]);
            }
        }

        public static IEnumerable<IPAddress> GetAddresses(this Relay relay)
        {
            return relay.Addresses.Select(x => IPAddress.Parse(x));
        }

        public static Relay? FirstOrNull(this IEnumerable<Relay> relays)
        {
            if (!relays.Any()) return null;
            return relays.First();
        }

        public static IEnumerable<Relay> GetRelaysWithFlags(this IEnumerable<Relay> relays, string[] flags)
        {
            return relays.Where((x, i) => flags.All(y => x.Flags.Contains(y))).ToArray();
        }

        public static IEnumerable<Relay> GetRelaysWithFlag(this IEnumerable<Relay> relays, string flag)
        {
            return relays.Where((x, i) => x.Flags.Contains(flag));
        }

        public static IEnumerable<Relay> GetRelaysFromCountry(this IEnumerable<Relay> relays, string country)
        {
            return relays.Where((x, i) => x.Country.Contains(country));
        }

        public static Relay? FindRelayByIp(this IEnumerable<Relay> relays, string ip)
        {
            return relays.Where((x, i) => x.Addresses.Contains(ip)).FirstOrNull();
        }

        public static Relay? FindRelayByFingerprint(this IEnumerable<Relay> relays, string fingerprint)
        {
            return relays.Where((x, i) => x.Fingerprint == fingerprint).FirstOrNull();
        }

        public static IEnumerable<Relay> GetRelaysWithoutFlags(this IEnumerable<Relay> relays, string[] flags)
        {
            return relays.Where((x, i) => !flags.Any(y => x.Flags.Contains(y)));
        }

        public static IEnumerable<Relay> GetRelaysWithoutFlag(this IEnumerable<Relay> relays, string flag)
        {
            return relays.Where((x, i) => !x.Flags.Contains(flag));
        }
    }
}

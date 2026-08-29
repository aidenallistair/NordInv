using System;
using TaleWorlds.MountAndBlade;

namespace NordInvasion.Utils
{
    /// <summary>
    /// Безопасное получение идентификатора сетевого игрока.
    /// В сингле/коопе MissionPeer может быть null - нельзя обращаться напрямую
    /// (MissionPeer.Peer.Communicator ломается на локальных агентах).
    /// </summary>
    public static class NIPeers
    {
        public static string GetPeerId(Agent agent)
        {
            if (agent == null) return "unknown";
            try
            {
                if (agent.MissionPeer != null && agent.MissionPeer.Peer != null)
                {
                    var name = agent.MissionPeer.Peer.Name;
                    if (!string.IsNullOrEmpty(name)) return name;
                }
            }
            catch
            {
                // локальный агент / агент без пира - идем на fallback
            }
            return agent.Name != null ? agent.Name.ToString() : "unknown";
        }

        /// <summary>
        /// SteamID (64) сетевой сессии, если доступен. Reflection-safe: имя свойства
        /// менялось между версиями Bannerlord (SteamId64 / Id). Пустая строка, если нет
        /// (сингл, локальный хост, старая версия) - бэкенд сам делает fallback на name_md5.
        /// </summary>
        public static string GetSteamId(Agent agent)
        {
            if (agent == null) return "";
            try
            {
                var peer = agent.MissionPeer != null ? agent.MissionPeer.Peer : null;
                if (peer == null) return "";
                var type = peer.GetType();
                foreach (var propName in new[] { "SteamId64", "Id", "SessionId" })
                {
                    var prop = type.GetProperty(propName);
                    if (prop == null || !prop.CanRead) continue;
                    object val = prop.GetValue(peer, null);
                    if (val == null) continue;
                    string s = val.ToString();
                    if (string.IsNullOrEmpty(s) || s == "0") continue;
                    return s;
                }
            }
            catch
            {
                // нет такого свойства в этой версии - fallback на имя
            }
            return "";
        }

        /// <summary>
        /// Стабильный id игрока для бэкенда. Формула совпадает с PHP (src/backend-php/lib.php):
        /// steam_id -> "steam_&lt;id&gt;", иначе "name_&lt;md5(peer_name)&gt;".
        /// </summary>
        public static string MakePlayerId(string steamId, string name)
        {
            if (!string.IsNullOrEmpty(steamId)) return "steam_" + steamId;
            string n = (name ?? "unknown").Trim();
            if (n == "") return "";
            using (var md5 = System.Security.Cryptography.MD5.Create())
            {
                byte[] hash = md5.ComputeHash(System.Text.Encoding.UTF8.GetBytes(n));
                var sb = new System.Text.StringBuilder();
                foreach (byte b in hash) sb.Append(b.ToString("x2"));
                return "name_" + sb.ToString();
            }
        }
    }
}

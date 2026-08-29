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
    }
}

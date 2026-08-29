using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Network.Messages;

namespace NordInvasion.Multiplayer
{
    /// <summary>
    /// Сетевые сообщения для синхронизации стройки и экономики в MP.
    /// В Bannerlord MP кастомные сообщения наследуются от GameNetworkMessage
    /// и регистрируются через ModuleNetworkData.
    /// Для MVP используем простой подход: клиент -> сервер запрос, сервер -> все клиенты broadcast.
    /// Используем только Integer компрессию чтобы не зависеть от конкретных полей CompressionBasic.
    /// </summary>
    internal static class NICompression
    {
        public static readonly CompressionInfo.Integer BuildType = new CompressionInfo.Integer(0, 32, true);
        public static readonly CompressionInfo.Integer Gold = new CompressionInfo.Integer(0, 1000000, true);
        public static readonly CompressionInfo.Integer Wave = new CompressionInfo.Integer(0, 1000, true);
        public static readonly CompressionInfo.Integer Pos = new CompressionInfo.Integer(-100000, 100000, true);
        public static readonly CompressionInfo.Integer Yaw = new CompressionInfo.Integer(-36000, 36000, true); // yaw*100
    }

    public sealed class RequestBuildMessage : GameNetworkMessage
    {
        public Managers.FortressBuildManager.BuildType BuildType { get; set; }
        public Vec3 Position { get; set; }
        public float Yaw { get; set; }

        public RequestBuildMessage() { }

        public RequestBuildMessage(Managers.FortressBuildManager.BuildType type, Vec3 pos, float yaw)
        {
            BuildType = type;
            Position = pos;
            Yaw = yaw;
        }

        protected override void OnWrite()
        {
            WriteIntToPacket((int)BuildType, NICompression.BuildType);
            WriteIntToPacket((int)(Position.x * 10f), NICompression.Pos);
            WriteIntToPacket((int)(Position.y * 10f), NICompression.Pos);
            WriteIntToPacket((int)(Position.z * 10f), NICompression.Pos);
            WriteIntToPacket((int)(Yaw * 100f), NICompression.Yaw);
        }

        protected override bool OnRead()
        {
            bool result = true;
            int typeInt = ReadIntFromPacket(NICompression.BuildType, ref result);
            BuildType = (Managers.FortressBuildManager.BuildType)typeInt;
            int x = ReadIntFromPacket(NICompression.Pos, ref result);
            int y = ReadIntFromPacket(NICompression.Pos, ref result);
            int z = ReadIntFromPacket(NICompression.Pos, ref result);
            Position = new Vec3(x / 10f, y / 10f, z / 10f);
            int yawInt = ReadIntFromPacket(NICompression.Yaw, ref result);
            Yaw = yawInt / 100f;
            return result;
        }

        protected override MultiplayerMessageFilter OnGetLogFilter() => MultiplayerMessageFilter.Mission;
        protected override string OnGetLogFormat() => $"RequestBuild {BuildType} at {Position}";
    }

    public sealed class BuildPlacedMessage : GameNetworkMessage
    {
        public string PropId { get; set; } = "";
        public Vec3 Position { get; set; }
        public float Yaw { get; set; }
        public string FallbackId { get; set; } = "";

        public BuildPlacedMessage() { }

        public BuildPlacedMessage(string propId, string fallbackId, Vec3 pos, float yaw)
        {
            PropId = propId;
            FallbackId = fallbackId;
            Position = pos;
            Yaw = yaw;
        }

        protected override void OnWrite()
        {
            WriteStringToPacket(PropId ?? "");
            WriteStringToPacket(FallbackId ?? "");
            WriteIntToPacket((int)(Position.x * 10f), NICompression.Pos);
            WriteIntToPacket((int)(Position.y * 10f), NICompression.Pos);
            WriteIntToPacket((int)(Position.z * 10f), NICompression.Pos);
            WriteIntToPacket((int)(Yaw * 100f), NICompression.Yaw);
        }

        protected override bool OnRead()
        {
            bool result = true;
            PropId = ReadStringFromPacket(ref result);
            FallbackId = ReadStringFromPacket(ref result);
            int x = ReadIntFromPacket(NICompression.Pos, ref result);
            int y = ReadIntFromPacket(NICompression.Pos, ref result);
            int z = ReadIntFromPacket(NICompression.Pos, ref result);
            Position = new Vec3(x / 10f, y / 10f, z / 10f);
            int yawInt = ReadIntFromPacket(NICompression.Yaw, ref result);
            Yaw = yawInt / 100f;
            return result;
        }

        protected override MultiplayerMessageFilter OnGetLogFilter() => MultiplayerMessageFilter.Mission;
        protected override string OnGetLogFormat() => $"BuildPlaced {PropId} at {Position}";
    }

    public sealed class GoldSyncMessage : GameNetworkMessage
    {
        public int Gold { get; set; }
        public int Wood { get; set; }
        public int Metal { get; set; }

        public GoldSyncMessage() { }
        public GoldSyncMessage(int gold, int wood, int metal)
        {
            Gold = gold;
            Wood = wood;
            Metal = metal;
        }

        protected override void OnWrite()
        {
            WriteIntToPacket(Gold, NICompression.Gold);
            WriteIntToPacket(Wood, NICompression.Gold);
            WriteIntToPacket(Metal, NICompression.Gold);
        }

        protected override bool OnRead()
        {
            bool result = true;
            Gold = ReadIntFromPacket(NICompression.Gold, ref result);
            Wood = ReadIntFromPacket(NICompression.Gold, ref result);
            Metal = ReadIntFromPacket(NICompression.Gold, ref result);
            return result;
        }

        protected override MultiplayerMessageFilter OnGetLogFilter() => MultiplayerMessageFilter.Mission;
        protected override string OnGetLogFormat() => $"GoldSync {Gold}g {Wood}w {Metal}m";
    }

    public sealed class WaveStateMessage : GameNetworkMessage
    {
        public int WaveNumber { get; set; }
        public int BotsAlive { get; set; }
        public int BotsTotal { get; set; }

        public WaveStateMessage() { }
        public WaveStateMessage(int wave, int alive, int total)
        {
            WaveNumber = wave;
            BotsAlive = alive;
            BotsTotal = total;
        }

        protected override void OnWrite()
        {
            WriteIntToPacket(WaveNumber, NICompression.Wave);
            WriteIntToPacket(BotsAlive, NICompression.Wave);
            WriteIntToPacket(BotsTotal, NICompression.Wave);
        }

        protected override bool OnRead()
        {
            bool result = true;
            WaveNumber = ReadIntFromPacket(NICompression.Wave, ref result);
            BotsAlive = ReadIntFromPacket(NICompression.Wave, ref result);
            BotsTotal = ReadIntFromPacket(NICompression.Wave, ref result);
            return result;
        }

        protected override MultiplayerMessageFilter OnGetLogFilter() => MultiplayerMessageFilter.Mission;
        protected override string OnGetLogFormat() => $"Wave {WaveNumber} {BotsAlive}/{BotsTotal}";
    }
}

using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Network.Messages;

namespace PersistentEmpiresLib.NetworkMessages.Server
{
    public enum FactionKeyType
    {
        DoorKey = 0,
        ChestKey = 1,
        Other = 2
    }

    [DefineGameNetworkMessageTypeForMod(GameNetworkMessageSendType.FromServer)]
    public sealed class SyncFactionKey : GameNetworkMessage
    {
        public int FactionIndex { get; private set; }
        public string PlayerId { get; private set; }
        public FactionKeyType KeyType { get; private set; }

        public SyncFactionKey() { }

        public SyncFactionKey(int factionIndex, string playerId, FactionKeyType keyType)
        {
            this.FactionIndex = factionIndex;
            this.PlayerId = playerId;
            this.KeyType = keyType;
        }

        protected override MultiplayerMessageFilter OnGetLogFilter()
        {
            return MultiplayerMessageFilter.None;
        }

        protected override string OnGetLogFormat()
        {
            return $"SyncFactionKey | Faction: {FactionIndex}, Player: {PlayerId}, KeyType: {KeyType}";
        }

        protected override bool OnRead()
        {
            bool result = true;
            this.FactionIndex = GameNetworkMessage.ReadIntFromPacket(new CompressionInfo.Integer(0, 200, true), ref result);
            this.PlayerId = GameNetworkMessage.ReadStringFromPacket(ref result);
            this.KeyType = (FactionKeyType)GameNetworkMessage.ReadIntFromPacket(new CompressionInfo.Integer(0, 10, true), ref result);

            // Sicherheit: Falls PlayerId leer oder null ist, setzen wir eine Standard-ID
            if (string.IsNullOrEmpty(this.PlayerId))
            {
                this.PlayerId = "Unknown_Player";
            }

            return result;
        }

        protected override void OnWrite()
        {
            GameNetworkMessage.WriteIntToPacket(this.FactionIndex, new CompressionInfo.Integer(0, 200, true));
            GameNetworkMessage.WriteStringToPacket(this.PlayerId);
            GameNetworkMessage.WriteIntToPacket((int)this.KeyType, new CompressionInfo.Integer(0, 10, true));
        }
    }
}


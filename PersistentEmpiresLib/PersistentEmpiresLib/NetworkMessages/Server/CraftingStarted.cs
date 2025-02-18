using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Network.Messages;

namespace PersistentEmpiresLib.NetworkMessages.Server
{
    [DefineGameNetworkMessageTypeForMod(GameNetworkMessageSendType.FromServer)]
    public sealed class CraftingStarted : GameNetworkMessage
    {
        public int PlayerID { get; private set; }
        public int CraftingStationID { get; private set; }
        public int CraftIndex { get; private set; }

        public CraftingStarted() { }

        public CraftingStarted(int playerId, int craftingStationId, int craftIndex)
        {
            this.PlayerID = playerId;
            this.CraftingStationID = craftingStationId;
            this.CraftIndex = craftIndex;
        }

        protected override MultiplayerMessageFilter OnGetLogFilter()
        {
            return MultiplayerMessageFilter.MissionObjects;
        }

        protected override string OnGetLogFormat()
        {
            return $"🛠️ Crafting gestartet: Station {CraftingStationID}, Spieler {PlayerID}, Index {CraftIndex}";
        }

        protected override bool OnRead()
        {
            bool result = true;
            this.PlayerID = GameNetworkMessage.ReadIntFromPacket(new CompressionInfo.Integer(0, 10000, true), ref result);
            this.CraftingStationID = GameNetworkMessage.ReadIntFromPacket(new CompressionInfo.Integer(0, 10000, true), ref result);
            this.CraftIndex = GameNetworkMessage.ReadIntFromPacket(new CompressionInfo.Integer(0, 1024, true), ref result);
            return result;
        }

        protected override void OnWrite()
        {
            GameNetworkMessage.WriteIntToPacket(this.PlayerID, new CompressionInfo.Integer(0, 10000, true));
            GameNetworkMessage.WriteIntToPacket(this.CraftingStationID, new CompressionInfo.Integer(0, 10000, true));
            GameNetworkMessage.WriteIntToPacket(this.CraftIndex, new CompressionInfo.Integer(0, 1024, true));
        }
    }
}

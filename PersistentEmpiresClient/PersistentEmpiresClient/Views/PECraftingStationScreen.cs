using PersistentEmpiresLib.SceneScripts;
using PersistentEmpiresLib.Helpers;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Network.Messages;
using System.Linq;

namespace PersistentEmpiresLib.NetworkMessages.Server
{
    [DefineGameNetworkMessageTypeForMod(GameNetworkMessageSendType.FromServer)]
    public sealed class CraftingCompleted : GameNetworkMessage
    {
        public int PlayerID { get; private set; }
        public string CraftedItem { get; private set; }
        public int CraftingStationID { get; private set; }

        public CraftingCompleted() { }

        public CraftingCompleted(int playerId, string craftedItem, int craftingStationId)
        {
            this.PlayerID = playerId;
            this.CraftedItem = craftedItem ?? throw new System.ArgumentNullException(nameof(craftedItem), "❌ Fehler: Kein Item angegeben!");
            this.CraftingStationID = craftingStationId;
        }

        protected override MultiplayerMessageFilter OnGetLogFilter()
        {
            return MultiplayerMessageFilter.MissionObjects;
        }

        protected override string OnGetLogFormat()
        {
            return $"✅ Crafting abgeschlossen: {CraftedItem} für Spieler {PlayerID} in Station {CraftingStationID}";
        }

        protected override bool OnRead()
        {
            bool result = true;
            this.PlayerID = GameNetworkMessage.ReadIntFromPacket(new CompressionInfo.Integer(0, 10000, true), ref result);
            this.CraftedItem = GameNetworkMessage.ReadStringFromPacket(ref result);
            this.CraftingStationID = GameNetworkMessage.ReadIntFromPacket(new CompressionInfo.Integer(0, 10000, true), ref result);
            return result;
        }

        protected override void OnWrite()
        {
            GameNetworkMessage.WriteIntToPacket(this.PlayerID, new CompressionInfo.Integer(0, 10000, true));
            GameNetworkMessage.WriteStringToPacket(this.CraftedItem);
            GameNetworkMessage.WriteIntToPacket(this.CraftingStationID, new CompressionInfo.Integer(0, 10000, true));
        }
    }
}

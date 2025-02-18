using PersistentEmpiresLib.Data;
using PersistentEmpiresLib.Helpers;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Network.Messages;

namespace PersistentEmpiresLib.NetworkMessages.Server
{
    [DefineGameNetworkMessageTypeForMod(GameNetworkMessageSendType.FromServer)]
    public sealed class OpenCraftingStation : GameNetworkMessage
    {
        public int StationID { get; private set; }
        public Inventory PlayerInventory { get; private set; }

        public OpenCraftingStation() { }

        public OpenCraftingStation(int stationId, Inventory playerInventory)
        {
            this.StationID = stationId;
            this.PlayerInventory = playerInventory ?? throw new System.ArgumentNullException("❌ OpenCraftingStation: Ungültige Inventardaten übergeben!");
        }

        protected override MultiplayerMessageFilter OnGetLogFilter()
        {
            return MultiplayerMessageFilter.MissionObjects;
        }

        protected override string OnGetLogFormat()
        {
            return $"📌 OpenCraftingStation: Spieler öffnet Schmiede {StationID}";
        }

        protected override bool OnRead()
        {
            bool result = true;
            this.StationID = GameNetworkMessage.ReadIntFromPacket(new CompressionInfo.Integer(0, 10000, true), ref result);
            this.PlayerInventory = PENetworkModule.ReadInventoryPlayer(ref result);
            return result;
        }

        protected override void OnWrite()
        {
            GameNetworkMessage.WriteIntToPacket(this.StationID, new CompressionInfo.Integer(0, 10000, true));
            PENetworkModule.WriteInventoryPlayer(this.PlayerInventory);
        }
    }
}

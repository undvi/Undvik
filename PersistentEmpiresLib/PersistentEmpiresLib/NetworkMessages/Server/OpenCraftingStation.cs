using PersistentEmpiresLib.Data;
using PersistentEmpiresLib.Helpers;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Network.Messages;

namespace PersistentEmpiresLib.NetworkMessages.Server
{
    [DefineGameNetworkMessageTypeForMod(GameNetworkMessageSendType.FromServer)]
    public sealed class OpenCraftingStation : GameNetworkMessage
    {
        public MissionObject Station { get; private set; }
        public Inventory PlayerInventory { get; private set; }

        public OpenCraftingStation() { }

        public OpenCraftingStation(MissionObject station, Inventory playerInventory)
        {
            if (station == null || playerInventory == null)
            {
                throw new System.ArgumentNullException("❌ OpenCraftingStation: Ungültige Daten übergeben!");
            }

            this.Station = station;
            this.PlayerInventory = playerInventory;
        }

        protected override MultiplayerMessageFilter OnGetLogFilter()
        {
            return MultiplayerMessageFilter.MissionObjects;
        }

        protected override string OnGetLogFormat()
        {
            return $"📌 OpenCraftingStation: Player öffnet Schmiede {Station?.Id}";
        }

        protected override bool OnRead()
        {
            bool result = true;

            int missionObjectId = GameNetworkMessage.ReadMissionObjectIdFromPacket(ref result);
            this.Station = Mission.MissionNetworkHelper.GetMissionObjectFromMissionObjectId(missionObjectId);
            this.PlayerInventory = PENetworkModule.ReadInventoryPlayer(ref result);

            if (!result || this.Station == null || this.PlayerInventory == null)
            {
                InformationManager.DisplayMessage(new InformationMessage("⚠️ Fehler beim Lesen der Crafting-Station-Daten!"));
                return false;
            }

            return result;
        }

        protected override void OnWrite()
        {
            if (this.Station == null || this.PlayerInventory == null)
            {
                InformationManager.DisplayMessage(new InformationMessage("⚠️ Fehler: Keine gültigen Daten für Crafting-Station!"));
                return;
            }

            GameNetworkMessage.WriteMissionObjectIdToPacket(this.Station.Id);
            PENetworkModule.WriteInventoryPlayer(this.PlayerInventory);
        }
    }
}

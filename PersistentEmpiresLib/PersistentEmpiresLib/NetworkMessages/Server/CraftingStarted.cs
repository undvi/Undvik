using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Network.Messages;

namespace PersistentEmpiresLib.NetworkMessages.Server
{
    [DefineGameNetworkMessageTypeForMod(GameNetworkMessageSendType.FromServer)]
    public sealed class CraftingStarted : GameNetworkMessage
    {
        public MissionObject CraftingStation { get; private set; }
        public NetworkCommunicator Player { get; private set; }
        public int CraftIndex { get; private set; }

        public CraftingStarted() { }

        public CraftingStarted(MissionObject craftingStation, NetworkCommunicator player, int craftIndex)
        {
            if (craftingStation == null || player == null)
            {
                throw new System.ArgumentNullException("❌ CraftingStarted: Ungültige Parameter übergeben!");
            }

            this.CraftingStation = craftingStation;
            this.Player = player;
            this.CraftIndex = craftIndex;
        }

        protected override MultiplayerMessageFilter OnGetLogFilter()
        {
            return MultiplayerMessageFilter.MissionObjects;
        }

        protected override string OnGetLogFormat()
        {
            return $"🛠️ Crafting gestartet: {CraftingStation?.Id}, Spieler: {Player?.UserName}, Index: {CraftIndex}";
        }

        protected override bool OnRead()
        {
            bool result = true;

            int missionObjectId = GameNetworkMessage.ReadMissionObjectIdFromPacket(ref result);
            this.CraftingStation = Mission.MissionNetworkHelper.GetMissionObjectFromMissionObjectId(missionObjectId);
            this.Player = GameNetworkMessage.ReadNetworkPeerReferenceFromPacket(ref result);
            this.CraftIndex = GameNetworkMessage.ReadIntFromPacket(new CompressionInfo.Integer(0, 1024, true), ref result);

            if (!result || this.CraftingStation == null || this.Player == null)
            {
                InformationManager.DisplayMessage(new InformationMessage("⚠️ Fehler beim Lesen der Crafting-Daten!"));
                return false;
            }

            return result;
        }

        protected override void OnWrite()
        {
            if (this.CraftingStation == null || this.Player == null)
            {
                InformationManager.DisplayMessage(new InformationMessage("⚠️ Fehler: Keine gültigen Daten für Crafting-Start!"));
                return;
            }

            GameNetworkMessage.WriteMissionObjectIdToPacket(this.CraftingStation.Id);
            GameNetworkMessage.WriteNetworkPeerReferenceToPacket(this.Player);
            GameNetworkMessage.WriteIntToPacket(this.CraftIndex, new CompressionInfo.Integer(0, 1024, true));
        }
    }
}

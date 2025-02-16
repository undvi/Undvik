using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Network.Messages;

namespace PersistentEmpiresLib.NetworkMessages.Server
{
    [DefineGameNetworkMessageTypeForMod(GameNetworkMessageSendType.FromServer)]
    public sealed class CraftingCompleted : GameNetworkMessage
    {
        public NetworkCommunicator Player { get; private set; }

        public CraftingCompleted() { }

        public CraftingCompleted(NetworkCommunicator player)
        {
            if (player == null)
            {
                throw new System.ArgumentNullException("❌ Fehler: Spieler ist null in CraftingCompleted!");
            }

            this.Player = player;
        }

        protected override MultiplayerMessageFilter OnGetLogFilter()
        {
            return MultiplayerMessageFilter.MissionObjects;
        }

        protected override string OnGetLogFormat()
        {
            return $"✅ Crafting abgeschlossen: {Player?.UserName ?? "Unbekannter Spieler"}";
        }

        protected override bool OnRead()
        {
            bool result = true;
            this.Player = GameNetworkMessage.ReadNetworkPeerReferenceFromPacket(ref result);

            if (!result || this.Player == null)
            {
                InformationManager.DisplayMessage(new InformationMessage("⚠️ Fehler beim Lesen der Crafting-Daten!"));
                return false;
            }

            return result;
        }

        protected override void OnWrite()
        {
            if (this.Player == null)
            {
                InformationManager.DisplayMessage(new InformationMessage("⚠️ Fehler: Keine gültigen Daten für Crafting-Abschluss!"));
                return;
            }

            GameNetworkMessage.WriteNetworkPeerReferenceToPacket(this.Player);
        }
    }
}

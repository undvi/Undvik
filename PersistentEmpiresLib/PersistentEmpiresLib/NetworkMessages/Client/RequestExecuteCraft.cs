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
            this.Player = player ?? throw new System.ArgumentNullException(nameof(player), "❌ Fehler: Spieler ist null in CraftingCompleted!");
        }

        protected override MultiplayerMessageFilter OnGetLogFilter()
        {
            return MultiplayerMessageFilter.MissionObjects;
        }

        protected override string OnGetLogFormat()
        {
            return this.Player != null
                ? $"✅ Crafting abgeschlossen: {Player.UserName}"
                : "⚠️ Fehler: Spieler ist NULL beim Crafting-Abschluss!";
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

            return true;
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

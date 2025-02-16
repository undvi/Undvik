using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Network.Messages;

namespace PersistentEmpiresLib.NetworkMessages.Server
{
    [DefineGameNetworkMessageTypeForMod(GameNetworkMessageSendType.FromServer)]
    public sealed class PEAcademyOpened : GameNetworkMessage
    {
        public NetworkCommunicator Player { get; private set; }

        public PEAcademyOpened() { }

        public PEAcademyOpened(NetworkCommunicator player)
        {
            this.Player = player ?? throw new System.ArgumentNullException(nameof(player), "❌ Fehler: Spieler ist null in PEAcademyOpened!");
        }

        protected override MultiplayerMessageFilter OnGetLogFilter()
        {
            return MultiplayerMessageFilter.MissionObjects;
        }

        protected override string OnGetLogFormat()
        {
            return this.Player != null
                ? $"🏛️ Akademie geöffnet für {Player.UserName}"
                : "⚠️ Fehler: Spieler NULL beim Akademie-Event!";
        }

        protected override bool OnRead()
        {
            bool result = true;
            this.Player = GameNetworkMessage.ReadNetworkPeerReferenceFromPacket(ref result);

            if (!result || this.Player == null)
            {
                InformationManager.DisplayMessage(new InformationMessage("⚠️ Fehler beim Lesen der Akademie-Daten!"));
                return false;
            }

            return true;
        }

        protected override void OnWrite()
        {
            if (this.Player == null)
            {
                InformationManager.DisplayMessage(new InformationMessage("⚠️ Fehler: Ungültige Akademie-Daten für Netzwerksynchronisation!"));
                return;
            }

            GameNetworkMessage.WriteNetworkPeerReferenceToPacket(this.Player);
        }
    }
}

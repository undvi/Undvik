using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Network.Messages;

namespace PersistentEmpiresLib.NetworkMessages.Server
{
    [DefineGameNetworkMessageTypeForMod(GameNetworkMessageSendType.FromServer)]
    public sealed class PEInfluenceUpdated : GameNetworkMessage
    {
        public NetworkCommunicator Player { get; private set; }
        public string FactionName { get; private set; }
        public int Influence { get; private set; }
        public int FactionInfluence { get; private set; }

        public PEInfluenceUpdated() { }

        public PEInfluenceUpdated(NetworkCommunicator player, int influence, string factionName, int factionInfluence)
        {
            this.Player = player ?? throw new System.ArgumentNullException(nameof(player), "❌ Fehler: Spieler ist null in PEInfluenceUpdated!");
            this.Influence = influence;
            this.FactionName = !string.IsNullOrWhiteSpace(factionName) ? factionName : "Unabhängig";
            this.FactionInfluence = factionInfluence;
        }

        protected override MultiplayerMessageFilter OnGetLogFilter()
        {
            return MultiplayerMessageFilter.MissionObjects;
        }

        protected override string OnGetLogFormat()
        {
            return this.Player != null
                ? $"📢 Einfluss aktualisiert: {Player.UserName} hat {Influence} Einfluss (Fraktion: {FactionName} - {FactionInfluence})"
                : "⚠️ Fehler: Spieler NULL bei Einfluss-Update!";
        }

        protected override bool OnRead()
        {
            bool result = true;
            this.Player = GameNetworkMessage.ReadNetworkPeerReferenceFromPacket(ref result);
            this.Influence = GameNetworkMessage.ReadIntFromPacket(new CompressionInfo.Integer(0, 1000000, true), ref result);
            this.FactionName = GameNetworkMessage.ReadStringFromPacket(ref result);
            this.FactionInfluence = GameNetworkMessage.ReadIntFromPacket(new CompressionInfo.Integer(0, 10000000, true), ref result);

            if (!result || this.Player == null)
            {
                InformationManager.DisplayMessage(new InformationMessage("⚠️ Fehler beim Lesen der Einfluss-Daten!"));
                return false;
            }

            return true;
        }

        protected override void OnWrite()
        {
            if (this.Player == null)
            {
                InformationManager.DisplayMessage(new InformationMessage("⚠️ Fehler: Ungültige Einfluss-Daten für Netzwerksynchronisation!"));
                return;
            }

            GameNetworkMessage.WriteNetworkPeerReferenceToPacket(this.Player);
            GameNetworkMessage.WriteIntToPacket(this.Influence, new CompressionInfo.Integer(0, 1000000, true));
            GameNetworkMessage.WriteStringToPacket(this.FactionName);
            GameNetworkMessage.WriteIntToPacket(this.FactionInfluence, new CompressionInfo.Integer(0, 10000000, true));
        }
    }
}

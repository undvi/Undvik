using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Network.Messages;

namespace PersistentEmpiresLib.NetworkMessages.Server
{
    /// <summary>
    /// Diese Nachricht wird vom Server gesendet, wenn ein Blueprint freigeschaltet wurde.
    /// Timer- oder Coroutine-basierte Logik ist hier nicht vorhanden.
    /// </summary>
    [DefineGameNetworkMessageTypeForMod(GameNetworkMessageSendType.FromServer)]
    public sealed class PEAcademyBlueprintUnlocked : GameNetworkMessage
    {
        public NetworkCommunicator Player { get; private set; }
        public string BlueprintID { get; private set; }

        public PEAcademyBlueprintUnlocked() { }

        public PEAcademyBlueprintUnlocked(NetworkCommunicator player, string blueprintID)
        {
            this.Player = player ?? throw new System.ArgumentNullException(nameof(player), "❌ Fehler: Spieler ist null in PEAcademyBlueprintUnlocked!");
            this.BlueprintID = blueprintID ?? throw new System.ArgumentNullException(nameof(blueprintID), "❌ Fehler: Blueprint-ID ist null!");
        }

        protected override MultiplayerMessageFilter OnGetLogFilter()
        {
            return MultiplayerMessageFilter.MissionObjects;
        }

        protected override string OnGetLogFormat()
        {
            return this.Player != null
                ? $"✅ {Player.UserName} hat das Blueprint {BlueprintID} freigeschaltet!"
                : "⚠️ Fehler: Spieler NULL beim Freischalten eines Blueprints!";
        }

        protected override bool OnRead()
        {
            bool result = true;
            // Lese den Spieler und die Blueprint-ID aus dem Paket
            this.Player = GameNetworkMessage.ReadNetworkPeerReferenceFromPacket(ref result);
            this.BlueprintID = GameNetworkMessage.ReadStringFromPacket(ref result);

            // Überprüfen auf ungültige Daten
            if (!result || this.Player == null || string.IsNullOrEmpty(this.BlueprintID))
            {
                InformationManager.DisplayMessage(new InformationMessage("⚠️ Fehler beim Lesen der Blueprint-Daten!"));
                return false;
            }

            return true;
        }

        protected override void OnWrite()
        {
            // Prüfen, ob die Daten gültig sind, bevor sie in das Paket geschrieben werden
            if (this.Player == null || string.IsNullOrEmpty(this.BlueprintID))
            {
                InformationManager.DisplayMessage(new InformationMessage("⚠️ Fehler: Ungültige Blueprint-Daten für Netzwerksynchronisation!"));
                return;
            }

            // Schreibe den Spieler und die Blueprint-ID in das Paket
            GameNetworkMessage.WriteNetworkPeerReferenceToPacket(this.Player);
            GameNetworkMessage.WriteStringToPacket(this.BlueprintID);
        }
    }
}

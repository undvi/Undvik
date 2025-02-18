using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Network.Messages;
using PersistentEmpiresLib.Factions;
using System.Linq;
using TaleWorlds.Library;

namespace PersistentEmpiresLib.NetworkMessages.Server
{
    [DefineGameNetworkMessageTypeForMod(GameNetworkMessageSendType.FromServer)]
    public sealed class AddMarshallIdToFaction : GameNetworkMessage
    {
        public string MarshallId { get; private set; }
        public int FactionIndex { get; private set; }

        public AddMarshallIdToFaction() { }

        public AddMarshallIdToFaction(string marshallId, int factionIndex)
        {
            this.MarshallId = marshallId;
            this.FactionIndex = factionIndex;
        }

        protected override MultiplayerMessageFilter OnGetLogFilter()
        {
            return MultiplayerMessageFilter.Administration;
        }

        protected override string OnGetLogFormat()
        {
            return $"🔹 Marshall {MarshallId} wurde zur Fraktion {FactionIndex} hinzugefügt.";
        }

        protected override bool OnRead()
        {
            bool result = true;
            this.FactionIndex = GameNetworkMessage.ReadIntFromPacket(new CompressionInfo.Integer(-1, 200, true), ref result);
            this.MarshallId = GameNetworkMessage.ReadStringFromPacket(ref result);
            return result;
        }

        protected override void OnWrite()
        {
            GameNetworkMessage.WriteIntToPacket(this.FactionIndex, new CompressionInfo.Integer(-1, 200, true));
            GameNetworkMessage.WriteStringToPacket(this.MarshallId);
        }

        public static bool CanAddMarshall(Faction faction, NetworkCommunicator requester)
        {
            if (faction.Rank < 2)
            {
                InformationManager.DisplayMessage(new InformationMessage("❌ Deine Fraktion muss mindestens Rang 2 sein, um Marschälle zu ernennen."));
                return false;
            }

            if (faction.lordId != requester.VirtualPlayer.ToPlayerId())
            {
                InformationManager.DisplayMessage(new InformationMessage("❌ Nur der Fraktionsführer kann Marschälle ernennen."));
                return false;
            }

            int maxMarshalls = faction.MaxMembers / 10; // 1 Marschall pro 10 Mitglieder
            if (faction.marshalls.Count >= maxMarshalls)
            {
                InformationManager.DisplayMessage(new InformationMessage($"⚠️ Maximale Anzahl an Marschällen erreicht! (Max: {maxMarshalls})"));
                return false;
            }

            return true;
        }

        public static void TryAddMarshall(Faction faction, string marshallId, NetworkCommunicator requester)
        {
            if (!CanAddMarshall(faction, requester))
            {
                return;
            }

            if (faction.marshalls.Contains(marshallId))
            {
                InformationManager.DisplayMessage(new InformationMessage("⚠️ Dieser Spieler ist bereits ein Marschall."));
                return;
            }

            faction.marshalls.Add(marshallId);

            GameNetwork.BeginBroadcastModuleEvent();
            GameNetwork.WriteMessage(new AddMarshallIdToFaction(marshallId, faction.FactionIndex));
            GameNetwork.EndBroadcastModuleEvent();

            // 🔹 Nachricht an alle Mitglieder der Fraktion senden
            faction.AddToFactionLog($"🔹 {requester.UserName} hat {marshallId} zum Marschall ernannt.");
            InformationManager.DisplayMessage(new InformationMessage($"🎖️ {marshallId} wurde zum Marschall von {faction.name} ernannt!"));
        }
    }
}

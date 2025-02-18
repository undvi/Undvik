using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Network.Messages;
using PersistentEmpiresLib.Factions;
using TaleWorlds.Core;
using TaleWorlds.Library;
using System.Linq;

namespace PersistentEmpiresLib.NetworkMessages.Server
{
    [DefineGameNetworkMessageTypeForMod(GameNetworkMessageSendType.FromServer)]
    public sealed class PlayerJoinedFaction : GameNetworkMessage
    {
        public int FactionIndex { get; private set; }
        public int JoinedFrom { get; private set; }
        public NetworkCommunicator Player { get; private set; }

        private const int InfluenceCostForSwitch = 50;  // Einflusskosten für Fraktionswechsel
        private const int GoldBonusForJoining = 100;    // Optionaler Goldbonus für den Beitritt

        public PlayerJoinedFaction() { }

        public PlayerJoinedFaction(int factionIndex, int joinedFrom, NetworkCommunicator player)
        {
            this.FactionIndex = factionIndex;
            this.JoinedFrom = joinedFrom;
            this.Player = player;
        }

        protected override MultiplayerMessageFilter OnGetLogFilter()
        {
            return MultiplayerMessageFilter.General;
        }

        protected override string OnGetLogFormat()
        {
            return $"📢 Spieler {Player.UserName} ist Fraktion {FactionIndex} beigetreten (vorher in {JoinedFrom})";
        }

        protected override bool OnRead()
        {
            bool result = true;
            this.FactionIndex = GameNetworkMessage.ReadIntFromPacket(new CompressionInfo.Integer(-1, 200, true), ref result);
            this.JoinedFrom = GameNetworkMessage.ReadIntFromPacket(new CompressionInfo.Integer(-1, 200, true), ref result);
            this.Player = GameNetworkMessage.ReadNetworkPeerReferenceFromPacket(ref result);

            return result;
        }

        protected override void OnWrite()
        {
            GameNetworkMessage.WriteIntToPacket(this.FactionIndex, new CompressionInfo.Integer(-1, 200, true));
            GameNetworkMessage.WriteIntToPacket(this.JoinedFrom, new CompressionInfo.Integer(-1, 200, true));
            GameNetworkMessage.WriteNetworkPeerReferenceToPacket(this.Player);
        }

        public static void BroadcastPlayerJoinedFaction(int factionIndex, int joinedFrom, NetworkCommunicator player)
        {
            if (player == null)
            {
                InformationManager.DisplayMessage(new InformationMessage("⚠️ Fehler: Spielerreferenz ist null in PlayerJoinedFaction."));
                return;
            }

            Faction newFaction = FactionManager.GetFactionByIndex(factionIndex);
            Faction previousFaction = FactionManager.GetFactionByIndex(joinedFrom);
            PersistentEmpireRepresentative playerRep = player.GetComponent<PersistentEmpireRepresentative>();

            if (newFaction == null)
            {
                InformationManager.DisplayMessage(new InformationMessage($"⚠️ Fehler: Fraktion {factionIndex} nicht gefunden, Spieler {player.UserName} kann nicht beitreten."));
                return;
            }

            // Falls der Spieler bereits in einer anderen Fraktion war, entfernen
            if (previousFaction != null && previousFaction.members.Contains(player))
            {
                previousFaction.members.Remove(player);
                InformationManager.DisplayMessage(new InformationMessage($"📢 {player.UserName} hat die Fraktion {previousFaction.name} verlassen."));
            }

            // Überprüfung: Hat der Spieler genug Einfluss, um die Fraktion zu wechseln?
            if (playerRep != null && !playerRep.ReduceIfHaveEnoughInfluence(InfluenceCostForSwitch))
            {
                InformationManager.DisplayMessage(new InformationMessage($"❌ {player.UserName} hat nicht genug Einfluss ({InfluenceCostForSwitch} benötigt), um die Fraktion zu wechseln."));
                return;
            }

            // Spieler zur neuen Fraktion hinzufügen
            if (!newFaction.members.Contains(player))
            {
                newFaction.members.Add(player);
            }

            // Falls der Spieler bereits einen höheren Rang in der Fraktion hatte, wird er beibehalten
            int previousRank = playerRep?.GetFactionRank() ?? 1;
            int newRank = previousRank > 1 ? previousRank : 1;
            playerRep?.SetFactionRank(newRank);

            // Optionaler Goldbonus für den Beitritt zu einer Fraktion
            playerRep?.AddGold(GoldBonusForJoining);
            InformationManager.DisplayMessage(new InformationMessage($"💰 {player.UserName} hat {GoldBonusForJoining} Gold für den Fraktionsbeitritt erhalten."));

            // Serverweite Benachrichtigung
            GameNetwork.BeginBroadcastModuleEvent();
            GameNetwork.WriteMessage(new PlayerJoinedFaction(factionIndex, joinedFrom, player));
            GameNetwork.EndBroadcastModuleEvent();

            // Fraktionsmitgliedern mitteilen
            foreach (var member in newFaction.members)
            {
                InformationManager.DisplayMessage(new InformationMessage($"📢 {player.UserName} ist der Fraktion {newFaction.name} beigetreten."));
            }

            // UI aktualisieren
            GameNetwork.BeginBroadcastModuleEvent();
            GameNetwork.WriteMessage(new UpdateFactionFromServer(newFaction, factionIndex));
            GameNetwork.EndBroadcastModuleEvent();
        }
    }
}

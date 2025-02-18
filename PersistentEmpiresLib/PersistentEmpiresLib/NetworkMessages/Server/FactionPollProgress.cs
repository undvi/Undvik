using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Network.Messages;
using PersistentEmpiresLib.Factions;
using TaleWorlds.Core;
using TaleWorlds.Library;
using System.Collections.Generic;

namespace PersistentEmpiresLib.NetworkMessages.Server
{
    [DefineGameNetworkMessageTypeForMod(GameNetworkMessageSendType.FromServer)]
    public sealed class FactionPollProgress : GameNetworkMessage
    {
        public int FactionIndex { get; private set; }
        public int VotesAccepted { get; private set; }
        public int VotesRejected { get; private set; }
        public int RequiredVotes { get; private set; } // ✅ Mindeststimmen für den Wahlausgang

        private static Dictionary<int, HashSet<string>> voterRegistry = new Dictionary<int, HashSet<string>>(); // ✅ Verhindert doppelte Stimmen

        public FactionPollProgress() { }

        public FactionPollProgress(int factionIndex, int votesAccepted, int votesRejected, int requiredVotes)
        {
            this.FactionIndex = factionIndex;
            this.VotesAccepted = votesAccepted;
            this.VotesRejected = votesRejected;
            this.RequiredVotes = requiredVotes;
        }

        protected override MultiplayerMessageFilter OnGetLogFilter()
        {
            return MultiplayerMessageFilter.Administration;
        }

        protected override string OnGetLogFormat()
        {
            return $"📊 Abstimmungsfortschritt für Fraktion {FactionIndex}: {VotesAccepted} Ja / {VotesRejected} Nein | Benötigt: {RequiredVotes}";
        }

        protected override bool OnRead()
        {
            bool result = true;
            this.FactionIndex = GameNetworkMessage.ReadIntFromPacket(new CompressionInfo.Integer(-1, 200), ref result);
            this.VotesAccepted = GameNetworkMessage.ReadIntFromPacket(CompressionBasic.PlayerCompressionInfo, ref result);
            this.VotesRejected = GameNetworkMessage.ReadIntFromPacket(CompressionBasic.PlayerCompressionInfo, ref result);
            this.RequiredVotes = GameNetworkMessage.ReadIntFromPacket(CompressionBasic.PlayerCompressionInfo, ref result);
            return result;
        }

        protected override void OnWrite()
        {
            GameNetworkMessage.WriteIntToPacket(this.FactionIndex, new CompressionInfo.Integer(-1, 200));
            GameNetworkMessage.WriteIntToPacket(this.VotesAccepted, CompressionBasic.PlayerCompressionInfo);
            GameNetworkMessage.WriteIntToPacket(this.VotesRejected, CompressionBasic.PlayerCompressionInfo);
            GameNetworkMessage.WriteIntToPacket(this.RequiredVotes, CompressionBasic.PlayerCompressionInfo);
        }

        public static void HandleVote(NetworkCommunicator voter, int factionIndex, bool accepted)
        {
            Faction faction = FactionManager.GetFactionByIndex(factionIndex);
            if (faction == null)
            {
                InformationManager.DisplayMessage(new InformationMessage("⚠️ Fraktion nicht gefunden. Abstimmung nicht möglich."));
                return;
            }

            // ✅ Verhindern, dass Spieler mehrfach abstimmen
            if (!voterRegistry.ContainsKey(factionIndex))
                voterRegistry[factionIndex] = new HashSet<string>();

            if (voterRegistry[factionIndex].Contains(voter.VirtualPlayer.ToPlayerId()))
            {
                InformationManager.DisplayMessage(new InformationMessage("❌ Du hast bereits abgestimmt!"));
                return;
            }

            voterRegistry[factionIndex].Add(voter.VirtualPlayer.ToPlayerId());

            // ✅ Stimmen zählen
            if (accepted) faction.VotesAccepted++;
            else faction.VotesRejected++;

            int totalVotes = faction.VotesAccepted + faction.VotesRejected;
            int requiredVotes = faction.MaxMembers / 2 + 1; // ✅ Mehrheit erforderlich

            // ✅ Abstimmung beenden, wenn die Mehrheit erreicht ist
            if (faction.VotesAccepted >= requiredVotes)
            {
                FinalizePoll(factionIndex, true);
                return;
            }

            if (faction.VotesRejected >= requiredVotes)
            {
                FinalizePoll(factionIndex, false);
                return;
            }

            // ✅ Live-Update senden
            BroadcastPollProgress(factionIndex, faction.VotesAccepted, faction.VotesRejected, requiredVotes);
        }

        public static void BroadcastPollProgress(int factionIndex, int votesAccepted, int votesRejected, int requiredVotes)
        {
            Faction faction = FactionManager.GetFactionByIndex(factionIndex);
            if (faction == null)
            {
                InformationManager.DisplayMessage(new InformationMessage("⚠️ Fraktion nicht gefunden, Abstimmungsfortschritt nicht sendbar."));
                return;
            }

            GameNetwork.BeginBroadcastModuleEvent();
            GameNetwork.WriteMessage(new FactionPollProgress(factionIndex, votesAccepted, votesRejected, requiredVotes));
            GameNetwork.EndBroadcastModuleEvent();

            InformationManager.DisplayMessage(new InformationMessage(
                $"📢 Abstimmung in {faction.name}: {votesAccepted} Ja / {votesRejected} Nein | Benötigt: {requiredVotes}"
            ));
        }

        private static void FinalizePoll(int factionIndex, bool accepted)
        {
            Faction faction = FactionManager.GetFactionByIndex(factionIndex);
            if (faction == null) return;

            string resultMessage = accepted ? "✅ Wahl erfolgreich!" : "❌ Wahl abgelehnt!";
            InformationManager.DisplayMessage(new InformationMessage($"📢 Fraktion {faction.name}: {resultMessage}"));

            // ✅ Alle Stimmen zurücksetzen
            faction.VotesAccepted = 0;
            faction.VotesRejected = 0;
            voterRegistry[factionIndex].Clear();

            // ✅ Fraktionslog aktualisieren
            faction.AddToFactionLog($"📜 Abstimmung abgeschlossen: {resultMessage}");

            // ✅ Falls Wahl akzeptiert wurde, Lord ernennen (falls es eine Lord-Wahl war)
            if (accepted && faction.PendingLordCandidate != null)
            {
                faction.lordId = faction.PendingLordCandidate.VirtualPlayer.ToPlayerId();
                faction.PendingLordCandidate = null;
                InformationManager.DisplayMessage(new InformationMessage($"👑 Neuer Lord der Fraktion {faction.name}: {faction.lordId}"));
            }
        }
    }
}

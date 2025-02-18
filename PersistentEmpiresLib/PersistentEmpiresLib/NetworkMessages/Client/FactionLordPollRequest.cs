using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Network.Messages;
using PersistentEmpiresLib.Factions;
using PersistentEmpiresLib.Helpers;
using System;

namespace PersistentEmpiresLib.NetworkMessages.Client
{
    [DefineGameNetworkMessageTypeForMod(GameNetworkMessageSendType.FromClient)]
    public sealed class FactionLordPollRequest : GameNetworkMessage
    {
        private const int RequiredFactionRank = 2;  // Mindest-Rang der Fraktion für Lord-Wahlen
        private const int PollCostGold = 50000;      // Kosten für das Starten einer Lord-Wahl
        private const int PollCostInfluence = 100;   // Kosten in Einfluss
        private const int PollCooldown = 86400;      // Cooldown: 24h in Sekunden

        public FactionLordPollRequest() { }

        public FactionLordPollRequest(NetworkCommunicator player)
        {
            this.Player = player;
        }

        public NetworkCommunicator Player { get; private set; }

        protected override MultiplayerMessageFilter OnGetLogFilter()
        {
            return MultiplayerMessageFilter.General;
        }

        protected override string OnGetLogFormat()
        {
            return $"Faction Lord Poll Request | Requested by: {Player?.UserName ?? "Unknown"}";
        }

        protected override bool OnRead()
        {
            bool result = true;
            this.Player = GameNetworkMessage.ReadNetworkPeerReferenceFromPacket(ref result);
            return result;
        }

        protected override void OnWrite()
        {
            GameNetworkMessage.WriteNetworkPeerReferenceToPacket(this.Player);
        }

        public static bool TryStartLordPoll(NetworkCommunicator requestingPlayer, NetworkCommunicator candidate)
        {
            if (requestingPlayer == null || candidate == null)
            {
                InformationManager.DisplayMessage(new InformationMessage("Error: Invalid request - player or candidate missing."));
                return false;
            }

            PersistentEmpireRepresentative requesterRep = requestingPlayer.GetComponent<PersistentEmpireRepresentative>();
            PersistentEmpireRepresentative candidateRep = candidate.GetComponent<PersistentEmpireRepresentative>();

            if (requesterRep == null || candidateRep == null || requesterRep.GetFaction() == null)
            {
                InformationManager.DisplayMessage(new InformationMessage("Error: Requester or candidate is not in a faction."));
                return false;
            }

            Faction faction = requesterRep.GetFaction();

            // 🔹 Überprüfung: Fraktion muss den Mindest-Rang haben
            if (faction.Rank < RequiredFactionRank)
            {
                InformationManager.DisplayMessage(new InformationMessage("Your faction must be at least Rank 2 to start a Lord election."));
                return false;
            }

            // 🔹 Überprüfung: Cooldown (24h) für Lord-Wahlen
            if (faction.pollUnlockedAt > DateTimeOffset.UtcNow.ToUnixTimeSeconds())
            {
                InformationManager.DisplayMessage(new InformationMessage("Your faction must wait before starting another election."));
                return false;
            }

            // 🔹 Überprüfung: Fraktion muss genug Gold & Einfluss haben
            if (faction.Gold < PollCostGold || faction.Influence < PollCostInfluence)
            {
                InformationManager.DisplayMessage(new InformationMessage("Your faction does not have enough gold or influence to start a Lord election."));
                return false;
            }

            // 🔹 Überprüfung: Kandidat muss Mitglied der Fraktion sein
            if (!faction.members.Contains(candidate))
            {
                InformationManager.DisplayMessage(new InformationMessage("The selected candidate is not in your faction."));
                return false;
            }

            // 🔹 Gold & Einfluss abziehen und Wahl starten
            faction.Gold -= PollCostGold;
            faction.Influence -= PollCostInfluence;
            faction.pollUnlockedAt = DateTimeOffset.UtcNow.ToUnixTimeSeconds() + PollCooldown; // 24h Cooldown setzen

            // Lord-Wahl an den Server senden
            GameNetwork.BeginBroadcastModuleEvent();
            GameNetwork.WriteMessage(new FactionLordPollOpened(requestingPlayer, candidate));
            GameNetwork.EndBroadcastModuleEvent();

            InformationManager.DisplayMessage(new InformationMessage($"A Lord election has started in faction {faction.name}."));
            return true;
        }
    }
}

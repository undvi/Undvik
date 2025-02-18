using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Network.Messages;
using PersistentEmpiresLib.Factions;
using PersistentEmpiresLib.Helpers;
using TaleWorlds.Core;
using TaleWorlds.Library;
using System;

namespace PersistentEmpiresLib.NetworkMessages.Server
{
    [DefineGameNetworkMessageTypeForMod(GameNetworkMessageSendType.FromServer)]
    public sealed class FactionLordPollOpened : GameNetworkMessage
    {
        private const int RequiredRank = 2; // Mindest-Rang für eine Lord-Wahl
        private const int PollCost = 50000; // Kosten für die Lord-Wahl in Gold
        private const int PollCooldown = 86400; // 24 Stunden Cooldown

        public FactionLordPollOpened() { }

        public FactionLordPollOpened(NetworkCommunicator pollCreator, NetworkCommunicator lordCandidate)
        {
            this.PollCreator = pollCreator;
            this.LordCandidate = lordCandidate;
        }

        public NetworkCommunicator PollCreator { get; private set; }
        public NetworkCommunicator LordCandidate { get; private set; }

        protected override MultiplayerMessageFilter OnGetLogFilter()
        {
            return MultiplayerMessageFilter.General;
        }

        protected override string OnGetLogFormat()
        {
            return $"🗳️ Lord-Wahl gestartet! Fraktion: {PollCreator.GetComponent<PersistentEmpireRepresentative>().GetFaction()?.name}";
        }

        protected override bool OnRead()
        {
            bool result = true;
            this.PollCreator = GameNetworkMessage.ReadNetworkPeerReferenceFromPacket(ref result);
            this.LordCandidate = GameNetworkMessage.ReadNetworkPeerReferenceFromPacket(ref result);
            return result;
        }

        protected override void OnWrite()
        {
            GameNetworkMessage.WriteNetworkPeerReferenceToPacket(this.PollCreator);
            GameNetworkMessage.WriteNetworkPeerReferenceToPacket(this.LordCandidate);
        }

        public static bool TryOpenPoll(NetworkCommunicator pollCreator, NetworkCommunicator lordCandidate)
        {
            PersistentEmpireRepresentative creatorRep = pollCreator.GetComponent<PersistentEmpireRepresentative>();
            PersistentEmpireRepresentative candidateRep = lordCandidate.GetComponent<PersistentEmpireRepresentative>();

            if (creatorRep == null || candidateRep == null || creatorRep.GetFaction() == null)
            {
                InformationManager.DisplayMessage(new InformationMessage("⚠️ Fehler: Ungültige Fraktionsdaten."));
                return false;
            }

            Faction faction = creatorRep.GetFaction();

            // ✅ Mindest-Rang Überprüfung
            if (faction.Rank < RequiredRank)
            {
                InformationManager.DisplayMessage(new InformationMessage("❌ Eure Fraktion muss mindestens Rang 2 haben, um eine Lord-Wahl zu starten."));
                return false;
            }

            // ✅ Überprüfung, ob genug Gold vorhanden ist
            if (faction.Gold < PollCost)
            {
                InformationManager.DisplayMessage(new InformationMessage("❌ Eure Fraktion hat nicht genug Gold für eine Lord-Wahl (50.000 Gold benötigt)."));
                return false;
            }

            // ✅ Kandidat muss in der gleichen Fraktion sein
            if (creatorRep.GetFactionIndex() != candidateRep.GetFactionIndex())
            {
                InformationManager.DisplayMessage(new InformationMessage("❌ Der gewählte Lord-Kandidat ist nicht in eurer Fraktion."));
                return false;
            }

            // ✅ Cooldown prüfen (24h zwischen Wahlen)
            if (faction.pollUnlockedAt > DateTimeOffset.UtcNow.ToUnixTimeSeconds())
            {
                InformationManager.DisplayMessage(new InformationMessage("⚠️ Eure Fraktion kann erst nach 24 Stunden eine neue Wahl starten."));
                return false;
            }

            // ✅ Gold abziehen und Wahl starten
            faction.Gold -= PollCost;
            faction.pollUnlockedAt = DateTimeOffset.UtcNow.ToUnixTimeSeconds() + PollCooldown;

            GameNetwork.BeginBroadcastModuleEvent();
            GameNetwork.WriteMessage(new FactionLordPollOpened(pollCreator, lordCandidate));
            GameNetwork.EndBroadcastModuleEvent();

            // ✅ Nachricht an alle Fraktionsmitglieder
            faction.AddToFactionLog($"🗳️ {pollCreator.UserName} hat eine Lord-Wahl für {lordCandidate.UserName} gestartet.");
            InformationManager.DisplayMessage(new InformationMessage($"🗳️ Eine Lord-Wahl wurde in der Fraktion {faction.name} gestartet. 50.000 Gold wurden abgezogen."));

            return true;
        }
    }
}

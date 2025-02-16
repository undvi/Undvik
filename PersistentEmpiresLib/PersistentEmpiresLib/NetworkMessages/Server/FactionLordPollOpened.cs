using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Network.Messages;
using PersistentEmpiresLib.Factions;
using PersistentEmpiresLib.Helpers;
using TaleWorlds.Core;

namespace PersistentEmpiresLib.NetworkMessages.Server
{
    [DefineGameNetworkMessageTypeForMod(GameNetworkMessageSendType.FromServer)]
    public sealed class FactionLordPollOpened : GameNetworkMessage
    {
        private const int RequiredRank = 2; // Mindest-Rang für eine Lord-Wahl
        private const int PollCost = 50000; // Kosten für die Lord-Wahl in Gold

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
            return "Server started a faction lord polling";
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
            if (creatorRep == null || creatorRep.GetFaction() == null) return false;

            Faction faction = creatorRep.GetFaction();

            // Überprüfe, ob die Fraktion Rang 2 oder höher hat
            if (faction.Rank < RequiredRank)
            {
                InformationManager.DisplayMessage(new InformationMessage("Your faction must be at least Rank 2 to start a Lord election."));
                return false;
            }

            // Überprüfe, ob genug Gold vorhanden ist
            if (faction.Gold < PollCost)
            {
                InformationManager.DisplayMessage(new InformationMessage("Your faction does not have enough gold to start a Lord election."));
                return false;
            }

            // Gold abziehen und Wahl starten
            faction.Gold -= PollCost;
            GameNetwork.BeginBroadcastModuleEvent();
            GameNetwork.WriteMessage(new FactionLordPollOpened(pollCreator, lordCandidate));
            GameNetwork.EndBroadcastModuleEvent();

            InformationManager.DisplayMessage(new InformationMessage($"A Lord election has started in faction {faction.name}. 50,000 gold has been deducted."));
            return true;
        }
    }
}

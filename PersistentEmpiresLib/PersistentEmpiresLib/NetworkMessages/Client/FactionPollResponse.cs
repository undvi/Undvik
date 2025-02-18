using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Network.Messages;
using PersistentEmpiresLib.Factions;
using PersistentEmpiresLib.Helpers;
using System.Linq;
using TaleWorlds.Core;

namespace PersistentEmpiresLib.NetworkMessages.Client
{
    [DefineGameNetworkMessageTypeForMod(GameNetworkMessageSendType.FromClient)]
    public sealed class FactionPollResponse : GameNetworkMessage
    {
        public bool Accepted { get; private set; }
        public int FactionIndex { get; private set; }
        public NetworkCommunicator Voter { get; private set; }

        public FactionPollResponse() { }

        public FactionPollResponse(int factionIndex, NetworkCommunicator voter, bool accepted)
        {
            this.FactionIndex = factionIndex;
            this.Voter = voter;
            this.Accepted = accepted;
        }

        protected override bool OnRead()
        {
            bool result = true;
            this.FactionIndex = GameNetworkMessage.ReadIntFromPacket(new CompressionInfo.Integer(-1, 200, true), ref result);
            this.Voter = GameNetworkMessage.ReadNetworkPeerReferenceFromPacket(ref result);
            this.Accepted = GameNetworkMessage.ReadBoolFromPacket(ref result);
            return result;
        }

        protected override void OnWrite()
        {
            GameNetworkMessage.WriteIntToPacket(this.FactionIndex, new CompressionInfo.Integer(-1, 200, true));
            GameNetworkMessage.WriteNetworkPeerReferenceToPacket(this.Voter);
            GameNetworkMessage.WriteBoolToPacket(this.Accepted);
        }

        protected override MultiplayerMessageFilter OnGetLogFilter()
        {
            return MultiplayerMessageFilter.Administration;
        }

        protected override string OnGetLogFormat()
        {
            return $"Faction poll response received from {Voter?.UserName ?? "Unknown"} for faction {FactionIndex}: " + (this.Accepted ? "Accepted." : "Not accepted.");
        }

        public static void HandleFactionPollResponse(NetworkCommunicator sender, bool accepted)
        {
            if (sender == null)
            {
                InformationManager.DisplayMessage(new InformationMessage("Error: Invalid voter."));
                return;
            }

            PersistentEmpireRepresentative senderRep = sender.GetComponent<PersistentEmpireRepresentative>();
            if (senderRep == null || senderRep.GetFaction() == null)
            {
                InformationManager.DisplayMessage(new InformationMessage("Error: Voter is not in a faction."));
                return;
            }

            Faction faction = senderRep.GetFaction();
            if (!FactionPollManager.IsPollActive(faction.FactionIndex))
            {
                InformationManager.DisplayMessage(new InformationMessage("There is no active faction poll."));
                return;
            }

            if (FactionPollManager.HasAlreadyVoted(sender, faction.FactionIndex))
            {
                InformationManager.DisplayMessage(new InformationMessage("You have already voted in this poll."));
                return;
            }

            FactionPollManager.RecordVote(faction.FactionIndex, sender, accepted);

            // Broadcast Abstimmungsergebnis an Fraktionsmitglieder
            GameNetwork.BeginBroadcastModuleEvent();
            GameNetwork.WriteMessage(new FactionPollProgress(faction.FactionIndex, FactionPollManager.GetAcceptedVotes(faction.FactionIndex), FactionPollManager.GetRejectedVotes(faction.FactionIndex)));
            GameNetwork.EndBroadcastModuleEvent();

            InformationManager.DisplayMessage(new InformationMessage($"Vote recorded for {sender.UserName}: " + (accepted ? "Accepted." : "Rejected.")));
        }
    }
}

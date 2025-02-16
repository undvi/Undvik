using PersistentEmpiresLib.Factions;
using PersistentEmpiresLib.Helpers;
using PersistentEmpiresLib.NetworkMessages.Client;
using PersistentEmpiresLib.NetworkMessages.Server;
using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace PersistentEmpiresLib.PersistentEmpiresMission.MissionBehaviors
{
    public class FactionPollComponent : MissionNetwork
    {
        private InformationComponent _informationComponent;
        private FactionsBehavior _factionsBehavior;
        private Dictionary<int, FactionPoll> _ongoingPolls;

        private int LordPollRequiredGold = 1000; // Standardwert (kann konfigurierbar sein)
        private int LordPollTimeOut = 60; // Sekunden für den Poll-Cooldown

        public override void OnBehaviorInitialize()
        {
            base.OnBehaviorInitialize();
            this._ongoingPolls = new Dictionary<int, FactionPoll>();
            this._informationComponent = base.Mission.GetMissionBehavior<InformationComponent>();
            this._factionsBehavior = base.Mission.GetMissionBehavior<FactionsBehavior>();
            this.AddRemoveMessageHandlers(GameNetwork.NetworkMessageHandlerRegisterer.RegisterMode.Add);

            if (GameNetwork.IsServer)
            {
                this.LordPollRequiredGold = ConfigManager.GetIntConfig("LordPollRequiredGold", 1000);
                this.LordPollTimeOut = ConfigManager.GetIntConfig("LordPollTimeOut", 60);
            }
        }

        public override void OnRemoveBehavior()
        {
            base.OnRemoveBehavior();
            this.AddRemoveMessageHandlers(GameNetwork.NetworkMessageHandlerRegisterer.RegisterMode.Remove);
        }

        public override void OnMissionTick(float dt)
        {
            base.OnMissionTick(dt);
            if (this._ongoingPolls == null) return;

            foreach (FactionPoll poll in this._ongoingPolls.Values.ToList())
            {
                if (poll.IsOpen)
                {
                    poll.Tick();
                }
            }
        }

        public void OpenLordPollServer(NetworkCommunicator pollCreatorPeer, NetworkCommunicator targetPeer)
        {
            if (pollCreatorPeer == null || targetPeer == null)
            {
                return;
            }

            if (!pollCreatorPeer.IsConnectionActive || !targetPeer.IsConnectionActive)
            {
                this._informationComponent.SendAnnouncementToPlayer("Target player not found", pollCreatorPeer);
                return;
            }

            PersistentEmpireRepresentative creatorRep = pollCreatorPeer.GetComponent<PersistentEmpireRepresentative>();
            PersistentEmpireRepresentative targetRep = targetPeer.GetComponent<PersistentEmpireRepresentative>();

            if (creatorRep == null || targetRep == null || creatorRep.GetFaction() == null || targetRep.GetFaction() == null)
            {
                return;
            }

            Faction faction = targetRep.GetFaction();
            int factionIndex = targetRep.GetFactionIndex();

            if (creatorRep.GetFactionIndex() != factionIndex)
            {
                this._informationComponent.SendAnnouncementToPlayer("Your candidate is not in the same faction as you", pollCreatorPeer);
                return;
            }

            if (this._ongoingPolls.ContainsKey(factionIndex) && this._ongoingPolls[factionIndex].IsOpen)
            {
                this._informationComponent.SendAnnouncementToPlayer("There is already an ongoing poll", pollCreatorPeer);
                return;
            }

            if (!creatorRep.ReduceIfHaveEnoughGold(LordPollRequiredGold))
            {
                this._informationComponent.SendMessage($"You need {LordPollRequiredGold} dinars to start a poll", 0xFF0000FF, pollCreatorPeer);
                return;
            }

            // Lord-Wahl starten
            this.StartLordPoll(targetPeer, pollCreatorPeer);
        }

        private void StartLordPoll(NetworkCommunicator targetPeer, NetworkCommunicator pollCreatorPeer)
        {
            PersistentEmpireRepresentative targetRep = targetPeer.GetComponent<PersistentEmpireRepresentative>();

            this._ongoingPolls[targetRep.GetFactionIndex()] = new FactionPoll(
                FactionPoll.Type.Lord,
                targetRep.GetFactionIndex(),
                targetRep.GetFaction(),
                targetPeer
            );

            foreach (NetworkCommunicator player in this._ongoingPolls[targetRep.GetFactionIndex()].ParticipantsToVote)
            {
                GameNetwork.BeginModuleEventAsServer(player);
                GameNetwork.WriteMessage(new FactionLordPollOpened(pollCreatorPeer, targetPeer));
                GameNetwork.EndModuleEventAsServer();
            }
        }

        public void Vote(bool accepted)
        {
            if (GameNetwork.IsServer)
            {
                if (GameNetwork.MyPeer != null)
                {
                    this.ApplyVote(GameNetwork.MyPeer, accepted);
                    return;
                }
            }
            else
            {
                GameNetwork.BeginModuleEventAsClient();
                GameNetwork.WriteMessage(new FactionPollResponse(accepted));
                GameNetwork.EndModuleEventAsClient();
            }
        }

        private void ApplyVote(NetworkCommunicator peer, bool accepted)
        {
            PersistentEmpireRepresentative rep = peer.GetComponent<PersistentEmpireRepresentative>();
            if (!_ongoingPolls.ContainsKey(rep.GetFactionIndex())) return;

            FactionPoll poll = _ongoingPolls[rep.GetFactionIndex()];
            if (!poll.ApplyVote(peer, accepted)) return;

            foreach (NetworkCommunicator player in poll.GetPollProgressReceivers())
            {
                GameNetwork.BeginModuleEventAsServer(player);
                GameNetwork.WriteMessage(new FactionPollProgress(poll.AcceptedCount, poll.RejectedCount));
                GameNetwork.EndModuleEventAsServer();
            }

            this.UpdatePollProgress(poll.AcceptedCount, poll.RejectedCount);
        }

        private void UpdatePollProgress(int accepted, int rejected)
        {
            this.OnPollUpdate?.Invoke(accepted, rejected);
        }

        public void OnLordPollClosedOnServer(FactionPoll poll)
        {
            bool accepted = poll.GotEnoughAcceptVotesToEnd();
            if (poll.GotEnoughRejectVotesToEnd() || !poll.TargetPlayer.IsConnectionActive)
            {
                accepted = false;
            }

            GameNetwork.BeginBroadcastModuleEvent();
            GameNetwork.WriteMessage(new FactionLordPollClosed(poll.TargetPlayer, accepted, poll.FactionIndex));
            GameNetwork.EndBroadcastModuleEvent(GameNetwork.EventBroadcastFlags.None, null);

            poll.Close();
            this.CloseLordPoll(accepted, poll.TargetPlayer, poll.FactionIndex);

            if (accepted)
            {
                Faction faction = poll.TargetPlayer.GetComponent<PersistentEmpireRepresentative>().GetFaction();
                faction.pollUnlockedAt = DateTimeOffset.UtcNow.ToUnixTimeSeconds() + (10 * 60);
                this._factionsBehavior.SetFactionLord(poll.TargetPlayer, poll.FactionIndex);
            }
        }

        private void CloseLordPoll(bool accepted, NetworkCommunicator targetPeer, int factionIndex)
        {
            if (this._ongoingPolls.ContainsKey(factionIndex))
            {
                this._ongoingPolls[factionIndex].Close();
                this._ongoingPolls.Remove(factionIndex);
            }
            this.OnPollClosed?.Invoke(accepted, targetPeer);
        }
    }
}

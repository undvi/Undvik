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

        private int BaseLordPollGold = 1000;
        private int BaseLordPollCooldown = 86400; // 24h
        private int BaseLordPollTimeOut = 60; // 60 Sek

        public override void OnBehaviorInitialize()
        {
            base.OnBehaviorInitialize();
            _ongoingPolls = new Dictionary<int, FactionPoll>();
            _informationComponent = base.Mission.GetMissionBehavior<InformationComponent>();
            _factionsBehavior = base.Mission.GetMissionBehavior<FactionsBehavior>();
            this.AddRemoveMessageHandlers(GameNetwork.NetworkMessageHandlerRegisterer.RegisterMode.Add);
        }

        public override void OnMissionTick(float dt)
        {
            base.OnMissionTick(dt);
            if (_ongoingPolls == null) return;

            foreach (FactionPoll poll in _ongoingPolls.Values.ToList())
            {
                if (poll.IsOpen)
                {
                    poll.Tick();
                }
            }
        }

        public void OpenLordPollServer(NetworkCommunicator pollCreatorPeer, NetworkCommunicator targetPeer)
        {
            if (pollCreatorPeer == null || targetPeer == null) return;
            if (!pollCreatorPeer.IsConnectionActive || !targetPeer.IsConnectionActive)
            {
                _informationComponent.SendAnnouncementToPlayer("Target player not found", pollCreatorPeer);
                return;
            }

            PersistentEmpireRepresentative creatorRep = pollCreatorPeer.GetComponent<PersistentEmpireRepresentative>();
            PersistentEmpireRepresentative targetRep = targetPeer.GetComponent<PersistentEmpireRepresentative>();
            if (creatorRep == null || targetRep == null || creatorRep.GetFaction() == null || targetRep.GetFaction() == null)
                return;

            Faction faction = targetRep.GetFaction();
            int factionIndex = targetRep.GetFactionIndex();

            if (faction.lordId != creatorRep.PlayerId && !faction.marshalls.Contains(creatorRep.PlayerId))
            {
                _informationComponent.SendAnnouncementToPlayer("Only the Lord or a Marshall can start an election!", pollCreatorPeer);
                return;
            }

            if (faction.pollUnlockedAt > DateTimeOffset.UtcNow.ToUnixTimeSeconds())
            {
                _informationComponent.SendAnnouncementToPlayer("Your faction must wait before starting another election!", pollCreatorPeer);
                return;
            }

            if (creatorRep.GetFaction().Rank < 2)
            {
                _informationComponent.SendAnnouncementToPlayer("You must have at least Rank 2 to start an election!", pollCreatorPeer);
                return;
            }

            if (targetRep.GetFaction().Rank < 2)
            {
                _informationComponent.SendAnnouncementToPlayer("Your candidate must have at least Rank 2!", pollCreatorPeer);
                return;
            }

            if (creatorRep.GetFactionIndex() != factionIndex)
            {
                _informationComponent.SendAnnouncementToPlayer("Your candidate is not in the same faction as you", pollCreatorPeer);
                return;
            }

            if (_ongoingPolls.ContainsKey(factionIndex) && _ongoingPolls[factionIndex].IsOpen)
            {
                _informationComponent.SendAnnouncementToPlayer("There is already an ongoing poll", pollCreatorPeer);
                return;
            }

            int requiredGold = GetDynamicPollGoldCost(faction);
            if (!creatorRep.ReduceIfHaveEnoughGold(requiredGold))
            {
                _informationComponent.SendMessage($"You need {requiredGold} dinars to start a poll", 0xFF0000FF, pollCreatorPeer);
                return;
            }

            StartLordPoll(targetPeer, pollCreatorPeer);
        }

        private void StartLordPoll(NetworkCommunicator targetPeer, NetworkCommunicator pollCreatorPeer)
        {
            PersistentEmpireRepresentative targetRep = targetPeer.GetComponent<PersistentEmpireRepresentative>();
            _ongoingPolls[targetRep.GetFactionIndex()] = new FactionPoll(
                FactionPoll.Type.Lord,
                targetRep.GetFactionIndex(),
                targetRep.GetFaction(),
                targetPeer
            );

            foreach (NetworkCommunicator player in _ongoingPolls[targetRep.GetFactionIndex()].ParticipantsToVote)
            {
                GameNetwork.BeginModuleEventAsServer(player);
                GameNetwork.WriteMessage(new FactionLordPollOpened(pollCreatorPeer, targetPeer));
                GameNetwork.EndModuleEventAsServer();
            }

            _informationComponent.SendAnnouncementToFaction(targetRep.GetFactionIndex(), $"Election started! Candidate: {targetPeer.Name}");
            InformationManager.DisplayMessage(new InformationMessage($"[Election] {pollCreatorPeer.Name} started a poll for {targetPeer.Name}."));
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
            CloseLordPoll(accepted, poll.TargetPlayer, poll.FactionIndex);

            if (accepted)
            {
                Faction faction = poll.TargetPlayer.GetComponent<PersistentEmpireRepresentative>().GetFaction();
                faction.pollUnlockedAt = DateTimeOffset.UtcNow.ToUnixTimeSeconds() + GetDynamicPollCooldown(faction);
                _factionsBehavior.SetFactionLord(poll.TargetPlayer, poll.FactionIndex);

                NotifyVassalsAboutNewLord(faction);

                _informationComponent.SendAnnouncementToFaction(faction.FactionIndex, $"New Lord elected: {poll.TargetPlayer.Name}");
                InformationManager.DisplayMessage(new InformationMessage($"[Election] New Lord elected: {poll.TargetPlayer.Name}"));
            }
        }

        private void CloseLordPoll(bool accepted, NetworkCommunicator targetPeer, int factionIndex)
        {
            if (_ongoingPolls.ContainsKey(factionIndex))
            {
                _ongoingPolls[factionIndex].Close();
                _ongoingPolls.Remove(factionIndex);
            }
            this.OnPollClosed?.Invoke(accepted, targetPeer);
        }

        private int GetDynamicPollGoldCost(Faction faction)
        {
            return BaseLordPollGold + (faction.MemberCount * 50); // Je mehr Mitglieder, desto teurer
        }

        private int GetDynamicPollCooldown(Faction faction)
        {
            return BaseLordPollCooldown + (faction.MemberCount * 600); // Je größer, desto längerer Cooldown
        }

        private void NotifyVassalsAboutNewLord(Faction faction)
        {
            if (faction.Vassals.Count > 0)
            {
                foreach (var vassal in faction.Vassals)
                {
                    _informationComponent.SendAnnouncementToFaction(vassal.FactionIndex, $"⚔️ Your liege has changed! {faction.name} now ruled by {faction.LordName}.");
                }
            }
        }
    }
}

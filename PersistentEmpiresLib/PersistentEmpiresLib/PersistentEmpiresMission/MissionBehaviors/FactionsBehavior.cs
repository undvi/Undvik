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

        private int LordPollRequiredGold = 1000;
        private int LordPollTimeOut = 60;
        private int LordPollCooldown = 86400; // 24h in Sekunden

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

            // 🔹 Überprüfen, ob die Fraktion bereits eine Wahl hatte (Cooldown 24h)
            if (faction.pollUnlockedAt > DateTimeOffset.UtcNow.ToUnixTimeSeconds())
            {
                this._informationComponent.SendAnnouncementToPlayer("Your faction must wait before starting another election!", pollCreatorPeer);
                return;
            }

            // 🔹 Mindest-Rang-Anforderung für den Wahlstarter
            if (creatorRep.GetFaction().Rank < 2)
            {
                this._informationComponent.SendAnnouncementToPlayer("You must have at least Rank 2 to start an election!", pollCreatorPeer);
                return;
            }

            // 🔹 Mindest-Rang-Anforderung für den Kandidaten
            if (targetRep.GetFaction().Rank < 2)
            {
                this._informationComponent.SendAnnouncementToPlayer("Your candidate must have at least Rank 2!", pollCreatorPeer);
                return;
            }

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

            // 🔹 UI-Update für Fraktionsmitglieder
            this._informationComponent.SendAnnouncementToFaction(targetRep.GetFactionIndex(), $"Election started! Candidate: {targetRep.GetFaction().name}");
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
                faction.pollUnlockedAt = DateTimeOffset.UtcNow.ToUnixTimeSeconds() + LordPollCooldown; // 🔹 Cooldown von 24h setzen
                this._factionsBehavior.SetFactionLord(poll.TargetPlayer, poll.FactionIndex);

                // 🔹 UI-Update für Fraktionsmitglieder
                this._informationComponent.SendAnnouncementToFaction(faction.FactionIndex, $"New Lord elected: {poll.TargetPlayer.Name}");
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

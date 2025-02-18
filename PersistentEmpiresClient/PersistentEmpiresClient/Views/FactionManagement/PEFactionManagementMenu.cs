using System;
using System.Collections.Generic;
using PersistentEmpires.Views.ViewsVM.FactionManagement;
using PersistentEmpiresLib;
using PersistentEmpiresLib.NetworkMessages.Client;
using TaleWorlds.MountAndBlade;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade.View.MissionViews;

namespace PersistentEmpires.Views.Views.FactionManagement
{
    public class PEFactionManagementMenu : MissionView
    {
        private PEFactionManagementMenuVM _dataSource;
        private FactionUIComponent _factionManagementComponent;
        private Dictionary<int, long> _lastActionTimestamps = new Dictionary<int, long>();
        private const int DiplomacyCooldown = 3600; // 1 Stunde (in Sekunden)

        public override void OnMissionScreenInitialize()
        {
            base.OnMissionScreenInitialize();
            this._factionManagementComponent = base.Mission.GetMissionBehavior<FactionUIComponent>();
            this._factionManagementComponent.OnManagementMenuClick += this.OnOpen;

            this._dataSource = new PEFactionManagementMenuVM(
                (Faction faction, string action) =>
                {
                    if (!CanPerformAction(faction, action))
                    {
                        return;
                    }

                    GameNetwork.BeginModuleEventAsClient();
                    GameNetwork.WriteMessage(new DeclareFactionActionRequest(faction.FactionIndex, action));
                    GameNetwork.EndModuleEventAsClient();

                    _lastActionTimestamps[faction.FactionIndex] = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                }
            );
        }

        private bool CanPerformAction(Faction faction, string action)
        {
            PersistentEmpireRepresentative rep = GameNetwork.MyPeer.GetComponent<PersistentEmpireRepresentative>();
            if (rep == null || rep.GetFaction() == null) return false;

            Faction playerFaction = rep.GetFaction();
            string playerId = GameNetwork.MyPeer.VirtualPlayer.ToPlayerId();

            // ✅ Nur Lord oder Marshall darf Verwaltung betreiben
            if (playerFaction.lordId != playerId && !playerFaction.marshalls.Contains(playerId))
            {
                InformationManager.DisplayMessage(new InformationMessage("Only Lords or Marshalls can manage the faction."));
                return false;
            }

            // ✅ Cooldown prüfen
            if (_lastActionTimestamps.TryGetValue(playerFaction.FactionIndex, out long lastTime) &&
                DateTimeOffset.UtcNow.ToUnixTimeSeconds() - lastTime < DiplomacyCooldown)
            {
                InformationManager.DisplayMessage(new InformationMessage("You must wait before performing another action."));
                return false;
            }

            // ✅ **Rang-Prüfung für Verwaltung**
            if (action == "UpgradeRank" && playerFaction.Rank < 2)
            {
                InformationManager.DisplayMessage(new InformationMessage("You need at least Rank 2 to upgrade the faction rank."));
                return false;
            }
            if (action == "NominateSuccessor" && playerFaction.Rank < 3)
            {
                InformationManager.DisplayMessage(new InformationMessage("You need at least Rank 3 to nominate a successor."));
                return false;
            }
            if (action == "OverthrowLeader" && playerFaction.Rank < 4)
            {
                InformationManager.DisplayMessage(new InformationMessage("You need at least Rank 4 to overthrow the leader."));
                return false;
            }

            return true;
        }

        protected override void OnOpen()
        {
            PersistentEmpireRepresentative rep = GameNetwork.MyPeer.GetComponent<PersistentEmpireRepresentative>();
            if (rep == null || rep.GetFaction() == null) return;

            this._dataSource.RefreshManagementOptions(rep.GetFaction());
            base.OnOpen();
        }
    }
}

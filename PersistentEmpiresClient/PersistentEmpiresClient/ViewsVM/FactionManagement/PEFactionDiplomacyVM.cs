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
    public class PEFactionDiplomacyMenu : MissionView
    {
        private PEFactionDiplomacyVM _dataSource;
        private FactionUIComponent _factionManagementComponent;
        private Dictionary<int, long> _lastActionTimestamps = new Dictionary<int, long>();
        private const int DiplomacyCooldown = 3600; // 1 Stunde (in Sekunden)

        public override void OnMissionScreenInitialize()
        {
            base.OnMissionScreenInitialize();
            this._factionManagementComponent = base.Mission.GetMissionBehavior<FactionUIComponent>();
            this._factionManagementComponent.OnDiplomacyMenuClick += this.OnOpen;

            this._dataSource = new PEFactionDiplomacyVM(
                (Faction targetFaction, DeclareDiplomacyRequest.DiplomacyType action) =>
                {
                    if (!CanPerformAction())
                    {
                        InformationManager.DisplayMessage(new InformationMessage("You must wait before performing another diplomatic action."));
                        return;
                    }

                    GameNetwork.BeginModuleEventAsClient();
                    GameNetwork.WriteMessage(new DeclareDiplomacyRequest(targetFaction.FactionIndex, action));
                    GameNetwork.EndModuleEventAsClient();

                    _lastActionTimestamps[targetFaction.FactionIndex] = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                }
            );
        }

        private bool CanPerformAction()
        {
            PersistentEmpireRepresentative rep = GameNetwork.MyPeer.GetComponent<PersistentEmpireRepresentative>();
            if (rep == null || rep.GetFaction() == null) return false;

            Faction faction = rep.GetFaction();
            string playerId = GameNetwork.MyPeer.VirtualPlayer.ToPlayerId();

            if (faction.lordId != playerId && !faction.marshalls.Contains(playerId))
            {
                InformationManager.DisplayMessage(new InformationMessage("Only Lords or Marshalls can perform diplomatic actions."));
                return false;
            }

            if (_lastActionTimestamps.TryGetValue(faction.FactionIndex, out long lastTime) &&
                DateTimeOffset.UtcNow.ToUnixTimeSeconds() - lastTime < DiplomacyCooldown)
            {
                return false;
            }

            return true;
        }

        protected override void OnOpen()
        {
            PersistentEmpireRepresentative rep = GameNetwork.MyPeer.GetComponent<PersistentEmpireRepresentative>();
            if (rep == null || rep.GetFaction() == null) return;

            this._dataSource.RefreshDiplomacyOptions(rep.GetFaction());
            base.OnOpen();
        }
    }

    // Erweiterung: Faction Ränge hinzufügen
    public static class FactionRankSystem
    {
        private static Dictionary<int, int> _rankUpgradeCosts = new Dictionary<int, int>
        {
            {1, 100000},  // Rang 1 → 2 kostet 100000 Gold
            {2, 200000},  // Rang 2 → 3 kostet 200000 Gold
            {3, 300000},  // Rang 3 → 4 kostet 300000 Gold
            {4, 500000},  // Rang 4 → 5 kostet 500000 Gold
        };

        private static Dictionary<int, int> _maxFactionMembers = new Dictionary<int, int>
        {
            {1, 20},  // Rang 1: Max 20 Mitglieder
            {2, 30},  // Rang 2: Max 30 Mitglieder
            {3, 40},  // Rang 3: Max 50 Mitglieder + 1 Land
            {4, 60},  // Rang 4: Max 60 Mitglieder
            {5, 80},  // Rang 5: Max 80 Mitglieder + 1 weiteres Land
        };

        public static bool CanUpgradeFaction(Faction faction)
        {
            return _rankUpgradeCosts.ContainsKey(faction.Rank) && faction.Gold >= _rankUpgradeCosts[faction.Rank];
        }

        public static void UpgradeFactionRank(Faction faction)
        {
            if (!CanUpgradeFaction(faction)) return;

            faction.Gold -= _rankUpgradeCosts[faction.Rank];
            faction.Rank++;
            faction.MaxMembers = _maxFactionMembers[faction.Rank];

            InformationManager.DisplayMessage(new InformationMessage($"Faction {faction.name} has been upgraded to Rank {faction.Rank} with max {faction.MaxMembers} members!"));
        }
    }
}

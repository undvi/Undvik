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
                    if (!CanPerformAction(targetFaction, action))
                    {
                        return;
                    }

                    GameNetwork.BeginModuleEventAsClient();
                    GameNetwork.WriteMessage(new DeclareDiplomacyRequest(targetFaction.FactionIndex, action));
                    GameNetwork.EndModuleEventAsClient();

                    _lastActionTimestamps[targetFaction.FactionIndex] = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                }
            );
        }

        private bool CanPerformAction(Faction targetFaction, DeclareDiplomacyRequest.DiplomacyType action)
        {
            PersistentEmpireRepresentative rep = GameNetwork.MyPeer.GetComponent<PersistentEmpireRepresentative>();
            if (rep == null || rep.GetFaction() == null) return false;

            Faction faction = rep.GetFaction();
            string playerId = GameNetwork.MyPeer.VirtualPlayer.ToPlayerId();

            // ✅ Nur Lord oder Marshall darf Diplomatie betreiben
            if (faction.lordId != playerId && !faction.marshalls.Contains(playerId))
            {
                InformationManager.DisplayMessage(new InformationMessage("Only Lords or Marshalls can perform diplomatic actions."));
                return false;
            }

            // ✅ Cooldown prüfen
            if (_lastActionTimestamps.TryGetValue(faction.FactionIndex, out long lastTime) &&
                DateTimeOffset.UtcNow.ToUnixTimeSeconds() - lastTime < DiplomacyCooldown)
            {
                InformationManager.DisplayMessage(new InformationMessage("You must wait before performing another diplomatic action."));
                return false;
            }

            // ✅ **Rang-Prüfung für Diplomatie**
            if (action == DeclareDiplomacyRequest.DiplomacyType.Vassal && faction.Rank < 2)
            {
                InformationManager.DisplayMessage(new InformationMessage("You need at least Rank 2 to offer vassalage."));
                return false;
            }
            if (action == DeclareDiplomacyRequest.DiplomacyType.DeclareWar && faction.Rank < 2)
            {
                InformationManager.DisplayMessage(new InformationMessage("You need at least Rank 2 to declare war."));
                return false;
            }
            if (action == DeclareDiplomacyRequest.DiplomacyType.Alliance && faction.Rank < 3)
            {
                InformationManager.DisplayMessage(new InformationMessage("You need at least Rank 3 to form alliances."));
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
}


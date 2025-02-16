using PersistentEmpires.Views.ViewsVM.FactionManagement;
using PersistentEmpires.Views.ViewsVM.PETabMenu;
using PersistentEmpiresLib;
using TaleWorlds.MountAndBlade;
using System;
using System.Collections.Generic;

namespace PersistentEmpires.Views.Views.FactionManagement
{
    public class PEFactionDiplomacy : PEMenuItem
    {
        private Dictionary<int, long> _lastActionTimestamps = new Dictionary<int, long>();
        private const int DiplomacyCooldown = 3600; // 1 Stunde Cooldown

        public PEFactionDiplomacy() : base("PEFactionDiplomacy")
        {
        }

        public override void OnMissionScreenInitialize()
        {
            base.OnMissionScreenInitialize();
            this._factionManagementComponent.OnDiplomacyMenuClick += this.OnOpen;
            this._factionsBehavior.OnFactionMakePeace += this.OnFactionDeclaredAnything;
            this._factionsBehavior.OnFactionDeclaredWar += this.OnFactionDeclaredAnything;
            this._factionsBehavior.OnFactionTradeAgreement += this.OnFactionDeclaredAnything;
            this._factionsBehavior.OnFactionAlliance += this.OnFactionDeclaredAnything;
            this._factionsBehavior.OnFactionVassalage += this.OnFactionDeclaredAnything;

            this._dataSource = new PEFactionDiplomacyVM(this._factionsBehavior.Factions,

                // Krieg erklären
                (TabFactionVM faction) =>
                {
                    if (CanPerformDiplomaticAction(faction.FactionIndex, DeclareDiplomacyRequest.DiplomacyType.DeclareWar))
                    {
                        this._factionsBehavior.RequestDeclareWar(this._factionsBehavior.Factions[faction.FactionIndex], faction.FactionIndex);
                        RegisterDiplomaticAction(faction.FactionIndex);
                    }
                },

                // Frieden schließen
                (TabFactionVM faction) =>
                {
                    if (CanPerformDiplomaticAction(faction.FactionIndex, DeclareDiplomacyRequest.DiplomacyType.MakePeace))
                    {
                        this._factionsBehavior.RequestMakePeace(this._factionsBehavior.Factions[faction.FactionIndex], faction.FactionIndex);
                        RegisterDiplomaticAction(faction.FactionIndex);
                    }
                },

                // Handelsabkommen abschließen
                (TabFactionVM faction) =>
                {
                    if (CanPerformDiplomaticAction(faction.FactionIndex, DeclareDiplomacyRequest.DiplomacyType.TradeAgreement))
                    {
                        this._factionsBehavior.RequestTradeAgreement(this._factionsBehavior.Factions[faction.FactionIndex], faction.FactionIndex);
                        RegisterDiplomaticAction(faction.FactionIndex);
                    }
                },

                // Allianz bilden
                (TabFactionVM faction) =>
                {
                    if (CanPerformDiplomaticAction(faction.FactionIndex, DeclareDiplomacyRequest.DiplomacyType.Alliance))
                    {
                        this._factionsBehavior.RequestFormAlliance(this._factionsBehavior.Factions[faction.FactionIndex], faction.FactionIndex);
                        RegisterDiplomaticAction(faction.FactionIndex);
                    }
                },

                // Vasallenschaft anbieten
                (TabFactionVM faction) =>
                {
                    if (CanPerformDiplomaticAction(faction.FactionIndex, DeclareDiplomacyRequest.DiplomacyType.Vassal))
                    {
                        this._factionsBehavior.RequestOfferVassalage(this._factionsBehavior.Factions[faction.FactionIndex], faction.FactionIndex);
                        RegisterDiplomaticAction(faction.FactionIndex);
                    }
                }
            );
        }

        private bool CanPerformDiplomaticAction(int factionIndex, DeclareDiplomacyRequest.DiplomacyType action)
        {
            PersistentEmpireRepresentative rep = GameNetwork.MyPeer.GetComponent<PersistentEmpireRepresentative>();
            if (rep == null || rep.GetFaction() == null) return false;

            Faction faction = rep.GetFaction();
            string playerId = GameNetwork.MyPeer.VirtualPlayer.ToPlayerId();

            // Nur Lord oder Marschall kann Diplomatie betreiben
            if (faction.lordId != playerId && !faction.marshalls.Contains(playerId))
            {
                InformationManager.DisplayMessage(new InformationMessage("Only Lords or Marshalls can perform diplomatic actions."));
                return false;
            }

            // Cooldown prüfen
            if (_lastActionTimestamps.TryGetValue(factionIndex, out long lastTime) &&
                DateTimeOffset.UtcNow.ToUnixTimeSeconds() - lastTime < DiplomacyCooldown)
            {
                InformationManager.DisplayMessage(new InformationMessage("You must wait before performing another diplomatic action."));
                return false;
            }

            // Ranganforderungen prüfen
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

        private void RegisterDiplomaticAction(int factionIndex)
        {
            _lastActionTimestamps[factionIndex] = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        }

        private void OnFactionDeclaredAnything(int declarer, int declaredTo)
        {
            PersistentEmpireRepresentative persistentEmpireRepresentative = GameNetwork.MyPeer.GetComponent<PersistentEmpireRepresentative>();
            if (persistentEmpireRepresentative == null) return;
            if (declarer == persistentEmpireRepresentative.GetFactionIndex())
            {
                ((PEFactionDiplomacyVM)this._dataSource).RefreshValues(
                    this._factionsBehavior.Factions,
                    persistentEmpireRepresentative.GetFaction(),
                    persistentEmpireRepresentative.GetFactionIndex()
                );
            }
        }

        protected override void OnOpen()
        {
            PersistentEmpireRepresentative persistentEmpireRepresentative = GameNetwork.MyPeer.GetComponent<PersistentEmpireRepresentative>();
            ((PEFactionDiplomacyVM)this._dataSource).RefreshValues(
                this._factionsBehavior.Factions,
                persistentEmpireRepresentative.GetFaction(),
                persistentEmpireRepresentative.GetFactionIndex()
            );
            base.OnOpen();
        }
    }
}

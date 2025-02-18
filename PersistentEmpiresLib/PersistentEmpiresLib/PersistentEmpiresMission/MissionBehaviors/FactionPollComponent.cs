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
        private int TaxCollectionInterval = 300; // Alle 5 Minuten Steuern sammeln
        private Dictionary<int, float> FactionTaxes = new Dictionary<int, float>(); // Steuersätze pro Fraktion
        private Dictionary<int, int> FactionRanks = new Dictionary<int, int>(); // Fraktionsränge basierend auf Mitgliedszahlen
        private Dictionary<int, List<string>> FactionExportOrders = new Dictionary<int, List<string>>(); // Export-Aufträge
        private Dictionary<int, string> FactionRegionBonuses = new Dictionary<int, string>(); // Gebietsboni
        private Dictionary<int, int> FactionVassals = new Dictionary<int, int>(); // Vasallenbeziehungen

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

        public void SetFactionTax(int factionIndex, float taxRate)
        {
            if (taxRate < 0f || taxRate > 0.3f) return;
            FactionTaxes[factionIndex] = taxRate;
        }

        public void CollectFactionTaxes()
        {
            foreach (var faction in _factionsBehavior.GetAllFactions())
            {
                if (FactionTaxes.ContainsKey(faction.FactionIndex))
                {
                    float taxRate = FactionTaxes[faction.FactionIndex];
                    foreach (var member in faction.Members)
                    {
                        member.ReduceGold(member.Gold * taxRate);
                        faction.AddGold(member.Gold * taxRate);
                    }
                }
            }
        }

        public void GenerateFactionExportOrders()
        {
            foreach (var faction in _factionsBehavior.GetAllFactions())
            {
                if (!FactionExportOrders.ContainsKey(faction.FactionIndex))
                {
                    FactionExportOrders[faction.FactionIndex] = new List<string>();
                }
                FactionExportOrders[faction.FactionIndex].Add("Export Holz nach Stadt X");
                FactionExportOrders[faction.FactionIndex].Add("Lieferung von Erz an Schmiede");
            }
        }

        public void AssignFactionRegionBonus(int factionIndex, string bonus)
        {
            if (!FactionRegionBonuses.ContainsKey(factionIndex))
            {
                FactionRegionBonuses[factionIndex] = bonus;
            }
        }

        public void SetFactionVassal(int overlordFaction, int vassalFaction)
        {
            if (!FactionVassals.ContainsKey(vassalFaction))
            {
                FactionVassals[vassalFaction] = overlordFaction;
            }
        }

        public void UpgradeFactionRank(int factionIndex)
        {
            if (!FactionRanks.ContainsKey(factionIndex))
            {
                FactionRanks[factionIndex] = 1;
            }
            else if (FactionRanks[factionIndex] < 5)
            {
                FactionRanks[factionIndex]++;
            }
        }
    }
}

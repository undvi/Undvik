using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;
using TaleWorlds.ObjectSystem;

namespace PersistentEmpiresLib.Factions
{
    public class Faction
    {
        public BasicCultureObject basicCultureObject { get; private set; }
        public string name { get; set; }
        public List<NetworkCommunicator> members = new List<NetworkCommunicator>();

        public string lordId { get; set; } // ID des Lords
        public Team team { get; set; }
        public Banner banner { get; set; }

        // Verwaltung von Zugangsrechten
        public List<string> doorManagers { get; set; }
        public List<string> chestManagers { get; set; }
        public List<string> marshalls { get; set; }

        // Diplomatie & Vasallen
        public List<int> warDeclaredTo { get; set; }
        public List<int> tradeAgreements { get; set; }
        public List<int> alliances { get; set; }
        public Dictionary<int, string> vassals { get; set; }

        public long pollUnlockedAt { get; set; }

        // 🔹 Wirtschaftssystem
        public int Gold { get; set; } = 0;
        public int Influence { get; set; } = 0;
        public int TaxRate { get; set; } = 10; // Standard-Steuersatz
        public Dictionary<string, int> ResourceBonuses { get; set; } = new Dictionary<string, int>();
        public int ExportBonus { get; set; } = 0;

        // 🔹 Fraktionsränge & Mitgliederverwaltung
        public int Rank { get; set; } = 1;
        public int MaxMembers { get; set; } = 10;

        private static Dictionary<int, int> _rankUpgradeCosts = new Dictionary<int, int>
        {
            {1, 100000}, {2, 200000}, {3, 300000}, {4, 500000}
        };

        private static Dictionary<int, int> _maxFactionMembers = new Dictionary<int, int>
        {
            {1, 20}, {2, 30}, {3, 50}, {4, 60}, {5, 80}
        };

        // 🔹 Wachen / Soldaten (Zukünftige KI-Verteidigung)
        public List<string> AI_Guards { get; set; } = new List<string>();

        // -------------------------------
        //      FUNKTIONEN
        // -------------------------------

        public bool CanUpgradeFaction() => _rankUpgradeCosts.ContainsKey(this.Rank) && this.Gold >= _rankUpgradeCosts[this.Rank];

        public void UpgradeFactionRank()
        {
            if (!CanUpgradeFaction()) return;
            this.Gold -= _rankUpgradeCosts[this.Rank];
            this.Rank++;
            this.MaxMembers = _maxFactionMembers[this.Rank];

            InformationManager.DisplayMessage(new InformationMessage($"Faction {this.name} upgraded to Rank {this.Rank}! Max Members: {this.MaxMembers}"));
        }

        // -------------------------------
        //      STEUER- & EXPORTSYSTEM
        // -------------------------------

        public int CalculateTaxIncome()
        {
            int totalTax = (this.members.Count * this.TaxRate);
            this.Gold += totalTax;
            return totalTax;
        }

        public int CalculateExportEarnings(int tradeValue)
        {
            int earnings = tradeValue + (tradeValue * this.ExportBonus / 100);
            this.Gold += earnings;
            return earnings;
        }

        // -------------------------------
        //      DIPLOMATIE-MANAGEMENT
        // -------------------------------

        public bool IsAtWarWith(int factionId) => this.warDeclaredTo.Contains(factionId);
        public bool HasTradeAgreementWith(int factionId) => this.tradeAgreements.Contains(factionId);
        public bool IsAlliedWith(int factionId) => this.alliances.Contains(factionId);
        public bool IsVassalOf(int factionId) => this.vassals.ContainsKey(factionId);

        public void DeclareWar(int factionId)
        {
            if (!this.warDeclaredTo.Contains(factionId))
                this.warDeclaredTo.Add(factionId);
        }

        public void MakePeace(int factionId)
        {
            this.warDeclaredTo.Remove(factionId);
        }

        public void SignTradeAgreement(int factionId)
        {
            if (!this.tradeAgreements.Contains(factionId))
                this.tradeAgreements.Add(factionId);
        }

        public void BreakTradeAgreement(int factionId)
        {
            this.tradeAgreements.Remove(factionId);
        }

        public void FormAlliance(int factionId)
        {
            if (!this.alliances.Contains(factionId))
                this.alliances.Add(factionId);
        }

        public void BreakAlliance(int factionId)
        {
            this.alliances.Remove(factionId);
        }

        public void OfferVassalage(int factionId, string lord)
        {
            if (!this.vassals.ContainsKey(factionId))
                this.vassals[factionId] = lord;
        }

        public void RevokeVassalage(int factionId)
        {
            this.vassals.Remove(factionId);
        }

        // -------------------------------
        //      KONSTRUKTOREN
        // -------------------------------

        public Faction(BasicCultureObject basicCultureObject, Banner banner, string name)
        {
            this.basicCultureObject = basicCultureObject;
            this.name = name;
            this.banner = banner;
            this.lordId = "0";
            this.doorManagers = new List<string>();
            this.chestManagers = new List<string>();
            this.warDeclaredTo = new List<int>();
            this.tradeAgreements = new List<int>();
            this.alliances = new List<int>();
            this.vassals = new Dictionary<int, string>();
            this.marshalls = new List<string>();
            this.Rank = 1;
            this.Gold = 0;
            this.MaxMembers = 10;
        }

        public Faction(Banner banner, string name)
        {
            this.basicCultureObject = MBObjectManager.Instance.GetObject<BasicCultureObject>("empire");
            this.name = name;
            this.banner = banner;
            this.lordId = "0";
            this.doorManagers = new List<string>();
            this.chestManagers = new List<string>();
            this.warDeclaredTo = new List<int>();
            this.tradeAgreements = new List<int>();
            this.alliances = new List<int>();
            this.vassals = new Dictionary<int, string>();
            this.marshalls = new List<string>();
            this.Rank = 1;
            this.Gold = 0;
            this.MaxMembers = 10;
        }
    }
}

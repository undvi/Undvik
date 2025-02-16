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

        // ID des Lords
        public string lordId { get; set; }

        public Team team { get; set; }
        public Banner banner { get; set; }

        // Verwaltung von Zugangsrechten
        public List<string> doorManagers { get; set; }
        public List<string> chestManagers { get; set; }
        public List<string> marshalls { get; set; }

        // Diplomatische Beziehungen
        public List<int> warDeclaredTo { get; set; }
        public List<int> tradeAgreements { get; set; }
        public List<int> alliances { get; set; }
        public Dictionary<int, string> vassals { get; set; }

        public long pollUnlockedAt { get; set; }

        // Fraktionsrang-System
        public int Rank { get; set; } = 1;  // Standardmäßig auf Rang 1
        public int Gold { get; set; } = 0;  // Startkapital
        public int MaxMembers { get; set; } = 10;  // Standard-Mitgliedergrenze

        // -------------------------------
        //      RANK MANAGEMENT
        // -------------------------------

        private static Dictionary<int, int> _rankUpgradeCosts = new Dictionary<int, int>
        {
            {1, 100000},  // Rang 1 → 2 kostet 100000 Gold
            {2, 200000},  // Rang 2 → 3 kostet 200000 Gold
            {3, 300000},  // Rang 3 → 4 kostet 300000 Gold
            {4, 500000},  // Rang 4 → 5 kostet 500000 Gold
        };

        private static Dictionary<int, int> _maxFactionMembers = new Dictionary<int, int>
        {
            {1, 20},  // Rang 1: Max 10 Mitglieder
            {2, 30},  // Rang 2: Max 20 Mitglieder
            {3, 50},  // Rang 3: Max 50 Mitglieder + 1 Land
            {4, 60},  // Rang 4: Max 60 Mitglieder
            {5, 80},  // Rang 5: Max 80 Mitglieder + 1 weiteres Land
        };

        public bool CanUpgradeFaction()
        {
            return _rankUpgradeCosts.ContainsKey(this.Rank) && this.Gold >= _rankUpgradeCosts[this.Rank];
        }

        public void UpgradeFactionRank()
        {
            if (!CanUpgradeFaction()) return;

            this.Gold -= _rankUpgradeCosts[this.Rank];
            this.Rank++;
            this.MaxMembers = _maxFactionMembers[this.Rank];

            InformationManager.DisplayMessage(new InformationMessage($"Faction {this.name} has been upgraded to Rank {this.Rank}! Max Members: {this.MaxMembers}"));
        }

        // -------------------------------
        //      SERIALIZATION METHODS
        // -------------------------------

        public string SerializeMarshalls()
        {
            return string.Join("|", this.marshalls);
        }

        public void LoadMarshallsFromSerialized(string serialized)
        {
            this.marshalls = serialized?.Split('|').ToList() ?? new List<string>();
        }

        public string SerializeTradeAgreements()
        {
            return string.Join("|", this.tradeAgreements);
        }

        public void LoadTradeAgreementsFromSerialized(string serialized)
        {
            this.tradeAgreements = serialized?.Split('|').Select(int.Parse).ToList() ?? new List<int>();
        }

        public string SerializeAlliances()
        {
            return string.Join("|", this.alliances);
        }

        public void LoadAlliancesFromSerialized(string serialized)
        {
            this.alliances = serialized?.Split('|').Select(int.Parse).ToList() ?? new List<int>();
        }

        public string SerializeVassals()
        {
            return string.Join(";", this.vassals.Select(x => $"{x.Key}:{x.Value}"));
        }

        public void LoadVassalsFromSerialized(string serialized)
        {
            this.vassals = serialized?.Split(';')
                .Select(x => x.Split(':'))
                .ToDictionary(x => int.Parse(x[0]), x => x[1]) ?? new Dictionary<int, string>();
        }

        // -------------------------------
        //      DIPLOMACY MANAGEMENT
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
        //      CONSTRUCTORS
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

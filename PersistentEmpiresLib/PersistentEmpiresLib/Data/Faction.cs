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
        public int PrestigePoints { get; set; } = 0;
        public int TaxRate { get; set; } = 10; // Standard-Steuersatz
        public Dictionary<string, int> ResourceBonuses { get; set; } = new Dictionary<string, int>();
        public int ExportBonus { get; set; } = 0;

        // 🔹 Fraktionsränge & Mitgliederverwaltung
        public int Rank { get; set; } = 1;
        public int MaxMembers { get; private set; }
        public List<string> SuccessorList { get; set; } = new List<string>();

        private static Dictionary<int, int> _rankUpgradeCosts = new Dictionary<int, int>
        {
            {1, 100000}, {2, 200000}, {3, 300000}, {4, 500000}
        };

        private static Dictionary<int, int> _maxFactionMembers = new Dictionary<int, int>
        {
            {1, 20}, {2, 30}, {3, 50}, {4, 60}, {5, 80}
        };

        // 🔹 Kriegssystem
        public enum WarType { Standard, Handelskrieg, Ueberfall, Eroberung }
        public Dictionary<int, WarType> warTypes { get; set; } = new Dictionary<int, WarType>();

        // 🔹 Wachen / Soldaten (Zukünftige KI-Verteidigung)
        public List<string> AI_Guards { get; set; } = new List<string>();

        // -------------------------------
        //      FUNKTIONEN
        // -------------------------------

        public bool CanUpgradeFaction() => _rankUpgradeCosts.ContainsKey(this.Rank) && this.Gold >= _rankUpgradeCosts[this.Rank] && this.PrestigePoints >= this.Rank * 50;

        public void UpgradeFactionRank()
        {
            if (!CanUpgradeFaction()) return;
            this.Gold -= _rankUpgradeCosts[this.Rank];
            this.PrestigePoints -= this.Rank * 50;
            this.Rank++;
            this.MaxMembers = _maxFactionMembers[this.Rank];

            InformationManager.DisplayMessage(new InformationMessage($"Faction {this.name} upgraded to Rank {this.Rank}! Max Members: {this.MaxMembers}"));
        }

        public void GainPrestige(int amount)
        {
            this.PrestigePoints += amount;
        }

        public bool CanAddMember()
        {
            return this.members.Count < this.MaxMembers;
        }

        public bool AddMember(NetworkCommunicator member)
        {
            if (CanAddMember())
            {
                this.members.Add(member);
                return true;
            }
            return false;
        }

        public void RemoveMember(NetworkCommunicator member)
        {
            this.members.Remove(member);
        }

        public bool CanDeclareWar()
        {
            return this.lordId != "0" || this.marshalls.Contains(this.lordId);
        }

        public void DeclareWar(int factionId, WarType type)
        {
            if (!this.warDeclaredTo.Contains(factionId) && CanDeclareWar())
            {
                this.warDeclaredTo.Add(factionId);
                this.warTypes[factionId] = type;
            }
        }

        public void SelectNewLeader()
        {
            if (this.SuccessorList.Count > 0)
            {
                this.lordId = this.SuccessorList[0];
                this.SuccessorList.RemoveAt(0);
            }
        }

        public void HireGuard(string guardId)
        {
            this.AI_Guards.Add(guardId);
        }

        public void DeployGuards()
        {
            // Logik zur Platzierung von NPC-Wachen
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
            this.MaxMembers = _maxFactionMembers[this.Rank];
        }
    }
}

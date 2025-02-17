using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Network.Messages;
using PersistentEmpiresLib.Systems;
using PersistentEmpiresLib.Factions;

namespace PersistentEmpiresLib.Systems
{
    public class PEBuildingSystem : MissionObject
    {
        private Dictionary<string, List<BuildingZone>> FactionBuildings = new Dictionary<string, List<BuildingZone>>();
        private Dictionary<string, BuildingData> AvailableBuildings = new Dictionary<string, BuildingData>();
        private List<BuildingZone> NeutralZones = new List<BuildingZone>();

        private const int BaseInfluenceCost = 500;
        private const float RefundFactor = 0.5f; // Spieler erhalten 50% der Kosten zurück beim Abriss.

        public void InitializeSystem()
        {
            LoadAvailableBuildings();
            LoadNeutralZones();
        }

        private void LoadAvailableBuildings()
        {
            AvailableBuildings["MarketStall"] = new BuildingData("Market Stall", 200, 50, false, 1, new Dictionary<string, int> { { "Hardwood", 10 }, { "Stone", 5 } });
            AvailableBuildings["Warehouse"] = new BuildingData("Warehouse", 500, 100, false, 1, new Dictionary<string, int> { { "Hardwood", 20 }, { "Stone", 15 }, { "Bretter", 10 } });
            AvailableBuildings["FactionHall"] = new BuildingData("Faction Hall", 1000, 200, true, 2, new Dictionary<string, int> { { "Hardwood", 50 }, { "Stone", 50 }, { "Bretter", 30 }, { "Lehm", 20 }, { "Eisenbarren", 5 } });
            AvailableBuildings["Blacksmith"] = new BuildingData("Blacksmith", 800, 150, false, 2, new Dictionary<string, int> { { "Hardwood", 30 }, { "Stone", 30 }, { "Eisenbarren", 10 } });
        }

        private void LoadNeutralZones()
        {
            NeutralZones.Add(new BuildingZone("Neutral Zone 1", false, 1000));
            NeutralZones.Add(new BuildingZone("Neutral Zone 2", false, 1200));
        }

        public bool CanBuildOnZone(NetworkCommunicator player, string factionName, BuildingZone zone)
        {
            return FactionBuildings.ContainsKey(factionName) && FactionBuildings[factionName].Contains(zone) || NeutralZones.Contains(zone) && zone.IsOwnedBy(player);
        }

        public void BuyNeutralZone(NetworkCommunicator player, BuildingZone zone)
        {
            var playerInventory = player.GetComponent<PlayerInventory>();

            if (!NeutralZones.Contains(zone) || zone.IsOccupied)
            {
                InformationManager.DisplayMessage(new InformationMessage("⚠️ Fehler: Diese Zone ist nicht verfügbar!"));
                return;
            }

            if (!playerInventory.HasGold(zone.Cost))
            {
                InformationManager.DisplayMessage(new InformationMessage("⚠️ Nicht genug Gold, um diese Zone zu kaufen!"));
                return;
            }

            playerInventory.RemoveGold(zone.Cost);
            zone.SetOwner(player);

            InformationManager.DisplayMessage(new InformationMessage($"✅ {player.UserName} hat die Bauzone {zone.ZoneName} erworben!"));
        }

        public void ConstructBuilding(NetworkCommunicator player, string buildingType, BuildingZone zone)
        {
            var influenceSystem = Mission.Current.GetMissionBehavior<PEInfluenceSystem>();
            var faction = PEFactionManager.GetFactionOfPlayer(player);
            var playerInventory = player.GetComponent<PlayerInventory>();

            if (!AvailableBuildings.TryGetValue(buildingType, out var buildingData))
            {
                InformationManager.DisplayMessage(new InformationMessage("⚠️ Fehler: Ungültiger Gebäudetyp!"));
                return;
            }

            if (buildingData.IsFactionOnly && (faction == null || !CanBuildOnZone(player, faction.Name, zone)))
            {
                InformationManager.DisplayMessage(new InformationMessage("⚠️ Fehler: Nur Fraktionsmitglieder können dieses Gebäude hier bauen!"));
                return;
            }

            // Überprüfe, ob Spieler genug Materialien hat
            foreach (var material in buildingData.MaterialCost)
            {
                if (!playerInventory.HasResource(material.Key, material.Value))
                {
                    InformationManager.DisplayMessage(new InformationMessage($"⚠️ Fehler: Nicht genug {material.Key}!"));
                    return;
                }
            }

            // Materialien & Kosten abziehen
            foreach (var material in buildingData.MaterialCost)
            {
                playerInventory.RemoveResource(material.Key, material.Value);
            }
            playerInventory.RemoveGold(buildingData.GoldCost);
            influenceSystem.RemoveInfluence(player, buildingData.InfluenceCost);

            if (faction != null)
            {
                FactionBuildings.TryAdd(faction.Name, new List<BuildingZone>());
                FactionBuildings[faction.Name].Add(zone);
                faction.AddGold(buildingData.GoldCost / 10); // Fraktion verdient Steuern (10% vom Baupreis)
            }

            zone.IsOccupied = true;

            GameNetwork.BeginBroadcastModuleEvent();
            GameNetwork.WriteMessage(new PEBuildingConstructed(player, buildingType, faction?.Name ?? "Independent"));
            GameNetwork.EndBroadcastModuleEvent(GameNetwork.EventBroadcastFlags.None, null);

            InformationManager.DisplayMessage(new InformationMessage($"✅ {player.UserName} hat {buildingData.Name} gebaut!"));
        }

        public void DemolishBuilding(NetworkCommunicator player, BuildingZone zone)
        {
            if (!zone.IsOccupied || (!NeutralZones.Contains(zone) && !CanBuildOnZone(player, PEFactionManager.GetFactionOfPlayer(player)?.Name, zone)))
            {
                InformationManager.DisplayMessage(new InformationMessage("⚠️ Fehler: Du kannst dieses Gebäude nicht abreißen!"));
                return;
            }

            var buildingData = AvailableBuildings.Values.FirstOrDefault(b => b.Name == zone.OccupiedBuilding);

            if (buildingData == null)
            {
                InformationManager.DisplayMessage(new InformationMessage("⚠️ Fehler: Gebäude-Daten nicht gefunden!"));
                return;
            }

            var playerInventory = player.GetComponent<PlayerInventory>();

            // Rückerstattung von 50% der Baukosten
            playerInventory.AddGold((int)(buildingData.GoldCost * RefundFactor));
            foreach (var material in buildingData.MaterialCost)
            {
                playerInventory.AddResource(material.Key, (int)(material.Value * RefundFactor));
            }

            zone.IsOccupied = false;

            InformationManager.DisplayMessage(new InformationMessage($"⛏️ {player.UserName} hat das Gebäude {buildingData.Name} abgerissen und 50% der Ressourcen zurückerhalten."));
        }
    }

    public class BuildingZone
    {
        public string ZoneName { get; set; }
        public bool IsOccupied { get; set; }
        public int Cost { get; }
        private NetworkCommunicator Owner { get; set; }

        public BuildingZone(string name, bool occupied, int cost)
        {
            ZoneName = name;
            IsOccupied = occupied;
            Cost = cost;
        }

        public bool IsOwnedBy(NetworkCommunicator player) => Owner == player;
        public void SetOwner(NetworkCommunicator player) => Owner = player;
    }

    public class BuildingData
    {
        public string Name { get; }
        public int GoldCost { get; }
        public int InfluenceCost { get; }
        public bool IsFactionOnly { get; }
        public int Tier { get; }
        public Dictionary<string, int> MaterialCost { get; }

        public BuildingData(string name, int goldCost, int influenceCost, bool isFactionOnly, int tier, Dictionary<string, int> materialCost)
        {
            Name = name;
            GoldCost = goldCost;
            InfluenceCost = influenceCost;
            IsFactionOnly = isFactionOnly;
            Tier = tier;
            MaterialCost = materialCost;
        }
    }
}

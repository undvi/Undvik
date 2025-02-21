// File: PEBuildingSystem.cs
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
        private const float RefundFactor = 0.5f;

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

        public void ConstructBuilding(NetworkCommunicator player, string buildingType, BuildingZone zone)
        {
            var playerInventory = player.GetComponent<PlayerInventory>();
            if (!AvailableBuildings.TryGetValue(buildingType, out var buildingData))
            {
                InformationManager.DisplayMessage(new InformationMessage("⚠️ Fehler: Ungültiger Gebäudetyp!"));
                return;
            }

            foreach (var material in buildingData.MaterialCost)
            {
                if (!playerInventory.HasResource(material.Key, material.Value))
                {
                    InformationManager.DisplayMessage(new InformationMessage($"⚠️ Fehler: Nicht genug {material.Key}!"));
                    return;
                }
            }

            foreach (var material in buildingData.MaterialCost)
            {
                playerInventory.RemoveResource(material.Key, material.Value);
            }
            playerInventory.RemoveGold(buildingData.GoldCost);

            zone.IsOccupied = true;
            InformationManager.DisplayMessage(new InformationMessage($"✅ {player.UserName} hat {buildingData.Name} gebaut!"));
        }
    }
}

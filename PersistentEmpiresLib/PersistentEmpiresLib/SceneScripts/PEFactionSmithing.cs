using System;
using System.Collections.Generic;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;
using PersistentEmpiresLib.NetworkMessages.Server;
using PersistentEmpiresLib.Factions;
using PersistentEmpiresLib.Systems;

namespace PersistentEmpiresLib.SceneScripts
{
    public class PEFactionSmithing : MissionObject
    {
        public int FactionIndex { get; private set; } = 2;
        public string FactionName { get; private set; } = "Unbenannte Fraktion";
        private List<string> BlueprintSlots = new List<string>(); // Speichert Blueprints der Schmiede
        private Dictionary<string, int> CraftingQueue = new Dictionary<string, int>(); // ItemID & Dauer
        private bool IsCrafting = false;
        private int CraftingProgress = 0;
        private int CraftingDuration = 0;
        private bool IsWeaponSmith = true; // Standard ist eine Waffenschmiede
        private PEAcademy AcademyInstance; // Referenz zur Akademie
        private Faction OwnerFaction; // Fraktion der Schmiede

        private const int MaxWeaponBlueprints = 10;
        private const int MaxArmorBlueprints = 20;
        private const int MaxQueueSize = 5;

        public void InitializeSmithy(int factionIndex, string factionName, bool isWeaponSmith, PEAcademy academy)
        {
            this.FactionIndex = factionIndex;
            this.FactionName = factionName;
            this.IsWeaponSmith = isWeaponSmith;
            this.AcademyInstance = academy;
            this.OwnerFaction = FactionManager.GetFactionByIndex(factionIndex);
        }

        public void SyncBlueprintsFromAcademy(NetworkCommunicator player)
        {
            if (AcademyInstance == null) return;
            var academyBlueprints = AcademyInstance.GetUnlockedBlueprints(player);

            foreach (var blueprint in academyBlueprints)
            {
                AddBlueprint(blueprint);
            }

            InformationManager.DisplayMessage(new InformationMessage($"🔄 Blueprints von {player.UserName} synchronisiert!"));
        }

        public void AddBlueprint(string blueprint)
        {
            int maxBlueprints = IsWeaponSmith ? MaxWeaponBlueprints : MaxArmorBlueprints;
            if (BlueprintSlots.Count < maxBlueprints && !BlueprintSlots.Contains(blueprint))
            {
                BlueprintSlots.Add(blueprint);
                InformationManager.DisplayMessage(new InformationMessage($"✅ Blueprint {blueprint} hinzugefügt!"));
            }
            else
            {
                InformationManager.DisplayMessage(new InformationMessage("⚠️ Blueprint-Slots voll oder bereits hinzugefügt!"));
            }
        }

        public bool CanCraftItem(string itemID)
        {
            return BlueprintSlots.Contains(itemID);
        }

        public void StartCrafting(NetworkCommunicator player, string itemID, int duration)
        {
            if (!CanCraftItem(itemID))
            {
                InformationManager.DisplayMessage(new InformationMessage("⚠️ Dieses Item ist nicht freigeschaltet!"));
                return;
            }

            if (CraftingQueue.Count >= MaxQueueSize)
            {
                InformationManager.DisplayMessage(new InformationMessage("⚠️ Die Herstellungsliste ist voll!"));
                return;
            }

            var playerInventory = player.GetComponent<PlayerInventory>();
            if (!playerInventory.HasRequiredMaterials(itemID, IsWeaponSmith))
            {
                InformationManager.DisplayMessage(new InformationMessage($"⚠️ {player.UserName} hat nicht genügend Materialien für {itemID}!"));
                return;
            }

            // Steuern an Fraktion überweisen
            int taxAmount = OwnerFaction != null ? (int)(duration * 0.1f) : 0;
            if (OwnerFaction != null)
            {
                OwnerFaction.AddGold(taxAmount);
            }

            playerInventory.ConsumeMaterials(itemID, IsWeaponSmith);
            CraftingQueue[itemID] = duration;
            if (!IsCrafting)
            {
                BeginNextCraftingItem();
            }
            InformationManager.DisplayMessage(new InformationMessage($"🔨 Herstellung von {itemID} gestartet!"));
        }

        private void BeginNextCraftingItem()
        {
            if (CraftingQueue.Count > 0)
            {
                var nextItem = CraftingQueue.GetEnumerator();
                nextItem.MoveNext();
                CraftingProgress = 0;
                CraftingDuration = nextItem.Current.Value;
                IsCrafting = true;
            }
        }

        public void Tick()
        {
            if (IsCrafting)
            {
                CraftingProgress++;
                if (CraftingProgress >= CraftingDuration)
                {
                    FinishCrafting();
                }
            }
        }

        private void FinishCrafting()
        {
            var nextItem = CraftingQueue.GetEnumerator();
            nextItem.MoveNext();
            GiveItemToPlayer(nextItem.Current.Key);
            CraftingQueue.Remove(nextItem.Current.Key);

            if (CraftingQueue.Count > 0)
            {
                BeginNextCraftingItem();
            }
            else
            {
                IsCrafting = false;
            }
            InformationManager.DisplayMessage(new InformationMessage("✅ Herstellung abgeschlossen!"));
        }

        private void GiveItemToPlayer(string itemID)
        {
            GameNetwork.BeginBroadcastModuleEvent();
            GameNetwork.WriteMessage(new CraftingCompleted(null, itemID, this));
            GameNetwork.EndBroadcastModuleEvent(GameNetwork.EventBroadcastFlags.None, null);
        }

        public void ResetBlueprints()
        {
            BlueprintSlots.Clear();
            InformationManager.DisplayMessage(new InformationMessage("🔄 Alle Blueprints wurden zurückgesetzt. Neue können nun ausgewählt werden."));
        }
    }
}

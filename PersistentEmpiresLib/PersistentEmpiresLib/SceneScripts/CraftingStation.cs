using PersistentEmpiresLib.Helpers;
using PersistentEmpiresLib.Factions;
using System;
using System.Collections.Generic;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;

namespace PersistentEmpiresLib.SceneScripts
{
    public class PEFactionSmithing : MissionObject
    {
        public int FactionIndex { get; private set; } = 2;
        public string FactionName { get; private set; } = "Simple Faction";
        private List<string> BlueprintSlots = new List<string>(); // Speichert die aktiven Blueprints
        private Dictionary<string, int> CraftingQueue = new Dictionary<string, int>(); // ItemID & verbleibende Zeit
        private bool IsCrafting = false;
        private int CraftingProgress = 0;
        private int CraftingDuration = 0;
        private const int MaxWeaponBlueprints = 10;
        private const int MaxArmorBlueprints = 20;
        private const int MaxQueueSize = 5;
        private bool IsWeaponSmith = true; // Standard ist eine Waffenschmiede

        public void InitializeSmithy(int factionIndex, string factionName, bool isWeaponSmith)
        {
            this.FactionIndex = factionIndex;
            this.FactionName = factionName;
            this.IsWeaponSmith = isWeaponSmith;
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

        public void StartCrafting(string itemID, int duration)
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

            if (!HasRequiredMaterials(itemID))
            {
                InformationManager.DisplayMessage(new InformationMessage("⚠️ Nicht genügend Materialien!"));
                return;
            }

            ConsumeMaterials(itemID);
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

        private bool HasRequiredMaterials(string itemID)
        {
            return PlayerInventory.HasMaterialsForItem(itemID);
        }

        private void ConsumeMaterials(string itemID)
        {
            PlayerInventory.ConsumeMaterialsForItem(itemID);
        }

        private void GiveItemToPlayer(string itemID)
        {
            PlayerInventory.AddItem(itemID);
            InformationManager.DisplayMessage(new InformationMessage($"🎁 {itemID} wurde ins Inventar gelegt!"));
        }

        public void ResetBlueprints()
        {
            BlueprintSlots.Clear();
            InformationManager.DisplayMessage(new InformationMessage("🔄 Alle Blueprints wurden zurückgesetzt. Neue können nun ausgewählt werden."));
        }
    }
}

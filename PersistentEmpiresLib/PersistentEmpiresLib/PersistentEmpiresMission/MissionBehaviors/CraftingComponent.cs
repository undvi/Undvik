using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.MountAndBlade;

namespace PersistentEmpiresLib.SceneScripts
{
    public class PE_CraftingReceipt : MissionObject
    {
        private Dictionary<int, List<CraftingRecipe>> WeaponRecipes = new Dictionary<int, List<CraftingRecipe>>()
        {
            { 1, new List<CraftingRecipe> { new CraftingRecipe("pe_dagger", new Dictionary<string, int> { { "pe_iron", 1 }, { "pe_wooden_sticks", 1 } }) } },
            { 2, new List<CraftingRecipe> { new CraftingRecipe("pe_sword", new Dictionary<string, int> { { "pe_steel", 2 }, { "pe_hardwood", 1 } }) } },
            { 3, new List<CraftingRecipe> { new CraftingRecipe("pe_waraxe", new Dictionary<string, int> { { "pe_hardwood", 2 }, { "pe_steel", 3 } }) } },
            { 4, new List<CraftingRecipe> { new CraftingRecipe("pe_greatsword", new Dictionary<string, int> { { "pe_hardwood", 3 }, { "pe_steel", 4 }, { "pe_gold", 1 } }) } },
            { 5, new List<CraftingRecipe> { new CraftingRecipe("pe_legendary_blade", new Dictionary<string, int> { { "pe_hardwood", 5 }, { "pe_steel", 6 }, { "pe_gold", 2 }, { "pe_diamond", 1 } }) } }
        };

        private Dictionary<int, List<CraftingRecipe>> ArmorRecipes = new Dictionary<int, List<CraftingRecipe>>()
        {
            { 1, new List<CraftingRecipe> { new CraftingRecipe("pe_leather_armor", new Dictionary<string, int> { { "pe_leather", 2 }, { "pe_flax", 1 } }) } },
            { 2, new List<CraftingRecipe> { new CraftingRecipe("pe_chainmail", new Dictionary<string, int> { { "pe_iron", 2 }, { "pe_leather", 1 } }) } },
            { 3, new List<CraftingRecipe> { new CraftingRecipe("pe_plate_armor", new Dictionary<string, int> { { "pe_steel", 3 }, { "pe_hardwood", 1 } }) } },
            { 4, new List<CraftingRecipe> { new CraftingRecipe("pe_knight_armor", new Dictionary<string, int> { { "pe_steel", 4 }, { "pe_hardwood", 2 }, { "pe_gold", 1 } }) } },
            { 5, new List<CraftingRecipe> { new CraftingRecipe("pe_royal_armor", new Dictionary<string, int> { { "pe_steel", 5 }, { "pe_gold", 2 }, { "pe_velvet", 1 } }) } }
        };

        /// <summary>
        /// Gibt eine Liste von Crafting-Rezepten für den angegebenen Tier-Level zurück.
        /// </summary>
        public List<CraftingRecipe> GetCraftingReceipts(int tier, bool isWeaponSmith)
        {
            var recipeDict = isWeaponSmith ? WeaponRecipes : ArmorRecipes;

            if (!recipeDict.ContainsKey(tier))
            {
                InformationManager.DisplayMessage(new InformationMessage($"⚠️ Ungültige Tier-Stufe: {tier}"));
                return new List<CraftingRecipe>();
            }
            return recipeDict[tier];
        }

        /// <summary>
        /// Überprüft, ob ein bestimmtes Item innerhalb eines Tier-Bereichs gecraftet werden kann.
        /// </summary>
        public bool CanCraft(int tier, string itemId, bool isWeaponSmith, List<string> blueprints)
        {
            var recipeDict = isWeaponSmith ? WeaponRecipes : ArmorRecipes;

            if (!recipeDict.ContainsKey(tier))
            {
                InformationManager.DisplayMessage(new InformationMessage($"⚠️ Tier {tier} existiert nicht!"));
                return false;
            }

            var recipe = recipeDict[tier].FirstOrDefault(r => r.ResultItem == itemId);
            if (recipe == null)
            {
                InformationManager.DisplayMessage(new InformationMessage($"⚠️ {itemId} kann auf diesem Tier nicht hergestellt werden!"));
                return false;
            }

            if (!blueprints.Contains(itemId))
            {
                InformationManager.DisplayMessage(new InformationMessage($"⚠️ Kein Blueprint für {itemId} vorhanden!"));
                return false;
            }

            return true;
        }

        /// <summary>
        /// Überprüft, ob der Spieler genügend Materialien für das Crafting hat.
        /// </summary>
        public bool HasRequiredMaterials(string itemId, Dictionary<string, int> playerMaterials, bool isWeaponSmith)
        {
            var recipe = (isWeaponSmith ? WeaponRecipes : ArmorRecipes)
                .SelectMany(x => x.Value)
                .FirstOrDefault(r => r.ResultItem == itemId);

            if (recipe == null)
                return false;

            foreach (var ingredient in recipe.Ingredients)
            {
                if (!playerMaterials.ContainsKey(ingredient.Key) || playerMaterials[ingredient.Key] < ingredient.Value)
                {
                    InformationManager.DisplayMessage(new InformationMessage($"❌ Fehlende Materialien für {itemId}: {ingredient.Key} ({ingredient.Value} benötigt)"));
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Entfernt Materialien nach erfolgreichem Crafting.
        /// </summary>
        public void ConsumeMaterials(string itemId, Dictionary<string, int> playerMaterials, bool isWeaponSmith)
        {
            var recipe = (isWeaponSmith ? WeaponRecipes : ArmorRecipes)
                .SelectMany(x => x.Value)
                .FirstOrDefault(r => r.ResultItem == itemId);

            if (recipe == null)
                return;

            foreach (var ingredient in recipe.Ingredients)
            {
                if (playerMaterials.ContainsKey(ingredient.Key))
                {
                    playerMaterials[ingredient.Key] -= ingredient.Value;
                    if (playerMaterials[ingredient.Key] <= 0)
                        playerMaterials.Remove(ingredient.Key);
                }
            }

            InformationManager.DisplayMessage(new InformationMessage($"🔨 {itemId} wurde erfolgreich hergestellt!"));
        }
    }

    /// <summary>
    /// Repräsentiert ein Crafting-Rezept mit Zutaten und einem Ergebnis.
    /// </summary>
    public class CraftingRecipe
    {
        public string ResultItem { get; private set; }
        public Dictionary<string, int> Ingredients { get; private set; }

        public CraftingRecipe(string resultItem, Dictionary<string, int> ingredients)
        {
            ResultItem = resultItem;
            Ingredients = ingredients;
        }

        public override string ToString()
        {
            string ingredientsString = string.Join(", ", Ingredients.Select(kvp => $"{kvp.Value}x {kvp.Key}"));
            return $"{ResultItem} = {ingredientsString}";
        }
    }
}

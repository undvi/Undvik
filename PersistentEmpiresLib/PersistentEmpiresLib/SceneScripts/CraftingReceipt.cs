using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.MountAndBlade;

namespace PersistentEmpiresLib.SceneScripts
{
    public class PE_CraftingReceipt : MissionObject
    {
        private Dictionary<int, List<CraftingRecipe>> CraftingRecipes = new Dictionary<int, List<CraftingRecipe>>()
        {
            { 1, new List<CraftingRecipe>
                {
                    new CraftingRecipe("pe_buildhammer", new Dictionary<string, int>
                    {
                        { "pe_wooden_sticks", 3 },
                        { "pe_stone", 2 }
                    })
                }
            },
            { 2, new List<CraftingRecipe>
                {
                    new CraftingRecipe("pe_buildhammer", new Dictionary<string, int>
                    {
                        { "pe_wooden_sticks", 2 },
                        { "pe_iron", 2 },
                        { "pe_leather", 1 }
                    })
                }
            },
            { 3, new List<CraftingRecipe>
                {
                    new CraftingRecipe("pe_buildhammer", new Dictionary<string, int>
                    {
                        { "pe_hardwood", 3 },
                        { "pe_steel", 2 },
                        { "pe_gold", 1 }
                    })
                }
            }
        };

        /// <summary>
        /// Gibt eine Liste von Crafting-Rezepten für den angegebenen Tier-Level zurück.
        /// </summary>
        public List<CraftingRecipe> GetCraftingReceipts(int tier)
        {
            if (!CraftingRecipes.ContainsKey(tier))
            {
                InformationManager.DisplayMessage(new InformationMessage($"⚠️ Ungültige Tier-Stufe: {tier}"));
                return new List<CraftingRecipe>();
            }
            return CraftingRecipes[tier];
        }

        /// <summary>
        /// Überprüft, ob ein bestimmtes Item innerhalb eines Tier-Bereichs gecraftet werden kann.
        /// </summary>
        public bool CanCraft(int tier, string itemId)
        {
            if (!CraftingRecipes.ContainsKey(tier))
            {
                InformationManager.DisplayMessage(new InformationMessage($"⚠️ Tier {tier} existiert nicht in den Crafting-Rezepten!"));
                return false;
            }

            return CraftingRecipes[tier].Any(recipe => recipe.ResultItem == itemId);
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


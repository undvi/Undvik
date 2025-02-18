using PersistentEmpiresLib.Data;
using PersistentEmpiresLib.Helpers;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Network.Messages;
using System.Collections.Generic;
using System.Linq;

namespace PersistentEmpiresClient.ViewModels
{
    public class PECraftingStationVM : ViewModel
    {
        private MBBindingList<PECraftingRecipeVM> _availableRecipes;

        public PECraftingStationVM()
        {
            AvailableRecipes = new MBBindingList<PECraftingRecipeVM>();
            LoadAvailableRecipes();
        }

        [DataSourceProperty]
        public MBBindingList<PECraftingRecipeVM> AvailableRecipes
        {
            get => _availableRecipes;
            set
            {
                if (value != _availableRecipes)
                {
                    _availableRecipes = value;
                    OnPropertyChanged(nameof(AvailableRecipes));
                }
            }
        }

        private void LoadAvailableRecipes()
        {
            var playerBlueprints = BlueprintResearchSystem.GetPlayerBlueprints();
            var allRecipes = CraftingRecipeDatabase.GetAllRecipes();

            AvailableRecipes.Clear();
            foreach (var recipe in allRecipes)
            {
                bool isUnlocked = playerBlueprints.Contains(recipe.BlueprintID);
                AvailableRecipes.Add(new PECraftingRecipeVM(recipe, isUnlocked));
            }
        }

        public void RefreshAvailableRecipes()
        {
            LoadAvailableRecipes();
            OnPropertyChanged(nameof(AvailableRecipes));
        }
    }
}

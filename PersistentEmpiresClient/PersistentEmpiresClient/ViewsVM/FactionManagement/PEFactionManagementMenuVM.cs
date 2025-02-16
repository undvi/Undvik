using PersistentEmpires.Views.ViewsVM.FactionManagement.ManagementMenu;
using System.Collections.Generic;
using TaleWorlds.Library;
using PersistentEmpiresLib.Factions;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;
using PersistentEmpiresLib.Helpers;

namespace PersistentEmpires.Views.ViewsVM.FactionManagement
{
    public class PEFactionManagementMenuVM : ViewModel
    {
        private MBBindingList<ManagementItemVM> _menuItems;

        public PEFactionManagementMenuVM(IEnumerable<ManagementItemVM> items)
        {
            this.MenuItems = new MBBindingList<ManagementItemVM>();
            if (items != null)
            {
                foreach (ManagementItemVM item in items)
                {
                    this.MenuItems.Add(item);
                }
            }
            this.RefreshValues();
        }

        public override void RefreshValues()
        {
            base.RefreshValues();
            this.MenuItems.ApplyActionOnAllItems(delegate (ManagementItemVM x)
            {
                x.RefreshValues();
            });
        }

        public void RefreshItems(IEnumerable<ManagementItemVM> items)
        {
            this.MenuItems.Clear();
            foreach (ManagementItemVM item in items)
            {
                this.MenuItems.Add(item);
            }
        }

        [DataSourceProperty]
        public MBBindingList<ManagementItemVM> MenuItems
        {
            get => this._menuItems;
            set
            {
                if (value != this._menuItems)
                {
                    this._menuItems = value;
                    base.OnPropertyChangedWithValue(value, "MenuItems");
                }
            }
        }

        // ✅ **Hinzufügen der Fraktionsrang-Upgrades**
        public static void AddFactionRankUpgradeOption(MBBindingList<ManagementItemVM> menuItems, Faction faction)
        {
            if (faction.lordId != GameNetwork.MyPeer.VirtualPlayer.ToPlayerId()) return; // ✅ Nur der Lord darf das Upgrade ausführen

            int currentRank = faction.Rank;
            if (currentRank >= 5) return; // ✅ Rang 5 ist das Maximum

            int upgradeCost = GetUpgradeCost(currentRank);
            int newMaxMembers = GetMaxMembers(currentRank + 1);

            menuItems.Add(new ManagementItemVM(
                new TextObject($"Upgrade Faction to Rank {currentRank + 1} (Cost: {upgradeCost} Gold)"),
                () =>
                {
                    if (faction.Gold < upgradeCost)
                    {
                        InformationManager.DisplayMessage(new InformationMessage("Not enough gold to upgrade the faction."));
                        return;
                    }

                    faction.Gold -= upgradeCost;
                    faction.Rank++;
                    faction.MaxMembers = newMaxMembers;

                    InformationManager.DisplayMessage(new InformationMessage($"Faction upgraded to Rank {faction.Rank} with {newMaxMembers} max members!"));
                }
            ));
        }

        // ✅ **Kosten für das Rang-Upgrade**
        private static int GetUpgradeCost(int rank)
        {
            switch (rank)
            {
                case 1: return 100000; // Rang 1 → 2 kostet 100000 Gold
                case 2: return 200000; // Rang 2 → 3 kostet 200000 Gold
                case 3: return 300000; // Rang 3 → 4 kostet 300000 Gold
                case 4: return 500000; // Rang 4 → 5 kostet 500000 Gold
                default: return int.MaxValue;
            }
        }

        // ✅ **Maximale Mitglieder für jeden Rang**
        private static int GetMaxMembers(int rank)
        {
            switch (rank)
            {
                case 1: return 20;
                case 2: return 30;
                case 3: return 50;
                case 4: return 60;
                case 5: return 80;
                default: return 80;
            }
        }
    }
}

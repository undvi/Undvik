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
        private int _maxMembers;
        private int _currentRank;
        private int _gold;

        public PEFactionManagementMenuVM(Faction faction, IEnumerable<ManagementItemVM> items)
        {
            this.Faction = faction;
            this.MenuItems = new MBBindingList<ManagementItemVM>();
            if (items != null)
            {
                foreach (ManagementItemVM item in items)
                {
                    this.MenuItems.Add(item);
                }
            }
            this.UpdateFactionData();
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

        public void UpdateFactionData()
        {
            this.MaxMembers = this.Faction.MaxMembers;
            this.CurrentRank = this.Faction.Rank;
            this.Gold = this.Faction.Gold;
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

        [DataSourceProperty]
        public int MaxMembers
        {
            get => this._maxMembers;
            set
            {
                if (value != this._maxMembers)
                {
                    this._maxMembers = value;
                    base.OnPropertyChangedWithValue(value, "MaxMembers");
                }
            }
        }

        [DataSourceProperty]
        public int CurrentRank
        {
            get => this._currentRank;
            set
            {
                if (value != this._currentRank)
                {
                    this._currentRank = value;
                    base.OnPropertyChangedWithValue(value, "CurrentRank");
                }
            }
        }

        [DataSourceProperty]
        public int Gold
        {
            get => this._gold;
            set
            {
                if (value != this._gold)
                {
                    this._gold = value;
                    base.OnPropertyChangedWithValue(value, "Gold");
                }
            }
        }

        public static void AddFactionRankUpgradeOption(MBBindingList<ManagementItemVM> menuItems, Faction faction)
        {
            if (faction.lordId != GameNetwork.MyPeer.VirtualPlayer.ToPlayerId()) return;

            int currentRank = faction.Rank;
            if (currentRank >= 5) return;

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

        private static int GetUpgradeCost(int rank)
        {
            return rank switch
            {
                1 => 100000,
                2 => 200000,
                3 => 300000,
                4 => 500000,
                _ => int.MaxValue,
            };
        }

        private static int GetMaxMembers(int rank)
        {
            return rank switch
            {
                1 => 20,
                2 => 30,
                3 => 50,
                4 => 60,
                5 => 80,
                _ => 80,
            };
        }

        public Faction Faction { get; }
    }
}

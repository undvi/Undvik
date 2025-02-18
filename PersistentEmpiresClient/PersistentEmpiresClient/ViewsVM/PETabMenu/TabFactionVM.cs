using PersistentEmpiresLib;
using PersistentEmpiresLib.Factions;
using System;
using System.Linq;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using PersistentEmpiresLib.Helpers;

namespace PersistentEmpires.Views.ViewsVM.PETabMenu
{
    public class TabFactionVM : ViewModel
    {
        private Action<TabFactionVM> _executeSelectFaction;
        public int FactionIndex;
        public Faction factionObj;

        public TabFactionVM(Faction faction, int factionIndex, Action<TabFactionVM> ExecuteSelectFaction)
        {
            this.factionObj = faction;
            this.FactionName = $"{faction.name} (Rank {faction.Rank})"; // ✅ Zeigt den Rang in der UI an
            this.Gold = faction.Gold; // ✅ Zeigt das Fraktionsgold an
            this.Influence = faction.Influence; // ✅ Zeigt den Einfluss an
            this.TaxRate = faction.TaxRate; // ✅ Steuersystem anzeigen
            this.TradeBonus = faction.ExportBonus; // ✅ Export-Bonus anzeigen

            BannerCode bannercode = BannerCode.CreateFrom(new Banner(faction.banner.Serialize()));
            this.BannerImage = new ImageIdentifierVM(bannercode, true);
            this.Members = new MBBindingList<TabPlayerVM>();
            this.Vassals = new MBBindingList<TabFactionVM>(); // ✅ Liste der Vasallen-Fraktionen
            this.Castles = new MBBindingList<CastleVM>();
            this._executeSelectFaction = ExecuteSelectFaction;
            this.FactionIndex = factionIndex;

            foreach (NetworkCommunicator peer in faction.members)
            {
                this.Members.Add(new TabPlayerVM(peer, faction.lordId == peer.VirtualPlayer.ToPlayerId()));
            }

            foreach (var vassal in faction.vassals)
            {
                this.Vassals.Add(new TabFactionVM(FactionManager.GetFactionByIndex(vassal.Key), vassal.Key, ExecuteSelectFaction));
            }

            this.MaxMembers = GetMaxMembers(faction.Rank); // ✅ Maximale Mitglieder basierend auf Rang setzen
            base.RefreshValues();
        }

        public void RemoveMemberAtIndex(int indexOf)
        {
            this.Members.RemoveAt(indexOf);
            base.OnPropertyChanged("MemberCount");
        }

        public void AddMember(TabPlayerVM tabPlayer)
        {
            if (!Members.Contains(tabPlayer))
                Members.Add(tabPlayer);
            base.OnPropertyChanged("MemberCount");
        }

        public void ExecuteSelectFaction()
        {
            this._executeSelectFaction(this);
        }

        // 🔹 Steuerberechnung für Fraktionsanführer (Einnahmen aus Steuern)
        public int CalculateTaxIncome()
        {
            return (this.Members.Count * this.TaxRate);
        }

        // 🔹 Berechnung von Export-Einnahmen
        public int CalculateTradeIncome(int baseValue)
        {
            return baseValue + (baseValue * this.TradeBonus / 100);
        }

        [DataSourceProperty]
        public bool CanSeeClasses
        {
            get
            {
                if (GameNetwork.MyPeer.GetComponent<PersistentEmpireRepresentative>() == null)
                {
                    return false;
                }
                return GameNetwork.MyPeer.GetComponent<PersistentEmpireRepresentative>().GetFactionIndex() == this.FactionIndex;
            }
        }

        [DataSourceProperty]
        public bool ShowWarIcon
        {
            get => this._showWarIcon;
            set
            {
                if (value != this._showWarIcon)
                {
                    this._showWarIcon = value;
                    base.OnPropertyChangedWithValue(value, "ShowWarIcon");
                }
            }
        }

        [DataSourceProperty]
        public string FactionName
        {
            get => _factionName;
            set
            {
                this._factionName = value;
                base.OnPropertyChangedWithValue(value, "FactionName");
            }
        }

        [DataSourceProperty]
        public int MaxMembers // ✅ Maximale Mitglieder basierend auf Rang
        {
            get => _maxMembers;
            set
            {
                this._maxMembers = value;
                base.OnPropertyChangedWithValue(value, "MaxMembers");
            }
        }

        [DataSourceProperty]
        public int Gold // ✅ Anzeige des Fraktions-Golds
        {
            get => _gold;
            set
            {
                this._gold = value;
                base.OnPropertyChangedWithValue(value, "Gold");
            }
        }

        [DataSourceProperty]
        public int Influence // ✅ Anzeige des Fraktions-Einflusses
        {
            get => _influence;
            set
            {
                this._influence = value;
                base.OnPropertyChangedWithValue(value, "Influence");
            }
        }

        [DataSourceProperty]
        public int TaxRate // ✅ Steuersystem (Anzeige des aktuellen Steuersatzes)
        {
            get => _taxRate;
            set
            {
                this._taxRate = value;
                base.OnPropertyChangedWithValue(value, "TaxRate");
            }
        }

        [DataSourceProperty]
        public int TradeBonus // ✅ Export-Bonus anzeigen
        {
            get => _tradeBonus;
            set
            {
                this._tradeBonus = value;
                base.OnPropertyChangedWithValue(value, "TradeBonus");
            }
        }

        [DataSourceProperty]
        public ImageIdentifierVM BannerImage
        {
            get => _bannerImage;
            set
            {
                this._bannerImage = value;
                base.OnPropertyChangedWithValue(value, "BannerImage");
            }
        }

        [DataSourceProperty]
        public bool IsSelected
        {
            get => _isSelected;
            set
            {
                this._isSelected = value;
                base.OnPropertyChangedWithValue(value, "IsSelected");
            }
        }

        [DataSourceProperty]
        public int MemberCount
        {
            get => this.Members.Count();
        }

        [DataSourceProperty]
        public MBBindingList<TabPlayerVM> Members
        {
            get => _members;
            set
            {
                this._members = value;
                base.OnPropertyChangedWithValue(value, "Members");
                base.OnPropertyChanged("MemberCount");
            }
        }

        [DataSourceProperty]
        public MBBindingList<CastleVM> Castles
        {
            get => _castles;
            set
            {
                this._castles = value;
                base.OnPropertyChangedWithValue(value, "Castles");
            }
        }

        [DataSourceProperty]
        public MBBindingList<TabFactionVM> Vassals // ✅ Anzeige von Vasallen-Fraktionen
        {
            get => _vassals;
            set
            {
                this._vassals = value;
                base.OnPropertyChangedWithValue(value, "Vassals");
            }
        }

        // ✅ Methode zur Berechnung der maximalen Mitglieder basierend auf dem Rang
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

        private string _factionName;
        private int _maxMembers;
        private int _gold;
        private int _influence;
        private int _taxRate;
        private int _tradeBonus;
        private ImageIdentifierVM _bannerImage;
        private MBBindingList<TabPlayerVM> _members;
        private MBBindingList<TabFactionVM> _vassals;
        private bool _isSelected;
        private MBBindingList<CastleVM> _castles;
        private bool _showWarIcon;
    }
}

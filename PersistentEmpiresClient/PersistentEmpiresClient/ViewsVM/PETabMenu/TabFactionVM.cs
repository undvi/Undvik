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
            BannerCode bannercode = BannerCode.CreateFrom(new Banner(faction.banner.Serialize()));
            this.BannerImage = new ImageIdentifierVM(bannercode, true);
            this.Members = new MBBindingList<TabPlayerVM>();
            this.Castles = new MBBindingList<CastleVM>();
            this._executeSelectFaction = ExecuteSelectFaction;
            this.FactionIndex = factionIndex;

            foreach (NetworkCommunicator peer in faction.members)
            {
                this.Members.Add(new TabPlayerVM(peer, faction.lordId == peer.VirtualPlayer.ToPlayerId()));
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
        public int MaxMembers // ✅ Zeigt die maximale Anzahl an Mitgliedern basierend auf Rang
        {
            get => _maxMembers;
            set
            {
                this._maxMembers = value;
                base.OnPropertyChangedWithValue(value, "MaxMembers");
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

        // ✅ Methode zur Berechnung der maximalen Mitglieder basierend auf dem Rang
        private static int GetMaxMembers(int rank)
        {
            switch (rank)
            {
                case 1: return 10;
                case 2: return 20;
                case 3: return 30;
                case 4: return 40;
                case 5: return 50;
                default: return 50;
            }
        }

        private string _factionName;
        private int _maxMembers;
        private ImageIdentifierVM _bannerImage;
        private MBBindingList<TabPlayerVM> _members;
        private bool _isSelected;
        private MBBindingList<CastleVM> _castles;
        private bool _showWarIcon;
    }
}


using PersistentEmpires.Views.ViewsVM.PETabMenu;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.ViewModelCollection.Input;

namespace PersistentEmpires.Views.ViewsVM
{
    public class PETabMenuVM : ViewModel
    {
        Dictionary<NetworkCommunicator, TabPlayerVM> peerToPlayerVM;

        public PETabMenuVM()
        {
            GameKey gameKey = HotKeyManager.GetCategory("ScoreboardHotKeyCategory").GetGameKey(35);
            this.ShowMouseKey = InputKeyItemVM.CreateFromGameKey(gameKey, false);
            this.Factions = new MBBindingList<TabFactionVM>();
            NetworkCommunicator.OnPeerAveragePingUpdated += this.OnPeerPingUpdated;
            MissionPeer.OnPlayerKilled += this.OnPlayerKilled;
            peerToPlayerVM = new Dictionary<NetworkCommunicator, TabPlayerVM>();
            base.RefreshValues();
        }

        private void OnPlayerKilled(MissionPeer killerPeer, MissionPeer killedPeer)
        {
            if (peerToPlayerVM.ContainsKey(killerPeer.GetNetworkPeer()))
            {
                peerToPlayerVM[killerPeer.GetNetworkPeer()].KillCount = killerPeer.KillCount;
            }
            if (peerToPlayerVM.ContainsKey(killedPeer.GetNetworkPeer()))
            {
                peerToPlayerVM[killedPeer.GetNetworkPeer()].DeathCount = killedPeer.DeathCount;
            }
        }

        private void OnPeerPingUpdated(NetworkCommunicator obj)
        {
            if (peerToPlayerVM.ContainsKey(obj))
            {
                peerToPlayerVM[obj].OnPropertyChanged("Ping");
            }
        }

        public void AddMember(int factionIndex, TabPlayerVM player)
        {
            this.peerToPlayerVM[player.GetPeer()] = player;
            this.Factions[factionIndex].AddMember(player);
            player.KillCount = player.GetPeer().GetComponent<MissionPeer>() == null ? 0 : player.GetPeer().GetComponent<MissionPeer>().KillCount;
            player.DeathCount = player.GetPeer().GetComponent<MissionPeer>() == null ? 0 : player.GetPeer().GetComponent<MissionPeer>().DeathCount;
            base.OnPropertyChanged("AllMemberCount");
        }

        public void RemoveMemberAtIndex(int factionIndex, int indexOf)
        {
            this.Factions[factionIndex].RemoveMemberAtIndex(indexOf);
            base.OnPropertyChanged("AllMemberCount");
        }

        public void UpdateFactionRank(int factionIndex, int newRank)
        {
            if (factionIndex < 0 || factionIndex >= this.Factions.Count) return;

            this.Factions[factionIndex].Rank = newRank;
            this.Factions[factionIndex].MaxMembers = GetMaxMembers(newRank);

            base.OnPropertyChanged("Factions");
            base.OnPropertyChanged("AllMemberCount");
        }

        public void SetMouseState(bool isMouseVisible)
        {
            this.IsMouseEnabled = isMouseVisible;
        }

        [DataSourceProperty]
        public bool IsActive
        {
            get => this._isActive;
            set
            {
                if (value != this._isActive)
                {
                    this._isActive = value;
                    base.OnPropertyChangedWithValue(value, "IsActive");
                }
            }
        }

        [DataSourceProperty]
        public int AllMemberCount
        {
            get => this.Factions.Sum(f => f.Members.Count);
        }

        [DataSourceProperty]
        public bool IsMouseEnabled
        {
            get => this._isMouseEnabled;
            set
            {
                if (value != this._isMouseEnabled)
                {
                    this._isMouseEnabled = value;
                    base.OnPropertyChangedWithValue(value, "IsMouseEnabled");
                }
            }
        }

        [DataSourceProperty]
        public InputKeyItemVM ShowMouseKey
        {
            get => this._showMouseKey;
            set
            {
                if (value != this._showMouseKey)
                {
                    this._showMouseKey = value;
                    base.OnPropertyChangedWithValue(value, "ShowMouseKey");
                }
            }
        }

        [DataSourceProperty]
        public MBBindingList<TabFactionVM> Factions
        {
            get => _factions;
            set
            {
                if (value != this._factions)
                {
                    this._factions = value;
                    base.OnPropertyChangedWithValue(value, "Factions");
                }
            }
        }

        [DataSourceProperty]
        public TabFactionVM SelectedFaction
        {
            get => _selectedFaction;
            set
            {
                if (value != this._selectedFaction)
                {
                    this._selectedFaction = value;
                    base.OnPropertyChangedWithValue(value, "SelectedFaction");
                }
            }
        }

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

        private bool _isActive;
        private MBBindingList<TabFactionVM> _factions;
        private TabFactionVM _selectedFaction;
        private bool _isMouseEnabled;
        private InputKeyItemVM _showMouseKey;
    }
}


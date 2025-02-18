using PersistentEmpiresLib.Factions;
using System;
using System.Linq;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace PersistentEmpires.Views.ViewsVM.FactionManagement
{
    public class PEFactionMembersVM : ViewModel
    {
        private string _title;
        private MBBindingList<PEFactionMemberItemVM> _members;
        private Action _onCancel;
        private Action<PEFactionMemberItemVM> _onApply;
        private Action _close;
        private PEFactionMemberItemVM _selectedMember;
        private string _buttonText;
        private string _searchPlayer;
        private MBBindingList<PEFactionMemberItemVM> _filteredMembers;
        private int _maxMembers;

        public PEFactionMembersVM(string title, string buttonText, Action onCancel, Action<PEFactionMemberItemVM> onApply, Action close, Faction faction)
        {
            this.Members = new MBBindingList<PEFactionMemberItemVM>();
            this.Title = title;
            this.ButtonText = buttonText;
            this._onCancel = onCancel;
            this._onApply = onApply;
            this._close = close;
            this._filteredMembers = new MBBindingList<PEFactionMemberItemVM>();
            this.MaxMembers = faction.MaxMembers;
        }

        public void RefreshItems(Faction faction, bool excludeMySelf = false)
        {
            this.SelectedMember = null;
            this.Members.Clear();

            foreach (NetworkCommunicator member in faction.members)
            {
                if (excludeMySelf && member == GameNetwork.MyPeer) continue;

                PEFactionMemberItemVM memberItemVm = new PEFactionMemberItemVM(member, faction, (PEFactionMemberItemVM selected) =>
                {
                    if (this._selectedMember != null)
                    {
                        this._selectedMember.IsSelected = false;
                    }
                    this.SelectedMember = selected;
                    this.SelectedMember.IsSelected = true;
                });

                this.Members.Add(memberItemVm);
            }

            this.RefreshValues();
        }

        [DataSourceProperty]
        public int MaxMembers
        {
            get => _maxMembers;
            set
            {
                if (value != _maxMembers)
                {
                    _maxMembers = value;
                    base.OnPropertyChangedWithValue(value, "MaxMembers");
                }
            }
        }
    }
}

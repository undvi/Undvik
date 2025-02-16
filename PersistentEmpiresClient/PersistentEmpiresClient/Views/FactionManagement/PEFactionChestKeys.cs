using PersistentEmpires.Views.ViewsVM.FactionManagement;
using PersistentEmpiresLib;
using PersistentEmpiresLib.Factions;
using PersistentEmpiresLib.NetworkMessages.Client;
using TaleWorlds.MountAndBlade;
using PersistentEmpiresLib.Helpers;
using TaleWorlds.Library;
using System;
using System.Collections.Generic;

namespace PersistentEmpires.Views.Views.FactionManagement
{
    public class PEFactionChestKeys : PEMenuItem
    {
        private bool _isRequestPending = false; // Schutz gegen Spam
        private int _selectedKeyType = 1; // Standardmäßig "Basic Key"
        private readonly List<string> _keyOptions = new List<string> { "Basic Key", "Advanced Key", "Master Key" };
        private PEFactionMembersVM _dataSource;

        public PEFactionChestKeys() : base("PEFactionMembers") { }

        public override void OnMissionScreenInitialize()
        {
            base.OnMissionScreenInitialize();
            this._factionManagementComponent.OnManageChestKeyClick += this.OnOpen;
            this._factionsBehavior.OnFactionKeyFetched += this.OnKeyFetched;

            this._dataSource = new PEFactionMembersVM("Chest Key Management", "Give/Take Key",
            () =>
            {
                CloseManagementMenu();
                this._factionManagementComponent.OnFactionManagementClickHandler();
            },
            (PEFactionMemberItemVM selectedMember) =>
            {
                if (_isRequestPending)
                {
                    PELogger.Log("Ignoring duplicate chest key request", LogLevel.Warning);
                    return;
                }

                string keyTypeName = _keyOptions[_selectedKeyType - 1];
                string confirmationMessage = $"Do you want to give/revoke a '{keyTypeName}' chest key to {selectedMember.Peer.VirtualPlayer.UserName}?";

                InformationManager.ShowInquiry(new InquiryData(
                    "Confirm Key Assignment",
                    confirmationMessage,
                    true,
                    true,
                    "Yes",
                    "No",
                    () => AssignKey(selectedMember),
                    () => PELogger.Log("Key assignment cancelled", LogLevel.Info)
                ));
            },
            () =>
            {
                CloseManagementMenu();
            });

            // **Dropdown für Schlüsselstufe**
            this._dataSource.AddDropdown("Select Key Level", _keyOptions, _selectedKeyType - 1, OnKeyTypeChanged);
        }

        private void AssignKey(PEFactionMemberItemVM selectedMember)
        {
            try
            {
                _isRequestPending = true;
                this._factionsBehavior.RequestChestKeyForUser(selectedMember.Peer, _selectedKeyType);
                selectedMember.IsGranted = !selectedMember.IsGranted;
                PELogger.Log($"Chest key ({_keyOptions[_selectedKeyType - 1]}) changed for: {selectedMember.Peer.VirtualPlayer.UserName}", LogLevel.Info);
                ShowMessageBox("Success", $"Key status changed for {selectedMember.Peer.VirtualPlayer.UserName}");
            }
            catch (Exception ex)
            {
                PELogger.Log($"Error assigning chest key: {ex.Message}", LogLevel.Error);
                ShowMessageBox("Error", "Failed to change key status.");
            }
            finally
            {
                _isRequestPending = false;
            }
        }

        private void OnKeyFetched(int factionIndex, string playerId, int keyType)
        {
            if (keyType < 1 || keyType > 3 || !this.IsActive) return;

            PELogger.Log($"Updating chest key UI for faction {factionIndex}, KeyType: {keyType}", LogLevel.Debug);

            foreach (PEFactionMemberItemVM item in _dataSource.Members)
            {
                if (item.Peer.VirtualPlayer.ToPlayerId() == playerId)
                {
                    item.IsGranted = true;
                    item.KeyType = keyType;
                }
            }
        }

        protected override void OnOpen()
        {
            if (_isRequestPending) return;

            PersistentEmpireRepresentative persistentEmpireRepresentative = GameNetwork.MyPeer.GetComponent<PersistentEmpireRepresentative>();
            if (persistentEmpireRepresentative == null)
            {
                PELogger.Log("Faction representative not found.", LogLevel.Warning);
                return;
            }

            Faction faction = persistentEmpireRepresentative.GetFaction();
            _dataSource.RefreshItems(faction, true);

            base.OnOpen();

            try
            {
                _isRequestPending = true;
                GameNetwork.BeginModuleEventAsClient();
                GameNetwork.WriteMessage(new RequestFactionKeys(1)); // Holt ALLE Schlüssel
                GameNetwork.EndModuleEventAsClient();
                PELogger.Log("Requested chest keys from server", LogLevel.Info);
            }
            catch (Exception ex)
            {
                PELogger.Log($"Error requesting faction keys: {ex.Message}", LogLevel.Error);
            }
            finally
            {
                _isRequestPending = false;
            }
        }

        private void OnKeyTypeChanged(int selectedIndex)
        {
            _selectedKeyType = selectedIndex + 1;
            PELogger.Log($"Selected Key Type changed to: {_keyOptions[selectedIndex]}", LogLevel.Info);
        }

        private void ShowMessageBox(string title, string message)
        {
            InformationManager.DisplayMessage(new InformationMessage($"{title}: {message}"));
        }
    }
}


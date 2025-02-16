using PersistentEmpires.Views.ViewsVM.FactionManagement;
using PersistentEmpiresLib;
using PersistentEmpiresLib.Factions;
using PersistentEmpiresLib.Helpers;
using TaleWorlds.MountAndBlade;
using System;
using TaleWorlds.Core;

namespace PersistentEmpires.Views.Views.FactionManagement
{
    public class PEFactionChangeName : PEMenuItem
    {
        public PEFactionChangeName() : base("PEFactionNameSelect") { }

        public override void OnMissionScreenInitialize()
        {
            base.OnMissionScreenInitialize();
            this._factionManagementComponent.OnNameChangeClick += this.OnOpen;

            this._dataSource = new PEFactionChangeNameVM(() =>
            {
                CloseManagementMenu();
                this._factionManagementComponent.OnFactionManagementClickHandler();
            },
            (string FactionName) =>
            {
                if (string.IsNullOrWhiteSpace(FactionName) || FactionName.Length < 3)
                {
                    PELogger.Log("Invalid faction name! Must be at least 3 characters.", LogLevel.Warning);
                    ShowMessageBox("Error", "Faction name must be at least 3 characters long.");
                    return;
                }

                try
                {
                    PELogger.Log($"Updating faction name to: {FactionName}", LogLevel.Info);
                    this._factionsBehavior.RequestUpdateFactionName(FactionName);
                    ShowMessageBox("Success", $"Your faction name has been updated to: {FactionName}");
                }
                catch (Exception ex)
                {
                    PELogger.Log($"Error updating faction name: {ex.Message}", LogLevel.Error);
                    ShowMessageBox("Error", "Failed to update the faction name. Please try again.");
                }

                CloseManagementMenu();
            },
            () =>
            {
                CloseManagementMenu();
            });
        }

        private void ShowMessageBox(string title, string message)
        {
            // Zeigt eine Nachricht im Spiel für den Spieler an
            InformationManager.DisplayMessage(new InformationMessage($"{title}: {message}"));
        }
    }
}

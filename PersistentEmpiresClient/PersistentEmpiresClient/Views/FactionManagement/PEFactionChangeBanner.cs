using PersistentEmpires.Views.ViewsVM.FactionManagement;
using PersistentEmpiresLib;
using PersistentEmpiresLib.Factions;
using PersistentEmpiresLib.Helpers;
using TaleWorlds.MountAndBlade;
using System;

namespace PersistentEmpires.Views.Views.FactionManagement
{
    public class PEFactionChangeBanner : PEMenuItem
    {
        public PEFactionChangeBanner() : base("PEFactionBannerCodeSelect") { }

        public override void OnMissionScreenInitialize()
        {
            base.OnMissionScreenInitialize();
            this._factionManagementComponent.OnBannerChangeClick += this.OnOpen;

            this._dataSource = new PEFactionChangeBannerVM(() =>
            {
                CloseManagementMenu();
                this._factionManagementComponent.OnFactionManagementClickHandler();
            },
            (string BannerCode) =>
            {
                if (string.IsNullOrWhiteSpace(BannerCode) || BannerCode.Length < 5)
                {
                    PELogger.Log("Invalid banner code!", LogLevel.Warning);
                    return;
                }

                try
                {
                    PELogger.Log($"Updating faction banner with code: {BannerCode}", LogLevel.Info);
                    this._factionsBehavior.RequestUpdateFactionBanner(BannerCode);
                    ShowMessageBox("Success", "Your faction banner has been updated successfully!");
                }
                catch (Exception ex)
                {
                    PELogger.Log($"Error updating banner: {ex.Message}", LogLevel.Error);
                    ShowMessageBox("Error", "Failed to update the faction banner. Please try again.");
                }

                this.CloseManagementMenu();
            },
            () =>
            {
                CloseManagementMenu();
            });
        }

        private void ShowMessageBox(string title, string message)
        {
            // Optional: GUI Message Box anzeigen
            InformationManager.DisplayMessage(new InformationMessage($"{title}: {message}"));
        }
    }
}


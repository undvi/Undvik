using PersistentEmpires.Views.ViewsVM;
using PersistentEmpires.Views.ViewsVM.ImportExport;
using PersistentEmpiresLib.Data;
using PersistentEmpiresLib.Helpers;
using PersistentEmpiresLib.NetworkMessages.Client;
using PersistentEmpiresLib.PersistentEmpiresMission.MissionBehaviors;
using PersistentEmpiresLib.SceneScripts;
using TaleWorlds.Core;
using TaleWorlds.Engine.GauntletUI;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TaleWorlds.ScreenSystem;
using PersistentEmpiresMod.Trading;

namespace PersistentEmpires.Views.Views
{
    public class PEImportExport : PEBaseInventoryScreen
    {
        private PEImportExportVM _dataSource;
        private ImportExportComponent _importExportComponent;
        private PE_ImportExport ActiveEntity;

        public PEImportExport()
        {
        }

        public override void OnMissionScreenInitialize()
        {
            base.OnMissionScreenInitialize();
            this._importExportComponent = base.Mission.GetMissionBehavior<ImportExportComponent>();
            this._importExportComponent.OnOpenImportExport += this.OnOpen;
            this._dataSource = new PEImportExportVM(base.HandleClickItem, HandleDeliverGoods);
            LoadTradeOrders();
        }

        private void LoadTradeOrders()
        {
            var activeOrders = ExportTradeSystem.GetActiveTradeOrders();
            _dataSource.UpdateTradeOrders(activeOrders);
        }

        private void HandleDeliverGoods(string good, int amount)
        {
            var playerFaction = PlayerEncounter.EncounteredMobileParty?.MapFaction;
            if (playerFaction != null)
            {
                ExportTradeSystem.DeliverGoods(playerFaction, good, amount);
                LoadTradeOrders(); // Aktualisiert das UI nach der Lieferung
            }
        }

        private void CloseImportExportAux()
        {
            this.IsActive = false;
            this._gauntletLayer.InputRestrictions.ResetInputRestrictions();
            base.MissionScreen.RemoveLayer(this._gauntletLayer);
            this._gauntletLayer = null;
        }

        public override void OnMissionTick(float dt)
        {
            base.OnMissionTick(dt);
            if (this._gauntletLayer != null && this.IsActive && (this._gauntletLayer.Input.IsHotKeyReleased("ToggleEscapeMenu") || this._gauntletLayer.Input.IsHotKeyReleased("Exit")))
            {
                this.CloseImportExport();
            }
        }

        public void CloseImportExport()
        {
            if (this.IsActive)
            {
                this.CloseImportExportAux();
            }
        }
    }
}
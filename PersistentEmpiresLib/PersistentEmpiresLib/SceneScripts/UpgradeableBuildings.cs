// File: UpgradeableBuildings.cs
using PersistentEmpiresLib.Data;
using PersistentEmpiresLib.NetworkMessages.Server;
using PersistentEmpiresLib.PersistentEmpiresMission.MissionBehaviors;
using PersistentEmpiresLib.SceneScripts.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TaleWorlds.ObjectSystem;

namespace PersistentEmpiresLib.SceneScripts
{
    public class PE_UpgradeableBuildings : PE_DestructableComponent, IMissionObjectHash
    {
        public int CurrentTier { get; private set; } = 0;
        public bool IsUpgrading { get; private set; }

        private GameEntity _currentTierState, _tier0State, _tier1State, _tier2State, _tier3State;
        private PE_InventoryEntity _upgradeInventory;
        private PlayerInventoryComponent _playerInventoryComponent;
        private int MaxTier = 3;

        protected override void OnInit()
        {
            _tier0State = GetChildByTag("Tier0");
            _tier1State = GetChildByTag("Tier1");
            _tier2State = GetChildByTag("Tier2");
            _tier3State = GetChildByTag("Tier3");

            SetInitialVisibility();

            _upgradeInventory = GetChildByTag("UpgradeInventory")?.GetFirstScriptOfType<PE_InventoryEntity>();
            _playerInventoryComponent = Mission.Current.GetMissionBehavior<PlayerInventoryComponent>();
        }

        private void SetInitialVisibility()
        {
            _tier0State.SetVisibilityExcludeParents(true);
            _tier1State.SetVisibilityExcludeParents(false);
            _tier2State?.SetVisibilityExcludeParents(false);
            _tier3State?.SetVisibilityExcludeParents(false);
            _currentTierState = _tier0State;
        }

        public void UpgradeBuilding()
        {
            if (CurrentTier >= MaxTier) return;

            GameEntity nextTier = GetNextUpgrade();
            if (nextTier == null) return;

            if (!_playerInventoryComponent.HasItem("UpgradeHammer"))
            {
                InformationManager.DisplayMessage(new InformationMessage("⚠️ Fehler: Upgrade-Werkzeug fehlt!"));
                return;
            }

            _currentTierState.SetVisibilityExcludeParents(false);
            _currentTierState = nextTier;
            _currentTierState.SetVisibilityExcludeParents(true);
            CurrentTier++;
            IsUpgrading = false;
        }

        public void DemolishBuilding()
        {
            if (CurrentTier == 0) return;

            _currentTierState.SetVisibilityExcludeParents(false);
            _currentTierState = _tier0State;
            _currentTierState.SetVisibilityExcludeParents(true);
            CurrentTier = 0;

            RefundResources();
        }

        private void RefundResources()
        {
            var receipts = GetUpgradeReceipts();
            foreach (var receipt in receipts)
            {
                _upgradeInventory.AddItem(receipt.UpgradeItem, receipt.NeededCount / 2);
            }
        }

        public void ApplyBuildingMaintenance()
        {
            if (!_playerInventoryComponent.HasResources(new Dictionary<string, int> { { "Gold", 5 }, { "Wood", 3 } }))
            {
                InformationManager.DisplayMessage(new InformationMessage("⚠️ Fehler: Nicht genug Ressourcen für Wartung!"));
                return;
            }

            _playerInventoryComponent.RemoveResources(new Dictionary<string, int> { { "Gold", 5 }, { "Wood", 3 } });
            InformationManager.DisplayMessage(new InformationMessage("🔧 Wartung abgeschlossen!"));
        }

        private GameEntity GetNextUpgrade() => CurrentTier switch
        {
            0 => _tier1State,
            1 => _tier2State,
            2 => _tier3State,
            _ => null
        };
    }
}

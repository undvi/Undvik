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
    public struct UpgradeReceipt
    {
        public UpgradeReceipt(string upgradeItemId, int neededCount)
        {
            this.UpgradeItem = MBObjectManager.Instance.GetObject<ItemObject>(upgradeItemId);
            this.NeededCount = neededCount;
        }
        public ItemObject UpgradeItem { get; private set; }
        public int NeededCount { get; private set; }
    }

    public class PE_UpgradeableBuildings : PE_DestructableComponent, IMissionObjectHash
    {
        public int CastleIndex = 0;
        public string BuildingName = "UNKNOWN";
        public string Tier0Tag, Tier1Tag, Tier2Tag, Tier3Tag, BuildingInteractiveTag;
        public int Tier1RequiredEngineering = 10, Tier2RequiredEngineering = 10, Tier3RequiredEngineering = 10;
        public int Tier1MaxHit = 200, Tier2MaxHit = 300, Tier3MaxHit = 400;

        private int MaxTier = 1;
        public bool IsUpgrading { get; private set; }
        public int CurrentTier { get; private set; } = 0;

        public string Tier1UpgradeReceipts, Tier2UpgradeReceipts, Tier3UpgradeReceipts;
        public string ParticleEffectOnUpgrade = "", SoundEffectOnUpgrade = "", BuildItem = "pe_buildhammer";

        private List<UpgradeReceipt> Tier1Upgrade, Tier2Upgrade, Tier3Upgrade;
        private GameEntity _currentTierState, _tier0State, _tier1State, _tier2State, _tier3State;
        private PE_InventoryEntity _upgradeInventory;
        private PlayerInventoryComponent _playerInventoryComponent;

        protected override void OnInit()
        {
            this.Tier1Upgrade = ParseUpgradeReceipts(Tier1UpgradeReceipts);
            this.Tier2Upgrade = ParseUpgradeReceipts(Tier2UpgradeReceipts);
            this.Tier3Upgrade = ParseUpgradeReceipts(Tier3UpgradeReceipts);

            this._tier0State = GetChildByTag(Tier0Tag);
            this._tier1State = GetChildByTag(Tier1Tag);
            this._tier2State = GetChildByTag(Tier2Tag);
            this._tier3State = GetChildByTag(Tier3Tag);

            _tier0State.SetVisibilityExcludeParents(true);
            _tier1State.SetVisibilityExcludeParents(false);
            if (_tier2State != null) { _tier2State.SetVisibilityExcludeParents(false); MaxTier = 2; }
            if (_tier3State != null) { _tier3State.SetVisibilityExcludeParents(false); MaxTier = 3; }

            _currentTierState = _tier0State;
            _upgradeInventory = GetChildByTag(BuildingInteractiveTag)?.GetFirstScriptOfType<PE_InventoryEntity>();
            _playerInventoryComponent = Mission.Current.GetMissionBehavior<PlayerInventoryComponent>();
            MaxHitPoint = Tier1MaxHit;
        }

        private GameEntity GetChildByTag(string tag)
        {
            return base.GameEntity.GetChildren().FirstOrDefault(g => g.Tags.Contains(tag));
        }

        private List<UpgradeReceipt> ParseUpgradeReceipts(string receiptList)
        {
            return receiptList.Split(',').Select(receipt =>
            {
                string[] split = receipt.Split('*');
                return new UpgradeReceipt(split[0], int.Parse(split[1]));
            }).ToList();
        }

        public int GetNextMaxHit() => CurrentTier switch
        {
            0 => Tier1MaxHit,
            1 => Tier2MaxHit,
            2 => Tier3MaxHit,
            _ => 0
        };

        public GameEntity GetNextUpgrade() => CurrentTier switch
        {
            0 => _tier1State,
            1 => _tier2State,
            2 => _tier3State,
            _ => null
        };

        public List<UpgradeReceipt> GetUpgradeReceipts() => CurrentTier switch
        {
            0 => Tier1Upgrade,
            1 => Tier2Upgrade,
            2 => Tier3Upgrade,
            _ => null
        };

        public int GetRequiredEngineeringForUpgrade() => CurrentTier switch
        {
            0 => Tier1RequiredEngineering,
            1 => Tier2RequiredEngineering,
            2 => Tier3RequiredEngineering,
            _ => -1
        };

        public void UpgradeBuilding()
        {
            GameEntity nextUpgrade = GetNextUpgrade();
            if (nextUpgrade == null) return;

            _currentTierState.SetVisibilityExcludeParents(false);
            _currentTierState = nextUpgrade;
            _currentTierState.SetVisibilityExcludeParents(true);
            CurrentTier++;

            MaxHitPoint = GetNextMaxHit();
            IsUpgrading = false;

            if (GameNetwork.IsServer)
            {
                SaveSystemBehavior.HandleCreateOrSaveUpgradebleBuilding(this);
            }
        }

        public void DemolishBuilding()
        {
            if (CurrentTier == 0) return;

            _currentTierState.SetVisibilityExcludeParents(false);
            _currentTierState = _tier0State;
            _currentTierState.SetVisibilityExcludeParents(true);
            CurrentTier = 0;

            // 50% der Ressourcen zurückgeben
            foreach (var receipt in GetUpgradeReceipts())
            {
                _upgradeInventory.AddItem(receipt.UpgradeItem, receipt.NeededCount / 2);
            }

            InformationComponent.Instance.SendMessage($"{BuildingName} wurde abgerissen! 50% der Ressourcen zurückerhalten.", 0x02ab89d9);
        }

        protected override bool OnHit(Agent attackerAgent, int damage, Vec3 impactPosition, Vec3 impactDirection, in MissionWeapon weapon, ScriptComponentBehavior attackerScriptComponentBehavior, out bool reportDamage)
        {
            reportDamage = false;
            if (attackerAgent == null) return false;

            NetworkCommunicator player = attackerAgent.MissionPeer.GetNetworkPeer();
            bool isAdmin = Main.IsPlayerAdmin(player);

            if (isAdmin && weapon.Item != null && weapon.Item.StringId == "pe_adminhammer")
            {
                if (CurrentTier < MaxTier) { IsUpgrading = true; SetHitPoint(MaxHitPoint, impactDirection, attackerScriptComponentBehavior); }
                return true;
            }

            if (weapon.Item == null || weapon.Item.StringId != BuildItem) return false;

            if (IsUpgrading)
            {
                if (attackerAgent.Character.GetSkillValue(DefaultSkills.Engineering) >= GetRequiredEngineeringForUpgrade() && HitPoint != MaxHitPoint)
                {
                    SetHitPoint(HitPoint + damage, impactDirection, attackerScriptComponentBehavior);
                }
            }
            else
            {
                InformationComponent.Instance.SendMessage($"Bau beginnt: {BuildingName}!", 0x02ab89d9, player);
                IsUpgrading = true;
            }

            return true;
        }
    }
}

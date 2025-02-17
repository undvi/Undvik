using PersistentEmpiresLib.NetworkMessages.Server;
using PersistentEmpiresLib.PersistentEmpiresMission.MissionBehaviors;
using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace PersistentEmpiresLib.SceneScripts
{
    public class PE_LadderBuilder : PE_DestructableComponent
    {
        public string LadderEntityTag = "ladder_entity";
        public string RepairItem = "pe_buildhammer";
        public int RequiredEngineeringSkillForRepair = 10;
        public string ParticleEffectOnDestroy = "";
        public string SoundEffectOnDestroy = "";
        public string ParticleEffectOnRepair = "";
        public string SoundEffectOnRepair = "";
        public bool DestroyedByStoneOnly = false;

        public string RepairItemRecipies = "pe_hardwood*2,pe_wooden_stick*1";
        public int RepairDamage = 20;

        private List<RepairReceipt> receipt = new List<RepairReceipt>();
        private bool ladderBuilt = false;
        private bool initialized = false;
        private SiegeLadder siegeLadder;

        protected override void OnInit()
        {
            this.HitPoint = 0;
            ParseRepairReceipts();
        }

        private void ParseRepairReceipts()
        {
            receipt.Clear();
            foreach (string entry in RepairItemRecipies.Split(','))
            {
                string[] parts = entry.Split('*');
                if (parts.Length == 2 && int.TryParse(parts[1], out int count))
                {
                    receipt.Add(new RepairReceipt(parts[0], count));
                }
                else
                {
                    Debug.Print($"[Error] Ungültige Reparatur-Definition: {entry}");
                }
            }
        }

        public override ScriptComponentBehavior.TickRequirement GetTickRequirement()
        {
            return initialized ? ScriptComponentBehavior.TickRequirement.None : ScriptComponentBehavior.TickRequirement.Tick;
        }

        protected override void OnTick(float dt)
        {
            if (!initialized)
            {
                siegeLadder = base.GameEntity.Parent.GetFirstScriptInFamilyDescending<SiegeLadder>();
                if (siegeLadder == null) return;

                siegeLadder.GameEntity.SetVisibilityExcludeParents(ladderBuilt);
                initialized = true;
            }
        }

        public void SetLadderBuilt(bool built)
        {
            if (ladderBuilt == built) return; // Vermeidet unnötige Änderungen

            ladderBuilt = built;
            siegeLadder?.GameEntity.SetVisibilityExcludeParents(built);
        }

        public override void SetHitPoint(float hitPoint, Vec3 impactDirection, ScriptComponentBehavior attackerScriptComponentBehavior)
        {
            if (hitPoint < 0) hitPoint = 0;
            if (hitPoint > MaxHitPoint) hitPoint = MaxHitPoint;

            if (HitPoint == hitPoint) return; // Kein unnötiges Update

            HitPoint = hitPoint;

            if (ladderBuilt && HitPoint <= 0)
            {
                PlayDestructionEffects();
                SetLadderBuilt(false);
            }
            else if (!ladderBuilt && HitPoint >= MaxHitPoint)
            {
                PlayRepairEffects();
                SetLadderBuilt(true);
            }

            if (GameNetwork.IsServer)
            {
                GameNetwork.BeginBroadcastModuleEvent();
                GameNetwork.WriteMessage(new SyncObjectHitpointsPE(this, impactDirection, this.HitPoint));
                GameNetwork.EndBroadcastModuleEvent(GameNetwork.EventBroadcastFlags.AddToMissionRecord, null);
            }
        }

        private void PlayRepairEffects()
        {
            if (!string.IsNullOrEmpty(ParticleEffectOnRepair))
            {
                Mission.Current.Scene.CreateBurstParticle(ParticleSystemManager.GetRuntimeIdByName(ParticleEffectOnRepair), base.GameEntity.GetGlobalFrame());
            }
            if (!string.IsNullOrEmpty(SoundEffectOnRepair))
            {
                Mission.Current.MakeSound(SoundEvent.GetEventIdFromString(SoundEffectOnRepair), base.GameEntity.GetGlobalFrame().origin, false, true, -1, -1);
            }
        }

        private void PlayDestructionEffects()
        {
            if (!string.IsNullOrEmpty(ParticleEffectOnDestroy))
            {
                Mission.Current.Scene.CreateBurstParticle(ParticleSystemManager.GetRuntimeIdByName(ParticleEffectOnDestroy), base.GameEntity.GetGlobalFrame());
            }
            if (!string.IsNullOrEmpty(SoundEffectOnDestroy))
            {
                Mission.Current.MakeSound(SoundEvent.GetEventIdFromString(SoundEffectOnDestroy), base.GameEntity.GetGlobalFrame().origin, false, true, -1, -1);
            }
        }

        protected override bool OnHit(Agent attackerAgent, int damage, Vec3 impactPosition, Vec3 impactDirection, in MissionWeapon weapon, ScriptComponentBehavior attackerScriptComponentBehavior, out bool reportDamage)
        {
            reportDamage = true;

            if (attackerAgent == null || weapon.Item == null || !attackerAgent.IsHuman || !attackerAgent.IsPlayerControlled)
                return false;

            NetworkCommunicator player = attackerAgent.MissionPeer.GetNetworkPeer();
            bool isAdmin = Main.IsPlayerAdmin(player);

            // Admin-Hammer für sofortiges Bauen
            if (isAdmin && weapon.Item.StringId == "pe_adminhammer")
            {
                SetHitPoint(MaxHitPoint, impactDirection, attackerScriptComponentBehavior);
                return true;
            }

            if (weapon.Item.StringId == RepairItem &&
                attackerAgent.Character.GetSkillValue(DefaultSkills.Engineering) >= RequiredEngineeringSkillForRepair &&
                !ladderBuilt)
            {
                if (!HasRequiredMaterials(player))
                {
                    InformPlayerMissingResources(player);
                    return false;
                }

                ConsumeRepairMaterials(player);
                SetHitPoint(HitPoint + RepairDamage, impactDirection, attackerScriptComponentBehavior);
                InformationComponent.Instance.SendMessage($"🔧 {HitPoint}/{MaxHitPoint} repariert!", 0x02ab89d9, player);
                return true;
            }
            else if (!ladderBuilt || (ladderBuilt && siegeLadder.State == SiegeLadder.LadderState.OnLand))
            {
                if (DestroyedByStoneOnly)
                {
                    if (weapon.CurrentUsageItem == null ||
                        (weapon.CurrentUsageItem.WeaponClass != WeaponClass.Stone && weapon.CurrentUsageItem.WeaponClass != WeaponClass.Boulder))
                    {
                        damage = 0;
                    }
                }

                SetHitPoint(HitPoint - damage, impactDirection, attackerScriptComponentBehavior);
                return true;
            }

            return false;
        }

        private bool HasRequiredMaterials(NetworkCommunicator player)
        {
            PersistentEmpireRepresentative rep = player.GetComponent<PersistentEmpireRepresentative>();
            return rep != null && receipt.All(r => rep.GetInventory().IsInventoryIncludes(r.RepairItem, r.NeededCount));
        }

        private void ConsumeRepairMaterials(NetworkCommunicator player)
        {
            PersistentEmpireRepresentative rep = player.GetComponent<PersistentEmpireRepresentative>();
            if (rep == null) return;

            foreach (RepairReceipt r in receipt)
            {
                rep.GetInventory().RemoveCountedItem(r.RepairItem, r.NeededCount);
            }
        }

        private void InformPlayerMissingResources(NetworkCommunicator player)
        {
            InformationComponent.Instance.SendMessage("⚠️ Fehlende Ressourcen für die Reparatur:", 0x02ab89d9, player);
            foreach (RepairReceipt r in receipt)
            {
                InformationComponent.Instance.SendMessage($"❌ {r.NeededCount}x {r.RepairItem.Name}", 0x02ab89d9, player);
            }
        }
    }
}

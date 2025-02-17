using PersistentEmpiresLib.NetworkMessages.Server;
using PersistentEmpiresLib.PersistentEmpiresMission.MissionBehaviors;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace PersistentEmpiresLib.SceneScripts
{
    public class PE_SiegeTowerBuilder : PE_DestructableComponent
    {
        public int RequiredEngineeringSkillForRepair = 10;
        public string RepairItemRecipies = "pe_hardwood*2,pe_wooden_stick*1";
        public string RepairItem = "pe_buildhammer";
        public string ParticleEffectOnDestroy = "";
        public string SoundEffectOnDestroy = "";
        public string ParticleEffectOnRepair = "";
        public string SoundEffectOnRepair = "";
        public int RepairDamage = 20;

        private List<RepairReceipt> receipt = new List<RepairReceipt>();
        private bool siegeTowerBuilt = false;
        private bool initialized = false;
        private bool siegeTowerDestroyed = false;
        private long resetAt = 0;
        private SiegeTower siegeTower;

        protected override void OnInit()
        {
            this.HitPoint = 0;
            this.ParseRepairReceipts();
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

        protected override void OnTick(float dt)
        {
            try
            {
                if (!initialized)
                {
                    this.siegeTower = base.GameEntity.Parent.GetFirstScriptInFamilyDescending<SiegeTower>();
                    if (this.siegeTower == null) return;
                    this.siegeTower.DestructionComponent.OnDestroyed += this.SiegeTowerOnDestroyed;
                    this.siegeTower.GameEntity.SetVisibilityExcludeParents(false);
                    initialized = true;
                }
            }
            catch (Exception e)
            {
                Debug.Print($"[Error] OnTick Exception: {e.Message}");
            }

            if (siegeTowerDestroyed && DateTimeOffset.UtcNow.ToUnixTimeSeconds() >= resetAt)
            {
                this.Reset();
                if (GameNetwork.IsServer)
                {
                    GameNetwork.BeginBroadcastModuleEvent();
                    GameNetwork.WriteMessage(new ResetSiegeTower(this));
                    GameNetwork.EndBroadcastModuleEvent(GameNetwork.EventBroadcastFlags.None);
                }
            }
        }

        private void SiegeTowerOnDestroyed(DestructableComponent target, Agent attackerAgent, in MissionWeapon weapon, ScriptComponentBehavior attackerScriptComponentBehavior, int inflictedDamage)
        {
            siegeTowerDestroyed = true;
            resetAt = DateTimeOffset.UtcNow.ToUnixTimeSeconds() + 5;
        }

        public override ScriptComponentBehavior.TickRequirement GetTickRequirement()
        {
            return initialized ? ScriptComponentBehavior.TickRequirement.None : ScriptComponentBehavior.TickRequirement.Tick;
        }

        public void Reset()
        {
            if (siegeTower == null) return;

            MethodInfo resetMethod = typeof(SiegeTower).GetMethod("OnMissionReset", BindingFlags.NonPublic | BindingFlags.Instance);
            resetMethod?.Invoke(siegeTower, new object[] { });

            siegeTower.DestructionComponent.Reset();
            siegeTower.GameEntity.SetVisibilityExcludeParents(false);
            siegeTowerBuilt = false;
            SetHitPoint(0, new Vec3(0, 0, 0), null);
            siegeTowerDestroyed = false;
        }

        public override void SetHitPoint(float hitPoint, Vec3 impactDirection, ScriptComponentBehavior attackerScriptComponentBehavior)
        {
            if (hitPoint < 0) hitPoint = 0;
            if (hitPoint > MaxHitPoint) hitPoint = MaxHitPoint;

            HitPoint = hitPoint;

            if (!siegeTowerBuilt && HitPoint >= MaxHitPoint)
            {
                PlayEffectsAndSounds();
                siegeTower.GameEntity.SetVisibilityExcludeParents(true);
                siegeTowerBuilt = true;
            }

            if (GameNetwork.IsServer)
            {
                GameNetwork.BeginBroadcastModuleEvent();
                GameNetwork.WriteMessage(new SyncObjectHitpointsPE(this, impactDirection, this.HitPoint));
                GameNetwork.EndBroadcastModuleEvent(GameNetwork.EventBroadcastFlags.AddToMissionRecord, null);
            }
        }

        private void PlayEffectsAndSounds()
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

        protected override bool OnHit(Agent attackerAgent, int damage, Vec3 impactPosition, Vec3 impactDirection, in MissionWeapon weapon, ScriptComponentBehavior attackerScriptComponentBehavior, out bool reportDamage)
        {
            reportDamage = true;
            if (attackerAgent == null || weapon.Item == null || !attackerAgent.IsHuman || !attackerAgent.IsPlayerControlled) return false;

            NetworkCommunicator player = attackerAgent.MissionPeer.GetNetworkPeer();
            bool isAdmin = Main.IsPlayerAdmin(player);

            if (isAdmin && weapon.Item.StringId == "pe_adminhammer")
            {
                SetHitPoint(MaxHitPoint, impactDirection, attackerScriptComponentBehavior);
                return true;
            }

            if (weapon.Item.StringId != RepairItem) return false;

            if (attackerAgent.Character.GetSkillValue(DefaultSkills.Engineering) >= RequiredEngineeringSkillForRepair && HitPoint < MaxHitPoint && !siegeTowerBuilt)
            {
                PersistentEmpireRepresentative rep = player.GetComponent<PersistentEmpireRepresentative>();
                if (rep == null) return false;

                if (!receipt.All(r => rep.GetInventory().IsInventoryIncludes(r.RepairItem, r.NeededCount)))
                {
                    InformPlayerMissingResources(player);
                    return false;
                }

                foreach (RepairReceipt r in receipt)
                {
                    rep.GetInventory().RemoveCountedItem(r.RepairItem, r.NeededCount);
                }

                SetHitPoint(HitPoint + RepairDamage, impactDirection, attackerScriptComponentBehavior);
                InformationComponent.Instance.SendMessage($"🔧 {HitPoint}/{MaxHitPoint} repariert!", 0x02ab89d9, player);
                return true;
            }

            return false;
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

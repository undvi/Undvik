using System;
using System.Collections.Generic;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Network.Messages;
using TaleWorlds.ObjectSystem;
using PersistentEmpiresLib.SceneScripts;
using PersistentEmpiresLib.Helpers;

namespace PersistentEmpiresLib.NetworkMessages.Server.StructuralSync
{
    #region Add / Modify Object Properties

    // Fügt BodyFlags zu einem MissionObject hinzu.
    [DefineGameNetworkMessageTypeForMod(GameNetworkMessageSendType.FromServer)]
    public sealed class AddMissionObjectBodyFlagPE : GameNetworkMessage
    {
        public MissionObject MissionObject { get; private set; }
        public BodyFlags BodyFlags { get; private set; }
        public bool ApplyToChildren { get; private set; }

        public AddMissionObjectBodyFlagPE() { }

        public AddMissionObjectBodyFlagPE(MissionObject missionObject, BodyFlags bodyFlags, bool applyToChildren)
        {
            MissionObject = missionObject;
            BodyFlags = bodyFlags;
            ApplyToChildren = applyToChildren;
        }

        protected override MultiplayerMessageFilter OnGetLogFilter() => MultiplayerMessageFilter.MissionObjectsDetailed;
        protected override string OnGetLogFormat()
        {
            return $"Add bodyflags: {BodyFlags} to MissionObject with ID: {MissionObject?.Id ?? -1} and name: {MissionObject?.GameEntity.Name ?? "unbekannt"}{(ApplyToChildren ? "" : " (nicht für Kinder)")}";
        }

        protected override bool OnRead()
        {
            bool result = true;
            MissionObject = Mission.MissionNetworkHelper.GetMissionObjectFromMissionObjectId(
                GameNetworkMessage.ReadMissionObjectIdFromPacket(ref result));
            BodyFlags = (BodyFlags)GameNetworkMessage.ReadIntFromPacket(CompressionBasic.FlagsCompressionInfo, ref result);
            ApplyToChildren = GameNetworkMessage.ReadBoolFromPacket(ref result);
            return result;
        }

        protected override void OnWrite()
        {
            GameNetworkMessage.WriteMissionObjectIdToPacket(MissionObject?.Id ?? -1);
            GameNetworkMessage.WriteIntToPacket((int)BodyFlags, CompressionBasic.FlagsCompressionInfo);
            GameNetworkMessage.WriteBoolToPacket(ApplyToChildren);
        }
    }

    // Fügt physikalische Eigenschaften zu einem MissionObject hinzu.
    [DefineGameNetworkMessageTypeForMod(GameNetworkMessageSendType.FromServer)]
    public sealed class AddPhysicsToMissionObject : GameNetworkMessage
    {
        public MissionObject MissionObject { get; private set; }
        public Vec3 InitialVelocity { get; set; }
        public Vec3 AngularVelocity { get; set; }
        public string PhysicsMaterial { get; set; }

        public AddPhysicsToMissionObject() { }
        public AddPhysicsToMissionObject(MissionObject missionObject, Vec3 initialVelocity, Vec3 angularVelocity, string physicsMaterial)
        {
            MissionObject = missionObject;
            InitialVelocity = initialVelocity;
            AngularVelocity = angularVelocity;
            PhysicsMaterial = physicsMaterial;
        }

        protected override MultiplayerMessageFilter OnGetLogFilter() => MultiplayerMessageFilter.MissionObjectsDetailed;
        protected override string OnGetLogFormat() => "Physics added to game object";
        protected override bool OnRead()
        {
            bool result = true;
            MissionObject = Mission.MissionNetworkHelper.GetMissionObjectFromMissionObjectId(
                GameNetworkMessage.ReadMissionObjectIdFromPacket(ref result));
            InitialVelocity = GameNetworkMessage.ReadVec3FromPacket(CompressionMission.SpawnedItemVelocityCompressionInfo, ref result);
            AngularVelocity = GameNetworkMessage.ReadVec3FromPacket(CompressionMission.SpawnedItemAngularVelocityCompressionInfo, ref result);
            PhysicsMaterial = GameNetworkMessage.ReadStringFromPacket(ref result);
            return result;
        }
        protected override void OnWrite()
        {
            GameNetworkMessage.WriteMissionObjectIdToPacket(MissionObject?.Id ?? -1);
            GameNetworkMessage.WriteVec3ToPacket(InitialVelocity, CompressionMission.SpawnedItemVelocityCompressionInfo);
            GameNetworkMessage.WriteVec3ToPacket(AngularVelocity, CompressionMission.SpawnedItemAngularVelocityCompressionInfo);
            GameNetworkMessage.WriteStringToPacket(PhysicsMaterial);
        }
    }

    #endregion

    #region Resets

    // Setzt die Rüstung eines Agenten zurück.
    [DefineGameNetworkMessageTypeForMod(GameNetworkMessageSendType.FromServer)]
    public sealed class ResetAgentArmor : GameNetworkMessage
    {
        public Agent agent { get; set; }
        public Equipment equipment { get; set; }

        public ResetAgentArmor() { }
        public ResetAgentArmor(Agent agent, Equipment equipment)
        {
            this.agent = agent;
            this.equipment = equipment;
        }

        protected override MultiplayerMessageFilter OnGetLogFilter() => MultiplayerMessageFilter.Equipment;
        protected override string OnGetLogFormat() => "Update agent armors";

        protected override bool OnRead()
        {
            bool result = true;
            agent = Mission.MissionNetworkHelper.GetAgentFromIndex(GameNetworkMessage.ReadAgentIndexFromPacket(ref result));
            equipment = new Equipment();
            for (EquipmentIndex idx = EquipmentIndex.Weapon0; idx < EquipmentIndex.NumEquipmentSetSlots; idx++)
            {
                equipment.AddEquipmentToSlotWithoutAgent(idx, ModuleNetworkData.ReadItemReferenceFromPacket(MBObjectManager.Instance, ref result));
            }
            return result;
        }
        protected override void OnWrite()
        {
            GameNetworkMessage.WriteAgentIndexToPacket(agent.Index);
            for (EquipmentIndex idx = EquipmentIndex.Weapon0; idx < EquipmentIndex.NumEquipmentSetSlots; idx++)
            {
                ModuleNetworkData.WriteItemReferenceToPacket(equipment.GetEquipmentFromSlot(idx));
            }
        }
    }

    // Setzt einen Belagerungsram zurück.
    [DefineGameNetworkMessageTypeForMod(GameNetworkMessageSendType.FromServer)]
    public sealed class ResetBatteringRam : GameNetworkMessage
    {
        public MissionObject BatteringRam { get; private set; }

        public ResetBatteringRam() { }
        public ResetBatteringRam(MissionObject batteringRam)
        {
            BatteringRam = batteringRam;
        }

        protected override MultiplayerMessageFilter OnGetLogFilter() => MultiplayerMessageFilter.None;
        protected override string OnGetLogFormat() => "ResetBatteringRam";
        protected override bool OnRead()
        {
            bool result = true;
            BatteringRam = Mission.MissionNetworkHelper.GetMissionObjectFromMissionObjectId(
                GameNetworkMessage.ReadMissionObjectIdFromPacket(ref result));
            return result;
        }
        protected override void OnWrite()
        {
            GameNetworkMessage.WriteMissionObjectIdToPacket(BatteringRam?.Id ?? -1);
        }
    }

    // Setzt ein zerstörbares Objekt zurück.
    [DefineGameNetworkMessageTypeForMod(GameNetworkMessageSendType.FromServer)]
    public sealed class ResetDestructableItem : GameNetworkMessage
    {
        public MissionObject MissionObject { get; private set; }

        public ResetDestructableItem() { }
        public ResetDestructableItem(MissionObject missionObject)
        {
            MissionObject = missionObject;
        }
        protected override MultiplayerMessageFilter OnGetLogFilter() => MultiplayerMessageFilter.MissionObjectsDetailed | MultiplayerMessageFilter.SiegeWeaponsDetailed;
        protected override string OnGetLogFormat()
        {
            return $"Reset Object: {MissionObject?.Id ?? -1} and name: {MissionObject?.GameEntity.Name ?? "unbekannt"}";
        }
        protected override bool OnRead()
        {
            bool result = true;
            MissionObject = Mission.MissionNetworkHelper.GetMissionObjectFromMissionObjectId(
                GameNetworkMessage.ReadMissionObjectIdFromPacket(ref result));
            return result;
        }
        protected override void OnWrite()
        {
            GameNetworkMessage.WriteMissionObjectIdToPacket(MissionObject?.Id ?? -1);
        }
    }

    // Setzt einen Belagerungsturm zurück.
    [DefineGameNetworkMessageTypeForMod(GameNetworkMessageSendType.FromServer)]
    public sealed class ResetSiegeTower : GameNetworkMessage
    {
        public MissionObject SiegeTower { get; private set; }

        public ResetSiegeTower() { }
        public ResetSiegeTower(MissionObject siegeTower)
        {
            SiegeTower = siegeTower;
        }
        protected override MultiplayerMessageFilter OnGetLogFilter() => MultiplayerMessageFilter.None;
        protected override string OnGetLogFormat() => "ResetSiegeTower";
        protected override bool OnRead()
        {
            bool result = true;
            SiegeTower = Mission.MissionNetworkHelper.GetMissionObjectFromMissionObjectId(
                GameNetworkMessage.ReadMissionObjectIdFromPacket(ref result));
            return result;
        }
        protected override void OnWrite()
        {
            GameNetworkMessage.WriteMissionObjectIdToPacket(SiegeTower?.Id ?? -1);
        }
    }

    #endregion

    #region Hitpoints Synchronization

    // Vereinheitlichte Klasse zur Synchronisation der Trefferpunkte von Objekten.
    // Diese ersetzt die vorherigen Varianten "SyncObjectHitpointsForDestructibleWithItem" und "SyncObjectHitpointsPE".
    [DefineGameNetworkMessageTypeForMod(GameNetworkMessageSendType.FromServer)]
    public sealed class SyncObjectHitpoints : GameNetworkMessage
    {
        public MissionObject MissionObject { get; private set; }
        public float Hitpoints { get; private set; }
        public Vec3 ImpactDirection { get; private set; }

        public SyncObjectHitpoints() { }
        public SyncObjectHitpoints(MissionObject missionObject, Vec3 impactDirection, float hitpoints)
        {
            MissionObject = missionObject;
            ImpactDirection = impactDirection;
            Hitpoints = hitpoints;
        }
        protected override bool OnRead()
        {
            bool result = true;
            MissionObject = Mission.MissionNetworkHelper.GetMissionObjectFromMissionObjectId(
                GameNetworkMessage.ReadMissionObjectIdFromPacket(ref result));
            Hitpoints = GameNetworkMessage.ReadFloatFromPacket(CompressionMission.UsableGameObjectHealthCompressionInfo, ref result);
            ImpactDirection = GameNetworkMessage.ReadVec3FromPacket(CompressionMission.UsableGameObjectBlowDirection, ref result);
            return result;
        }
        protected override void OnWrite()
        {
            GameNetworkMessage.WriteMissionObjectIdToPacket(MissionObject?.Id ?? -1);
            GameNetworkMessage.WriteFloatToPacket(MathF.Max(Hitpoints, 0f), CompressionMission.UsableGameObjectHealthCompressionInfo);
            GameNetworkMessage.WriteVec3ToPacket(ImpactDirection, CompressionMission.UsableGameObjectBlowDirection);
        }
        protected override MultiplayerMessageFilter OnGetLogFilter() =>
            MultiplayerMessageFilter.MissionObjectsDetailed | MultiplayerMessageFilter.SiegeWeaponsDetailed;
        protected override string OnGetLogFormat()
        {
            return $"Synchronize HitPoints: {Hitpoints} of MissionObject with Id: {MissionObject?.Id ?? -1} and name: {MissionObject?.GameEntity.Name ?? "unbekannt"}";
        }
    }

    #endregion

    #region Building / Gathering Updates

    // Aktualisiert Castle-Daten
    [DefineGameNetworkMessageTypeForMod(GameNetworkMessageSendType.FromServer)]
    public sealed class UpdateCastle : GameNetworkMessage
    {
        public PE_CastleBanner CastleBanner { get; set; }
        public UpdateCastle() { }
        public UpdateCastle(PE_CastleBanner castleBanner)
        {
            CastleBanner = castleBanner;
        }
        protected override MultiplayerMessageFilter OnGetLogFilter() => MultiplayerMessageFilter.MissionObjects;
        protected override string OnGetLogFormat() => "Castle Updated";
        protected override bool OnRead()
        {
            bool result = true;
            CastleBanner = (PE_CastleBanner)Mission.MissionNetworkHelper.GetMissionObjectFromMissionObjectId(
                GameNetworkMessage.ReadMissionObjectIdFromPacket(ref result));
            CastleBanner.CastleIndex = GameNetworkMessage.ReadIntFromPacket(new CompressionInfo.Integer(0, 200), ref result);
            CastleBanner.FactionIndex = GameNetworkMessage.ReadIntFromPacket(new CompressionInfo.Integer(-1, 200), ref result);
            return result;
        }
        protected override void OnWrite()
        {
            GameNetworkMessage.WriteMissionObjectIdToPacket(CastleBanner?.Id ?? -1);
            GameNetworkMessage.WriteIntToPacket(CastleBanner.CastleIndex, new CompressionInfo.Integer(0, 200));
            GameNetworkMessage.WriteIntToPacket(CastleBanner.FactionIndex, new CompressionInfo.Integer(-1, 200));
        }
    }

    // Aktualisiert, ob ein Item-Gathering-Objekt zerstört wurde.
    [DefineGameNetworkMessageTypeForMod(GameNetworkMessageSendType.FromServer)]
    public sealed class UpdateItemGatheringDestroyed : GameNetworkMessage
    {
        public PE_ItemGathering MissionObject;
        public bool IsDestroyed { get; set; }

        public UpdateItemGatheringDestroyed() { }
        public UpdateItemGatheringDestroyed(PE_ItemGathering missionObject, bool isDestroyed)
        {
            MissionObject = missionObject;
            IsDestroyed = isDestroyed;
        }
        protected override MultiplayerMessageFilter OnGetLogFilter() => MultiplayerMessageFilter.MissionObjects;
        protected override string OnGetLogFormat() => "Received UpdateItemGatheringDestroyed";
        protected override bool OnRead()
        {
            bool result = true;
            MissionObject = (PE_ItemGathering)Mission.MissionNetworkHelper.GetMissionObjectFromMissionObjectId(
                GameNetworkMessage.ReadMissionObjectIdFromPacket(ref result));
            IsDestroyed = GameNetworkMessage.ReadBoolFromPacket(ref result);
            return result;
        }
        protected override void OnWrite()
        {
            GameNetworkMessage.WriteMissionObjectIdToPacket(MissionObject?.Id ?? -1);
            GameNetworkMessage.WriteBoolToPacket(IsDestroyed);
        }
    }

    #endregion
}

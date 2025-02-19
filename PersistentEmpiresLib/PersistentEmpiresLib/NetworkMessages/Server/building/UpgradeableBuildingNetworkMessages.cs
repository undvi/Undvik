using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Network.Messages;

namespace PersistentEmpiresLib.NetworkMessages.Server.UpgradeableBuilding
{
    #region Upgrade Building Tier

    [DefineGameNetworkMessageTypeForMod(GameNetworkMessageSendType.FromServer)]
    public sealed class UpgradeableBuildingSetTier : GameNetworkMessage
    {
        public int Tier { get; private set; }
        public MissionObject UpgradingObject { get; private set; }

        public UpgradeableBuildingSetTier() { }

        public UpgradeableBuildingSetTier(int tier, MissionObject upgradingObject)
        {
            Tier = tier;
            UpgradingObject = upgradingObject;
        }

        protected override MultiplayerMessageFilter OnGetLogFilter() => MultiplayerMessageFilter.MissionObjects;

        protected override string OnGetLogFormat()
        {
            return $"[Upgrade] Gebäude {UpgradingObject?.Id ?? -1} -> Stufe {Tier}";
        }

        protected override bool OnRead()
        {
            bool result = true;
            Tier = GameNetworkMessage.ReadIntFromPacket(new CompressionInfo.Integer(0, 4, true), ref result);
            int missionObjectId = GameNetworkMessage.ReadMissionObjectIdFromPacket(ref result);
            if (result)
            {
                UpgradingObject = Mission.MissionNetworkHelper.GetMissionObjectFromMissionObjectId(missionObjectId);
                if (UpgradingObject == null)
                {
                    Debug.Print($"[Error] UpgradeableBuildingSetTier: MissionObject mit ID {missionObjectId} nicht gefunden!", 0xFF0000);
                    result = false;
                }
            }
            return result;
        }

        protected override void OnWrite()
        {
            GameNetworkMessage.WriteIntToPacket(Tier, new CompressionInfo.Integer(0, 4, true));
            GameNetworkMessage.WriteMissionObjectIdToPacket(UpgradingObject?.Id ?? -1);
        }
    }

    #endregion

    #region Upgrade Building State

    [DefineGameNetworkMessageTypeForMod(GameNetworkMessageSendType.FromServer)]
    public sealed class UpgradeableBuildingUpgrading : GameNetworkMessage
    {
        public bool IsUpgrading { get; private set; }
        public MissionObject UpgradingObject { get; private set; }

        public UpgradeableBuildingUpgrading() { }

        public UpgradeableBuildingUpgrading(bool isUpgrading, MissionObject upgradingObject)
        {
            IsUpgrading = isUpgrading;
            UpgradingObject = upgradingObject;
        }

        protected override MultiplayerMessageFilter OnGetLogFilter() => MultiplayerMessageFilter.MissionObjects;

        protected override string OnGetLogFormat()
        {
            return $"[Upgrade] Gebäude: {UpgradingObject?.Id ?? -1} | Status: {(IsUpgrading ? "Start" : "Stop")}";
        }

        protected override bool OnRead()
        {
            bool result = true;
            IsUpgrading = GameNetworkMessage.ReadBoolFromPacket(ref result);
            int missionObjectId = GameNetworkMessage.ReadMissionObjectIdFromPacket(ref result);
            if (result)
            {
                UpgradingObject = Mission.MissionNetworkHelper.GetMissionObjectFromMissionObjectId(missionObjectId);
                if (UpgradingObject == null)
                {
                    Debug.Print($"[Error] UpgradeableBuildingUpgrading: MissionObject mit ID {missionObjectId} nicht gefunden!", 0xFF0000);
                    result = false;
                }
            }
            return result;
        }

        protected override void OnWrite()
        {
            GameNetworkMessage.WriteBoolToPacket(IsUpgrading);
            GameNetworkMessage.WriteMissionObjectIdToPacket(UpgradingObject?.Id ?? -1);
        }
    }

    #endregion
}

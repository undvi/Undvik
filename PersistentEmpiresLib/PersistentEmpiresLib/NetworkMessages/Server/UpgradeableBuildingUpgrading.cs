using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Network.Messages;

namespace PersistentEmpiresLib.NetworkMessages.Server
{
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

        protected override MultiplayerMessageFilter OnGetLogFilter()
        {
            return MultiplayerMessageFilter.MissionObjects;
        }

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
}

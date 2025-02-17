using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Network.Messages;

namespace PersistentEmpiresLib.NetworkMessages.Server
{
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

        protected override MultiplayerMessageFilter OnGetLogFilter()
        {
            return MultiplayerMessageFilter.MissionObjects;
        }

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
}

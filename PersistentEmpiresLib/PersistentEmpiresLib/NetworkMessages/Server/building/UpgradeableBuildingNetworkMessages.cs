// File: UpgradeableBuildingNetworkMessages.cs
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Network.Messages;
using TaleWorlds.Library;

namespace PersistentEmpiresLib.NetworkMessages.Server.UpgradeableBuilding
{
    [DefineGameNetworkMessageTypeForMod(GameNetworkMessageSendType.FromServer)]
    public sealed class UpgradeableBuildingSetTier : GameNetworkMessage
    {
        public int Tier { get; private set; }
        public MissionObject UpgradingObject { get; private set; }
        public int FactionIndex { get; private set; }
        public int RequiredWood { get; private set; }
        public int RequiredStone { get; private set; }
        public int RequiredIron { get; private set; }

        public UpgradeableBuildingSetTier() { }

        public UpgradeableBuildingSetTier(int tier, MissionObject upgradingObject, int factionIndex, int requiredWood, int requiredStone, int requiredIron)
        {
            Tier = tier;
            UpgradingObject = upgradingObject;
            FactionIndex = factionIndex;
            RequiredWood = requiredWood;
            RequiredStone = requiredStone;
            RequiredIron = requiredIron;
        }

        protected override MultiplayerMessageFilter OnGetLogFilter()
            => MultiplayerMessageFilter.MissionObjects;

        protected override string OnGetLogFormat()
            => $"[Upgrade] Gebäude {UpgradingObject?.Id ?? -1} -> Stufe {Tier}, Faction {FactionIndex}, Holz {RequiredWood}, Stein {RequiredStone}, Eisen {RequiredIron}";

        protected override bool OnRead()
        {
            bool result = true;
            Tier = GameNetworkMessage.ReadIntFromPacket(new CompressionInfo.Integer(0, 4, true), ref result);
            int missionObjectId = GameNetworkMessage.ReadMissionObjectIdFromPacket(ref result);
            FactionIndex = GameNetworkMessage.ReadIntFromPacket(new CompressionInfo.Integer(-1, 1000, true), ref result);
            RequiredWood = GameNetworkMessage.ReadIntFromPacket(new CompressionInfo.Integer(0, 999999, true), ref result);
            RequiredStone = GameNetworkMessage.ReadIntFromPacket(new CompressionInfo.Integer(0, 999999, true), ref result);
            RequiredIron = GameNetworkMessage.ReadIntFromPacket(new CompressionInfo.Integer(0, 999999, true), ref result);

            if (!result) return false;

            UpgradingObject = Mission.MissionNetworkHelper.GetMissionObjectFromMissionObjectId(missionObjectId);
            return UpgradingObject != null;
        }

        protected override void OnWrite()
        {
            GameNetworkMessage.WriteIntToPacket(Tier, new CompressionInfo.Integer(0, 4, true));
            GameNetworkMessage.WriteMissionObjectIdToPacket(UpgradingObject?.Id ?? -1);
            GameNetworkMessage.WriteIntToPacket(FactionIndex, new CompressionInfo.Integer(-1, 1000, true));
            GameNetworkMessage.WriteIntToPacket(RequiredWood, new CompressionInfo.Integer(0, 999999, true));
            GameNetworkMessage.WriteIntToPacket(RequiredStone, new CompressionInfo.Integer(0, 999999, true));
            GameNetworkMessage.WriteIntToPacket(RequiredIron, new CompressionInfo.Integer(0, 999999, true));
        }
    }
}
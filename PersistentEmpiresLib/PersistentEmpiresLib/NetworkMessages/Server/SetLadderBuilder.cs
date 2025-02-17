using PersistentEmpiresLib.SceneScripts;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Network.Messages;

namespace PersistentEmpiresLib.NetworkMessages.Server
{
    [DefineGameNetworkMessageTypeForMod(GameNetworkMessageSendType.FromServer)]
    public sealed class SetLadderBuilder : GameNetworkMessage
    {
        public PE_LadderBuilder LadderBuilder { get; private set; }
        public bool LadderBuilt { get; private set; }

        public SetLadderBuilder() { }

        public SetLadderBuilder(PE_LadderBuilder ladderBuilder, bool ladderBuilt)
        {
            LadderBuilder = ladderBuilder;
            LadderBuilt = ladderBuilt;
        }

        protected override MultiplayerMessageFilter OnGetLogFilter()
        {
            return MultiplayerMessageFilter.MissionObjects;
        }

        protected override string OnGetLogFormat()
        {
            return $"[Ladder] ID: {LadderBuilder?.Id ?? -1} | Status: {(LadderBuilt ? "Built" : "Not Built")}";
        }

        protected override bool OnRead()
        {
            bool result = true;

            int missionObjectId = GameNetworkMessage.ReadMissionObjectIdFromPacket(ref result);
            LadderBuilder = Mission.MissionNetworkHelper.GetMissionObjectFromMissionObjectId(missionObjectId) as PE_LadderBuilder;
            LadderBuilt = GameNetworkMessage.ReadBoolFromPacket(ref result);

            if (LadderBuilder == null)
            {
                Debug.Print($"[Error] SetLadderBuilder: Leiter-Objekt mit ID {missionObjectId} nicht gefunden!", 0xFF0000);
                result = false;
            }

            return result;
        }

        protected override void OnWrite()
        {
            GameNetworkMessage.WriteMissionObjectIdToPacket(LadderBuilder?.Id ?? -1);
            GameNetworkMessage.WriteBoolToPacket(LadderBuilt);
        }
    }
}

using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Network.Messages;

namespace PersistentEmpiresLib.NetworkMessages.Client
{
    [DefineGameNetworkMessageTypeForMod(GameNetworkMessageSendType.FromClient)]
    public sealed class PEAcademyUnlockBlueprint : GameNetworkMessage
    {
        public int BlueprintID { get; private set; }

        public PEAcademyUnlockBlueprint() { }

        public PEAcademyUnlockBlueprint(int blueprintId)
        {
            this.BlueprintID = blueprintId;
        }

        protected override MultiplayerMessageFilter OnGetLogFilter()
        {
            return MultiplayerMessageFilter.MissionObjects;
        }

        protected override string OnGetLogFormat()
        {
            return $"🔓 Spieler möchte Blueprint mit ID {BlueprintID} freischalten.";
        }

        protected override bool OnRead()
        {
            bool result = true;
            this.BlueprintID = GameNetworkMessage.ReadIntFromPacket(new CompressionInfo.Integer(0, 10000, true), ref result);
            return result;
        }

        protected override void OnWrite()
        {
            GameNetworkMessage.WriteIntToPacket(this.BlueprintID, new CompressionInfo.Integer(0, 10000, true));
        }
    }
}

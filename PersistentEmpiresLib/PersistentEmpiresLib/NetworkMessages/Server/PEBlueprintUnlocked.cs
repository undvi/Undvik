using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Network.Messages;

namespace PersistentEmpiresLib.NetworkMessages.Server
{
    [DefineGameNetworkMessageTypeForMod(GameNetworkMessageSendType.FromServer)]
    public sealed class PEBlueprintUnlocked : GameNetworkMessage
    {
        public NetworkCommunicator Player { get; private set; }
        public string BlueprintID { get; private set; }

        public PEBlueprintUnlocked() { }

        public PEBlueprintUnlocked(NetworkCommunicator player, string blueprintID)
        {
            this.Player = player;
            this.BlueprintID = blueprintID;
        }

        protected override MultiplayerMessageFilter OnGetLogFilter()
        {
            return MultiplayerMessageFilter.MissionObjects;
        }

        protected override string OnGetLogFormat()
        {
            return $"🔓 Blueprint freigeschaltet: {BlueprintID} für {Player.UserName}";
        }

        protected override bool OnRead()
        {
            bool result = true;
            this.Player = GameNetworkMessage.ReadNetworkPeerReferenceFromPacket(ref result);
            this.BlueprintID = GameNetworkMessage.ReadStringFromPacket(ref result);
            return result;
        }

        protected override void OnWrite()
        {
            GameNetworkMessage.WriteNetworkPeerReferenceToPacket(this.Player);
            GameNetworkMessage.WriteStringToPacket(this.BlueprintID);
        }
    }
}

using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Network.Messages;

namespace PersistentEmpiresLib.NetworkMessages.Client
{
    [DefineGameNetworkMessageTypeForMod(GameNetworkMessageSendType.FromClient)]
    public sealed class PEAcademyUnlockBlueprint : GameNetworkMessage
    {
        public NetworkCommunicator Player { get; private set; }
        public int BlueprintID { get; private set; }

        public PEAcademyUnlockBlueprint() { }

        public PEAcademyUnlockBlueprint(NetworkCommunicator player, int blueprintId)
        {
            this.Player = player ?? throw new System.ArgumentNullException(nameof(player), "❌ Fehler: Spieler ist null in PEAcademyUnlockBlueprint!");
            this.BlueprintID = blueprintId;
        }

        protected override MultiplayerMessageFilter OnGetLogFilter()
        {
            return MultiplayerMessageFilter.MissionObjects;
        }

        protected override string OnGetLogFormat()
        {
            return this.Player != null
                ? $"🔓 {Player.UserName} möchte Blueprint mit ID {BlueprintID} freischalten."
                : "⚠️ Fehler: Kein gültiger Spieler für Blueprint-Freischaltung!";
        }

        protected override bool OnRead()
        {
            bool result = true;
            this.Player = GameNetworkMessage.ReadNetworkPeerReferenceFromPacket(ref result);
            this.BlueprintID = GameNetworkMessage.ReadIntFromPacket(new CompressionInfo.Integer(0, 10000, true), ref result);
            return result;
        }

        protected override void OnWrite()
        {
            GameNetworkMessage.WriteNetworkPeerReferenceToPacket(this.Player);
            GameNetworkMessage.WriteIntToPacket(this.BlueprintID, new CompressionInfo.Integer(0, 10000, true));
        }
    }
}

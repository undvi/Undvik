using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Network.Messages;

namespace PersistentEmpiresLib.NetworkMessages.Client
{
	[DefineGameNetworkMessageTypeForMod(GameNetworkMessageSendType.FromClient)]
	public sealed class PEAcademyEnter : GameNetworkMessage
	{
		public NetworkCommunicator Player { get; private set; }

		public PEAcademyEnter() { }

		public PEAcademyEnter(NetworkCommunicator player)
		{
			this.Player = player ?? throw new System.ArgumentNullException(nameof(player), "❌ Fehler: Spieler ist null in PEAcademyEnter!");
		}

		protected override MultiplayerMessageFilter OnGetLogFilter()
		{
			return MultiplayerMessageFilter.MissionObjects;
		}

		protected override string OnGetLogFormat()
		{
			return this.Player != null
				? $"📜 {Player.UserName} möchte die Akademie betreten."
				: "⚠️ Fehler: Kein gültiger Spieler für Akademie-Beitritt!";
		}

		protected override bool OnRead()
		{
			bool result = true;
			this.Player = GameNetworkMessage.ReadNetworkPeerReferenceFromPacket(ref result);
			return result;
		}

		protected override void OnWrite()
		{
			GameNetworkMessage.WriteNetworkPeerReferenceToPacket(this.Player);
		}
	}
}

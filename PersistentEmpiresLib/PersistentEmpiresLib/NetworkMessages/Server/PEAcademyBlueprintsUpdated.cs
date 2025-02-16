using System.Collections.Generic;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Network.Messages;

namespace PersistentEmpiresLib.NetworkMessages.Server
{
	[DefineGameNetworkMessageTypeForMod(GameNetworkMessageSendType.FromServer)]
	public sealed class PEAcademyBlueprintsUpdated : GameNetworkMessage
	{
		public NetworkCommunicator Player { get; private set; }
		public List<string> Blueprints { get; private set; }

		public PEAcademyBlueprintsUpdated() { }

		public PEAcademyBlueprintsUpdated(NetworkCommunicator player, List<string> blueprints)
		{
			this.Player = player ?? throw new System.ArgumentNullException(nameof(player), "❌ Fehler: Spieler ist null in PEAcademyBlueprintsUpdated!");
			this.Blueprints = blueprints ?? new List<string>();
		}

		protected override MultiplayerMessageFilter OnGetLogFilter()
		{
			return MultiplayerMessageFilter.MissionObjects;
		}

		protected override string OnGetLogFormat()
		{
			return this.Player != null
				? $"📜 Blueprints aktualisiert für {Player.UserName}: {string.Join(", ", Blueprints)}"
				: "⚠️ Fehler: Spieler NULL bei Blueprints-Update!";
		}

		protected override bool OnRead()
		{
			bool result = true;
			this.Player = GameNetworkMessage.ReadNetworkPeerReferenceFromPacket(ref result);
			int count = GameNetworkMessage.ReadIntFromPacket(new CompressionInfo.Integer(0, 100, true), ref result);
			this.Blueprints = new List<string>();

			for (int i = 0; i < count; i++)
			{
				this.Blueprints.Add(GameNetworkMessage.ReadStringFromPacket(ref result));
			}

			return result;
		}

		protected override void OnWrite()
		{
			if (this.Player == null)
			{
				InformationManager.DisplayMessage(new InformationMessage("⚠️ Fehler: Ungültige Blueprint-Daten für Netzwerksynchronisation!"));
				return;
			}

			GameNetworkMessage.WriteNetworkPeerReferenceToPacket(this.Player);
			GameNetworkMessage.WriteIntToPacket(this.Blueprints.Count, new CompressionInfo.Integer(0, 100, true));

			foreach (var blueprint in this.Blueprints)
			{
				GameNetworkMessage.WriteStringToPacket(blueprint);
			}
		}
	}
}

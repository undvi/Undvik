using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Network.Messages;

namespace PersistentEmpiresLib.NetworkMessages.Server
{
	[DefineGameNetworkMessageTypeForMod(GameNetworkMessageSendType.FromServer)]
	public sealed class PEAcademyBlueprintUnlocked : GameNetworkMessage
	{
		public NetworkCommunicator Player { get; private set; }
		public string BlueprintID { get; private set; }

		public PEAcademyBlueprintUnlocked() { }

		public PEAcademyBlueprintUnlocked(NetworkCommunicator player, string blueprintID)
		{
			this.Player = player ?? throw new System.ArgumentNullException(nameof(player), "❌ Fehler: Spieler ist null in PEAcademyBlueprintUnlocked!");
			this.BlueprintID = blueprintID ?? throw new System.ArgumentNullException(nameof(blueprintID), "❌ Fehler: Blueprint-ID ist null!");
		}

		protected override MultiplayerMessageFilter OnGetLogFilter()
		{
			return MultiplayerMessageFilter.MissionObjects;
		}

		protected override string OnGetLogFormat()
		{
			return this.Player != null
				? $"✅ {Player.UserName} hat das Blueprint {BlueprintID} freigeschaltet!"
				: "⚠️ Fehler: Spieler NULL beim Freischalten eines Blueprints!";
		}

		protected override bool OnRead()
		{
			bool result = true;
			this.Player = GameNetworkMessage.ReadNetworkPeerReferenceFromPacket(ref result);
			this.BlueprintID = GameNetworkMessage.ReadStringFromPacket(ref result);

			if (!result || this.Player == null || string.IsNullOrEmpty(this.BlueprintID))
			{
				InformationManager.DisplayMessage(new InformationMessage("⚠️ Fehler beim Lesen der Blueprint-Daten!"));
				return false;
			}

			return true;
		}

		protected override void OnWrite()
		{
			if (this.Player == null || string.IsNullOrEmpty(this.BlueprintID))
			{
				InformationManager.DisplayMessage(new InformationMessage("⚠️ Fehler: Ungültige Blueprint-Daten für Netzwerksynchronisation!"));
				return;
			}

			GameNetworkMessage.WriteNetworkPeerReferenceToPacket(this.Player);
			GameNetworkMessage.WriteStringToPacket(this.BlueprintID);
		}
	}
}

using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Network.Messages;

namespace PersistentEmpiresLib.NetworkMessages.Server.Notifications
{
	// Lokale Nachricht, die sowohl Bubble- als auch lokale Nachrichten abdeckt.
	[DefineGameNetworkMessageTypeForMod(GameNetworkMessageSendType.FromServer)]
	public sealed class LocalMessage : GameNetworkMessage
	{
		public string Message { get; private set; }
		public NetworkCommunicator Sender { get; private set; }

		public LocalMessage() { }

		public LocalMessage(NetworkCommunicator sender, string message)
		{
			this.Sender = sender;
			this.Message = message;
		}

		protected override MultiplayerMessageFilter OnGetLogFilter() => MultiplayerMessageFilter.General;
		protected override string OnGetLogFormat() => "LocalMessage received";

		protected override bool OnRead()
		{
			bool result = true;
			this.Sender = GameNetworkMessage.ReadNetworkPeerReferenceFromPacket(ref result);
			this.Message = GameNetworkMessage.ReadStringFromPacket(ref result);
			return result;
		}

		protected override void OnWrite()
		{
			GameNetworkMessage.WriteNetworkPeerReferenceToPacket(this.Sender);
			GameNetworkMessage.WriteStringToPacket(this.Message);
		}
	}

	// Einheitliche Informationsnachricht, die beide Varianten zusammenfasst.
	// Falls keine Farbe angegeben wird, wird ein Standardwert (z.B. Weiß: 0xFFFFFFFF) genutzt.
	[DefineGameNetworkMessageTypeForMod(GameNetworkMessageSendType.FromServer)]
	public sealed class UnifiedInformationMessage : GameNetworkMessage
	{
		public string Message { get; private set; }
		public uint Color { get; private set; } // Standardfarbe, falls nicht anders gesetzt

		public UnifiedInformationMessage() { }

		public UnifiedInformationMessage(string message, uint color = 0xFFFFFFFF)
		{
			this.Message = message;
			this.Color = color;
		}

		protected override MultiplayerMessageFilter OnGetLogFilter() => MultiplayerMessageFilter.Administration;
		protected override string OnGetLogFormat() => "Unified information message sent";

		protected override bool OnRead()
		{
			bool result = true;
			this.Message = GameNetworkMessage.ReadStringFromPacket(ref result);
			this.Color = GameNetworkMessage.ReadUintFromPacket(CompressionBasic.ColorCompressionInfo, ref result);
			return result;
		}

		protected override void OnWrite()
		{
			GameNetworkMessage.WriteStringToPacket(this.Message);
			GameNetworkMessage.WriteUintToPacket(this.Color, CompressionBasic.ColorCompressionInfo);
		}
	}

	// Nachricht, um neue Regeln an einen Client zu senden.
	[DefineGameNetworkMessageTypeForMod(GameNetworkMessageSendType.FromServer)]
	public sealed class SendRulesToNewClientMessage : GameNetworkMessage
	{
		public int PackageId { get; private set; }
		public int PackageCount { get; private set; }
		public string ConfigChunk { get; private set; }

		public SendRulesToNewClientMessage() { }

		public SendRulesToNewClientMessage(int packageId, int packageCount, string configChunk)
		{
			this.PackageId = packageId;
			this.PackageCount = packageCount;
			this.ConfigChunk = configChunk;
		}

		protected override MultiplayerMessageFilter OnGetLogFilter() => MultiplayerMessageFilter.None;
		protected override string OnGetLogFormat() => "SendRulesToNewClientMessage";

		protected override bool OnRead()
		{
			bool bufferReadValid = true;
			PackageId = ReadIntFromPacket(new CompressionInfo.Integer(0, 1000, true), ref bufferReadValid);
			PackageCount = ReadIntFromPacket(new CompressionInfo.Integer(0, 1000, true), ref bufferReadValid);
			ConfigChunk = ReadStringFromPacket(ref bufferReadValid);
			return bufferReadValid;
		}

		protected override void OnWrite()
		{
			WriteIntToPacket(PackageId, new CompressionInfo.Integer(0, 1000, true));
			WriteIntToPacket(PackageCount, new CompressionInfo.Integer(0, 1000, true));
			WriteStringToPacket(ConfigChunk);
		}
	}
}

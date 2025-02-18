using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Network.Messages;
using PersistentEmpiresLib.Factions;

namespace PersistentEmpiresLib.NetworkMessages.Client
{
	public class DeclareConquestRequest : GameNetworkMessage
	{
		public enum ConquestActionType
		{
			CaptureTerritory,
			AssignAsVassal,
			PillageTerritory
		}

		public int FactionId { get; private set; }
		public int TargetCastleId { get; private set; }
		public ConquestActionType Action { get; private set; }

		public DeclareConquestRequest(int factionId, int targetCastleId, ConquestActionType action)
		{
			this.FactionId = factionId;
			this.TargetCastleId = targetCastleId;
			this.Action = action;
		}

		public DeclareConquestRequest()
		{
		}

		protected override bool OnRead()
		{
			bool bufferReadValid = true;
			this.FactionId = GameNetworkMessage.ReadIntFromPacket(ref bufferReadValid);
			this.TargetCastleId = GameNetworkMessage.ReadIntFromPacket(ref bufferReadValid);
			this.Action = (ConquestActionType)GameNetworkMessage.ReadIntFromPacket(ref bufferReadValid);
			return bufferReadValid;
		}

		protected override void OnWrite()
		{
			GameNetworkMessage.WriteIntToPacket(this.FactionId);
			GameNetworkMessage.WriteIntToPacket(this.TargetCastleId);
			GameNetworkMessage.WriteIntToPacket((int)this.Action);
		}

		protected override MultiplayerMessageFilter OnGetLogFilter()
		{
			return MultiplayerMessageFilter.FactionManagement;
		}

		protected override string OnGetLogFormat()
		{
			return $"Conquest Request: {Action} by Faction {FactionId} on Castle {TargetCastleId}";
		}
	}
}

using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Network.Messages;
using TaleWorlds.ObjectSystem;
using PersistentEmpiresLib.Factions;
using System;

namespace PersistentEmpiresLib.NetworkMessages.Client
{
	public class DeclareFactionActionRequest : GameNetworkMessage
	{
		public enum FactionActionType
		{
			AddMember,
			RemoveMember,
			UpgradeRank,
			ChangeLeader,
			AssignMarshall
		}

		public int FactionId { get; private set; }
		public FactionActionType Action { get; private set; }
		public string TargetPlayerId { get; private set; }

		public DeclareFactionActionRequest(int factionId, FactionActionType action, string targetPlayerId = "")
		{
			this.FactionId = factionId;
			this.Action = action;
			this.TargetPlayerId = targetPlayerId;
		}

		public DeclareFactionActionRequest()
		{
		}

		protected override bool OnRead()
		{
			bool bufferReadValid = true;
			this.FactionId = GameNetworkMessage.ReadIntFromPacket(ref bufferReadValid);
			this.Action = (FactionActionType)GameNetworkMessage.ReadIntFromPacket(ref bufferReadValid);
			this.TargetPlayerId = GameNetworkMessage.ReadStringFromPacket(ref bufferReadValid);
			return bufferReadValid;
		}

		protected override void OnWrite()
		{
			GameNetworkMessage.WriteIntToPacket(this.FactionId);
			GameNetworkMessage.WriteIntToPacket((int)this.Action);
			GameNetworkMessage.WriteStringToPacket(this.TargetPlayerId);
		}

		protected override MultiplayerMessageFilter OnGetLogFilter()
		{
			return MultiplayerMessageFilter.FactionManagement;
		}

		protected override string OnGetLogFormat()
		{
			return $"Faction Action Request: {Action} on Faction {FactionId} for Player {TargetPlayerId}";
		}
	}
}

using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Network.Messages;
using PersistentEmpiresLib.Factions;
using PersistentEmpiresLib.NetworkMessages.Client;
using System;

namespace PersistentEmpiresLib.NetworkMessages.Server
{
	public class HandleFactionAction : GameNetworkMessage
	{
		protected override bool OnRead()
		{
			bool bufferReadValid = true;
			int factionId = GameNetworkMessage.ReadIntFromPacket(ref bufferReadValid);
			DeclareFactionActionRequest.FactionActionType action = (DeclareFactionActionRequest.FactionActionType)GameNetworkMessage.ReadIntFromPacket(ref bufferReadValid);
			string targetPlayerId = GameNetworkMessage.ReadStringFromPacket(ref bufferReadValid);

			if (bufferReadValid)
			{
				ExecuteFactionAction(factionId, action, targetPlayerId);
			}
			return bufferReadValid;
		}

		protected override void OnWrite()
		{
			// Server-seitige Verarbeitung, kein Schreiben nötig
		}

		private void ExecuteFactionAction(int factionId, DeclareFactionActionRequest.FactionActionType action, string targetPlayerId)
		{
			Faction faction = FactionManager.GetFactionById(factionId);
			if (faction == null)
			{
				return;
			}

			switch (action)
			{
				case DeclareFactionActionRequest.FactionActionType.AddMember:
					if (faction.CanAddMember() && !string.IsNullOrEmpty(targetPlayerId))
					{
						faction.AddMember(targetPlayerId);
						GameNetwork.BroadcastNetworkMessage(new SyncFaction(faction));
					}
					break;

				case DeclareFactionActionRequest.FactionActionType.RemoveMember:
					faction.RemoveMember(targetPlayerId);
					GameNetwork.BroadcastNetworkMessage(new SyncFaction(faction));
					break;

				case DeclareFactionActionRequest.FactionActionType.UpgradeRank:
					if (faction.CanUpgradeFaction())
					{
						faction.UpgradeFactionRank();
						GameNetwork.BroadcastNetworkMessage(new SyncFaction(faction));
					}
					break;

				case DeclareFactionActionRequest.FactionActionType.ChangeLeader:
					faction.SelectNewLeader(targetPlayerId);
					GameNetwork.BroadcastNetworkMessage(new SyncFaction(faction));
					break;

				case DeclareFactionActionRequest.FactionActionType.AssignMarshall:
					faction.AssignMarshall(targetPlayerId);
					GameNetwork.BroadcastNetworkMessage(new SyncFaction(faction));
					break;
			}
		}
	}
}

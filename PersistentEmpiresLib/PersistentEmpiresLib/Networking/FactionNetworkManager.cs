using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Network.Messages;
using PersistentEmpiresLib.Factions;
using PersistentEmpiresLib.NetworkMessages.Server;
using PersistentEmpiresLib.NetworkMessages.Client;
using System.Collections.Generic;

namespace PersistentEmpiresLib.Networking
{
	public static class FactionNetworkManager
	{
		public static void SyncFactionToClients(Faction faction)
		{
			if (faction == null) return;
			GameNetwork.BroadcastNetworkMessage(new SyncFaction(faction));
		}

		public static void HandleIncomingFactionRequest(DeclareFactionActionRequest request)
		{
			if (request == null) return;

			Faction faction = FactionManager.GetFactionById(request.FactionId);
			if (faction == null) return;

			HandleFactionAction handler = new HandleFactionAction();
			handler.ExecuteFactionAction(request.FactionId, request.Action, request.TargetPlayerId);
		}

		public static void RegisterNetworkHandlers()
		{
			GameNetwork.AddNetworkHandler<DeclareFactionActionRequest>(HandleIncomingFactionRequest);
			GameNetwork.AddNetworkHandler<SyncFaction>(message =>
			{
				Faction faction = FactionManager.GetFactionById(message.FactionId);
				if (faction != null)
				{
					faction.UpdateFromSyncMessage(message);
				}
			});
		}
	}
}

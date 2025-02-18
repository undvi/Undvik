using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Network.Messages;
using PersistentEmpiresLib.NetworkMessages.Client;
using PersistentEmpiresLib.NetworkMessages.Server;
using PersistentEmpiresLib.Factions;

namespace PersistentEmpiresLib.Networking
{
	public static class NetworkMessageHandler
	{
		public static void RegisterNetworkMessages()
		{
			GameNetwork.AddNetworkHandler<DeclareFactionActionRequest>(HandleFactionActionRequest);
			GameNetwork.AddNetworkHandler<DeclareDiplomacyRequest>(HandleDiplomacyRequest);
			GameNetwork.AddNetworkHandler<SyncFaction>(HandleFactionSync);
		}

		private static void HandleFactionActionRequest(DeclareFactionActionRequest request)
		{
			if (request == null) return;
			FactionNetworkManager.HandleIncomingFactionRequest(request);
		}

		private static void HandleDiplomacyRequest(DeclareDiplomacyRequest request)
		{
			if (request == null) return;
			HandleDiplomacyAction handler = new HandleDiplomacyAction();
			handler.ExecuteDiplomacyAction(request.FactionId, request.TargetFactionId, request.Action);
		}

		private static void HandleFactionSync(SyncFaction message)
		{
			Faction faction = FactionManager.GetFactionById(message.FactionId);
			if (faction != null)
			{
				faction.UpdateFromSyncMessage(message);
			}
		}
	}
}

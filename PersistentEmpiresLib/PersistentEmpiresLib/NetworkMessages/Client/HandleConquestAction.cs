using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Network.Messages;
using PersistentEmpiresLib.Factions;
using PersistentEmpiresLib.NetworkMessages.Client;
using System;

namespace PersistentEmpiresLib.NetworkMessages.Server
{
	public class HandleConquestAction : GameNetworkMessage
	{
		protected override bool OnRead()
		{
			bool bufferReadValid = true;
			int factionId = GameNetworkMessage.ReadIntFromPacket(ref bufferReadValid);
			int targetCastleId = GameNetworkMessage.ReadIntFromPacket(ref bufferReadValid);
			DeclareConquestRequest.ConquestActionType action = (DeclareConquestRequest.ConquestActionType)GameNetworkMessage.ReadIntFromPacket(ref bufferReadValid);

			if (bufferReadValid)
			{
				ExecuteConquestAction(factionId, targetCastleId, action);
			}
			return bufferReadValid;
		}

		protected override void OnWrite()
		{
			// Server-seitige Verarbeitung, kein Schreiben nötig
		}

		private void ExecuteConquestAction(int factionId, int targetCastleId, DeclareConquestRequest.ConquestActionType action)
		{
			Faction faction = FactionManager.GetFactionById(factionId);
			Castle targetCastle = CastlesBehavior.GetCastleById(targetCastleId);

			if (faction == null || targetCastle == null)
			{
				return;
			}

			switch (action)
			{
				case DeclareConquestRequest.ConquestActionType.CaptureTerritory:
					if (faction.CanCaptureTerritory())
					{
						targetCastle.SetFaction(factionId);
						GameNetwork.BroadcastNetworkMessage(new SyncFaction(faction));
						GameNetwork.BroadcastNetworkMessage(new SyncCastle(targetCastle));
					}
					break;

				case DeclareConquestRequest.ConquestActionType.AssignAsVassal:
					faction.CreateVassal(targetCastleId);
					GameNetwork.BroadcastNetworkMessage(new SyncFaction(faction));
					GameNetwork.BroadcastNetworkMessage(new SyncCastle(targetCastle));
					break;

				case DeclareConquestRequest.ConquestActionType.PillageTerritory:
					targetCastle.Pillage();
					faction.AddGold(targetCastle.GetPillageReward());
					GameNetwork.BroadcastNetworkMessage(new SyncFaction(faction));
					GameNetwork.BroadcastNetworkMessage(new SyncCastle(targetCastle));
					break;
			}
		}
	}
}

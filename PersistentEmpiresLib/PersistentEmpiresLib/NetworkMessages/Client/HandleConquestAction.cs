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
			int actionType = GameNetworkMessage.ReadIntFromPacket(ref bufferReadValid);
			return bufferReadValid;
		}

		protected override void OnWrite()
		{
			GameNetworkMessage.WriteIntToPacket(FactionId);
			GameNetworkMessage.WriteIntToPacket(TargetCastleId);
			GameNetworkMessage.WriteIntToPacket((int)Action);
		}

		public void ExecuteConquest(Faction faction, int targetCastleId, DeclareConquestRequest.ConquestActionType action)
		{
			if (action == DeclareConquestRequest.ConquestActionType.CaptureTerritory)
			{
				if (!faction.ValidateConquest())
				{
					Console.WriteLine("Fraktion kann kein weiteres Gebiet erobern!");
					return;
				}
				faction.AddTerritory(targetCastleId);
			}
			else if (action == DeclareConquestRequest.ConquestActionType.AssignAsVassal)
			{
				if (!faction.ValidateVassalOption())
				{
					Console.WriteLine("Fraktion kann keinen weiteren Vasallen verwalten!");
					return;
				}
				faction.CreateVassal(targetCastleId);
			}
			else if (action == DeclareConquestRequest.ConquestActionType.PillageTerritory)
			{
				faction.PillageTerritory(targetCastleId);
			}
		}
	}
}

using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Network.Messages;
using PersistentEmpiresLib.NetworkMessages.Client;
using PersistentEmpiresLib.NetworkMessages.Server;
using PersistentEmpiresLib.Systems;
using System.Collections.Generic;

namespace PersistentEmpiresLib.NetworkHandlers
{
	public class PEAcademyUnlockBlueprintHandler : MissionNetworkHandler
	{
		private Dictionary<NetworkCommunicator, List<int>> UnlockedBlueprints = new Dictionary<NetworkCommunicator, List<int>>();

		public override void Initialize()
		{
			GameNetwork.MessageHandlerManager.RegisterHandler<PEAcademyUnlockBlueprint>(OnBlueprintUnlock);
		}

		private void OnBlueprintUnlock(PEAcademyUnlockBlueprint message)
		{
			var player = message.Player;
			int blueprintId = message.BlueprintID;

			var influenceSystem = Mission.Current.GetMissionBehavior<PEInfluenceSystem>();
			var playerInventory = player.GetComponent<PlayerInventory>();

			if (influenceSystem == null || playerInventory == null)
			{
				InformationManager.DisplayMessage(new InformationMessage("⚠️ Fehler: Einfluss- oder Inventarsystem nicht gefunden!"));
				return;
			}

			int unlockCost = 30; // Einflusskosten für Blueprint-Freischaltung

			if (!influenceSystem.HasInfluence(player, unlockCost))
			{
				InformationManager.DisplayMessage(new InformationMessage($"⚠️ {player.UserName} hat nicht genug Einfluss ({unlockCost} benötigt)!"));
				return;
			}

			// Falls der Spieler noch keine freigeschalteten Blueprints hat, eine neue Liste anlegen
			if (!UnlockedBlueprints.ContainsKey(player))
			{
				UnlockedBlueprints[player] = new List<int>();
			}

			if (UnlockedBlueprints[player].Contains(blueprintId))
			{
				InformationManager.DisplayMessage(new InformationMessage($"⚠️ {player.UserName} hat diesen Blueprint bereits freigeschaltet!"));
				return;
			}

			// Einfluss abziehen und Blueprint freischalten
			influenceSystem.RemoveInfluence(player, unlockCost);
			UnlockedBlueprints[player].Add(blueprintId);

			// Synchronisierung mit Clients
			GameNetwork.BeginBroadcastModuleEvent();
			GameNetwork.WriteMessage(new PEAcademyBlueprintUnlocked(player, blueprintId));
			GameNetwork.EndBroadcastModuleEvent(GameNetwork.EventBroadcastFlags.None, null);

			InformationManager.DisplayMessage(new InformationMessage($"✅ {player.UserName} hat Blueprint {blueprintId} erfolgreich freigeschaltet! (-{unlockCost} Einfluss)"));
		}
	}
}

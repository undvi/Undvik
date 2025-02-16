using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Network.Messages;
using PersistentEmpiresLib.NetworkMessages.Client;
using PersistentEmpiresLib.NetworkMessages.Server;
using PersistentEmpiresLib.Systems;

namespace PersistentEmpiresLib.NetworkHandlers
{
    public class PEAcademyEnterHandler : MissionNetworkHandler
    {
        public override void Initialize()
        {
            GameNetwork.MessageHandlerManager.RegisterHandler<PEAcademyEnter>(OnAcademyEnter);
        }

        private void OnAcademyEnter(PEAcademyEnter message)
        {
            var player = message.Player;
            var influenceSystem = Mission.Current.GetMissionBehavior<PEInfluenceSystem>();
            var playerInventory = player.GetComponent<PlayerInventory>();

            if (influenceSystem == null || playerInventory == null)
            {
                InformationManager.DisplayMessage(new InformationMessage("⚠️ Fehler: Einfluss- oder Inventarsystem nicht gefunden!"));
                return;
            }

            int requiredGold = 500;
            int requiredInfluence = 50;

            // Überprüfung, ob der Spieler die Kosten bezahlen kann
            if (!influenceSystem.HasInfluence(player, requiredInfluence))
            {
                InformationManager.DisplayMessage(new InformationMessage($"⚠️ {player.UserName} hat nicht genug Einfluss ({requiredInfluence} benötigt)!"));
                return;
            }

            if (!playerInventory.HasGold(requiredGold))
            {
                InformationManager.DisplayMessage(new InformationMessage($"⚠️ {player.UserName} hat nicht genug Gold ({requiredGold} benötigt)!"));
                return;
            }

            // Kosten abziehen
            influenceSystem.RemoveInfluence(player, requiredInfluence);
            playerInventory.RemoveGold(requiredGold);

            // Akademie-Eintritt synchronisieren
            GameNetwork.BeginBroadcastModuleEvent();
            GameNetwork.WriteMessage(new PEAcademyOpened(player));
            GameNetwork.EndBroadcastModuleEvent(GameNetwork.EventBroadcastFlags.None, null);

            InformationManager.DisplayMessage(new InformationMessage($"🏛️ {player.UserName} hat die Akademie betreten! (-{requiredGold} Gold, -{requiredInfluence} Einfluss)"));
        }
    }
}

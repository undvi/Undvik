using System.Collections.Generic;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Network.Messages;
using PersistentEmpiresLib.Systems;
using PersistentEmpiresLib.NetworkMessages.Server;

namespace PersistentEmpiresLib.Systems
{
    public class PEAcademy : MissionObject
    {
        private Dictionary<string, bool> UnlockedBlueprints = new Dictionary<string, bool>();
        private const int AcademyEntryCostGold = 500;
        private const int AcademyEntryCostInfluence = 50;

        /// <summary>
        /// Lässt einen Spieler die Akademie betreten, wenn er die Kosten bezahlen kann.
        /// </summary>
        public void EnterAcademy(NetworkCommunicator player)
        {
            var influenceSystem = Mission.Current?.GetMissionBehavior<PEInfluenceSystem>();
            var playerInventory = player.GetComponent<PlayerInventory>();

            if (influenceSystem == null)
            {
                InformationManager.DisplayMessage(new InformationMessage("⚠️ Fehler: Einfluss-System nicht gefunden!"));
                return;
            }

            if (playerInventory == null)
            {
                InformationManager.DisplayMessage(new InformationMessage("⚠️ Fehler: Spieler-Inventar nicht gefunden!"));
                return;
            }

            // Prüfen, ob der Spieler genug Einfluss und Gold hat
            if (!influenceSystem.HasInfluence(player, AcademyEntryCostInfluence))
            {
                InformationManager.DisplayMessage(new InformationMessage($"⚠️ {player.UserName} hat nicht genug Einfluss ({AcademyEntryCostInfluence} benötigt)!"));
                return;
            }

            if (!playerInventory.HasGold(AcademyEntryCostGold))
            {
                InformationManager.DisplayMessage(new InformationMessage($"⚠️ {player.UserName} hat nicht genug Gold ({AcademyEntryCostGold} benötigt)!"));
                return;
            }

            // Kosten abziehen
            influenceSystem.RemoveInfluence(player, AcademyEntryCostInfluence);
            playerInventory.RemoveGold(AcademyEntryCostGold);

            // Broadcast an Server & Clients, dass der Spieler die Akademie betreten hat
            GameNetwork.BeginBroadcastModuleEvent();
            GameNetwork.WriteMessage(new PEAcademyOpened(player));
            GameNetwork.EndBroadcastModuleEvent(GameNetwork.EventBroadcastFlags.None, null);

            InformationManager.DisplayMessage(new InformationMessage($"🏛️ {player.UserName} hat die Akademie betreten und {AcademyEntryCostGold} Gold & {AcademyEntryCostInfluence} Einfluss bezahlt!"));
        }

        /// <summary>
        /// Lässt den Spieler ein neues Blueprint freischalten.
        /// </summary>
        public void UnlockBlueprint(NetworkCommunicator player, string blueprintID)
        {
            if (UnlockedBlueprints.ContainsKey(blueprintID) && UnlockedBlueprints[blueprintID])
            {
                InformationManager.DisplayMessage(new InformationMessage($"⚠️ {player.UserName} hat Blueprint {blueprintID} bereits freigeschaltet!"));
                return;
            }

            UnlockedBlueprints[blueprintID] = true;
            InformationManager.DisplayMessage(new InformationMessage($"✅ {player.UserName} hat Blueprint {blueprintID} erfolgreich freigeschaltet!"));

            // Synchronisieren mit dem Client
            GameNetwork.BeginBroadcastModuleEvent();
            GameNetwork.WriteMessage(new PEAcademyBlueprintUnlocked(player, blueprintID));
            GameNetwork.EndBroadcastModuleEvent(GameNetwork.EventBroadcastFlags.None, null);
        }

        /// <summary>
        /// Gibt eine Liste aller freigeschalteten Blueprints zurück.
        /// </summary>
        public List<string> GetUnlockedBlueprints()
        {
            List<string> blueprints = new List<string>();
            foreach (var bp in UnlockedBlueprints)
            {
                if (bp.Value) blueprints.Add(bp.Key);
            }
            return blueprints;
        }

        /// <summary>
        /// Prüft, ob der Spieler die Akademie-Kosten zahlen kann.
        /// </summary>
        public bool HasEnoughResources(NetworkCommunicator player)
        {
            var influenceSystem = Mission.Current?.GetMissionBehavior<PEInfluenceSystem>();
            var playerInventory = player.GetComponent<PlayerInventory>();

            if (influenceSystem == null || playerInventory == null)
                return false;

            return influenceSystem.HasInfluence(player, AcademyEntryCostInfluence) &&
                   playerInventory.HasGold(AcademyEntryCostGold);
        }

        /// <summary>
        /// Zieht die Akademie-Kosten ab.
        /// </summary>
        public void DeductEntryCost(NetworkCommunicator player)
        {
            var influenceSystem = Mission.Current?.GetMissionBehavior<PEInfluenceSystem>();
            var playerInventory = player.GetComponent<PlayerInventory>();

            if (influenceSystem == null || playerInventory == null)
                return;

            influenceSystem.RemoveInfluence(player, AcademyEntryCostInfluence);
            playerInventory.RemoveGold(AcademyEntryCostGold);
        }
    }
}

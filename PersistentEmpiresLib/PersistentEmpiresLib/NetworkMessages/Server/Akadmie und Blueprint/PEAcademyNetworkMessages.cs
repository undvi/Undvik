using System;
using System.Collections.Generic;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Network.Messages;
using PersistentEmpiresLib.Systems;
using PersistentEmpiresLib.NetworkMessages.Client; // Für PEAcademyEnter
using PersistentEmpiresLib.Helpers;

namespace PersistentEmpiresLib.NetworkMessages.Server.Academy
{
    #region Academy Handlers

    // Handler für den Eintritt in die Akademie
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

    // Handler für das Freischalten von Blueprints in der Akademie
    public class PEAcademyUnlockBlueprintHandler : MissionNetworkHandler
    {
        // Verwaltet für jeden Spieler die bereits freigeschalteten Blueprints
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

            // Falls der Spieler noch keine freigeschalteten Blueprints hat, neue Liste anlegen
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

            // Sende die Freischaltung an alle Clients
            GameNetwork.BeginBroadcastModuleEvent();
            GameNetwork.WriteMessage(new PEAcademyBlueprintUnlocked(player, blueprintId));
            GameNetwork.EndBroadcastModuleEvent(GameNetwork.EventBroadcastFlags.None, null);

            InformationManager.DisplayMessage(new InformationMessage($"✅ {player.UserName} hat Blueprint {blueprintId} erfolgreich freigeschaltet! (-{unlockCost} Einfluss)"));
        }
    }

    #endregion

    #region Academy Messages

    // Nachricht, dass die Akademie erfolgreich geöffnet wurde
    [DefineGameNetworkMessageTypeForMod(GameNetworkMessageSendType.FromServer)]
    public sealed class PEAcademyOpened : GameNetworkMessage
    {
        public NetworkCommunicator Player { get; private set; }

        public PEAcademyOpened() { }

        public PEAcademyOpened(NetworkCommunicator player)
        {
            this.Player = player ?? throw new ArgumentNullException(nameof(player), "❌ Fehler: Spieler ist null in PEAcademyOpened!");
        }

        protected override MultiplayerMessageFilter OnGetLogFilter() => MultiplayerMessageFilter.MissionObjects;
        protected override string OnGetLogFormat() =>
            this.Player != null ? $"🏛️ Akademie geöffnet für {Player.UserName}" : "⚠️ Fehler: Spieler NULL beim Akademie-Event!";

        protected override bool OnRead()
        {
            bool result = true;
            Player = GameNetworkMessage.ReadNetworkPeerReferenceFromPacket(ref result);
            if (!result || Player == null)
            {
                InformationManager.DisplayMessage(new InformationMessage("⚠️ Fehler beim Lesen der Akademie-Daten!"));
                return false;
            }
            return true;
        }
        protected override void OnWrite()
        {
            if (Player == null)
            {
                InformationManager.DisplayMessage(new InformationMessage("⚠️ Fehler: Ungültige Akademie-Daten für Netzwerksynchronisation!"));
                return;
            }
            GameNetworkMessage.WriteNetworkPeerReferenceToPacket(Player);
        }
    }

    // Nachricht, die dem Client mitteilt, welche Blueprints freigeschaltet sind
    [DefineGameNetworkMessageTypeForMod(GameNetworkMessageSendType.FromServer)]
    public sealed class PEAcademyBlueprintsUpdated : GameNetworkMessage
    {
        public int PlayerID { get; private set; }
        public List<string> Blueprints { get; private set; }

        public PEAcademyBlueprintsUpdated() { }

        public PEAcademyBlueprintsUpdated(int playerId, List<string> blueprints)
        {
            this.PlayerID = playerId;
            this.Blueprints = blueprints ?? new List<string>();
        }

        protected override MultiplayerMessageFilter OnGetLogFilter() => MultiplayerMessageFilter.MissionObjects;
        protected override string OnGetLogFormat() => $"📜 Blueprints aktualisiert für Spieler mit ID {PlayerID}: {string.Join(", ", Blueprints)}";

        protected override bool OnRead()
        {
            bool result = true;
            PlayerID = GameNetworkMessage.ReadIntFromPacket(new CompressionInfo.Integer(0, 10000, true), ref result);
            int count = GameNetworkMessage.ReadIntFromPacket(new CompressionInfo.Integer(0, 100, true), ref result);
            Blueprints = new List<string>();
            for (int i = 0; i < count; i++)
            {
                Blueprints.Add(GameNetworkMessage.ReadStringFromPacket(ref result));
            }
            return result;
        }
        protected override void OnWrite()
        {
            GameNetworkMessage.WriteIntToPacket(PlayerID, new CompressionInfo.Integer(0, 10000, true));
            GameNetworkMessage.WriteIntToPacket(Blueprints.Count, new CompressionInfo.Integer(0, 100, true));
            foreach (var blueprint in Blueprints)
            {
                GameNetworkMessage.WriteStringToPacket(blueprint);
            }
        }
    }

    // Nachricht, dass ein Blueprint freigeschaltet wurde
    // Diese Klasse ersetzt die frühere PEBlueprintUnlocked (doppelt vorhanden)
    [DefineGameNetworkMessageTypeForMod(GameNetworkMessageSendType.FromServer)]
    public sealed class PEAcademyBlueprintUnlocked : GameNetworkMessage
    {
        public NetworkCommunicator Player { get; private set; }
        public string BlueprintID { get; private set; }

        public PEAcademyBlueprintUnlocked() { }

        public PEAcademyBlueprintUnlocked(NetworkCommunicator player, int blueprintId)
            : this(player, blueprintId.ToString()) { }

        public PEAcademyBlueprintUnlocked(NetworkCommunicator player, string blueprintID)
        {
            this.Player = player ?? throw new ArgumentNullException(nameof(player), "❌ Fehler: Spieler ist null in PEAcademyBlueprintUnlocked!");
            this.BlueprintID = blueprintID ?? throw new ArgumentNullException(nameof(blueprintID), "❌ Fehler: Blueprint-ID ist null!");
        }

        protected override MultiplayerMessageFilter OnGetLogFilter() => MultiplayerMessageFilter.MissionObjects;
        protected override string OnGetLogFormat() =>
            this.Player != null ? $"✅ {Player.UserName} hat das Blueprint {BlueprintID} freigeschaltet!" : "⚠️ Fehler: Spieler NULL beim Freischalten eines Blueprints!";
        protected override bool OnRead()
        {
            bool result = true;
            Player = GameNetworkMessage.ReadNetworkPeerReferenceFromPacket(ref result);
            BlueprintID = GameNetworkMessage.ReadStringFromPacket(ref result);
            if (!result || Player == null || string.IsNullOrEmpty(BlueprintID))
            {
                InformationManager.DisplayMessage(new InformationMessage("⚠️ Fehler beim Lesen der Blueprint-Daten!"));
                return false;
            }
            return true;
        }
        protected override void OnWrite()
        {
            if (Player == null || string.IsNullOrEmpty(BlueprintID))
            {
                InformationManager.DisplayMessage(new InformationMessage("⚠️ Fehler: Ungültige Blueprint-Daten für Netzwerksynchronisation!"));
                return;
            }
            GameNetworkMessage.WriteNetworkPeerReferenceToPacket(Player);
            GameNetworkMessage.WriteStringToPacket(BlueprintID);
        }
    }

    #endregion
}

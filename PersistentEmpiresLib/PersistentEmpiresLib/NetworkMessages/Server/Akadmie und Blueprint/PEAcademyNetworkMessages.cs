using System;
using System.IO;
using System.Collections.Generic;
using Newtonsoft.Json; // <-- Falls du es nicht hast, über NuGet installieren
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Network.Messages;
using TaleWorlds.Core;
using TaleWorlds.Library; // Für InformationManager.DisplayMessage u.a.
// using ... deine anderen Namespaces

namespace PersistentEmpiresLib.NetworkMessages.Server.Academy
{
    // Nachricht: Ein Spieler will manuell (in der Akademie) einen Blueprint freischalten
    [DefineGameNetworkMessageTypeForMod(GameNetworkMessageSendType.FromServer)]
    public sealed class PEAcademyUnlockBlueprint : GameNetworkMessage
    {
        public NetworkCommunicator Player { get; private set; }
        public int BlueprintID { get; private set; } // Z.B. 100, 101,...

        public PEAcademyUnlockBlueprint() { }
        public PEAcademyUnlockBlueprint(NetworkCommunicator player, int blueprintId)
        {
            Player = player;
            BlueprintID = blueprintId;
        }

        protected override bool OnRead()
        {
            bool result = true;
            Player = GameNetworkMessage.ReadNetworkPeerReferenceFromPacket(ref result);
            BlueprintID = GameNetworkMessage.ReadIntFromPacket(new CompressionInfo.Integer(0, 5000, true), ref result);
            return result;
        }
        protected override void OnWrite()
        {
            GameNetworkMessage.WriteNetworkPeerReferenceToPacket(Player);
            GameNetworkMessage.WriteIntToPacket(BlueprintID, new CompressionInfo.Integer(0, 5000, true));
        }

        protected override MultiplayerMessageFilter OnGetLogFilter() => MultiplayerMessageFilter.Mission;
        protected override string OnGetLogFormat() =>
            $"PEAcademyUnlockBlueprint -> Player {Player?.UserName}, Blueprint {BlueprintID}";
    }

    // Nachricht an alle Clients: "Spieler X hat Blueprint Y freigeschaltet"
    [DefineGameNetworkMessageTypeForMod(GameNetworkMessageSendType.FromServer)]
    public sealed class PEAcademyBlueprintUnlocked : GameNetworkMessage
    {
        public NetworkCommunicator Player { get; private set; }
        public string BlueprintID { get; private set; }

        public PEAcademyBlueprintUnlocked() { }
        public PEAcademyBlueprintUnlocked(NetworkCommunicator player, string blueprintId)
        {
            Player = player;
            BlueprintID = blueprintId;
        }

        protected override bool OnRead()
        {
            bool result = true;
            Player = GameNetworkMessage.ReadNetworkPeerReferenceFromPacket(ref result);
            BlueprintID = GameNetworkMessage.ReadStringFromPacket(ref result);
            return result;
        }
        protected override void OnWrite()
        {
            GameNetworkMessage.WriteNetworkPeerReferenceToPacket(Player);
            GameNetworkMessage.WriteStringToPacket(BlueprintID);
        }

        protected override MultiplayerMessageFilter OnGetLogFilter() => MultiplayerMessageFilter.General;
        protected override string OnGetLogFormat()
            => $"Blueprint {BlueprintID} wurde für {Player?.UserName} freigeschaltet.";
    }

    // Nachricht an den Client: Komplette Liste seiner freigeschalteten Blueprints
    // (damit das Client-UI z. B. Rezepte anzeigen kann)
    [DefineGameNetworkMessageTypeForMod(GameNetworkMessageSendType.FromServer)]
    public sealed class PEAcademyBlueprintsUpdated : GameNetworkMessage
    {
        public int PlayerID { get; private set; }
        public List<string> BlueprintList { get; private set; }

        public PEAcademyBlueprintsUpdated() { }
        public PEAcademyBlueprintsUpdated(int playerID, List<string> blueprintList)
        {
            PlayerID = playerID;
            BlueprintList = blueprintList;
        }

        protected override bool OnRead()
        {
            bool result = true;
            PlayerID = GameNetworkMessage.ReadIntFromPacket(new CompressionInfo.Integer(0, 100000, true), ref result);
            int count = GameNetworkMessage.ReadIntFromPacket(new CompressionInfo.Integer(0, 5000, true), ref result);
            BlueprintList = new List<string>(count);
            for (int i = 0; i < count; i++)
            {
                BlueprintList.Add(GameNetworkMessage.ReadStringFromPacket(ref result));
            }
            return result;
        }
        protected override void OnWrite()
        {
            GameNetworkMessage.WriteIntToPacket(PlayerID, new CompressionInfo.Integer(0, 100000, true));
            GameNetworkMessage.WriteIntToPacket(BlueprintList.Count, new CompressionInfo.Integer(0, 5000, true));
            foreach (var bp in BlueprintList)
            {
                GameNetworkMessage.WriteStringToPacket(bp);
            }
        }

        protected override MultiplayerMessageFilter OnGetLogFilter() => MultiplayerMessageFilter.General;
        protected override string OnGetLogFormat() =>
            $"PEAcademyBlueprintsUpdated -> Player {PlayerID}, Blueprints: {string.Join(",", BlueprintList)}";
    }


    // Haupt-Handler: Managt die Listen, lädt & speichert JSON, etc.
    public class PEAcademyUnlockBlueprintHandler : MissionNetworkHandler
    {
        private const string BlueprintSaveFile = "BlueprintData.json";

        // z. B. <SpielerGUID, HashSet<blueprintString>>
        private Dictionary<string, HashSet<string>> unlockedBlueprints
            = new Dictionary<string, HashSet<string>>();

        public override void OnMissionBehaviorInitialize()
        {
            base.OnMissionBehaviorInitialize();

            // Für Client -> Server Meldungen (z. B. Akademie-Kauf)
            GameNetwork.MessageHandlerManager.RegisterHandler<PEAcademyUnlockBlueprint>(OnBlueprintUnlock);

            // Bei Serverstart: JSON laden
            if (GameNetwork.IsServer)
            {
                LoadBlueprintData();
            }
        }

        public override void OnRemoveBehavior()
        {
            base.OnRemoveBehavior();
            // Beim Entladen der Mission, z. B. Server-Shutdown -> JSON speichern
            if (GameNetwork.IsServer)
            {
                SaveBlueprintData();
            }
        }

        // Wird aufgerufen, wenn der Client manuell in der Akademie Blueprint X kaufen will
        private void OnBlueprintUnlock(PEAcademyUnlockBlueprint message)
        {
            if (!GameNetwork.IsServer) return; // Nur serverseitig handeln
            if (message.Player == null) return;

            var blueprintId = message.BlueprintID.ToString();
            // Z. B. Influence-Kosten erheben
            int unlockCostInfluence = 50;
            // (Kein Gold? Dann in der AcademyEnter o.ä. realisieren, je nach System)

            // Ermitteln, ob der Spieler genug Influence hat (Beispiel)
            // Du müsstest dein eigenes Influence-System implementieren
            var influenceSystem = Mission.Current.GetMissionBehavior<PEInfluenceSystem>();
            if (influenceSystem == null || !influenceSystem.HasInfluence(message.Player, unlockCostInfluence))
            {
                InformationManager.DisplayMessage(new InformationMessage($"❌ Nicht genug Einfluss! Benötigt: {unlockCostInfluence}"));
                return;
            }
            // Einfluss abziehen
            influenceSystem.RemoveInfluence(message.Player, unlockCostInfluence);

            // Nun das eigentliche Freischalten (ohne weitere Kosten)
            TryUnlockBlueprint(message.Player, blueprintId, ignoreCosts: true);
        }

        /// <summary>
        /// Versucht, dem Spieler den angegebenen Blueprint einzutragen.
        /// ignoreCosts = true -> Keine zusätzlichen Kosten (z. B. Fall: Crafting).
        /// Ansonsten könnte man hier optional Gold/Eisen/etc. abziehen.
        /// </summary>
        public bool TryUnlockBlueprint(NetworkCommunicator player, string blueprintId, bool ignoreCosts = false)
        {
            string playerKey = GetPlayerKey(player);

            if (!unlockedBlueprints.ContainsKey(playerKey))
                unlockedBlueprints[playerKey] = new HashSet<string>();

            if (unlockedBlueprints[playerKey].Contains(blueprintId))
            {
                // Bereits freigeschaltet
                return false;
            }

            // Optionale Ressourcenkosten, falls du willst
            if (!ignoreCosts)
            {
                // Hier z. B. Gold / Eisenbarren / Influence checken
                // ...
            }

            // Freischalten
            unlockedBlueprints[playerKey].Add(blueprintId);

            // An alle Clients broadcasten:
            GameNetwork.BeginBroadcastModuleEvent();
            GameNetwork.WriteMessage(new PEAcademyBlueprintUnlocked(player, blueprintId));
            GameNetwork.EndBroadcastModuleEvent();

            InformationManager.DisplayMessage(new InformationMessage(
                $"🎉 {player.UserName} hat Blueprint '{blueprintId}' freigeschaltet!"
            ));

            // Anschließend dem Spieler seine komplette Blueprint-Liste schicken (optional),
            // damit das UI alle Rezepte aktualisieren kann
            SendBlueprintsToPlayer(player);

            // Jetzt oder in definierter Intervalldauer kann man die Daten speichern:
            SaveBlueprintData();

            return true;
        }

        // Schickt dem angegebenen Player die Liste aller seiner Blueprints
        public void SendBlueprintsToPlayer(NetworkCommunicator player)
        {
            string playerKey = GetPlayerKey(player);

            var list = unlockedBlueprints.ContainsKey(playerKey)
                ? new List<string>(unlockedBlueprints[playerKey])
                : new List<string>();

            // Player-ID aus NetworkCommunicator
            int playerId = player.GetUniquePlayerId();

            // Sende an nur diesen einen Spieler:
            GameNetwork.BeginSendModuleEvent(player);
            GameNetwork.WriteMessage(new PEAcademyBlueprintsUpdated(playerId, list));
            GameNetwork.EndSendModuleEvent();
        }

        // JSON speichern
        private void SaveBlueprintData()
        {
            try
            {
                var dictToSave = new Dictionary<string, List<string>>();
                foreach (var kvp in unlockedBlueprints)
                {
                    dictToSave[kvp.Key] = new List<string>(kvp.Value);
                }
                string json = JsonConvert.SerializeObject(dictToSave, Formatting.Indented);
                File.WriteAllText(BlueprintSaveFile, json);
            }
            catch (Exception ex)
            {
                InformationManager.DisplayMessage(new InformationMessage(
                    $"⚠️ Fehler beim Speichern der Blueprint-Daten: {ex.Message}"
                ));
            }
        }

        // JSON laden
        private void LoadBlueprintData()
        {
            if (!File.Exists(BlueprintSaveFile))
            {
                InformationManager.DisplayMessage(new InformationMessage(
                    $"BlueprintData.json nicht gefunden, starte mit leerem Datensatz."
                ));
                return;
            }
            try
            {
                string json = File.ReadAllText(BlueprintSaveFile);
                var loadedDict = JsonConvert.DeserializeObject<Dictionary<string, List<string>>>(json);
                if (loadedDict != null)
                {
                    unlockedBlueprints.Clear();
                    foreach (var kvp in loadedDict)
                    {
                        unlockedBlueprints[kvp.Key] = new HashSet<string>(kvp.Value);
                    }
                }
            }
            catch (Exception ex)
            {
                InformationManager.DisplayMessage(new InformationMessage(
                    $"⚠️ Fehler beim Laden der Blueprint-Daten: {ex.Message}"
                ));
            }
        }

        private string GetPlayerKey(NetworkCommunicator player)
        {
            // z. B. unique ID -> "SteamID_12345" oder "GUID_xxx"
            // Hier demonstrativ:
            return player.GetUniquePlayerId().ToString();
        }
    }
}

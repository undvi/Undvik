using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Network.Messages;
using PersistentEmpiresLib.Factions;
using TaleWorlds.Core;
using TaleWorlds.Library;

namespace PersistentEmpiresLib.NetworkMessages.Server
{
    [DefineGameNetworkMessageTypeForMod(GameNetworkMessageSendType.FromServer)]
    public sealed class FactionUpdateLord : GameNetworkMessage
    {
        public int FactionIndex { get; private set; }
        public NetworkCommunicator Player { get; private set; }

        private const int InfluenceCost = 500; // Einflusskosten für die Ernennung eines neuen Lords

        public FactionUpdateLord() { }

        public FactionUpdateLord(int factionIndex, NetworkCommunicator player)
        {
            this.FactionIndex = factionIndex;
            this.Player = player;
        }

        protected override MultiplayerMessageFilter OnGetLogFilter()
        {
            return MultiplayerMessageFilter.Administration;
        }

        protected override string OnGetLogFormat()
        {
            return $"⚔️ Fraktion {FactionIndex} hat einen neuen Lord: {Player.UserName}";
        }

        protected override bool OnRead()
        {
            bool result = true;
            this.FactionIndex = GameNetworkMessage.ReadIntFromPacket(new CompressionInfo.Integer(-1, 200, true), ref result);
            this.Player = GameNetworkMessage.ReadNetworkPeerReferenceFromPacket(ref result);
            return result;
        }

        protected override void OnWrite()
        {
            GameNetworkMessage.WriteIntToPacket(this.FactionIndex, new CompressionInfo.Integer(-1, 200, true));
            GameNetworkMessage.WriteNetworkPeerReferenceToPacket(this.Player);
        }

        public static void BroadcastNewLord(NetworkCommunicator requester, int factionIndex, NetworkCommunicator newLord)
        {
            Faction faction = FactionManager.GetFactionByIndex(factionIndex);
            if (faction == null)
            {
                InformationManager.DisplayMessage(new InformationMessage($"⚠️ Fraktion {factionIndex} nicht gefunden. Aktion abgebrochen."));
                return;
            }

            PersistentEmpireRepresentative requesterRep = requester.GetComponent<PersistentEmpireRepresentative>();
            PersistentEmpireRepresentative newLordRep = newLord.GetComponent<PersistentEmpireRepresentative>();

            if (requesterRep == null || newLordRep == null)
            {
                InformationManager.DisplayMessage(new InformationMessage("⚠️ Fehler: Spielerreferenz ist null."));
                return;
            }

            // 🔹 Nur der aktuelle Lord oder ein Marschall mit Einfluss kann einen neuen Lord ernennen
            if (faction.lordId != requester.VirtualPlayer.ToPlayerId() && !faction.marshalls.Contains(requester.VirtualPlayer.ToPlayerId()))
            {
                InformationManager.DisplayMessage(new InformationMessage($"❌ {requester.UserName} hat keine Berechtigung, einen neuen Lord zu ernennen."));
                return;
            }

            // 🔹 Ernennung zum Lord kostet Einfluss
            if (!requesterRep.ReduceIfHaveEnoughInfluence(InfluenceCost))
            {
                InformationManager.DisplayMessage(new InformationMessage($"❌ Nicht genug Einfluss! {InfluenceCost} Einfluss erforderlich."));
                return;
            }

            // 🔹 Neuen Lord setzen
            faction.lordId = newLord.VirtualPlayer.ToPlayerId();

            GameNetwork.BeginBroadcastModuleEvent();
            GameNetwork.WriteMessage(new FactionUpdateLord(factionIndex, newLord));
            GameNetwork.EndBroadcastModuleEvent();

            InformationManager.DisplayMessage(new InformationMessage($"👑 {newLord.UserName} wurde zum neuen Lord der Fraktion {faction.name} ernannt."));

            // 🔹 Fraktionsmitglieder informieren
            foreach (var member in faction.members)
            {
                InformationManager.DisplayMessage(new InformationMessage($"📢 {newLord.UserName} ist nun der neue Lord eurer Fraktion!"));
            }

            // 🔹 Log für die Fraktion
            faction.AddToFactionLog($"📜 {newLord.UserName} wurde von {requester.UserName} zum neuen Lord ernannt.");
        }
    }
}

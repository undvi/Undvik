using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Network.Messages;
using PersistentEmpiresLib.Factions;
using TaleWorlds.Core;
using TaleWorlds.Library;

namespace PersistentEmpiresLib.NetworkMessages.Server
{
    [DefineGameNetworkMessageTypeForMod(GameNetworkMessageSendType.FromServer)]
    public sealed class FactionUpdateMarshall : GameNetworkMessage
    {
        public int FactionIndex { get; private set; }
        public NetworkCommunicator TargetPlayer { get; private set; }
        public bool IsMarshall { get; private set; }

        private const int InfluenceCost = 100; // Einflusskosten für die Ernennung zum Marschall

        public FactionUpdateMarshall() { }

        public FactionUpdateMarshall(int factionIndex, NetworkCommunicator targetPlayer, bool isMarshall)
        {
            this.FactionIndex = factionIndex;
            this.TargetPlayer = targetPlayer;
            this.IsMarshall = isMarshall;
        }

        protected override MultiplayerMessageFilter OnGetLogFilter()
        {
            return MultiplayerMessageFilter.Administration;
        }

        protected override string OnGetLogFormat()
        {
            return $"⚔️ Fraktion {FactionIndex}: {TargetPlayer.UserName} ist nun ein Marschall: {IsMarshall}";
        }

        protected override bool OnRead()
        {
            bool result = true;
            this.FactionIndex = GameNetworkMessage.ReadIntFromPacket(new CompressionInfo.Integer(-1, 200, true), ref result);
            this.TargetPlayer = GameNetworkMessage.ReadNetworkPeerReferenceFromPacket(ref result);
            this.IsMarshall = GameNetworkMessage.ReadBoolFromPacket(ref result);
            return result;
        }

        protected override void OnWrite()
        {
            GameNetworkMessage.WriteIntToPacket(this.FactionIndex, new CompressionInfo.Integer(-1, 200, true));
            GameNetworkMessage.WriteNetworkPeerReferenceToPacket(this.TargetPlayer);
            GameNetworkMessage.WriteBoolToPacket(this.IsMarshall);
        }

        public static void BroadcastMarshallUpdate(NetworkCommunicator requester, int factionIndex, NetworkCommunicator targetPlayer, bool isMarshall)
        {
            Faction faction = FactionManager.GetFactionByIndex(factionIndex);
            if (faction == null)
            {
                InformationManager.DisplayMessage(new InformationMessage($"⚠️ Fraktion {factionIndex} nicht gefunden. Aktion abgebrochen."));
                return;
            }

            PersistentEmpireRepresentative requesterRep = requester.GetComponent<PersistentEmpireRepresentative>();
            PersistentEmpireRepresentative targetRep = targetPlayer.GetComponent<PersistentEmpireRepresentative>();

            if (requesterRep == null || targetRep == null)
            {
                InformationManager.DisplayMessage(new InformationMessage("⚠️ Fehler: Spielerreferenz ist null."));
                return;
            }

            // 🔹 Nur der Lord oder ein hoher Marschall darf neue Marschalls ernennen
            if (faction.lordId != requester.VirtualPlayer.ToPlayerId() && !faction.marshalls.Contains(requester.VirtualPlayer.ToPlayerId()))
            {
                InformationManager.DisplayMessage(new InformationMessage($"❌ {requester.UserName} hat keine Berechtigung, Marschälle zu ernennen oder zu entfernen."));
                return;
            }

            // 🔹 Ernennung zum Marschall kostet Einfluss
            if (isMarshall && !requesterRep.ReduceIfHaveEnoughInfluence(InfluenceCost))
            {
                InformationManager.DisplayMessage(new InformationMessage($"❌ Nicht genug Einfluss! {InfluenceCost} Einfluss erforderlich."));
                return;
            }

            if (isMarshall)
            {
                if (!faction.marshalls.Contains(targetPlayer.VirtualPlayer.ToPlayerId()))
                {
                    faction.marshalls.Add(targetPlayer.VirtualPlayer.ToPlayerId());
                }
            }
            else
            {
                faction.marshalls.Remove(targetPlayer.VirtualPlayer.ToPlayerId());
            }

            GameNetwork.BeginBroadcastModuleEvent();
            GameNetwork.WriteMessage(new FactionUpdateMarshall(factionIndex, targetPlayer, isMarshall));
            GameNetwork.EndBroadcastModuleEvent();

            string statusMessage = isMarshall ? "zum Marschall befördert" : "vom Marschall-Rang entfernt";
            InformationManager.DisplayMessage(new InformationMessage($"📢 {targetPlayer.UserName} wurde {statusMessage} in der Fraktion {faction.name}."));

            // 🔹 Fraktionsmitglieder informieren
            foreach (var member in faction.members)
            {
                InformationManager.DisplayMessage(new InformationMessage($"📢 {targetPlayer.UserName} ist jetzt {statusMessage}."));
            }

            // 🔹 Log für die Fraktion
            faction.AddToFactionLog($"📜 {targetPlayer.UserName} wurde {statusMessage} von {requester.UserName}.");
        }
    }
}

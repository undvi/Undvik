using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Network.Messages;
using PersistentEmpiresLib.Factions;
using PersistentEmpiresLib.Helpers;
using System;

namespace PersistentEmpiresLib.NetworkMessages.Client
{
    [DefineGameNetworkMessageTypeForMod(GameNetworkMessageSendType.FromClient)]
    public sealed class FactionAssignMarshall : GameNetworkMessage
    {
        private const int RequiredFactionRank = 2;  // Mindest-Rang der Fraktion für Marschälle
        private const int MaxMarshallsPerFaction = 5; // Maximale Anzahl an Marschällen pro Fraktion
        private const int MarshallCostGold = 10000;   // Kosten für die Ernennung eines Marschalls
        private const int MarshallCostInfluence = 50; // Kosten in Einfluss

        public FactionAssignMarshall() { }

        public FactionAssignMarshall(NetworkCommunicator targetPlayer)
        {
            this.TargetPlayer = targetPlayer;
        }

        public NetworkCommunicator TargetPlayer { get; private set; }

        protected override MultiplayerMessageFilter OnGetLogFilter()
        {
            return MultiplayerMessageFilter.Administration;
        }

        protected override string OnGetLogFormat()
        {
            return $"Assigning Marshall to faction | Player: {TargetPlayer?.UserName ?? "Unknown"}";
        }

        protected override bool OnRead()
        {
            bool result = true;
            this.TargetPlayer = GameNetworkMessage.ReadNetworkPeerReferenceFromPacket(ref result);
            return result;
        }

        protected override void OnWrite()
        {
            GameNetworkMessage.WriteNetworkPeerReferenceToPacket(this.TargetPlayer);
        }

        public static bool TryAssignMarshall(NetworkCommunicator requestingPlayer, NetworkCommunicator newMarshall)
        {
            if (requestingPlayer == null || newMarshall == null)
            {
                InformationManager.DisplayMessage(new InformationMessage("Error: Invalid request - player or target missing."));
                return false;
            }

            PersistentEmpireRepresentative requesterRep = requestingPlayer.GetComponent<PersistentEmpireRepresentative>();
            PersistentEmpireRepresentative targetRep = newMarshall.GetComponent<PersistentEmpireRepresentative>();

            if (requesterRep == null || targetRep == null || requesterRep.GetFaction() == null)
            {
                InformationManager.DisplayMessage(new InformationMessage("Error: Requester or target is not in a faction."));
                return false;
            }

            Faction faction = requesterRep.GetFaction();

            // 🔹 Überprüfung: Spieler muss Anführer sein
            if (faction.lordId != requestingPlayer.VirtualPlayer.ToPlayerId())
            {
                InformationManager.DisplayMessage(new InformationMessage("Only the faction leader can assign a Marshall."));
                return false;
            }

            // 🔹 Überprüfung: Fraktion muss den Mindest-Rang haben
            if (faction.Rank < RequiredFactionRank)
            {
                InformationManager.DisplayMessage(new InformationMessage("Your faction must be at least Rank 2 to assign a Marshall."));
                return false;
            }

            // 🔹 Überprüfung: Maximale Anzahl an Marschällen
            if (faction.marshalls.Count >= MaxMarshallsPerFaction)
            {
                InformationManager.DisplayMessage(new InformationMessage("Your faction already has the maximum number of Marshalls."));
                return false;
            }

            // 🔹 Überprüfung: Fraktion muss genug Gold & Einfluss haben
            if (faction.Gold < MarshallCostGold || faction.Influence < MarshallCostInfluence)
            {
                InformationManager.DisplayMessage(new InformationMessage("Your faction does not have enough gold or influence to assign a Marshall."));
                return false;
            }

            // 🔹 Zielspieler muss Mitglied der Fraktion sein
            if (!faction.members.Contains(newMarshall))
            {
                InformationManager.DisplayMessage(new InformationMessage("The selected player is not in your faction."));
                return false;
            }

            // 🔹 Gold & Einfluss abziehen und Marschall zuweisen
            faction.Gold -= MarshallCostGold;
            faction.Influence -= MarshallCostInfluence;
            faction.marshalls.Add(newMarshall.VirtualPlayer.ToPlayerId());

            // Synchronisation über das Netzwerk
            GameNetwork.BeginBroadcastModuleEvent();
            GameNetwork.WriteMessage(new FactionUpdateMarshall(faction.FactionIndex, newMarshall, true));
            GameNetwork.EndBroadcastModuleEvent();

            InformationManager.DisplayMessage(new InformationMessage($"{newMarshall.UserName} has been assigned as a Marshall in faction {faction.name}."));
            return true;
        }
    }
}

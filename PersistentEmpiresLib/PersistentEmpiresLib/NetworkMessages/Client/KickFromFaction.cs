using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Network.Messages;
using PersistentEmpiresLib.Factions;
using PersistentEmpiresLib.Helpers;
using TaleWorlds.Core;

namespace PersistentEmpiresLib.NetworkMessages.Client
{
    [DefineGameNetworkMessageTypeForMod(GameNetworkMessageSendType.FromClient)]
    public sealed class KickFromFaction : GameNetworkMessage
    {
        public NetworkCommunicator Target { get; private set; }

        public KickFromFaction() { }

        public KickFromFaction(NetworkCommunicator target)
        {
            this.Target = target;
        }

        protected override MultiplayerMessageFilter OnGetLogFilter()
        {
            return MultiplayerMessageFilter.Administration;
        }

        protected override string OnGetLogFormat()
        {
            return $"Kick request for player {Target?.UserName ?? "Unknown"} from faction.";
        }

        protected override bool OnRead()
        {
            bool result = true;
            this.Target = GameNetworkMessage.ReadNetworkPeerReferenceFromPacket(ref result);
            return result;
        }

        protected override void OnWrite()
        {
            GameNetworkMessage.WriteNetworkPeerReferenceToPacket(this.Target);
        }

        public static void HandleKickRequest(NetworkCommunicator sender, NetworkCommunicator target)
        {
            if (sender == null || target == null)
            {
                InformationManager.DisplayMessage(new InformationMessage("Error: Invalid sender or target."));
                return;
            }

            PersistentEmpireRepresentative senderRep = sender.GetComponent<PersistentEmpireRepresentative>();
            PersistentEmpireRepresentative targetRep = target.GetComponent<PersistentEmpireRepresentative>();

            if (senderRep == null || targetRep == null || senderRep.GetFaction() == null || targetRep.GetFaction() == null)
            {
                InformationManager.DisplayMessage(new InformationMessage("Error: One of the players is not in a faction."));
                return;
            }

            Faction faction = senderRep.GetFaction();

            // 🔹 Überprüfung, ob der Kickende berechtigt ist
            if (faction.lordId != sender.VirtualPlayer.ToPlayerId() && !faction.marshalls.Contains(sender.VirtualPlayer.ToPlayerId()))
            {
                InformationManager.DisplayMessage(new InformationMessage("Only the Lord or a Marshall can kick players."));
                return;
            }

            // 🔹 Der Lord kann nicht aus seiner eigenen Fraktion gekickt werden
            if (faction.lordId == target.VirtualPlayer.ToPlayerId())
            {
                InformationManager.DisplayMessage(new InformationMessage("The Lord cannot be kicked. Use elections instead."));
                return;
            }

            // 🔹 Entferne das Mitglied aus der Fraktion
            faction.members.Remove(target);

            // 🔹 Broadcast an alle Fraktionsmitglieder
            GameNetwork.BeginBroadcastModuleEvent();
            GameNetwork.WriteMessage(new PlayerKickedFromFaction(target, faction.FactionIndex));
            GameNetwork.EndBroadcastModuleEvent();

            InformationManager.DisplayMessage(new InformationMessage($"{target.UserName} has been kicked from the faction {faction.name}."));
        }
    }

    [DefineGameNetworkMessageTypeForMod(GameNetworkMessageSendType.FromServer)]
    public sealed class PlayerKickedFromFaction : GameNetworkMessage
    {
        public NetworkCommunicator Target { get; private set; }
        public int FactionIndex { get; private set; }

        public PlayerKickedFromFaction() { }

        public PlayerKickedFromFaction(NetworkCommunicator target, int factionIndex)
        {
            this.Target = target;
            this.FactionIndex = factionIndex;
        }

        protected override bool OnRead()
        {
            bool result = true;
            this.Target = GameNetworkMessage.ReadNetworkPeerReferenceFromPacket(ref result);
            this.FactionIndex = GameNetworkMessage.ReadIntFromPacket(new CompressionInfo.Integer(-1, 200, true), ref result);
            return result;
        }

        protected override void OnWrite()
        {
            GameNetworkMessage.WriteNetworkPeerReferenceToPacket(this.Target);
            GameNetworkMessage.WriteIntToPacket(this.FactionIndex, new CompressionInfo.Integer(-1, 200, true));
        }

        protected override MultiplayerMessageFilter OnGetLogFilter()
        {
            return MultiplayerMessageFilter.Administration;
        }

        protected override string OnGetLogFormat()
        {
            return $"Player {Target?.UserName ?? "Unknown"} kicked from faction {FactionIndex}.";
        }
    }
}

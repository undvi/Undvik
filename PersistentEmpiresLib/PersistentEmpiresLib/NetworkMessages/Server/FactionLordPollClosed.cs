using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Network.Messages;
using PersistentEmpiresLib.Factions;
using System.Linq;

namespace PersistentEmpiresLib.NetworkMessages.Server
{
    [DefineGameNetworkMessageTypeForMod(GameNetworkMessageSendType.FromServer)]
    public sealed class FactionLordPollClosed : GameNetworkMessage
    {
        private const int LordChangeCost = 50000; // Kosten für den Lordwechsel

        public FactionLordPollClosed() { }

        public FactionLordPollClosed(NetworkCommunicator targetPlayer, bool accepted, int factionIndex)
        {
            this.TargetPlayer = targetPlayer;
            this.Accepted = accepted;
            this.FactionIndex = factionIndex;
        }

        public NetworkCommunicator TargetPlayer { get; private set; }
        public bool Accepted { get; private set; }
        public int FactionIndex { get; private set; }

        protected override MultiplayerMessageFilter OnGetLogFilter()
        {
            return MultiplayerMessageFilter.Administration;
        }

        protected override string OnGetLogFormat()
        {
            return "Faction selected a new lord";
        }

        protected override bool OnRead()
        {
            bool result = true;
            this.TargetPlayer = GameNetworkMessage.ReadNetworkPeerReferenceFromPacket(ref result);
            this.Accepted = GameNetworkMessage.ReadBoolFromPacket(ref result);
            this.FactionIndex = GameNetworkMessage.ReadIntFromPacket(new CompressionInfo.Integer(-1, 200), ref result);
            return result;
        }

        protected override void OnWrite()
        {
            GameNetworkMessage.WriteNetworkPeerReferenceToPacket(this.TargetPlayer);
            GameNetworkMessage.WriteBoolToPacket(this.Accepted);
            GameNetworkMessage.WriteIntToPacket(this.FactionIndex, new CompressionInfo.Integer(-1, 200));
        }

        public static bool CanChangeLord(Faction faction)
        {
            if (faction.Rank < 2)
            {
                return false; // Nur Fraktionen mit Rang 2 oder höher können Lords wechseln
            }

            if (!faction.members.Contains(GameNetwork.MyPeer))
            {
                return false; // Spieler muss in der Fraktion sein
            }

            if (faction.Gold < LordChangeCost)
            {
                return false; // Fraktion hat nicht genug Gold
            }

            return true;
        }

        public static void TryChangeLord(Faction faction, NetworkCommunicator newLord)
        {
            if (!CanChangeLord(faction))
            {
                if (faction.Gold < LordChangeCost)
                {
                    InformationManager.DisplayMessage(new InformationMessage("Your faction does not have enough gold to change the Lord."));
                }
                else
                {
                    InformationManager.DisplayMessage(new InformationMessage("Your faction rank is too low to change the Lord."));
                }
                return;
            }

            if (!faction.members.Contains(newLord))
            {
                InformationManager.DisplayMessage(new InformationMessage("The selected player is not in your faction."));
                return;
            }

            // Gold abziehen
            faction.Gold -= LordChangeCost;

            // Lord wechseln
            faction.lordId = newLord.VirtualPlayer.ToPlayerId();
            InformationManager.DisplayMessage(new InformationMessage($"The new Lord of faction {faction.name} is now {newLord.UserName}."));
        }
    }
}


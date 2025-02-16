using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Network.Messages;
using PersistentEmpiresLib.Factions;
using PersistentEmpiresLib.Helpers;
using TaleWorlds.Core;

namespace PersistentEmpiresLib.NetworkMessages.Server
{
    [DefineGameNetworkMessageTypeForMod(GameNetworkMessageSendType.FromServer)]
    public sealed class FactionPollCancelled : GameNetworkMessage
    {
        public int FactionIndex { get; private set; }

        public FactionPollCancelled() { }

        public FactionPollCancelled(int factionIndex)
        {
            this.FactionIndex = factionIndex;
        }

        protected override bool OnRead()
        {
            bool result = true;
            this.FactionIndex = GameNetworkMessage.ReadIntFromPacket(new CompressionInfo.Integer(-1, 200), ref result);
            return result;
        }

        protected override void OnWrite()
        {
            GameNetworkMessage.WriteIntToPacket(this.FactionIndex, new CompressionInfo.Integer(-1, 200));
        }

        protected override MultiplayerMessageFilter OnGetLogFilter()
        {
            return MultiplayerMessageFilter.Administration;
        }

        protected override string OnGetLogFormat()
        {
            return $"Poll for faction {FactionIndex} has been cancelled.";
        }

        public static bool TryCancelPoll(int factionIndex)
        {
            Faction faction = FactionManager.GetFactionByIndex(factionIndex);
            if (faction == null)
            {
                InformationManager.DisplayMessage(new InformationMessage("Faction not found, poll cancellation failed."));
                return false;
            }

            // Broadcast Message an alle Spieler
            GameNetwork.BeginBroadcastModuleEvent();
            GameNetwork.WriteMessage(new FactionPollCancelled(factionIndex));
            GameNetwork.EndBroadcastModuleEvent();

            InformationManager.DisplayMessage(new InformationMessage($"The ongoing poll for faction {faction.name} has been cancelled."));

            return true;
        }
    }
}

using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Network.Messages;
using PersistentEmpiresLib.Factions;
using System.Linq;

namespace PersistentEmpiresLib.NetworkMessages.Server
{
    [DefineGameNetworkMessageTypeForMod(GameNetworkMessageSendType.FromServer)]
    public sealed class AddMarshallIdToFaction : GameNetworkMessage
    {
        public string MarshallId;
        public int FactionIndex;

        public AddMarshallIdToFaction() { }

        public AddMarshallIdToFaction(string marshallId, int factionIndex)
        {
            this.MarshallId = marshallId;
            this.FactionIndex = factionIndex;
        }

        protected override MultiplayerMessageFilter OnGetLogFilter()
        {
            return MultiplayerMessageFilter.None;
        }

        protected override string OnGetLogFormat()
        {
            return "AddMarshallIdToFaction";
        }

        protected override bool OnRead()
        {
            bool result = true;
            this.FactionIndex = GameNetworkMessage.ReadIntFromPacket(new CompressionInfo.Integer(-1, 200, true), ref result);
            this.MarshallId = GameNetworkMessage.ReadStringFromPacket(ref result);
            return result;
        }

        protected override void OnWrite()
        {
            GameNetworkMessage.WriteIntToPacket(this.FactionIndex, new CompressionInfo.Integer(-1, 200, true));
            GameNetworkMessage.WriteStringToPacket(this.MarshallId);
        }

        public static bool CanAddMarshall(Faction faction)
        {
            if (faction.Rank < 2)
            {
                return false; // Nur Fraktionen ab Rang 2 können Marschälle ernennen
            }

            int maxMarshalls = faction.MaxMembers / 10; // Beispielregel: 1 Marschall pro 10 Mitglieder
            return faction.marshalls.Count < maxMarshalls;
        }

        public static void TryAddMarshall(Faction faction, string marshallId)
        {
            if (!CanAddMarshall(faction))
            {
                InformationManager.DisplayMessage(new InformationMessage("Your faction rank is too low or you already have the maximum number of marshalls."));
                return;
            }

            if (!faction.marshalls.Contains(marshallId))
            {
                faction.marshalls.Add(marshallId);
                InformationManager.DisplayMessage(new InformationMessage($"New Marshall added: {marshallId}"));
            }
        }
    }
}

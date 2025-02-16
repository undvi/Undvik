using PersistentEmpiresLib.Factions;
using PersistentEmpiresLib.Helpers;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Network.Messages;

namespace PersistentEmpiresLib.NetworkMessages.Server
{
    [DefineGameNetworkMessageTypeForMod(GameNetworkMessageSendType.FromServer)]
    public sealed class UpdateFactionFromServer : GameNetworkMessage
    {
        public string Name { get; private set; }
        public string BannerCode { get; private set; }
        public int FactionIndex { get; private set; }

        public UpdateFactionFromServer() { }

        public UpdateFactionFromServer(Faction updatedFaction, int factionIndex)
        {
            this.FactionIndex = factionIndex;
            this.Name = string.IsNullOrEmpty(updatedFaction.name) ? "Unknown Faction" : updatedFaction.name;
            this.BannerCode = string.IsNullOrEmpty(updatedFaction.banner.Serialize()) ? "DefaultBanner" : updatedFaction.banner.Serialize();
        }

        protected override MultiplayerMessageFilter OnGetLogFilter()
        {
            return MultiplayerMessageFilter.General;
        }

        protected override string OnGetLogFormat()
        {
            return $"Faction Update | Index: {FactionIndex}, Name: {Name}, BannerCode: {BannerCode}";
        }

        protected override bool OnRead()
        {
            bool result = true;
            this.FactionIndex = GameNetworkMessage.ReadIntFromPacket(new CompressionInfo.Integer(-1, 200, true), ref result);
            this.Name = GameNetworkMessage.ReadStringFromPacket(ref result);
            this.BannerCode = PENetworkModule.ReadBannerCodeFromPacket(ref result);

            // Sicherheitschecks für leere Werte
            if (string.IsNullOrEmpty(this.Name))
                this.Name = "Unknown Faction";

            if (string.IsNullOrEmpty(this.BannerCode))
                this.BannerCode = "DefaultBanner";

            return result;
        }

        protected override void OnWrite()
        {
            GameNetworkMessage.WriteIntToPacket(this.FactionIndex, new CompressionInfo.Integer(-1, 200, true));
            GameNetworkMessage.WriteStringToPacket(this.Name);
            PENetworkModule.WriteBannerCodeToPacket(this.BannerCode);
        }
    }
}

using PersistentEmpiresLib.Factions;
using PersistentEmpiresLib.Helpers;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Network.Messages;
using System.Collections.Generic;

namespace PersistentEmpiresLib.NetworkMessages.Server
{
    [DefineGameNetworkMessageTypeForMod(GameNetworkMessageSendType.FromServer)]
    public sealed class UpdateFactionFromServer : GameNetworkMessage
    {
        public string Name { get; private set; }
        public string BannerCode { get; private set; }
        public int FactionIndex { get; private set; }
        public float TaxRate { get; private set; }
        public string ExportOrders { get; private set; }
        public string TerritoryBonus { get; private set; }
        public List<int> Vassals { get; private set; }
        public int FactionRank { get; private set; }

        public UpdateFactionFromServer() { }

        public UpdateFactionFromServer(Faction updatedFaction, int factionIndex)
        {
            this.FactionIndex = factionIndex;
            this.Name = string.IsNullOrEmpty(updatedFaction.name) ? "Unknown Faction" : updatedFaction.name;
            this.BannerCode = string.IsNullOrEmpty(updatedFaction.banner.Serialize()) ? "DefaultBanner" : updatedFaction.banner.Serialize();
            this.TaxRate = updatedFaction.TaxRate;
            this.ExportOrders = updatedFaction.GetExportOrdersAsString();
            this.TerritoryBonus = updatedFaction.TerritoryBonus;
            this.Vassals = updatedFaction.GetVassalFactionIndexes();
            this.FactionRank = updatedFaction.Rank;
        }

        protected override MultiplayerMessageFilter OnGetLogFilter()
        {
            return MultiplayerMessageFilter.General;
        }

        protected override string OnGetLogFormat()
        {
            return $"Faction Update | Index: {FactionIndex}, Name: {Name}, BannerCode: {BannerCode}, TaxRate: {TaxRate}, TerritoryBonus: {TerritoryBonus}, Rank: {FactionRank}";
        }

        protected override bool OnRead()
        {
            bool result = true;
            this.FactionIndex = GameNetworkMessage.ReadIntFromPacket(new CompressionInfo.Integer(-1, 200, true), ref result);
            this.Name = GameNetworkMessage.ReadStringFromPacket(ref result);
            this.BannerCode = PENetworkModule.ReadBannerCodeFromPacket(ref result);
            this.TaxRate = GameNetworkMessage.ReadFloatFromPacket(ref result);
            this.ExportOrders = GameNetworkMessage.ReadStringFromPacket(ref result);
            this.TerritoryBonus = GameNetworkMessage.ReadStringFromPacket(ref result);
            this.Vassals = GameNetworkMessage.ReadListFromPacket(() => GameNetworkMessage.ReadIntFromPacket(new CompressionInfo.Integer(-1, 200, true), ref result), ref result);
            this.FactionRank = GameNetworkMessage.ReadIntFromPacket(new CompressionInfo.Integer(1, 5, true), ref result);

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
            GameNetworkMessage.WriteFloatToPacket(this.TaxRate);
            GameNetworkMessage.WriteStringToPacket(this.ExportOrders);
            GameNetworkMessage.WriteStringToPacket(this.TerritoryBonus);
            GameNetworkMessage.WriteListToPacket(this.Vassals, (value) => GameNetworkMessage.WriteIntToPacket(value, new CompressionInfo.Integer(-1, 200, true)));
            GameNetworkMessage.WriteIntToPacket(this.FactionRank, new CompressionInfo.Integer(1, 5, true));
        }
    }
}

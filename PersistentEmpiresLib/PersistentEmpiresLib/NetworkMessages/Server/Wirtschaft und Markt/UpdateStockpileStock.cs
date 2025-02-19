using PersistentEmpiresLib.SceneScripts;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Network.Messages;
using System.Collections.Generic;

namespace PersistentEmpiresLib.NetworkMessages.Server
{
    [DefineGameNetworkMessageTypeForMod(GameNetworkMessageSendType.FromServer)]
    public sealed class UpdateStockpileStock : GameNetworkMessage
    {
        public MissionObject StockpileMarket { get; private set; }
        public List<int> ItemIndexes { get; private set; }
        public List<int> NewStocks { get; private set; }
        public List<float> MarketTaxRates { get; private set; }
        public List<int> ItemPrices { get; private set; } // Spielerdefinierte Preise

        public UpdateStockpileStock() { }

        public UpdateStockpileStock(MissionObject stockpileMarket, List<int> itemIndexes, List<int> newStocks, List<float> marketTaxRates, List<int> itemPrices)
        {
            this.StockpileMarket = stockpileMarket ?? throw new System.ArgumentNullException(nameof(stockpileMarket), "⚠️ Fehler: StockpileMarket ist null!");
            this.ItemIndexes = itemIndexes ?? new List<int>();
            this.NewStocks = newStocks ?? new List<int>();
            this.MarketTaxRates = marketTaxRates ?? new List<float>();
            this.ItemPrices = itemPrices ?? new List<int>();
        }

        protected override MultiplayerMessageFilter OnGetLogFilter()
        {
            return MultiplayerMessageFilter.MissionObjects;
        }

        protected override string OnGetLogFormat()
        {
            return this.StockpileMarket != null
                ? $"🔄 Lager-Update: {ItemIndexes.Count} Items aktualisiert"
                : "⚠️ Fehler: StockpileMarket ist NULL!";
        }

        protected override bool OnRead()
        {
            bool result = true;
            this.StockpileMarket = Mission.MissionNetworkHelper.GetMissionObjectFromMissionObjectId(
                GameNetworkMessage.ReadMissionObjectIdFromPacket(ref result)
            );

            int count = GameNetworkMessage.ReadIntFromPacket(new CompressionInfo.Integer(0, 4096, true), ref result);
            this.ItemIndexes = new List<int>();
            this.NewStocks = new List<int>();
            this.MarketTaxRates = new List<float>();
            this.ItemPrices = new List<int>();

            for (int i = 0; i < count; i++)
            {
                this.ItemIndexes.Add(GameNetworkMessage.ReadIntFromPacket(new CompressionInfo.Integer(0, 4096, true), ref result));
                this.NewStocks.Add(GameNetworkMessage.ReadIntFromPacket(new CompressionInfo.Integer(0, PE_StockpileMarket.MAX_STOCK_COUNT, true), ref result));
                this.MarketTaxRates.Add(GameNetworkMessage.ReadFloatFromPacket(new CompressionInfo.Float(0f, 1f, 0.01f), ref result));
                this.ItemPrices.Add(GameNetworkMessage.ReadIntFromPacket(new CompressionInfo.Integer(1, 100000, true), ref result)); // Preise dynamisch
            }

            return result;
        }

        protected override void OnWrite()
        {
            if (this.StockpileMarket == null)
            {
                InformationManager.DisplayMessage(new InformationMessage("⚠️ Fehler: Kein gültiges Lager für Netzwerksynchronisation!"));
                return;
            }

            GameNetworkMessage.WriteMissionObjectIdToPacket(this.StockpileMarket.Id);
            GameNetworkMessage.WriteIntToPacket(this.ItemIndexes.Count, new CompressionInfo.Integer(0, 4096, true));

            for (int i = 0; i < this.ItemIndexes.Count; i++)
            {
                GameNetworkMessage.WriteIntToPacket(this.ItemIndexes[i], new CompressionInfo.Integer(0, 4096, true));
                GameNetworkMessage.WriteIntToPacket(this.NewStocks[i], new CompressionInfo.Integer(0, PE_StockpileMarket.MAX_STOCK_COUNT, true));
                GameNetworkMessage.WriteFloatToPacket(this.MarketTaxRates[i], new CompressionInfo.Float(0f, 1f, 0.01f));
                GameNetworkMessage.WriteIntToPacket(this.ItemPrices[i], new CompressionInfo.Integer(1, 100000, true)); // Dynamische Preise
            }
        }
    }
}

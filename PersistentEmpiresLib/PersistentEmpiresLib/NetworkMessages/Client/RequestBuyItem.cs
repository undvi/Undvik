using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Network.Messages;
using System;
using System.IO;

namespace PersistentEmpiresLib.NetworkMessages.Client
{
    [DefineGameNetworkMessageTypeForMod(GameNetworkMessageSendType.FromClient)]
    public sealed class RequestBuyItem : GameNetworkMessage
    {
        public MissionObject StockpileMarket { get; private set; }
        public int ItemIndex { get; private set; }
        public int BasePrice { get; private set; }
        public float MarketTaxRate { get; private set; }
        public int FinalPrice { get; private set; }
        public int AvailableStock { get; private set; }

        private static string logFilePath = "market_buy_log.txt";

        public RequestBuyItem() { }

        public RequestBuyItem(MissionObject stockpileMarket, int itemIndex, int basePrice, float marketTaxRate, int availableStock)
        {
            if (stockpileMarket == null)
                throw new ArgumentNullException(nameof(stockpileMarket), "⚠️ Fehler: StockpileMarket ist null!");

            if (basePrice <= 0)
                throw new ArgumentOutOfRangeException(nameof(basePrice), "⚠️ Fehler: Kaufpreis muss positiv sein!");

            if (availableStock <= 0)
            {
                throw new InvalidOperationException("⚠️ Fehler: Das Item ist nicht mehr verfügbar!");
            }

            this.StockpileMarket = stockpileMarket;
            this.ItemIndex = itemIndex;
            this.BasePrice = basePrice;
            this.MarketTaxRate = marketTaxRate;
            this.FinalPrice = CalculateFinalPrice(basePrice, marketTaxRate);
            this.AvailableStock = availableStock;

            LogBuyTransaction($"🛒 Kaufanfrage: Item {ItemIndex} für {basePrice} Gold (Steuer: {marketTaxRate * 100}%) → Endpreis: {FinalPrice} Gold | Lagerbestand: {AvailableStock}");
        }

        protected override MultiplayerMessageFilter OnGetLogFilter()
        {
            return MultiplayerMessageFilter.MissionObjects;
        }

        protected override string OnGetLogFormat()
        {
            return $"🛒 Kaufanfrage für Item {ItemIndex} | Preis: {BasePrice} Gold | Steuer: {MarketTaxRate * 100}% | Endpreis: {FinalPrice} Gold | Lagerbestand: {AvailableStock}";
        }

        protected override bool OnRead()
        {
            bool result = true;
            this.StockpileMarket = Mission.MissionNetworkHelper.GetMissionObjectFromMissionObjectId(
                GameNetworkMessage.ReadMissionObjectIdFromPacket(ref result)
            );
            this.ItemIndex = GameNetworkMessage.ReadIntFromPacket(new CompressionInfo.Integer(0, 4096, true), ref result);
            this.BasePrice = GameNetworkMessage.ReadIntFromPacket(new CompressionInfo.Integer(0, 100000, true), ref result);
            this.MarketTaxRate = GameNetworkMessage.ReadFloatFromPacket(new CompressionInfo.Float(0f, 1f, 0.01f), ref result);
            this.AvailableStock = GameNetworkMessage.ReadIntFromPacket(new CompressionInfo.Integer(0, 500, true), ref result);
            this.FinalPrice = CalculateFinalPrice(this.BasePrice, this.MarketTaxRate);

            LogBuyTransaction($"📥 Netzwerk-Update erhalten: Item {ItemIndex} für {BasePrice} Gold | Steuer: {MarketTaxRate * 100}% | Endpreis: {FinalPrice} Gold | Lagerbestand: {AvailableStock}");
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
            GameNetworkMessage.WriteIntToPacket(this.ItemIndex, new CompressionInfo.Integer(0, 4096, true));
            GameNetworkMessage.WriteIntToPacket(this.BasePrice, new CompressionInfo.Integer(0, 100000, true));
            GameNetworkMessage.WriteFloatToPacket(this.MarketTaxRate, new CompressionInfo.Float(0f, 1f, 0.01f));
            GameNetworkMessage.WriteIntToPacket(this.AvailableStock, new CompressionInfo.Integer(0, 500, true));

            LogBuyTransaction($"📤 Kaufanfrage gesendet: Item {ItemIndex} für {BasePrice} Gold | Steuer: {MarketTaxRate * 100}% | Endpreis: {FinalPrice} Gold | Lagerbestand: {AvailableStock}");
        }

        private static int CalculateFinalPrice(int price, float taxRate)
        {
            return (int)(price + (price * taxRate));
        }

        private static void LogBuyTransaction(string logEntry)
        {
            try
            {
                using (StreamWriter writer = new StreamWriter(logFilePath, true))
                {
                    writer.WriteLine($"{DateTime.UtcNow}: {logEntry}");
                }
            }
            catch (Exception ex)
            {
                Debug.Print($"⚠️ Fehler beim Schreiben ins Kauf-Log: {ex.Message}");
            }
        }
    }
}

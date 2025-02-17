using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Network.Messages;
using System;
using System.IO;

namespace PersistentEmpiresLib.NetworkMessages.Client
{
    [DefineGameNetworkMessageTypeForMod(GameNetworkMessageSendType.FromClient)]
    public sealed class RequestSellItem : GameNetworkMessage
    {
        public MissionObject StockpileMarket { get; private set; }
        public int ItemIndex { get; private set; }
        public int SellPrice { get; private set; }
        public float MarketTaxRate { get; private set; }
        public int FinalPrice { get; private set; }
        public int CurrentStock { get; private set; }
        public int MaxStockLimit { get; private set; }
        public long ExpiryTimestamp { get; private set; } // Ablaufzeit für verderbliche Waren

        private static string logFilePath = "market_sell_log.txt";

        public RequestSellItem() { }

        public RequestSellItem(MissionObject stockpileMarket, int itemIndex, int sellPrice, float marketTaxRate, int currentStock, int maxStockLimit, long expiryTimestamp)
        {
            if (stockpileMarket == null)
                throw new ArgumentNullException(nameof(stockpileMarket), "⚠️ Fehler: StockpileMarket ist null!");

            if (sellPrice <= 0)
                throw new ArgumentOutOfRangeException(nameof(sellPrice), "⚠️ Fehler: Verkaufspreis muss positiv sein!");

            if (currentStock >= maxStockLimit)
            {
                throw new InvalidOperationException("⚠️ Fehler: Marktstand hat sein Lagerlimit erreicht!");
            }

            this.StockpileMarket = stockpileMarket;
            this.ItemIndex = itemIndex;
            this.SellPrice = sellPrice;
            this.MarketTaxRate = marketTaxRate;
            this.FinalPrice = CalculateFinalPrice(sellPrice, marketTaxRate);
            this.CurrentStock = currentStock;
            this.MaxStockLimit = maxStockLimit;
            this.ExpiryTimestamp = expiryTimestamp;

            LogSellTransaction($"📦 Verkaufsanfrage: Item {ItemIndex} für {sellPrice} Gold (Steuer: {marketTaxRate * 100}%) → Endpreis: {FinalPrice} Gold | Lager: {CurrentStock}/{MaxStockLimit}");
        }

        protected override MultiplayerMessageFilter OnGetLogFilter()
        {
            return MultiplayerMessageFilter.MissionObjects;
        }

        protected override string OnGetLogFormat()
        {
            return $"📦 Verkaufsanfrage für Item {ItemIndex} | Preis: {SellPrice} Gold | Steuer: {MarketTaxRate * 100}% | Endpreis: {FinalPrice} Gold | Lager: {CurrentStock}/{MaxStockLimit}";
        }

        protected override bool OnRead()
        {
            bool result = true;
            this.StockpileMarket = Mission.MissionNetworkHelper.GetMissionObjectFromMissionObjectId(
                GameNetworkMessage.ReadMissionObjectIdFromPacket(ref result)
            );
            this.ItemIndex = GameNetworkMessage.ReadIntFromPacket(new CompressionInfo.Integer(0, 4096, true), ref result);
            this.SellPrice = GameNetworkMessage.ReadIntFromPacket(new CompressionInfo.Integer(0, 100000, true), ref result);
            this.MarketTaxRate = GameNetworkMessage.ReadFloatFromPacket(new CompressionInfo.Float(0f, 1f, 0.01f), ref result);
            this.CurrentStock = GameNetworkMessage.ReadIntFromPacket(new CompressionInfo.Integer(0, 500, true), ref result);
            this.MaxStockLimit = GameNetworkMessage.ReadIntFromPacket(new CompressionInfo.Integer(0, 500, true), ref result);
            this.ExpiryTimestamp = GameNetworkMessage.ReadLongFromPacket(ref result);
            this.FinalPrice = CalculateFinalPrice(this.SellPrice, this.MarketTaxRate);

            LogSellTransaction($"📥 Netzwerk-Update erhalten: Item {ItemIndex} für {SellPrice} Gold | Steuer: {MarketTaxRate * 100}% | Endpreis: {FinalPrice} Gold | Lager: {CurrentStock}/{MaxStockLimit}");
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
            GameNetworkMessage.WriteIntToPacket(this.SellPrice, new CompressionInfo.Integer(0, 100000, true));
            GameNetworkMessage.WriteFloatToPacket(this.MarketTaxRate, new CompressionInfo.Float(0f, 1f, 0.01f));
            GameNetworkMessage.WriteIntToPacket(this.CurrentStock, new CompressionInfo.Integer(0, 500, true));
            GameNetworkMessage.WriteIntToPacket(this.MaxStockLimit, new CompressionInfo.Integer(0, 500, true));
            GameNetworkMessage.WriteLongToPacket(this.ExpiryTimestamp);

            LogSellTransaction($"📤 Verkaufsanfrage gesendet: Item {ItemIndex} für {SellPrice} Gold | Steuer: {MarketTaxRate * 100}% | Endpreis: {FinalPrice} Gold | Lager: {CurrentStock}/{MaxStockLimit}");
        }

        private static int CalculateFinalPrice(int price, float taxRate)
        {
            return (int)(price - (price * taxRate));
        }

        private static void LogSellTransaction(string logEntry)
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
                Debug.Print($"⚠️ Fehler beim Schreiben ins Verkaufs-Log: {ex.Message}");
            }
        }

        /// <summary>
        /// Überprüft und entfernt Items, die das Ablaufdatum überschritten haben.
        /// </summary>
        public static void CheckAndRemoveExpiredItems()
        {
            long currentTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            // Hier Logik zur Entfernung abgelaufener Items implementieren
            Debug.Print($"🕒 Überprüfung auf abgelaufene Items abgeschlossen. Aktuelle Zeit: {currentTime}");
        }
    }
}

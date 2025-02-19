using System;
using System.Collections.Generic;
using System.IO;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Network.Messages;
using PersistentEmpiresLib.SceneScripts;
using PersistentEmpiresLib.Factions;
using PersistentEmpiresLib.Helpers;

namespace PersistentEmpiresLib.NetworkMessages.Server.Economic
{
    #region Horse Market Updates

    [DefineGameNetworkMessageTypeForMod(GameNetworkMessageSendType.FromServer)]
    public sealed class HorseMarketSetReserve : GameNetworkMessage
    {
        public PE_HorseMarket Market { get; private set; }
        public int Stock { get; private set; }
        public float MarketTaxRate { get; private set; }  // Dynamische Steuer
        public const int MaxMarketCapacity = 100; // Lagerbestandslimit

        public HorseMarketSetReserve() { }

        public HorseMarketSetReserve(PE_HorseMarket market, int stock, float marketTaxRate)
        {
            if (market == null)
                throw new ArgumentNullException(nameof(market), "⚠️ Fehler: HorseMarket ist null!");
            if (marketTaxRate < 0f || marketTaxRate > 1f)
                throw new ArgumentOutOfRangeException(nameof(marketTaxRate), "⚠️ Fehler: Steuer muss zwischen 0% und 100% liegen!");
            if (stock < 0 || stock > MaxMarketCapacity)
                throw new ArgumentOutOfRangeException(nameof(stock), $"⚠️ Fehler: Lagerbestand muss zwischen 0 und {MaxMarketCapacity} liegen!");

            Market = market;
            Stock = stock;
            MarketTaxRate = marketTaxRate;

            LogMarketTransaction($"📜 Pferdemarkt aktualisiert: {Market.Name} | Neuer Bestand: {Stock} | Steuer: {MarketTaxRate * 100}%");
        }

        protected override MultiplayerMessageFilter OnGetLogFilter() => MultiplayerMessageFilter.MissionObjects;
        protected override string OnGetLogFormat() =>
            Market != null
                ? $"🐎 Pferdemarkt-Update: Bestand auf {Stock} gesetzt (Steuer: {MarketTaxRate * 100}%)"
                : "⚠️ Fehler: Market ist NULL!";

        protected override bool OnRead()
        {
            bool result = true;
            Market = Mission.MissionNetworkHelper.GetMissionObjectFromMissionObjectId(
                GameNetworkMessage.ReadMissionObjectIdFromPacket(ref result)) as PE_HorseMarket;
            Stock = GameNetworkMessage.ReadIntFromPacket(new CompressionInfo.Integer(0, MaxMarketCapacity, true), ref result);
            MarketTaxRate = GameNetworkMessage.ReadFloatFromPacket(new CompressionInfo.Float(0f, 1f, 0.01f), ref result);
            if (Market == null)
            {
                InformationManager.DisplayMessage(new InformationMessage("⚠️ Fehler: Pferdemarkt konnte nicht aus dem Netzwerknachricht gelesen werden!"));
                return false;
            }
            if (MarketTaxRate < 0f || MarketTaxRate > 1f)
            {
                InformationManager.DisplayMessage(new InformationMessage("⚠️ Fehler: Steuerwert außerhalb des gültigen Bereichs!"));
                return false;
            }
            if (Stock < 0 || Stock > MaxMarketCapacity)
            {
                InformationManager.DisplayMessage(new InformationMessage($"⚠️ Fehler: Lagerbestand überschreitet Limit ({MaxMarketCapacity})!"));
                return false;
            }
            LogMarketTransaction($"📥 Netzwerk-Update erhalten: {Market.Name} | Bestand: {Stock} | Steuer: {MarketTaxRate * 100}%");
            return result;
        }

        protected override void OnWrite()
        {
            if (Market == null)
            {
                InformationManager.DisplayMessage(new InformationMessage("⚠️ Fehler: Kein gültiger Pferdemarkt für Netzwerksynchronisation!"));
                return;
            }
            try
            {
                GameNetworkMessage.WriteMissionObjectIdToPacket(Market.Id);
                GameNetworkMessage.WriteIntToPacket(Stock, new CompressionInfo.Integer(0, MaxMarketCapacity, true));
                GameNetworkMessage.WriteFloatToPacket(MarketTaxRate, new CompressionInfo.Float(0f, 1f, 0.01f));
                LogMarketTransaction($"📤 Datenpaket gesendet: {Market.Name} | Bestand: {Stock} | Steuer: {MarketTaxRate * 100}%");
            }
            catch (Exception ex)
            {
                InformationManager.DisplayMessage(new InformationMessage($"⚠️ Fehler beim Schreiben des Pferdemarkt-Datenpakets: {ex.Message}"));
            }
        }

        private static void LogMarketTransaction(string logEntry)
        {
            string logFilePath = "horse_market_log.txt";
            try
            {
                using (StreamWriter writer = new StreamWriter(logFilePath, true))
                {
                    writer.WriteLine($"{DateTime.UtcNow}: {logEntry}");
                }
            }
            catch (Exception ex)
            {
                Debug.Print($"⚠️ Fehler beim Schreiben ins Markt-Log: {ex.Message}");
            }
        }
    }

    #endregion

    #region Bank Updates

    [DefineGameNetworkMessageTypeForMod(GameNetworkMessageSendType.FromServer)]
    public sealed class OpenBank : GameNetworkMessage
    {
        public MissionObject Bank { get; private set; }
        public int Amount { get; private set; }
        public int TaxRate { get; private set; }

        public OpenBank() { }
        public OpenBank(MissionObject bank, int amount, int taxrate)
        {
            Bank = bank;
            Amount = amount;
            TaxRate = taxrate;
        }
        protected override MultiplayerMessageFilter OnGetLogFilter() => MultiplayerMessageFilter.MissionObjects;
        protected override string OnGetLogFormat() => "OpenBank";
        protected override bool OnRead()
        {
            bool result = true;
            Bank = Mission.MissionNetworkHelper.GetMissionObjectFromMissionObjectId(GameNetworkMessage.ReadMissionObjectIdFromPacket(ref result));
            Amount = GameNetworkMessage.ReadIntFromPacket(new CompressionInfo.Integer(0, 1000000000, true), ref result);
            TaxRate = GameNetworkMessage.ReadIntFromPacket(new CompressionInfo.Integer(0, 100, true), ref result);
            return result;
        }
        protected override void OnWrite()
        {
            GameNetworkMessage.WriteMissionObjectIdToPacket(Bank.Id);
            GameNetworkMessage.WriteIntToPacket(Amount, new CompressionInfo.Integer(0, 1000000000, true));
            GameNetworkMessage.WriteIntToPacket(TaxRate, new CompressionInfo.Integer(0, 100, true));
        }
    }

    #endregion

    #region Stockpile Market Updates

    [DefineGameNetworkMessageTypeForMod(GameNetworkMessageSendType.FromServer)]
    public sealed class OpenStockpileMarket : GameNetworkMessage
    {
        public MissionObject StockpileMarketEntity { get; private set; }
        public Inventory PlayerInventory { get; private set; }
        public float MarketTaxRate { get; private set; }  // Fraktionssteuer für Markt

        public OpenStockpileMarket() { }
        public OpenStockpileMarket(PE_StockpileMarket stockpileMarketEntity, Inventory playerInventory, float marketTaxRate)
        {
            if (stockpileMarketEntity == null)
                throw new ArgumentNullException(nameof(stockpileMarketEntity), "⚠️ Fehler: StockpileMarketEntity ist null!");
            if (playerInventory == null)
                throw new ArgumentNullException(nameof(playerInventory), "⚠️ Fehler: Spielerinventar ist null!");
            if (marketTaxRate < 0f || marketTaxRate > 1f)
                throw new ArgumentOutOfRangeException(nameof(marketTaxRate), "⚠️ Fehler: Steuer muss zwischen 0% und 100% liegen!");

            StockpileMarketEntity = stockpileMarketEntity;
            PlayerInventory = playerInventory;
            MarketTaxRate = marketTaxRate;
        }
        protected override MultiplayerMessageFilter OnGetLogFilter() => MultiplayerMessageFilter.MissionObjects;
        protected override string OnGetLogFormat() =>
            StockpileMarketEntity != null
                ? $"📦 Lager geöffnet für {StockpileMarketEntity.Name}, Steuer: {MarketTaxRate * 100}%"
                : "⚠️ Fehler: StockpileMarketEntity ist NULL!";
        protected override bool OnRead()
        {
            bool result = true;
            StockpileMarketEntity = Mission.MissionNetworkHelper.GetMissionObjectFromMissionObjectId(GameNetworkMessage.ReadMissionObjectIdFromPacket(ref result));
            PlayerInventory = PENetworkModule.ReadInventoryPlayer(ref result);
            MarketTaxRate = GameNetworkMessage.ReadFloatFromPacket(new CompressionInfo.Float(0f, 1f, 0.01f), ref result);
            return result;
        }
        protected override void OnWrite()
        {
            GameNetworkMessage.WriteMissionObjectIdToPacket(StockpileMarketEntity.Id);
            PENetworkModule.WriteInventoryPlayer(PlayerInventory);
            GameNetworkMessage.WriteFloatToPacket(MarketTaxRate, new CompressionInfo.Float(0f, 1f, 0.01f));
        }
    }

    // Update für mehrere Items (MultiStock) im Lager (Stockpile)
    [DefineGameNetworkMessageTypeForMod(GameNetworkMessageSendType.FromServer)]
    public sealed class UpdateStockpileMultiStock : GameNetworkMessage
    {
        public MissionObject StockpileMarket { get; private set; }
        public List<int> ItemIndexes { get; private set; }
        public List<int> NewStocks { get; private set; }
        public List<float> MarketTaxRates { get; private set; } // Fraktionssteuer pro Item
        public List<int> ItemPrices { get; private set; } // Spielerdefinierte Preise pro Item

        public UpdateStockpileMultiStock() { }
        public UpdateStockpileMultiStock(MissionObject stockpileMarket, List<int> itemIndexes, List<int> newStocks, List<float> marketTaxRates, List<int> itemPrices)
        {
            if (stockpileMarket == null) throw new ArgumentNullException(nameof(stockpileMarket), "⚠️ Fehler: StockpileMarket ist null!");
            if (itemIndexes == null || newStocks == null || marketTaxRates == null || itemPrices == null)
                throw new ArgumentNullException("⚠️ Fehler: Item-Listen dürfen nicht null sein!");
            StockpileMarket = stockpileMarket;
            ItemIndexes = itemIndexes;
            NewStocks = newStocks;
            MarketTaxRates = marketTaxRates;
            ItemPrices = itemPrices;
        }
        protected override MultiplayerMessageFilter OnGetLogFilter() => MultiplayerMessageFilter.MissionObjects;
        protected override string OnGetLogFormat() =>
            StockpileMarket != null
                ? $"🔄 Lager-Update für {StockpileMarket.Name}: {ItemIndexes.Count} Items aktualisiert"
                : "⚠️ Fehler: StockpileMarket ist NULL!";
        protected override bool OnRead()
        {
            bool result = true;
            StockpileMarket = Mission.MissionNetworkHelper.GetMissionObjectFromMissionObjectId(GameNetworkMessage.ReadMissionObjectIdFromPacket(ref result));
            int itemCount = GameNetworkMessage.ReadIntFromPacket(new CompressionInfo.Integer(0, 4096, true), ref result);
            ItemIndexes = new List<int>(itemCount);
            NewStocks = new List<int>(itemCount);
            MarketTaxRates = new List<float>(itemCount);
            ItemPrices = new List<int>(itemCount);
            for (int i = 0; i < itemCount; i++)
            {
                ItemIndexes.Add(GameNetworkMessage.ReadIntFromPacket(new CompressionInfo.Integer(0, 4096, true), ref result));
                NewStocks.Add(GameNetworkMessage.ReadIntFromPacket(new CompressionInfo.Integer(0, PE_StockpileMarket.MAX_STOCK_COUNT, true), ref result));
                MarketTaxRates.Add(GameNetworkMessage.ReadFloatFromPacket(new CompressionInfo.Float(0f, 1f, 0.01f), ref result));
                ItemPrices.Add(GameNetworkMessage.ReadIntFromPacket(new CompressionInfo.Integer(1, 100000, true), ref result));
            }
            return result;
        }
        protected override void OnWrite()
        {
            if (StockpileMarket == null)
            {
                InformationManager.DisplayMessage(new InformationMessage("⚠️ Fehler: Kein gültiges Lager für Netzwerksynchronisation!"));
                return;
            }
            GameNetworkMessage.WriteMissionObjectIdToPacket(StockpileMarket.Id);
            GameNetworkMessage.WriteIntToPacket(ItemIndexes.Count, new CompressionInfo.Integer(0, 4096, true));
            for (int i = 0; i < ItemIndexes.Count; i++)
            {
                GameNetworkMessage.WriteIntToPacket(ItemIndexes[i], new CompressionInfo.Integer(0, 4096, true));
                GameNetworkMessage.WriteIntToPacket(NewStocks[i], new CompressionInfo.Integer(0, PE_StockpileMarket.MAX_STOCK_COUNT, true));
                GameNetworkMessage.WriteFloatToPacket(MarketTaxRates[i], new CompressionInfo.Float(0f, 1f, 0.01f));
                GameNetworkMessage.WriteIntToPacket(ItemPrices[i], new CompressionInfo.Integer(1, 100000, true));
            }
        }
    }

    #endregion

    #region Trading Center Updates

    // Öffnet das Handelszentrum.
    [DefineGameNetworkMessageTypeForMod(GameNetworkMessageSendType.FromServer)]
    public sealed class OpenTradingCenter : GameNetworkMessage
    {
        public MissionObject TradingCenterEntity { get; private set; }
        public Inventory PlayerInventory { get; private set; }

        public OpenTradingCenter() { }
        public OpenTradingCenter(PE_TradeCenter tradeCenter, Inventory playerInventory)
        {
            TradingCenterEntity = tradeCenter;
            PlayerInventory = playerInventory;
        }
        protected override MultiplayerMessageFilter OnGetLogFilter() => MultiplayerMessageFilter.MissionObjects;
        protected override string OnGetLogFormat() => "OpenTradingCenter";
        protected override bool OnRead()
        {
            bool result = true;
            TradingCenterEntity = Mission.MissionNetworkHelper.GetMissionObjectFromMissionObjectId(GameNetworkMessage.ReadMissionObjectIdFromPacket(ref result));
            PlayerInventory = PENetworkModule.ReadInventoryPlayer(ref result);
            return result;
        }
        protected override void OnWrite()
        {
            GameNetworkMessage.WriteMissionObjectIdToPacket(TradingCenterEntity.Id);
            PENetworkModule.WriteInventoryPlayer(PlayerInventory);
        }
    }

    // Update für mehrere Items im Handelszentrum (MultiStock).
    [DefineGameNetworkMessageTypeForMod(GameNetworkMessageSendType.FromServer)]
    public sealed class UpdateTradingCenterMultiStock : GameNetworkMessage
    {
        public MissionObject TradingCenter { get; private set; }
        public List<int> ItemIndexes { get; private set; }
        public List<int> NewStocks { get; private set; }
        public List<float> TaxRates { get; private set; }
        public List<int> PlayerSetPrices { get; private set; }
        public int MaxStorageCapacity { get; private set; }

        public UpdateTradingCenterMultiStock() { }
        public UpdateTradingCenterMultiStock(MissionObject tradingCenter, List<int> itemIndexes, List<int> newStocks, List<float> taxRates, List<int> playerSetPrices, int maxStorageCapacity)
        {
            if (tradingCenter == null)
                throw new ArgumentNullException(nameof(tradingCenter), "⚠️ Fehler: Handelszentrum ist null!");
            TradingCenter = tradingCenter;
            ItemIndexes = itemIndexes;
            NewStocks = newStocks;
            TaxRates = taxRates;
            PlayerSetPrices = playerSetPrices;
            MaxStorageCapacity = maxStorageCapacity;
        }
        protected override MultiplayerMessageFilter OnGetLogFilter() => MultiplayerMessageFilter.MissionObjects;
        protected override string OnGetLogFormat() =>
            TradingCenter != null
                ? $"🔄 Handelszentrum-MultiStock: {ItemIndexes.Count} Items aktualisiert (Max: {MaxStorageCapacity})"
                : "⚠️ Fehler: Handelszentrum ist NULL!";
        protected override bool OnRead()
        {
            bool result = true;
            TradingCenter = Mission.MissionNetworkHelper.GetMissionObjectFromMissionObjectId(GameNetworkMessage.ReadMissionObjectIdFromPacket(ref result));
            int count = GameNetworkMessage.ReadIntFromPacket(new CompressionInfo.Integer(0, 4096, true), ref result);
            ItemIndexes = new List<int>();
            NewStocks = new List<int>();
            TaxRates = new List<float>();
            PlayerSetPrices = new List<int>();
            for (int i = 0; i < count; i++)
            {
                ItemIndexes.Add(GameNetworkMessage.ReadIntFromPacket(new CompressionInfo.Integer(0, 4096, true), ref result));
                NewStocks.Add(GameNetworkMessage.ReadIntFromPacket(new CompressionInfo.Integer(0, PE_TradeCenter.MAX_STOCK_COUNT, true), ref result));
                TaxRates.Add(GameNetworkMessage.ReadFloatFromPacket(new CompressionInfo.Float(0f, 1f, 0.01f), ref result));
                PlayerSetPrices.Add(GameNetworkMessage.ReadIntFromPacket(new CompressionInfo.Integer(0, 100000, true), ref result));
            }
            MaxStorageCapacity = GameNetworkMessage.ReadIntFromPacket(new CompressionInfo.Integer(0, 500, true), ref result);
            return result;
        }
        protected override void OnWrite()
        {
            if (TradingCenter == null)
            {
                InformationManager.DisplayMessage(new InformationMessage("⚠️ Fehler: Kein gültiges Handelszentrum für Netzwerksynchronisation!"));
                return;
            }
            GameNetworkMessage.WriteMissionObjectIdToPacket(TradingCenter.Id);
            GameNetworkMessage.WriteIntToPacket(ItemIndexes.Count, new CompressionInfo.Integer(0, 4096, true));
            for (int i = 0; i < ItemIndexes.Count; i++)
            {
                GameNetworkMessage.WriteIntToPacket(ItemIndexes[i], new CompressionInfo.Integer(0, 4096, true));
                GameNetworkMessage.WriteIntToPacket(NewStocks[i], new CompressionInfo.Integer(0, PE_TradeCenter.MAX_STOCK_COUNT, true));
                GameNetworkMessage.WriteFloatToPacket(TaxRates[i], new CompressionInfo.Float(0f, 1f, 0.01f));
                GameNetworkMessage.WriteIntToPacket(PlayerSetPrices[i], new CompressionInfo.Integer(0, 100000, true));
            }
            GameNetworkMessage.WriteIntToPacket(MaxStorageCapacity, new CompressionInfo.Integer(0, 500, true));
        }
    }

    // Update für ein einzelnes Item im Handelszentrum.
    [DefineGameNetworkMessageTypeForMod(GameNetworkMessageSendType.FromServer)]
    public sealed class UpdateTradingCenterStock : GameNetworkMessage
    {
        public MissionObject TradingCenter { get; private set; }
        public int NewStock { get; private set; }
        public int ItemIndex { get; private set; }
        public float TaxRate { get; private set; }
        public int PlayerSetPrice { get; private set; }
        public int MaxStorageCapacity { get; private set; }

        public UpdateTradingCenterStock() { }
        public UpdateTradingCenterStock(MissionObject tradingCenter, int newStock, int itemIndex, float taxRate, int playerSetPrice, int maxStorageCapacity)
        {
            TradingCenter = tradingCenter ?? throw new ArgumentNullException(nameof(tradingCenter), "⚠️ Fehler: Handelszentrum ist null!");
            NewStock = newStock;
            ItemIndex = itemIndex;
            TaxRate = taxRate;
            PlayerSetPrice = playerSetPrice;
            MaxStorageCapacity = maxStorageCapacity;
        }
        protected override MultiplayerMessageFilter OnGetLogFilter() => MultiplayerMessageFilter.MissionObjects;
        protected override string OnGetLogFormat() =>
            TradingCenter != null
                ? $"🔄 Handelszentrum-Update: Item {ItemIndex} → Bestand: {NewStock}/{MaxStorageCapacity}, Preis: {PlayerSetPrice} Gold, Steuer: {TaxRate * 100}%"
                : "⚠️ Fehler: TradingCenter ist NULL!";
        protected override bool OnRead()
        {
            bool result = true;
            TradingCenter = Mission.MissionNetworkHelper.GetMissionObjectFromMissionObjectId(GameNetworkMessage.ReadMissionObjectIdFromPacket(ref result));
            NewStock = GameNetworkMessage.ReadIntFromPacket(new CompressionInfo.Integer(0, PE_TradeCenter.MAX_STOCK_COUNT, true), ref result);
            ItemIndex = GameNetworkMessage.ReadIntFromPacket(new CompressionInfo.Integer(0, 4096, true), ref result);
            TaxRate = GameNetworkMessage.ReadFloatFromPacket(new CompressionInfo.Float(0f, 1f, 0.01f), ref result);
            PlayerSetPrice = GameNetworkMessage.ReadIntFromPacket(new CompressionInfo.Integer(0, 100000, true), ref result);
            MaxStorageCapacity = GameNetworkMessage.ReadIntFromPacket(new CompressionInfo.Integer(0, 500, true), ref result);
            return result;
        }
        protected override void OnWrite()
        {
            if (TradingCenter == null)
            {
                InformationManager.DisplayMessage(new InformationMessage("⚠️ Fehler: Kein gültiges Handelszentrum für Netzwerksynchronisation!"));
                return;
            }
            GameNetworkMessage.WriteMissionObjectIdToPacket(TradingCenter.Id);
            GameNetworkMessage.WriteIntToPacket(NewStock, new CompressionInfo.Integer(0, PE_TradeCenter.MAX_STOCK_COUNT, true));
            GameNetworkMessage.WriteIntToPacket(ItemIndex, new CompressionInfo.Integer(0, 4096, true));
            GameNetworkMessage.WriteFloatToPacket(TaxRate, new CompressionInfo.Float(0f, 1f, 0.01f));
            GameNetworkMessage.WriteIntToPacket(PlayerSetPrice, new CompressionInfo.Integer(0, 100000, true));
            GameNetworkMessage.WriteIntToPacket(MaxStorageCapacity, new CompressionInfo.Integer(0, 500, true));
        }
    }

    #endregion

    #region Gold & Money Updates

    [DefineGameNetworkMessageTypeForMod(GameNetworkMessageSendType.FromServer)]
    public sealed class PEGoldGain : GameNetworkMessage
    {
        public int Gain;
        public PEGoldGain() { }
        public PEGoldGain(int gold)
        {
            Gain = gold;
        }
        protected override MultiplayerMessageFilter OnGetLogFilter() => MultiplayerMessageFilter.General;
        protected override string OnGetLogFormat() => "Gold gain";
        protected override bool OnRead()
        {
            bool result = true;
            Gain = GameNetworkMessage.ReadIntFromPacket(new CompressionInfo.Integer(0, Int32.MaxValue, true), ref result);
            return result;
        }
        protected override void OnWrite()
        {
            GameNetworkMessage.WriteIntToPacket(Gain, new CompressionInfo.Integer(0, Int32.MaxValue, true));
        }
    }

    [DefineGameNetworkMessageTypeForMod(GameNetworkMessageSendType.FromServer)]
    public sealed class PEGoldLost : GameNetworkMessage
    {
        public int Lost;
        public PEGoldLost() { }
        public PEGoldLost(int gold)
        {
            Lost = gold;
        }
        protected override MultiplayerMessageFilter OnGetLogFilter() => MultiplayerMessageFilter.General;
        protected override string OnGetLogFormat() => "Gold lost";
        protected override bool OnRead()
        {
            bool result = true;
            Lost = GameNetworkMessage.ReadIntFromPacket(new CompressionInfo.Integer(0, Int32.MaxValue, true), ref result);
            return result;
        }
        protected override void OnWrite()
        {
            GameNetworkMessage.WriteIntToPacket(Lost, new CompressionInfo.Integer(0, Int32.MaxValue, true));
        }
    }

    [DefineGameNetworkMessageTypeForMod(GameNetworkMessageSendType.FromServer)]
    public sealed class RevealMoneyPouchServer : GameNetworkMessage
    {
        public NetworkCommunicator Player;
        public int Gold;

        public RevealMoneyPouchServer() { }
        public RevealMoneyPouchServer(NetworkCommunicator player, int gold)
        {
            Player = player;
            Gold = gold;
        }
        protected override MultiplayerMessageFilter OnGetLogFilter() => MultiplayerMessageFilter.General;
        protected override string OnGetLogFormat() => "Money pouch revealed";
        protected override bool OnRead()
        {
            bool result = true;
            Player = GameNetworkMessage.ReadNetworkPeerReferenceFromPacket(ref result);
            Gold = GameNetworkMessage.ReadIntFromPacket(new CompressionInfo.Integer(0, Int32.MaxValue, true), ref result);
            return result;
        }
        protected override void OnWrite()
        {
            GameNetworkMessage.WriteNetworkPeerReferenceToPacket(Player);
            GameNetworkMessage.WriteIntToPacket(Gold, new CompressionInfo.Integer(0, Int32.MaxValue, true));
        }
    }

    [DefineGameNetworkMessageTypeForMod(GameNetworkMessageSendType.FromServer)]
    public sealed class UpdateMoneychestGold : GameNetworkMessage
    {
        public MissionObject Chest;
        public long Gold;

        public UpdateMoneychestGold() { }
        public UpdateMoneychestGold(MissionObject chest, long gold)
        {
            Chest = chest;
            Gold = gold;
        }
        protected override MultiplayerMessageFilter OnGetLogFilter() => MultiplayerMessageFilter.None;
        protected override string OnGetLogFormat() => "Server UpdateMoneychestGold";
        protected override bool OnRead()
        {
            bool result = true;
            Chest = Mission.MissionNetworkHelper.GetMissionObjectFromMissionObjectId(GameNetworkMessage.ReadMissionObjectIdFromPacket(ref result));
            Gold = GameNetworkMessage.ReadLongFromPacket(new CompressionInfo.LongInteger(0, 9999999999, true), ref result);
            return result;
        }
        protected override void OnWrite()
        {
            GameNetworkMessage.WriteMissionObjectIdToPacket(Chest.Id);
            GameNetworkMessage.WriteLongToPacket(Gold, new CompressionInfo.LongInteger(0, 9999999999, true));
        }
    }

    #endregion
}

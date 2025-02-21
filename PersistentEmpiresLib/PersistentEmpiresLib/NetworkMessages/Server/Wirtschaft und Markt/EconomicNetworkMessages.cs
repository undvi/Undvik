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

    /// <summary>
    /// Aktualisiert den Pferdemarkt (Stock, Tax, FactionIndex).
    /// Anbindung ans Tier-System:
    ///  - HorseMarket könnte als Teil deines Tier-Systems dienen (Zucht, Verkauf).
    ///  - Könnte man ausbauen, damit Tiere vermehrt oder
    ///    ins Bausystem (z.B. Stall) integriert werden.
    /// </summary>
    [DefineGameNetworkMessageTypeForMod(GameNetworkMessageSendType.FromServer)]
    public sealed class HorseMarketSetReserve : GameNetworkMessage
    {
        public PE_HorseMarket Market { get; private set; }
        public int Stock { get; private set; }
        public float MarketTaxRate { get; private set; }
        public int FactionIndex { get; private set; }   // NEU: Fraktionszuordnung möglich
        public const int MaxMarketCapacity = 100;       // Lagerbestandslimit

        public HorseMarketSetReserve() { }

        public HorseMarketSetReserve(PE_HorseMarket market, int stock, float marketTaxRate, int factionIndex)
        {
            if (market == null)
                throw new ArgumentNullException(nameof(market), "⚠️ Fehler: HorseMarket ist null!");
            if (marketTaxRate < 0f || marketTaxRate > 1f)
                throw new ArgumentOutOfRangeException(nameof(marketTaxRate), "⚠️ Steuer muss 0–100% sein!");
            if (stock < 0 || stock > MaxMarketCapacity)
                throw new ArgumentOutOfRangeException(nameof(stock),
                    $"⚠️ Fehler: Lagerbestand muss zwischen 0 und {MaxMarketCapacity} liegen!");

            Market = market;
            Stock = stock;
            MarketTaxRate = marketTaxRate;
            FactionIndex = factionIndex;

            LogMarketTransaction($"📜 Pferdemarkt: {Market.Name}, Bestand: {Stock}, Steuer: {MarketTaxRate * 100}%, Faction: {FactionIndex}");
        }

        protected override MultiplayerMessageFilter OnGetLogFilter()
            => MultiplayerMessageFilter.MissionObjects;

        protected override string OnGetLogFormat()
            => Market != null
                ? $"🐎 Pferdemarkt-Update: {Market.Name}, Bestand {Stock}, Steuer: {MarketTaxRate * 100}%, Faction: {FactionIndex}"
                : "⚠️ Market ist NULL!";

        protected override bool OnRead()
        {
            bool result = true;
            Market = Mission.MissionNetworkHelper.GetMissionObjectFromMissionObjectId(
                GameNetworkMessage.ReadMissionObjectIdFromPacket(ref result)) as PE_HorseMarket;
            Stock = GameNetworkMessage.ReadIntFromPacket(
                new CompressionInfo.Integer(0, MaxMarketCapacity, true), ref result);
            MarketTaxRate = GameNetworkMessage.ReadFloatFromPacket(
                new CompressionInfo.Float(0f, 1f, 0.01f), ref result);
            FactionIndex = GameNetworkMessage.ReadIntFromPacket(
                new CompressionInfo.Integer(-1, 1000, true), ref result);

            if (!result || Market == null)
            {
                InformationManager.DisplayMessage(
                    new InformationMessage("⚠️ Fehler: Pferdemarkt-Daten fehlerhaft!"));
                return false;
            }

            // Beispiel: Falls Krieg oder Fraktions-Blockade checken
            if (IsFactionAtWar(FactionIndex))
            {
                // Hier könntest du die Transaktion blockieren,
                // oder den Stock limitieren, ...
                InformationManager.DisplayMessage(new InformationMessage(
                    $"⚠️ Krieg aktiv! {Market.Name} kann ggf. nicht voll beliefert werden."
                ));
            }

            LogMarketTransaction($"📥 HorseMarketSetReserve -> {Market.Name}, Stock: {Stock}, Steuer: {MarketTaxRate * 100}%, Faction: {FactionIndex}");
            return true;
        }

        protected override void OnWrite()
        {
            if (Market == null)
            {
                InformationManager.DisplayMessage(new InformationMessage(
                    "⚠️ Fehler: Kein gültiger Pferdemarkt für Netzwerksync!"));
                return;
            }
            try
            {
                GameNetworkMessage.WriteMissionObjectIdToPacket(Market.Id);
                GameNetworkMessage.WriteIntToPacket(Stock,
                    new CompressionInfo.Integer(0, MaxMarketCapacity, true));
                GameNetworkMessage.WriteFloatToPacket(MarketTaxRate,
                    new CompressionInfo.Float(0f, 1f, 0.01f));
                GameNetworkMessage.WriteIntToPacket(FactionIndex,
                    new CompressionInfo.Integer(-1, 1000, true));

                LogMarketTransaction($"📤 Datenpaket gesendet: {Market.Name}, Stock {Stock}, Steuer: {MarketTaxRate * 100}%, Faction: {FactionIndex}");
            }
            catch (Exception ex)
            {
                InformationManager.DisplayMessage(new InformationMessage(
                    $"⚠️ Fehler beim Schreiben des Pferdemarkt-Datenpakets: {ex.Message}"));
            }
        }

        // Beispiel: Check, ob die Fraktion gerade Krieg führt
        // (Roadmap: Kriegssystem/Blockaden).
        private bool IsFactionAtWar(int factionIndex)
        {
            // Falls du FactionManager einsetzt:
            // var faction = FactionManager.GetFactionByIndex(factionIndex);
            // return faction != null && faction.IsAtWar; (fiktives Feld)
            return false;
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

    /// <summary>
    /// Öffnet die Bank für einen Spieler. Neuer Parameter "FactionIndex" als Beispiel:
    ///  - So könntest du z. B. prüfen, ob der Spieler
    ///    berechtigt ist, diese Bank zu nutzen (z. B. nur für bestimmte Fraktion).
    /// </summary>
    [DefineGameNetworkMessageTypeForMod(GameNetworkMessageSendType.FromServer)]
    public sealed class OpenBank : GameNetworkMessage
    {
        public MissionObject Bank { get; private set; }
        public int Amount { get; private set; }
        public int TaxRate { get; private set; }
        public int FactionIndex { get; private set; } // NEU: Bank könnte fraktionsbasiert sein

        public OpenBank() { }

        public OpenBank(MissionObject bank, int amount, int taxRate, int factionIndex)
        {
            Bank = bank;
            Amount = amount;
            TaxRate = taxRate;
            FactionIndex = factionIndex;
        }

        protected override MultiplayerMessageFilter OnGetLogFilter()
            => MultiplayerMessageFilter.MissionObjects;

        protected override string OnGetLogFormat()
            => "OpenBank";

        protected override bool OnRead()
        {
            bool result = true;
            Bank = Mission.MissionNetworkHelper.GetMissionObjectFromMissionObjectId(
                GameNetworkMessage.ReadMissionObjectIdFromPacket(ref result));
            Amount = GameNetworkMessage.ReadIntFromPacket(
                new CompressionInfo.Integer(0, 1000000000, true), ref result);
            TaxRate = GameNetworkMessage.ReadIntFromPacket(
                new CompressionInfo.Integer(0, 100, true), ref result);
            FactionIndex = GameNetworkMessage.ReadIntFromPacket(
                new CompressionInfo.Integer(-1, 1000, true), ref result);

            if (!result || Bank == null)
            {
                InformationManager.DisplayMessage(new InformationMessage(
                    "⚠️ Fehler beim Lesen von Bank-Daten!"));
                return false;
            }

            // Beispiel: Falls der Spieler einer gegnerischen Fraktion angehört
            // => ggf. blockieren
            return true;
        }

        protected override void OnWrite()
        {
            GameNetworkMessage.WriteMissionObjectIdToPacket(Bank?.Id ?? -1);
            GameNetworkMessage.WriteIntToPacket(Amount,
                new CompressionInfo.Integer(0, 1000000000, true));
            GameNetworkMessage.WriteIntToPacket(TaxRate,
                new CompressionInfo.Integer(0, 100, true));
            GameNetworkMessage.WriteIntToPacket(FactionIndex,
                new CompressionInfo.Integer(-1, 1000, true));
        }
    }

    #endregion

    #region Stockpile Market Updates

    /// <summary>
    /// Öffnet das Stockpile-Lager.
    /// Roadmap: 
    ///  - Bausystem -> Lagerhaus
    ///  - Fraktionsabhängig, passive Produktion, etc.
    /// </summary>
    [DefineGameNetworkMessageTypeForMod(GameNetworkMessageSendType.FromServer)]
    public sealed class OpenStockpileMarket : GameNetworkMessage
    {
        public MissionObject StockpileMarketEntity { get; private set; }
        public Inventory PlayerInventory { get; private set; }
        public float MarketTaxRate { get; private set; }  // Fraktionssteuer
        public int FactionIndex { get; private set; }     // NEU: evtl. relevant für Ownership

        public OpenStockpileMarket() { }

        public OpenStockpileMarket(
            PE_StockpileMarket stockpileMarketEntity,
            Inventory playerInventory,
            float marketTaxRate,
            int factionIndex)
        {
            if (stockpileMarketEntity == null)
                throw new ArgumentNullException(nameof(stockpileMarketEntity),
                    "⚠️ Fehler: StockpileMarketEntity ist null!");
            if (playerInventory == null)
                throw new ArgumentNullException(nameof(playerInventory),
                    "⚠️ Fehler: Spielerinventar ist null!");
            if (marketTaxRate < 0f || marketTaxRate > 1f)
                throw new ArgumentOutOfRangeException(nameof(marketTaxRate),
                    "⚠️ Steuer muss zwischen 0% und 100% liegen!");

            StockpileMarketEntity = stockpileMarketEntity;
            PlayerInventory = playerInventory;
            MarketTaxRate = marketTaxRate;
            FactionIndex = factionIndex;
        }

        protected override MultiplayerMessageFilter OnGetLogFilter()
            => MultiplayerMessageFilter.MissionObjects;

        protected override string OnGetLogFormat()
            => StockpileMarketEntity != null
                ? $"📦 Lager geöffnet für {StockpileMarketEntity.Name}, Steuer: {MarketTaxRate * 100}%, Faction: {FactionIndex}"
                : "⚠️ Fehler: StockpileMarketEntity ist NULL!";

        protected override bool OnRead()
        {
            bool result = true;
            StockpileMarketEntity = Mission.MissionNetworkHelper.GetMissionObjectFromMissionObjectId(
                GameNetworkMessage.ReadMissionObjectIdFromPacket(ref result));
            PlayerInventory = PENetworkModule.ReadInventoryPlayer(ref result);
            MarketTaxRate = GameNetworkMessage.ReadFloatFromPacket(
                new CompressionInfo.Float(0f, 1f, 0.01f), ref result);
            FactionIndex = GameNetworkMessage.ReadIntFromPacket(
                new CompressionInfo.Integer(-1, 1000, true), ref result);

            return result;
        }

        protected override void OnWrite()
        {
            GameNetworkMessage.WriteMissionObjectIdToPacket(StockpileMarketEntity?.Id ?? -1);
            PENetworkModule.WriteInventoryPlayer(PlayerInventory);
            GameNetworkMessage.WriteFloatToPacket(MarketTaxRate,
                new CompressionInfo.Float(0f, 1f, 0.01f));
            GameNetworkMessage.WriteIntToPacket(FactionIndex,
                new CompressionInfo.Integer(-1, 1000, true));
        }
    }

    // Update für mehrere Items (MultiStock) im Lager
    [DefineGameNetworkMessageTypeForMod(GameNetworkMessageSendType.FromServer)]
    public sealed class UpdateStockpileMultiStock : GameNetworkMessage
    {
        public MissionObject StockpileMarket { get; private set; }
        public List<int> ItemIndexes { get; private set; }
        public List<int> NewStocks { get; private set; }
        public List<float> MarketTaxRates { get; private set; }
        public List<int> ItemPrices { get; private set; }
        public int FactionIndex { get; private set; } // NEU: Falls Lager fraktionsgebunden

        public UpdateStockpileMultiStock() { }

        public UpdateStockpileMultiStock(
            MissionObject stockpileMarket,
            List<int> itemIndexes,
            List<int> newStocks,
            List<float> marketTaxRates,
            List<int> itemPrices,
            int factionIndex)
        {
            if (stockpileMarket == null)
                throw new ArgumentNullException(nameof(stockpileMarket), "⚠️ StockpileMarket ist null!");
            if (itemIndexes == null || newStocks == null || marketTaxRates == null || itemPrices == null)
                throw new ArgumentNullException("⚠️ Fehler: Item-Listen dürfen nicht null sein!");

            StockpileMarket = stockpileMarket;
            ItemIndexes = itemIndexes;
            NewStocks = newStocks;
            MarketTaxRates = marketTaxRates;
            ItemPrices = itemPrices;
            FactionIndex = factionIndex;
        }

        protected override MultiplayerMessageFilter OnGetLogFilter()
            => MultiplayerMessageFilter.MissionObjects;

        protected override string OnGetLogFormat() =>
            StockpileMarket != null
                ? $"🔄 Lager-Update für {StockpileMarket.Name}: {ItemIndexes.Count} Items aktualisiert, Faction: {FactionIndex}"
                : "⚠️ Fehler: StockpileMarket ist NULL!";

        protected override bool OnRead()
        {
            bool result = true;
            StockpileMarket = Mission.MissionNetworkHelper.GetMissionObjectFromMissionObjectId(
                GameNetworkMessage.ReadMissionObjectIdFromPacket(ref result));
            int itemCount = GameNetworkMessage.ReadIntFromPacket(
                new CompressionInfo.Integer(0, 4096, true), ref result);

            ItemIndexes = new List<int>(itemCount);
            NewStocks = new List<int>(itemCount);
            MarketTaxRates = new List<float>(itemCount);
            ItemPrices = new List<int>(itemCount);

            for (int i = 0; i < itemCount; i++)
            {
                ItemIndexes.Add(GameNetworkMessage.ReadIntFromPacket(
                    new CompressionInfo.Integer(0, 4096, true), ref result));
                NewStocks.Add(GameNetworkMessage.ReadIntFromPacket(
                    new CompressionInfo.Integer(0, PE_StockpileMarket.MAX_STOCK_COUNT, true), ref result));
                MarketTaxRates.Add(GameNetworkMessage.ReadFloatFromPacket(
                    new CompressionInfo.Float(0f, 1f, 0.01f), ref result));
                ItemPrices.Add(GameNetworkMessage.ReadIntFromPacket(
                    new CompressionInfo.Integer(1, 100000, true), ref result));
            }

            FactionIndex = GameNetworkMessage.ReadIntFromPacket(
                new CompressionInfo.Integer(-1, 1000, true), ref result);

            return result;
        }

        protected override void OnWrite()
        {
            if (StockpileMarket == null)
            {
                InformationManager.DisplayMessage(new InformationMessage(
                    "⚠️ Fehler: Kein gültiges Lager für Netzwerksync!"));
                return;
            }

            GameNetworkMessage.WriteMissionObjectIdToPacket(StockpileMarket.Id);
            GameNetworkMessage.WriteIntToPacket(ItemIndexes.Count,
                new CompressionInfo.Integer(0, 4096, true));

            for (int i = 0; i < ItemIndexes.Count; i++)
            {
                GameNetworkMessage.WriteIntToPacket(ItemIndexes[i],
                    new CompressionInfo.Integer(0, 4096, true));
                GameNetworkMessage.WriteIntToPacket(NewStocks[i],
                    new CompressionInfo.Integer(0, PE_StockpileMarket.MAX_STOCK_COUNT, true));
                GameNetworkMessage.WriteFloatToPacket(MarketTaxRates[i],
                    new CompressionInfo.Float(0f, 1f, 0.01f));
                GameNetworkMessage.WriteIntToPacket(ItemPrices[i],
                    new CompressionInfo.Integer(1, 100000, true));
            }

            GameNetworkMessage.WriteIntToPacket(FactionIndex,
                new CompressionInfo.Integer(-1, 1000, true));
        }
    }

    #endregion

    #region Trading Center Updates

    [DefineGameNetworkMessageTypeForMod(GameNetworkMessageSendType.FromServer)]
    public sealed class OpenTradingCenter : GameNetworkMessage
    {
        public MissionObject TradingCenterEntity { get; private set; }
        public Inventory PlayerInventory { get; private set; }
        public int FactionIndex { get; private set; } // NEU: z. B. Eigentümer-Fraktion

        public OpenTradingCenter() { }

        public OpenTradingCenter(
            PE_TradeCenter tradeCenter,
            Inventory playerInventory,
            int factionIndex)
        {
            TradingCenterEntity = tradeCenter;
            PlayerInventory = playerInventory;
            FactionIndex = factionIndex;
        }

        protected override MultiplayerMessageFilter OnGetLogFilter()
            => MultiplayerMessageFilter.MissionObjects;

        protected override string OnGetLogFormat()
            => "OpenTradingCenter";

        protected override bool OnRead()
        {
            bool result = true;
            TradingCenterEntity = Mission.MissionNetworkHelper.GetMissionObjectFromMissionObjectId(
                GameNetworkMessage.ReadMissionObjectIdFromPacket(ref result));
            PlayerInventory = PENetworkModule.ReadInventoryPlayer(ref result);
            FactionIndex = GameNetworkMessage.ReadIntFromPacket(
                new CompressionInfo.Integer(-1, 1000, true), ref result);
            return result;
        }

        protected override void OnWrite()
        {
            GameNetworkMessage.WriteMissionObjectIdToPacket(TradingCenterEntity?.Id ?? -1);
            PENetworkModule.WriteInventoryPlayer(PlayerInventory);
            GameNetworkMessage.WriteIntToPacket(FactionIndex,
                new CompressionInfo.Integer(-1, 1000, true));
        }
    }

    [DefineGameNetworkMessageTypeForMod(GameNetworkMessageSendType.FromServer)]
    public sealed class UpdateTradingCenterMultiStock : GameNetworkMessage
    {
        public MissionObject TradingCenter { get; private set; }
        public List<int> ItemIndexes { get; private set; }
        public List<int> NewStocks { get; private set; }
        public List<float> TaxRates { get; private set; }
        public List<int> PlayerSetPrices { get; private set; }
        public int MaxStorageCapacity { get; private set; }
        public int FactionIndex { get; private set; } // NEU

        public UpdateTradingCenterMultiStock() { }

        public UpdateTradingCenterMultiStock(
            MissionObject tradingCenter,
            List<int> itemIndexes,
            List<int> newStocks,
            List<float> taxRates,
            List<int> playerSetPrices,
            int maxStorageCapacity,
            int factionIndex)
        {
            if (tradingCenter == null)
                throw new ArgumentNullException(nameof(tradingCenter), "⚠️ Fehler: Handelszentrum ist null!");

            TradingCenter = tradingCenter;
            ItemIndexes = itemIndexes;
            NewStocks = newStocks;
            TaxRates = taxRates;
            PlayerSetPrices = playerSetPrices;
            MaxStorageCapacity = maxStorageCapacity;
            FactionIndex = factionIndex;
        }

        protected override MultiplayerMessageFilter OnGetLogFilter()
            => MultiplayerMessageFilter.MissionObjects;

        protected override string OnGetLogFormat() =>
            TradingCenter != null
                ? $"🔄 Handelszentrum-MultiStock: {ItemIndexes.Count} Items, Faction: {FactionIndex}, Max: {MaxStorageCapacity}"
                : "⚠️ Fehler: Handelszentrum ist NULL!";

        protected override bool OnRead()
        {
            bool result = true;
            TradingCenter = Mission.MissionNetworkHelper.GetMissionObjectFromMissionObjectId(
                GameNetworkMessage.ReadMissionObjectIdFromPacket(ref result));
            int count = GameNetworkMessage.ReadIntFromPacket(new CompressionInfo.Integer(0, 4096, true), ref result);

            ItemIndexes = new List<int>(count);
            NewStocks = new List<int>(count);
            TaxRates = new List<float>(count);
            PlayerSetPrices = new List<int>(count);

            for (int i = 0; i < count; i++)
            {
                ItemIndexes.Add(GameNetworkMessage.ReadIntFromPacket(new CompressionInfo.Integer(0, 4096, true), ref result));
                NewStocks.Add(GameNetworkMessage.ReadIntFromPacket(new CompressionInfo.Integer(0, PE_TradeCenter.MAX_STOCK_COUNT, true), ref result));
                TaxRates.Add(GameNetworkMessage.ReadFloatFromPacket(new CompressionInfo.Float(0f, 1f, 0.01f), ref result));
                PlayerSetPrices.Add(GameNetworkMessage.ReadIntFromPacket(new CompressionInfo.Integer(0, 100000, true), ref result));
            }
            MaxStorageCapacity = GameNetworkMessage.ReadIntFromPacket(new CompressionInfo.Integer(0, 500, true), ref result);
            FactionIndex = GameNetworkMessage.ReadIntFromPacket(new CompressionInfo.Integer(-1, 1000, true), ref result);
            return result;
        }

        protected override void OnWrite()
        {
            if (TradingCenter == null)
            {
                InformationManager.DisplayMessage(new InformationMessage(
                    "⚠️ Fehler: Kein gültiges Handelszentrum für Netzwerksync!"));
                return;
            }
            GameNetworkMessage.WriteMissionObjectIdToPacket(TradingCenter.Id);
            GameNetworkMessage.WriteIntToPacket(ItemIndexes.Count,
                new CompressionInfo.Integer(0, 4096, true));

            for (int i = 0; i < ItemIndexes.Count; i++)
            {
                GameNetworkMessage.WriteIntToPacket(ItemIndexes[i],
                    new CompressionInfo.Integer(0, 4096, true));
                GameNetworkMessage.WriteIntToPacket(NewStocks[i],
                    new CompressionInfo.Integer(0, PE_TradeCenter.MAX_STOCK_COUNT, true));
                GameNetworkMessage.WriteFloatToPacket(TaxRates[i],
                    new CompressionInfo.Float(0f, 1f, 0.01f));
                GameNetworkMessage.WriteIntToPacket(PlayerSetPrices[i],
                    new CompressionInfo.Integer(0, 100000, true));
            }
            GameNetworkMessage.WriteIntToPacket(MaxStorageCapacity,
                new CompressionInfo.Integer(0, 500, true));
            GameNetworkMessage.WriteIntToPacket(FactionIndex,
                new CompressionInfo.Integer(-1, 1000, true));
        }
    }

    [DefineGameNetworkMessageTypeForMod(GameNetworkMessageSendType.FromServer)]
    public sealed class UpdateTradingCenterStock : GameNetworkMessage
    {
        public MissionObject TradingCenter { get; private set; }
        public int NewStock { get; private set; }
        public int ItemIndex { get; private set; }
        public float TaxRate { get; private set; }
        public int PlayerSetPrice { get; private set; }
        public int MaxStorageCapacity { get; private set; }
        public int FactionIndex { get; private set; }

        public UpdateTradingCenterStock() { }

        public UpdateTradingCenterStock(
            MissionObject tradingCenter,
            int newStock,
            int itemIndex,
            float taxRate,
            int playerSetPrice,
            int maxStorageCapacity,
            int factionIndex)
        {
            TradingCenter = tradingCenter ?? throw new ArgumentNullException(nameof(tradingCenter), "⚠️ Fehler: Handelszentrum ist null!");
            NewStock = newStock;
            ItemIndex = itemIndex;
            TaxRate = taxRate;
            PlayerSetPrice = playerSetPrice;
            MaxStorageCapacity = maxStorageCapacity;
            FactionIndex = factionIndex;
        }

        protected override MultiplayerMessageFilter OnGetLogFilter()
            => MultiplayerMessageFilter.MissionObjects;

        protected override string OnGetLogFormat() =>
            TradingCenter != null
                ? $"🔄 Handelszentrum-Update: Item {ItemIndex}, Bestand {NewStock}/{MaxStorageCapacity}, Preis {PlayerSetPrice}, Steuer {TaxRate * 100}%, Faction {FactionIndex}"
                : "⚠️ Fehler: TradingCenter ist NULL!";

        protected override bool OnRead()
        {
            bool result = true;
            TradingCenter = Mission.MissionNetworkHelper.GetMissionObjectFromMissionObjectId(
                GameNetworkMessage.ReadMissionObjectIdFromPacket(ref result));
            NewStock = GameNetworkMessage.ReadIntFromPacket(
                new CompressionInfo.Integer(0, PE_TradeCenter.MAX_STOCK_COUNT, true), ref result);
            ItemIndex = GameNetworkMessage.ReadIntFromPacket(
                new CompressionInfo.Integer(0, 4096, true), ref result);
            TaxRate = GameNetworkMessage.ReadFloatFromPacket(
                new CompressionInfo.Float(0f, 1f, 0.01f), ref result);
            PlayerSetPrice = GameNetworkMessage.ReadIntFromPacket(
                new CompressionInfo.Integer(0, 100000, true), ref result);
            MaxStorageCapacity = GameNetworkMessage.ReadIntFromPacket(
                new CompressionInfo.Integer(0, 500, true), ref result);
            FactionIndex = GameNetworkMessage.ReadIntFromPacket(
                new CompressionInfo.Integer(-1, 1000, true), ref result);

            return result;
        }

        protected override void OnWrite()
        {
            if (TradingCenter == null)
            {
                InformationManager.DisplayMessage(new InformationMessage(
                    "⚠️ Fehler: Kein gültiges Handelszentrum für Netzwerksynchronisation!"));
                return;
            }
            GameNetworkMessage.WriteMissionObjectIdToPacket(TradingCenter.Id);
            GameNetworkMessage.WriteIntToPacket(NewStock,
                new CompressionInfo.Integer(0, PE_TradeCenter.MAX_STOCK_COUNT, true));
            GameNetworkMessage.WriteIntToPacket(ItemIndex,
                new CompressionInfo.Integer(0, 4096, true));
            GameNetworkMessage.WriteFloatToPacket(TaxRate,
                new CompressionInfo.Float(0f, 1f, 0.01f));
            GameNetworkMessage.WriteIntToPacket(PlayerSetPrice,
                new CompressionInfo.Integer(0, 100000, true));
            GameNetworkMessage.WriteIntToPacket(MaxStorageCapacity,
                new CompressionInfo.Integer(0, 500, true));
            GameNetworkMessage.WriteIntToPacket(FactionIndex,
                new CompressionInfo.Integer(-1, 1000, true));
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
            Gain = GameNetworkMessage.ReadIntFromPacket(
                new CompressionInfo.Integer(0, Int32.MaxValue, true), ref result);
            return result;
        }
        protected override void OnWrite()
        {
            GameNetworkMessage.WriteIntToPacket(Gain,
                new CompressionInfo.Integer(0, Int32.MaxValue, true));
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
            Lost = GameNetworkMessage.ReadIntFromPacket(
                new CompressionInfo.Integer(0, Int32.MaxValue, true), ref result);
            return result;
        }
        protected override void OnWrite()
        {
            GameNetworkMessage.WriteIntToPacket(Lost,
                new CompressionInfo.Integer(0, Int32.MaxValue, true));
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
            Gold = GameNetworkMessage.ReadIntFromPacket(
                new CompressionInfo.Integer(0, Int32.MaxValue, true), ref result);
            return result;
        }
        protected override void OnWrite()
        {
            GameNetworkMessage.WriteNetworkPeerReferenceToPacket(Player);
            GameNetworkMessage.WriteIntToPacket(Gold,
                new CompressionInfo.Integer(0, Int32.MaxValue, true));
        }
    }

    /// <summary>
    /// Aktualisiert den Inhalt einer Geldtruhe (z. B. Fraktionskasse).
    /// Hier könntest du anknüpfen:
    ///  - Persistenz (z. B. JSON/DB),
    ///  - Fraktionslogik (Nur Lord oder Marshall darf Geld entnehmen)
    ///  - Roadmap: Bauliche Erweiterungen (größere Schatzkammer)
    /// </summary>
    [DefineGameNetworkMessageTypeForMod(GameNetworkMessageSendType.FromServer)]
    public sealed class UpdateMoneychestGold : GameNetworkMessage
    {
        public MissionObject Chest;
        public long Gold;
        public int FactionIndex; // NEU: Optionale Zuordnung für Roadmap / Fraktionen

        public UpdateMoneychestGold() { }

        public UpdateMoneychestGold(MissionObject chest, long gold, int factionIndex)
        {
            Chest = chest;
            Gold = gold;
            FactionIndex = factionIndex;
        }

        protected override MultiplayerMessageFilter OnGetLogFilter() => MultiplayerMessageFilter.None;

        protected override string OnGetLogFormat()
            => $"Server UpdateMoneychestGold -> Chest {Chest?.Id}, Gold: {Gold}, Faction: {FactionIndex}";

        protected override bool OnRead()
        {
            bool result = true;
            Chest = Mission.MissionNetworkHelper.GetMissionObjectFromMissionObjectId(
                GameNetworkMessage.ReadMissionObjectIdFromPacket(ref result));
            Gold = GameNetworkMessage.ReadLongFromPacket(
                new CompressionInfo.LongInteger(0, 9999999999, true), ref result);
            FactionIndex = GameNetworkMessage.ReadIntFromPacket(
                new CompressionInfo.Integer(-1, 1000, true), ref result);

            if (!result || Chest == null)
            {
                InformationManager.DisplayMessage(new InformationMessage(
                    "⚠️ Fehler: Moneychest-Daten unvollständig!"));
                return false;
            }
            // Optional: Prüfen, ob Fraktion im Krieg, ob nur Lord Zugriff hat, etc.
            return true;
        }

        protected override void OnWrite()
        {
            GameNetworkMessage.WriteMissionObjectIdToPacket(Chest?.Id ?? -1);
            GameNetworkMessage.WriteLongToPacket(Gold,
                new CompressionInfo.LongInteger(0, 9999999999, true));
            GameNetworkMessage.WriteIntToPacket(FactionIndex,
                new CompressionInfo.Integer(-1, 1000, true));
        }
    }

    #endregion
}

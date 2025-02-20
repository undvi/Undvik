using System;
using System.Collections.Generic;
using System.IO;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Network.Messages;
using PersistentEmpiresLib.Data;
using PersistentEmpiresLib.Helpers;
using PersistentEmpiresLib.SceneScripts;

namespace PersistentEmpiresLib.NetworkMessages.Client.TradingCenter
{
    #region Bank Actions
    [DefineGameNetworkMessageTypeForMod(GameNetworkMessageSendType.FromClient)]
    public sealed class RequestBankAction : GameNetworkMessage
    {
        public MissionObject Bank { get; private set; }
        public int Amount { get; private set; }
        public bool Deposit { get; private set; }

        public RequestBankAction() { }
        public RequestBankAction(PE_Bank bank, int amount, bool deposit)
        {
            if (bank == null)
                throw new ArgumentNullException(nameof(bank), "Bank darf nicht null sein!");
            Bank = bank;
            Amount = amount;
            Deposit = deposit;
        }
        protected override MultiplayerMessageFilter OnGetLogFilter() => MultiplayerMessageFilter.MissionObjects;
        protected override string OnGetLogFormat() => "Request Bank Action";
        protected override bool OnRead()
        {
            bool result = true;
            Bank = Mission.MissionNetworkHelper.GetMissionObjectFromMissionObjectId(
                GameNetworkMessage.ReadMissionObjectIdFromPacket(ref result)
            );
            Amount = GameNetworkMessage.ReadIntFromPacket(new CompressionInfo.Integer(0, 1000000000, true), ref result);
            Deposit = GameNetworkMessage.ReadBoolFromPacket(ref result);
            return result;
        }
        protected override void OnWrite()
        {
            GameNetworkMessage.WriteMissionObjectIdToPacket(Bank.Id);
            GameNetworkMessage.WriteIntToPacket(Amount, new CompressionInfo.Integer(0, 1000000000, true));
            GameNetworkMessage.WriteBoolToPacket(Deposit);
        }
    }
    #endregion

    #region Trading Market Actions
    // Kaufanfrage für ein Item im Handelsmarkt
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
                throw new ArgumentNullException(nameof(stockpileMarket), "StockpileMarket darf nicht null sein!");
            if (basePrice <= 0)
                throw new ArgumentOutOfRangeException(nameof(basePrice), "Basispreis muss positiv sein!");
            StockpileMarket = stockpileMarket;
            ItemIndex = itemIndex;
            BasePrice = basePrice;
            MarketTaxRate = marketTaxRate;
            AvailableStock = availableStock;
            FinalPrice = CalculateFinalPrice(basePrice, marketTaxRate);
            LogBuyTransaction($"[Buy] Item {ItemIndex}: {basePrice} Gold, Steuer: {marketTaxRate * 100}% → Endpreis: {FinalPrice} Gold, Lager: {AvailableStock}");
        }
        protected override MultiplayerMessageFilter OnGetLogFilter() => MultiplayerMessageFilter.MissionObjects;
        protected override string OnGetLogFormat() =>
            $"[Buy] Anfrage für Item {ItemIndex} | Basispreis: {BasePrice} Gold, Steuer: {MarketTaxRate * 100}% → Endpreis: {FinalPrice} Gold, Lager: {AvailableStock}";
        protected override bool OnRead()
        {
            bool result = true;
            StockpileMarket = Mission.MissionNetworkHelper.GetMissionObjectFromMissionObjectId(
                GameNetworkMessage.ReadMissionObjectIdFromPacket(ref result)
            );
            ItemIndex = GameNetworkMessage.ReadIntFromPacket(new CompressionInfo.Integer(0, 4096, true), ref result);
            BasePrice = GameNetworkMessage.ReadIntFromPacket(new CompressionInfo.Integer(0, 100000, true), ref result);
            MarketTaxRate = GameNetworkMessage.ReadFloatFromPacket(new CompressionInfo.Float(0f, 1f, 0.01f), ref result);
            AvailableStock = GameNetworkMessage.ReadIntFromPacket(new CompressionInfo.Integer(0, 500, true), ref result);
            FinalPrice = CalculateFinalPrice(BasePrice, MarketTaxRate);
            LogBuyTransaction($"[Buy] Netzwerkupdate: Item {ItemIndex}: {BasePrice} Gold, Steuer: {MarketTaxRate * 100}% → Endpreis: {FinalPrice} Gold, Lager: {AvailableStock}");
            return result;
        }
        protected override void OnWrite()
        {
            if (StockpileMarket == null)
            {
                InformationManager.DisplayMessage(new InformationMessage("Kein gültiges Lager gefunden!"));
                return;
            }
            GameNetworkMessage.WriteMissionObjectIdToPacket(StockpileMarket.Id);
            GameNetworkMessage.WriteIntToPacket(ItemIndex, new CompressionInfo.Integer(0, 4096, true));
            GameNetworkMessage.WriteIntToPacket(BasePrice, new CompressionInfo.Integer(0, 100000, true));
            GameNetworkMessage.WriteFloatToPacket(MarketTaxRate, new CompressionInfo.Float(0f, 1f, 0.01f));
            GameNetworkMessage.WriteIntToPacket(AvailableStock, new CompressionInfo.Integer(0, 500, true));
            LogBuyTransaction($"[Buy] Anfrage gesendet: Item {ItemIndex}: {BasePrice} Gold, Steuer: {MarketTaxRate * 100}% → Endpreis: {FinalPrice} Gold, Lager: {AvailableStock}");
        }
        private static int CalculateFinalPrice(int price, float taxRate) => (int)(price + (price * taxRate));
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
                Debug.Print($"Fehler beim Loggen der Kauftransaktion: {ex.Message}");
            }
        }
    }

    // Verkauf eines Items im Handelsmarkt
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
        public long ExpiryTimestamp { get; private set; }

        private static string logFilePath = "market_sell_log.txt";

        public RequestSellItem() { }
        public RequestSellItem(MissionObject stockpileMarket, int itemIndex, int sellPrice, float marketTaxRate, int currentStock, int maxStockLimit, long expiryTimestamp)
        {
            if (stockpileMarket == null)
                throw new ArgumentNullException(nameof(stockpileMarket), "StockpileMarket darf nicht null sein!");
            if (sellPrice <= 0)
                throw new ArgumentOutOfRangeException(nameof(sellPrice), "Verkaufspreis muss positiv sein!");
            if (currentStock >= maxStockLimit)
                throw new InvalidOperationException("Lagerlimit erreicht!");

            StockpileMarket = stockpileMarket;
            ItemIndex = itemIndex;
            SellPrice = sellPrice;
            MarketTaxRate = marketTaxRate;
            FinalPrice = CalculateFinalPrice(sellPrice, marketTaxRate);
            CurrentStock = currentStock;
            MaxStockLimit = maxStockLimit;
            ExpiryTimestamp = expiryTimestamp;
            LogSellTransaction($"[Sell] Item {ItemIndex}: {sellPrice} Gold, Steuer: {MarketTaxRate * 100}% → Endpreis: {FinalPrice} Gold, Lager: {CurrentStock}/{MaxStockLimit}");
        }
        protected override MultiplayerMessageFilter OnGetLogFilter() => MultiplayerMessageFilter.MissionObjects;
        protected override string OnGetLogFormat() =>
            $"[Sell] Anfrage für Item {ItemIndex} | Preis: {SellPrice} Gold, Steuer: {MarketTaxRate * 100}% → Endpreis: {FinalPrice} Gold, Lager: {CurrentStock}/{MaxStockLimit}";
        protected override bool OnRead()
        {
            bool result = true;
            StockpileMarket = Mission.MissionNetworkHelper.GetMissionObjectFromMissionObjectId(
                GameNetworkMessage.ReadMissionObjectIdFromPacket(ref result)
            );
            ItemIndex = GameNetworkMessage.ReadIntFromPacket(new CompressionInfo.Integer(0, 4096, true), ref result);
            SellPrice = GameNetworkMessage.ReadIntFromPacket(new CompressionInfo.Integer(0, 100000, true), ref result);
            MarketTaxRate = GameNetworkMessage.ReadFloatFromPacket(new CompressionInfo.Float(0f, 1f, 0.01f), ref result);
            CurrentStock = GameNetworkMessage.ReadIntFromPacket(new CompressionInfo.Integer(0, 500, true), ref result);
            MaxStockLimit = GameNetworkMessage.ReadIntFromPacket(new CompressionInfo.Integer(0, 500, true), ref result);
            ExpiryTimestamp = GameNetworkMessage.ReadLongFromPacket(ref result);
            FinalPrice = CalculateFinalPrice(SellPrice, MarketTaxRate);
            LogSellTransaction($"[Sell] Netzwerkupdate: Item {ItemIndex}: {SellPrice} Gold, Steuer: {MarketTaxRate * 100}% → Endpreis: {FinalPrice} Gold, Lager: {CurrentStock}/{MaxStockLimit}");
            return result;
        }
        protected override void OnWrite()
        {
            if (StockpileMarket == null)
            {
                InformationManager.DisplayMessage(new InformationMessage("Kein gültiges Lager gefunden!"));
                return;
            }
            GameNetworkMessage.WriteMissionObjectIdToPacket(StockpileMarket.Id);
            GameNetworkMessage.WriteIntToPacket(ItemIndex, new CompressionInfo.Integer(0, 4096, true));
            GameNetworkMessage.WriteIntToPacket(SellPrice, new CompressionInfo.Integer(0, 100000, true));
            GameNetworkMessage.WriteFloatToPacket(MarketTaxRate, new CompressionInfo.Float(0f, 1f, 0.01f));
            GameNetworkMessage.WriteIntToPacket(CurrentStock, new CompressionInfo.Integer(0, 500, true));
            GameNetworkMessage.WriteIntToPacket(MaxStockLimit, new CompressionInfo.Integer(0, 500, true));
            GameNetworkMessage.WriteLongToPacket(ExpiryTimestamp);
            LogSellTransaction($"[Sell] Anfrage gesendet: Item {ItemIndex}: {SellPrice} Gold, Steuer: {MarketTaxRate * 100}% → Endpreis: {FinalPrice} Gold, Lager: {CurrentStock}/{MaxStockLimit}");
        }
        private static int CalculateFinalPrice(int price, float taxRate) => (int)(price - (price * taxRate));
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
                Debug.Print($"Fehler beim Loggen der Verkaufstransaktion: {ex.Message}");
            }
        }
    }
    #endregion

    #region Stockpile Actions
    // Entpackt eine Kiste aus dem Lager
    [DefineGameNetworkMessageTypeForMod(GameNetworkMessageSendType.FromClient)]
    public sealed class StockpileUnpackBox : GameNetworkMessage
    {
        public PE_StockpileMarket StockpileMarket { get; private set; }
        public int SlotId { get; private set; }
        public int StackSize { get; private set; }
        public string ItemType { get; private set; }
        public long ExpireTimestamp { get; private set; }
        public float StorageTaxRate { get; private set; }
        public const int MaxStackSize = 100;
        private static Dictionary<MissionObject, List<StockpileUnpackBox>> ActiveStockpileItems = new Dictionary<MissionObject, List<StockpileUnpackBox>>();

        public StockpileUnpackBox() { }
        public StockpileUnpackBox(int slotId, PE_StockpileMarket stockpileMarket, int stackSize, string itemType, long expireTimestamp, float storageTaxRate)
        {
            if (stockpileMarket == null)
                throw new ArgumentNullException(nameof(stockpileMarket), "StockpileMarket darf nicht null sein!");
            if (stackSize <= 0 || stackSize > MaxStackSize)
                throw new ArgumentOutOfRangeException(nameof(stackSize), $"StackSize muss zwischen 1 und {MaxStackSize} liegen!");
            if (string.IsNullOrEmpty(itemType))
                throw new ArgumentException("ItemType darf nicht leer sein!", nameof(itemType));

            SlotId = slotId;
            StockpileMarket = stockpileMarket;
            StackSize = stackSize;
            ItemType = itemType;
            ExpireTimestamp = expireTimestamp;
            StorageTaxRate = storageTaxRate;

            if (!ActiveStockpileItems.ContainsKey(stockpileMarket))
                ActiveStockpileItems[stockpileMarket] = new List<StockpileUnpackBox>();

            ActiveStockpileItems[stockpileMarket].Add(this);
            LogMarketTransaction($"[Unpack] {StackSize}x {ItemType} eingelagert (Ablauf: {ExpireTimestamp}) | Markt: {StockpileMarket.Name}");
        }
        protected override MultiplayerMessageFilter OnGetLogFilter() => MultiplayerMessageFilter.Administration;
        protected override string OnGetLogFormat() =>
            $"[Unpack] Slot {SlotId} - {StackSize}x {ItemType} (Ablauf: {ExpireTimestamp}) | Steuer: {StorageTaxRate * 100}%";
        protected override bool OnRead()
        {
            bool result = true;
            try
            {
                SlotId = GameNetworkMessage.ReadIntFromPacket(new CompressionInfo.Integer(0, 100, true), ref result);
                StockpileMarket = (PE_StockpileMarket)Mission.MissionNetworkHelper.GetMissionObjectFromMissionObjectId(
                    GameNetworkMessage.ReadMissionObjectIdFromPacket(ref result)
                );
                StackSize = GameNetworkMessage.ReadIntFromPacket(new CompressionInfo.Integer(1, MaxStackSize, true), ref result);
                ItemType = GameNetworkMessage.ReadStringFromPacket(ref result);
                ExpireTimestamp = GameNetworkMessage.ReadLongFromPacket(ref result);
                StorageTaxRate = GameNetworkMessage.ReadFloatFromPacket(new CompressionInfo.Float(0f, 1f, 0.01f), ref result);
                LogMarketTransaction($"[Unpack] Netzwerkupdate: {StackSize}x {ItemType} (Ablauf: {ExpireTimestamp}) | Markt: {StockpileMarket.Name}");
            }
            catch (Exception ex)
            {
                InformationManager.DisplayMessage(new InformationMessage($"Fehler beim Lesen der Stockpile-Daten: {ex.Message}"));
                return false;
            }
            return result;
        }
        protected override void OnWrite()
        {
            if (StockpileMarket == null)
            {
                InformationManager.DisplayMessage(new InformationMessage("Kein gültiges Lager für Netzwerksynchronisation!"));
                return;
            }
            try
            {
                GameNetworkMessage.WriteIntToPacket(SlotId, new CompressionInfo.Integer(0, 100, true));
                GameNetworkMessage.WriteMissionObjectIdToPacket(StockpileMarket.Id);
                GameNetworkMessage.WriteIntToPacket(StackSize, new CompressionInfo.Integer(1, MaxStackSize, true));
                GameNetworkMessage.WriteStringToPacket(ItemType);
                GameNetworkMessage.WriteLongToPacket(ExpireTimestamp);
                GameNetworkMessage.WriteFloatToPacket(StorageTaxRate, new CompressionInfo.Float(0f, 1f, 0.01f));
                LogMarketTransaction($"[Unpack] Daten gesendet: {StackSize}x {ItemType} (Ablauf: {ExpireTimestamp}) | Markt: {StockpileMarket.Name}");
            }
            catch (Exception ex)
            {
                InformationManager.DisplayMessage(new InformationMessage($"Fehler beim Schreiben der Stockpile-Daten: {ex.Message}"));
            }
        }
        private static void LogMarketTransaction(string logEntry)
        {
            string logFilePath = "stockpile_market_log.txt";
            try
            {
                using (StreamWriter writer = new StreamWriter(logFilePath, true))
                {
                    writer.WriteLine($"{DateTime.UtcNow}: {logEntry}");
                }
            }
            catch (Exception ex)
            {
                Debug.Print($"Fehler beim Schreiben ins Markt-Log: {ex.Message}");
            }
        }
    }
    #endregion

    #region Export/Import Actions
    // Anfrage zum Export eines Items aus einem Handelszentrum.
    [DefineGameNetworkMessageTypeForMod(GameNetworkMessageSendType.FromClient)]
    public sealed class RequestExportItem : GameNetworkMessage
    {
        public ItemObject Item { get; private set; }
        public MissionObject ImportExportEntity { get; private set; }
        public RequestExportItem() { }
        public RequestExportItem(ItemObject item, MissionObject importExportEntity)
        {
            Item = item;
            ImportExportEntity = importExportEntity;
        }
        protected override MultiplayerMessageFilter OnGetLogFilter() => MultiplayerMessageFilter.General;
        protected override string OnGetLogFormat() => "Request Export Item";
        protected override bool OnRead()
        {
            bool result = true;
            string itemId = GameNetworkMessage.ReadStringFromPacket(ref result);
            Item = MBObjectManager.Instance.GetObject<ItemObject>(itemId);
            ImportExportEntity = Mission.MissionNetworkHelper.GetMissionObjectFromMissionObjectId(
                GameNetworkMessage.ReadMissionObjectIdFromPacket(ref result)
            );
            return result;
        }
        protected override void OnWrite()
        {
            GameNetworkMessage.WriteStringToPacket(Item.StringId);
            GameNetworkMessage.WriteMissionObjectIdToPacket(ImportExportEntity.Id);
        }
    }

    // Anfrage zum Import eines Items in ein Handelszentrum.
    [DefineGameNetworkMessageTypeForMod(GameNetworkMessageSendType.FromClient)]
    public sealed class RequestImportItem : GameNetworkMessage
    {
        public ItemObject Item { get; private set; }
        public MissionObject ImportExportEntity { get; private set; }
        public RequestImportItem() { }
        public RequestImportItem(ItemObject item, MissionObject importExportEntity)
        {
            Item = item;
            ImportExportEntity = importExportEntity;
        }
        protected override MultiplayerMessageFilter OnGetLogFilter() => MultiplayerMessageFilter.General;
        protected override string OnGetLogFormat() => "Request Import Item";
        protected override bool OnRead()
        {
            bool result = true;
            string itemId = GameNetworkMessage.ReadStringFromPacket(ref result);
            Item = MBObjectManager.Instance.GetObject<ItemObject>(itemId);
            ImportExportEntity = Mission.MissionNetworkHelper.GetMissionObjectFromMissionObjectId(
                GameNetworkMessage.ReadMissionObjectIdFromPacket(ref result)
            );
            return result;
        }
        protected override void OnWrite()
        {
            GameNetworkMessage.WriteStringToPacket(Item.StringId);
            GameNetworkMessage.WriteMissionObjectIdToPacket(ImportExportEntity.Id);
        }
    }
    #endregion

    #region Withdraw/Deposit Actions
    // Anfrage zum Abheben oder Einzahlen im Geldkassensystem.
    [DefineGameNetworkMessageTypeForMod(GameNetworkMessageSendType.FromClient)]
    public sealed class WithdrawDepositMoneychest : GameNetworkMessage
    {
        public int Amount { get; set; }
        public MissionObject MoneyChest { get; set; }
        public bool Withdraw { get; set; }
        public WithdrawDepositMoneychest() { }
        public WithdrawDepositMoneychest(MissionObject mc, int amount, bool withdraw)
        {
            MoneyChest = mc;
            Amount = amount;
            Withdraw = withdraw;
        }
        protected override MultiplayerMessageFilter OnGetLogFilter() => MultiplayerMessageFilter.None;
        protected override string OnGetLogFormat() => "Request Withdraw/Deposit Moneychest";
        protected override bool OnRead()
        {
            bool result = true;
            Amount = GameNetworkMessage.ReadIntFromPacket(new CompressionInfo.Integer(0, 100000000, true), ref result);
            Withdraw = GameNetworkMessage.ReadBoolFromPacket(ref result);
            MoneyChest = Mission.MissionNetworkHelper.GetMissionObjectFromMissionObjectId(GameNetworkMessage.ReadMissionObjectIdFromPacket(ref result));
            return result;
        }
        protected override void OnWrite()
        {
            GameNetworkMessage.WriteIntToPacket(Amount, new CompressionInfo.Integer(0, 100000000, true));
            GameNetworkMessage.WriteBoolToPacket(Withdraw);
            GameNetworkMessage.WriteMissionObjectIdToPacket(MoneyChest.Id);
        }
    }
    #endregion

    #region Misc Trading Requests
    // Anfrage zum Schließen des Handelszentrums.
    [DefineGameNetworkMessageTypeForMod(GameNetworkMessageSendType.FromClient)]
    public sealed class RequestCloseTradingCenter : GameNetworkMessage
    {
        public MissionObject TradingCenter { get; private set; }
        public RequestCloseTradingCenter() { }
        public RequestCloseTradingCenter(MissionObject tradingCenter)
        {
            TradingCenter = tradingCenter;
        }
        protected override MultiplayerMessageFilter OnGetLogFilter() => MultiplayerMessageFilter.MissionObjects;
        protected override string OnGetLogFormat() => "Close Trading Center";
        protected override bool OnRead()
        {
            bool result = true;
            TradingCenter = Mission.MissionNetworkHelper.GetMissionObjectFromMissionObjectId(
                GameNetworkMessage.ReadMissionObjectIdFromPacket(ref result)
            );
            return result;
        }
        protected override void OnWrite() => GameNetworkMessage.WriteMissionObjectIdToPacket(TradingCenter.Id);
    }

    // Anfrage, um einen Schlüssel für die Truhe anzufordern.
    [DefineGameNetworkMessageTypeForMod(GameNetworkMessageSendType.FromClient)]
    public sealed class RequestDoorKey : GameNetworkMessage
    {
        public NetworkCommunicator Player { get; private set; }
        public RequestDoorKey() { }
        public RequestDoorKey(NetworkCommunicator player)
        {
            Player = player;
        }
        protected override MultiplayerMessageFilter OnGetLogFilter() => MultiplayerMessageFilter.Administration;
        protected override string OnGetLogFormat() => "Request Door Key";
        protected override bool OnRead()
        {
            bool result = true;
            Player = GameNetworkMessage.ReadNetworkPeerReferenceFromPacket(ref result);
            return result;
        }
        protected override void OnWrite() => GameNetworkMessage.WriteNetworkPeerReferenceToPacket(Player);
    }
    #endregion

    #region Trading Chat / Voice
    // Anfrage zum Senden eines Batch-Vox-Pakets.
    [DefineGameNetworkMessageTypeForMod(GameNetworkMessageSendType.FromClient)]
    public sealed class SendBatchVoice : GameNetworkMessage
    {
        public byte[] PackedBuffer;
        public int[] BufferLens;
        public SendBatchVoice() { }
        public SendBatchVoice(byte[][] bufferBatch, int[] bufferLens)
        {
            int sum = 0;
            for (int i = 0; i < bufferLens.Length; i++)
                sum += bufferLens[i];
            PackedBuffer = new byte[sum];
            int dstOffset = 0;
            for (int i = 0; i < bufferLens.Length; i++)
            {
                Buffer.BlockCopy(bufferBatch[i], 0, PackedBuffer, dstOffset, bufferLens[i]);
                dstOffset += bufferLens[i];
            }
            BufferLens = bufferLens;
        }
        protected override MultiplayerMessageFilter OnGetLogFilter() => MultiplayerMessageFilter.Mission;
        protected override string OnGetLogFormat() => "SendBatchVoice";
        protected override bool OnRead()
        {
            bool result = true;
            int bufferCount = GameNetworkMessage.ReadIntFromPacket(new CompressionInfo.Integer(0, 10, true), ref result);
            BufferLens = new int[bufferCount];
            int sum = 0;
            for (int i = 0; i < bufferCount; i++)
            {
                BufferLens[i] = GameNetworkMessage.ReadIntFromPacket(new CompressionInfo.Integer(0, 1440, true), ref result);
                sum += BufferLens[i];
            }
            PackedBuffer = new byte[sum];
            GameNetworkMessage.ReadByteArrayFromPacket(PackedBuffer, 0, sum, ref result);
            return result;
        }
        protected override void OnWrite()
        {
            GameNetworkMessage.WriteIntToPacket(BufferLens.Length, new CompressionInfo.Integer(0, 10, true));
            for (int i = 0; i < BufferLens.Length; i++)
                GameNetworkMessage.WriteIntToPacket(BufferLens[i], new CompressionInfo.Integer(0, 1440, true));
            GameNetworkMessage.WriteByteArrayToPacket(PackedBuffer, 0, PackedBuffer.Length);
        }
    }

    // Anfrage, um eine Chat-Nachricht (Shout) zu senden.
    [DefineGameNetworkMessageTypeForMod(GameNetworkMessageSendType.FromClient)]
    public sealed class ShoutMessage : GameNetworkMessage
    {
        public string Text { get; set; }
        public ShoutMessage() { }
        public ShoutMessage(string text)
        {
            Text = text;
        }
        protected override MultiplayerMessageFilter OnGetLogFilter() => MultiplayerMessageFilter.General;
        protected override string OnGetLogFormat() => "ShoutMessage chat";
        protected override bool OnRead()
        {
            bool result = true;
            Text = GameNetworkMessage.ReadStringFromPacket(ref result);
            return result;
        }
        protected override void OnWrite() => GameNetworkMessage.WriteStringToPacket(Text);
    }
    #endregion

    #region Misc Trading Requests

    [DefineGameNetworkMessageTypeForMod(GameNetworkMessageSendType.FromClient)]
    public sealed class WithdrawDepositMoneychest : GameNetworkMessage
    {
        public int Amount { get; private set; }
        public MissionObject MoneyChest { get; private set; }
        public bool Withdraw { get; private set; }

        public WithdrawDepositMoneychest() { }

        public WithdrawDepositMoneychest(MissionObject mc, int amount, bool withdraw)
        {
            MoneyChest = mc;
            Amount = amount;
            Withdraw = withdraw;
        }

        protected override MultiplayerMessageFilter OnGetLogFilter() => MultiplayerMessageFilter.None;
        protected override string OnGetLogFormat() => "Client WithdrawMoneychest";
        protected override bool OnRead()
        {
            bool result = true;
            Amount = GameNetworkMessage.ReadIntFromPacket(new CompressionInfo.Integer(0, 100000000, true), ref result);
            Withdraw = GameNetworkMessage.ReadBoolFromPacket(ref result);
            MoneyChest = Mission.MissionNetworkHelper.GetMissionObjectFromMissionObjectId(GameNetworkMessage.ReadMissionObjectIdFromPacket(ref result));
            return result;
        }
        protected override void OnWrite()
        {
            GameNetworkMessage.WriteIntToPacket(Amount, new CompressionInfo.Integer(0, 100000000, true));
            GameNetworkMessage.WriteBoolToPacket(Withdraw);
            GameNetworkMessage.WriteMissionObjectIdToPacket(MoneyChest.Id);
        }
    }

    #endregion

  
}

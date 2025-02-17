using PersistentEmpiresLib.SceneScripts;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Network.Messages;
using System;
using System.IO;
using System.Collections.Generic;
using TaleWorlds.Library;

namespace PersistentEmpiresLib.NetworkMessages.Client
{
    [DefineGameNetworkMessageTypeForMod(GameNetworkMessageSendType.FromClient)]
    public sealed class StockpileUnpackBox : GameNetworkMessage
    {
        public PE_StockpileMarket StockpileMarket { get; private set; }
        public int SlotId { get; private set; }
        public int StackSize { get; private set; }
        public string ItemType { get; private set; }
        public long ExpireTimestamp { get; private set; }
        public float StorageTaxRate { get; private set; } // Dynamische Steuerberechnung
        public const int MaxStackSize = 100;

        private static Dictionary<MissionObject, List<StockpileUnpackBox>> ActiveStockpileItems = new Dictionary<MissionObject, List<StockpileUnpackBox>>();

        public StockpileUnpackBox() { }

        public StockpileUnpackBox(int slotId, PE_StockpileMarket stockpileMarket, int stackSize, string itemType, long expireTimestamp, float storageTaxRate)
        {
            if (stockpileMarket == null)
                throw new ArgumentNullException(nameof(stockpileMarket), "⚠️ Fehler: StockpileMarket ist null!");

            if (stackSize <= 0 || stackSize > MaxStackSize)
                throw new ArgumentOutOfRangeException(nameof(stackSize), $"⚠️ Fehler: StackSize muss zwischen 1 und {MaxStackSize} liegen!");

            if (string.IsNullOrEmpty(itemType))
                throw new ArgumentException("⚠️ Fehler: ItemType darf nicht leer sein!", nameof(itemType));

            this.SlotId = slotId;
            this.StockpileMarket = stockpileMarket;
            this.StackSize = stackSize;
            this.ItemType = itemType;
            this.ExpireTimestamp = expireTimestamp;
            this.StorageTaxRate = storageTaxRate;

            if (!ActiveStockpileItems.ContainsKey(stockpileMarket))
                ActiveStockpileItems[stockpileMarket] = new List<StockpileUnpackBox>();

            ActiveStockpileItems[stockpileMarket].Add(this);

            LogMarketTransaction($"📦 Item eingelagert: {StackSize}x {ItemType} (Ablauf: {ExpireTimestamp}) | Markt: {StockpileMarket.Name}");
        }

        protected override MultiplayerMessageFilter OnGetLogFilter()
        {
            return MultiplayerMessageFilter.Administration;
        }

        protected override string OnGetLogFormat()
        {
            return $"📦 Lager-Entpackung: Slot {SlotId} - {StackSize}x {ItemType} (Ablauf: {ExpireTimestamp}) | Steuer: {StorageTaxRate * 100}%";
        }

        protected override bool OnRead()
        {
            bool result = true;
            try
            {
                this.SlotId = GameNetworkMessage.ReadIntFromPacket(new CompressionInfo.Integer(0, 100, true), ref result);
                this.StockpileMarket = (PE_StockpileMarket)Mission.MissionNetworkHelper.GetMissionObjectFromMissionObjectId(GameNetworkMessage.ReadMissionObjectIdFromPacket(ref result));
                this.StackSize = GameNetworkMessage.ReadIntFromPacket(new CompressionInfo.Integer(1, MaxStackSize, true), ref result);
                this.ItemType = GameNetworkMessage.ReadStringFromPacket(ref result);
                this.ExpireTimestamp = GameNetworkMessage.ReadLongFromPacket(ref result);
                this.StorageTaxRate = GameNetworkMessage.ReadFloatFromPacket(new CompressionInfo.Float(0f, 1f, 0.01f), ref result);

                if (this.StackSize < 1 || this.StackSize > MaxStackSize)
                {
                    InformationManager.DisplayMessage(new InformationMessage($"⚠️ Fehler: StackSize überschreitet Limit ({MaxStackSize})!"));
                    return false;
                }

                LogMarketTransaction($"📥 Netzwerk-Update erhalten: {StackSize}x {ItemType} (Ablauf: {ExpireTimestamp}) | Markt: {StockpileMarket.Name}");
            }
            catch (Exception ex)
            {
                InformationManager.DisplayMessage(new InformationMessage($"⚠️ Fehler beim Lesen der Stockpile-Daten: {ex.Message}"));
                return false;
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

            try
            {
                GameNetworkMessage.WriteIntToPacket(this.SlotId, new CompressionInfo.Integer(0, 100, true));
                GameNetworkMessage.WriteMissionObjectIdToPacket(this.StockpileMarket.Id);
                GameNetworkMessage.WriteIntToPacket(this.StackSize, new CompressionInfo.Integer(1, MaxStackSize, true));
                GameNetworkMessage.WriteStringToPacket(this.ItemType);
                GameNetworkMessage.WriteLongToPacket(this.ExpireTimestamp);
                GameNetworkMessage.WriteFloatToPacket(this.StorageTaxRate, new CompressionInfo.Float(0f, 1f, 0.01f));

                LogMarketTransaction($"📤 Datenpaket gesendet: {StackSize}x {ItemType} (Ablauf: {ExpireTimestamp}) | Markt: {StockpileMarket.Name}");
            }
            catch (Exception ex)
            {
                InformationManager.DisplayMessage(new InformationMessage($"⚠️ Fehler beim Schreiben der Stockpile-Daten: {ex.Message}"));
            }
        }

        public static void CleanupExpiredItems()
        {
            long now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

            foreach (var market in ActiveStockpileItems.Keys)
            {
                ActiveStockpileItems[market].RemoveAll(item =>
                {
                    if (item.ExpireTimestamp > 0 && item.ExpireTimestamp < now)
                    {
                        LogMarketTransaction($"🗑 Entfernt: {item.StackSize}x {item.ItemType} (verfallen)");
                        return true;
                    }
                    return false;
                });
            }
        }

        /// <summary>
        /// 📜 Speichert Markttransaktionen für spätere Analyse.
        /// </summary>
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
                Debug.Print($"⚠️ Fehler beim Schreiben ins Markt-Log: {ex.Message}");
            }
        }
    }
}

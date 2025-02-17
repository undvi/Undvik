using PersistentEmpiresLib.SceneScripts;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Network.Messages;
using System;
using System.IO;
using TaleWorlds.Library;

namespace PersistentEmpiresLib.NetworkMessages.Server
{
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

            this.Market = market;
            this.Stock = stock;
            this.MarketTaxRate = marketTaxRate;

            LogMarketTransaction($"📜 Pferdemarkt aktualisiert: {Market.Name} | Neuer Bestand: {Stock} | Steuer: {MarketTaxRate * 100}%");
        }

        protected override MultiplayerMessageFilter OnGetLogFilter()
        {
            return MultiplayerMessageFilter.MissionObjects;
        }

        protected override string OnGetLogFormat()
        {
            return this.Market != null
                ? $"🐎 Pferdemarkt-Update: Bestand auf {Stock} gesetzt (Steuer: {MarketTaxRate * 100}%)"
                : "⚠️ Fehler: Market ist NULL!";
        }

        protected override bool OnRead()
        {
            bool result = true;

            try
            {
                this.Market = Mission.MissionNetworkHelper.GetMissionObjectFromMissionObjectId(
                    GameNetworkMessage.ReadMissionObjectIdFromPacket(ref result)
                ) as PE_HorseMarket;

                this.Stock = GameNetworkMessage.ReadIntFromPacket(new CompressionInfo.Integer(0, MaxMarketCapacity, true), ref result);
                this.MarketTaxRate = GameNetworkMessage.ReadFloatFromPacket(new CompressionInfo.Float(0f, 1f, 0.01f), ref result);

                if (this.Market == null)
                {
                    InformationManager.DisplayMessage(new InformationMessage("⚠️ Fehler: Pferdemarkt konnte nicht aus dem Netzwerknachricht gelesen werden!"));
                    return false;
                }

                if (this.MarketTaxRate < 0f || this.MarketTaxRate > 1f)
                {
                    InformationManager.DisplayMessage(new InformationMessage("⚠️ Fehler: Steuerwert außerhalb des gültigen Bereichs!"));
                    return false;
                }

                if (this.Stock < 0 || this.Stock > MaxMarketCapacity)
                {
                    InformationManager.DisplayMessage(new InformationMessage($"⚠️ Fehler: Lagerbestand überschreitet Limit ({MaxMarketCapacity})!"));
                    return false;
                }

                LogMarketTransaction($"📥 Netzwerk-Update erhalten: {Market.Name} | Bestand: {Stock} | Steuer: {MarketTaxRate * 100}%");
            }
            catch (Exception ex)
            {
                InformationManager.DisplayMessage(new InformationMessage($"⚠️ Fehler beim Lesen des Pferdemarkt-Datenpakets: {ex.Message}"));
                return false;
            }

            return result;
        }

        protected override void OnWrite()
        {
            if (this.Market == null)
            {
                InformationManager.DisplayMessage(new InformationMessage("⚠️ Fehler: Kein gültiger Pferdemarkt für Netzwerksynchronisation!"));
                return;
            }

            try
            {
                GameNetworkMessage.WriteMissionObjectIdToPacket(this.Market.Id);
                GameNetworkMessage.WriteIntToPacket(this.Stock, new CompressionInfo.Integer(0, MaxMarketCapacity, true));
                GameNetworkMessage.WriteFloatToPacket(this.MarketTaxRate, new CompressionInfo.Float(0f, 1f, 0.01f));

                LogMarketTransaction($"📤 Datenpaket gesendet: {Market.Name} | Bestand: {Stock} | Steuer: {MarketTaxRate * 100}%");
            }
            catch (Exception ex)
            {
                InformationManager.DisplayMessage(new InformationMessage($"⚠️ Fehler beim Schreiben des Pferdemarkt-Datenpakets: {ex.Message}"));
            }
        }

        /// <summary>
        /// 📜 Speichert Markttransaktionen für spätere Analyse.
        /// </summary>
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
}

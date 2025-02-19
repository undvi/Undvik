using PersistentEmpiresLib.Data;
using PersistentEmpiresLib.Helpers;
using PersistentEmpiresLib.SceneScripts;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Network.Messages;

namespace PersistentEmpiresLib.NetworkMessages.Server
{
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
                throw new System.ArgumentNullException(nameof(stockpileMarketEntity), "⚠️ Fehler: StockpileMarketEntity ist null!");
            if (playerInventory == null)
                throw new System.ArgumentNullException(nameof(playerInventory), "⚠️ Fehler: Spielerinventar ist null!");
            if (marketTaxRate < 0f || marketTaxRate > 1f)
                throw new System.ArgumentOutOfRangeException(nameof(marketTaxRate), "⚠️ Fehler: Steuer muss zwischen 0% und 100% liegen!");

            this.StockpileMarketEntity = stockpileMarketEntity;
            this.PlayerInventory = playerInventory;
            this.MarketTaxRate = marketTaxRate;
        }

        protected override MultiplayerMessageFilter OnGetLogFilter()
        {
            return MultiplayerMessageFilter.MissionObjects;
        }

        protected override string OnGetLogFormat()
        {
            return this.StockpileMarketEntity != null
                ? $"📦 Lager geöffnet für {StockpileMarketEntity.Name}, Steuer: {MarketTaxRate * 100}%"
                : "⚠️ Fehler: StockpileMarketEntity ist NULL!";
        }

        protected override bool OnRead()
        {
            bool result = true;

            try
            {
                this.StockpileMarketEntity = Mission.MissionNetworkHelper.GetMissionObjectFromMissionObjectId(
                    GameNetworkMessage.ReadMissionObjectIdFromPacket(ref result)
                );

                this.PlayerInventory = PENetworkModule.ReadInventoryPlayer(ref result);
                this.MarketTaxRate = GameNetworkMessage.ReadFloatFromPacket(new CompressionInfo.Float(0f, 1f, 0.01f), ref result);
            }
            catch (System.Exception ex)
            {
                InformationManager.DisplayMessage(new InformationMessage($"⚠️ Fehler beim Lesen des Stockpile-Datenpakets: {ex.Message}"));
                return false;
            }

            return result;
        }

        protected override void OnWrite()
        {
            if (this.StockpileMarketEntity == null)
            {
                InformationManager.DisplayMessage(new InformationMessage("⚠️ Fehler: Kein gültiges Lager für Netzwerksynchronisation!"));
                return;
            }

            try
            {
                GameNetworkMessage.WriteMissionObjectIdToPacket(this.StockpileMarketEntity.Id);
                PENetworkModule.WriteInventoryPlayer(this.PlayerInventory);
                GameNetworkMessage.WriteFloatToPacket(this.MarketTaxRate, new CompressionInfo.Float(0f, 1f, 0.01f));
            }
            catch (System.Exception ex)
            {
                InformationManager.DisplayMessage(new InformationMessage($"⚠️ Fehler beim Schreiben des Stockpile-Datenpakets: {ex.Message}"));
            }
        }
    }
}

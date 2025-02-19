using PersistentEmpiresLib.SceneScripts;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Network.Messages;

namespace PersistentEmpiresLib.NetworkMessages.Server
{
    [DefineGameNetworkMessageTypeForMod(GameNetworkMessageSendType.FromServer)]
    public sealed class UpdateTradingCenterStock : GameNetworkMessage
    {
        public MissionObject TradingCenter { get; private set; }
        public int NewStock { get; private set; }
        public int ItemIndex { get; private set; }
        public float TaxRate { get; private set; }
        public int PlayerSetPrice { get; private set; } // Spieler kann eigenen Preis festlegen
        public int MaxStorageCapacity { get; private set; } // Maximale Lagermenge für diesen Markt

        public UpdateTradingCenterStock() { }

        public UpdateTradingCenterStock(MissionObject tradingCenter, int newStock, int itemIndex, float taxRate, int playerSetPrice, int maxStorageCapacity)
        {
            this.TradingCenter = tradingCenter ?? throw new System.ArgumentNullException(nameof(tradingCenter), "⚠️ Fehler: Handelszentrum ist null!");
            this.ItemIndex = itemIndex;
            this.NewStock = newStock;
            this.TaxRate = taxRate;
            this.PlayerSetPrice = playerSetPrice;
            this.MaxStorageCapacity = maxStorageCapacity;
        }

        protected override MultiplayerMessageFilter OnGetLogFilter()
        {
            return MultiplayerMessageFilter.MissionObjects;
        }

        protected override string OnGetLogFormat()
        {
            return this.TradingCenter != null
                ? $"🔄 Handelszentrum-Update: Item {ItemIndex} → Bestand: {NewStock}/{MaxStorageCapacity}, Preis: {PlayerSetPrice} Gold, Steuer: {TaxRate * 100}%"
                : "⚠️ Fehler: TradingCenter ist NULL!";
        }

        protected override bool OnRead()
        {
            bool result = true;
            this.TradingCenter = Mission.MissionNetworkHelper.GetMissionObjectFromMissionObjectId(
                GameNetworkMessage.ReadMissionObjectIdFromPacket(ref result)
            );
            this.NewStock = GameNetworkMessage.ReadIntFromPacket(new CompressionInfo.Integer(0, PE_TradeCenter.MAX_STOCK_COUNT, true), ref result);
            this.ItemIndex = GameNetworkMessage.ReadIntFromPacket(new CompressionInfo.Integer(0, 4096, true), ref result);
            this.TaxRate = GameNetworkMessage.ReadFloatFromPacket(new CompressionInfo.Float(0f, 1f, 0.01f), ref result);
            this.PlayerSetPrice = GameNetworkMessage.ReadIntFromPacket(new CompressionInfo.Integer(0, 100000, true), ref result); // Maximal 100000 Gold pro Item
            this.MaxStorageCapacity = GameNetworkMessage.ReadIntFromPacket(new CompressionInfo.Integer(0, 500, true), ref result); // Maximale Lagergröße 500 Items

            return result;
        }

        protected override void OnWrite()
        {
            if (this.TradingCenter == null)
            {
                InformationManager.DisplayMessage(new InformationMessage("⚠️ Fehler: Kein gültiges Handelszentrum für Netzwerksynchronisation!"));
                return;
            }

            GameNetworkMessage.WriteMissionObjectIdToPacket(this.TradingCenter.Id);
            GameNetworkMessage.WriteIntToPacket(this.NewStock, new CompressionInfo.Integer(0, PE_TradeCenter.MAX_STOCK_COUNT, true));
            GameNetworkMessage.WriteIntToPacket(this.ItemIndex, new CompressionInfo.Integer(0, 4096, true));
            GameNetworkMessage.WriteFloatToPacket(this.TaxRate, new CompressionInfo.Float(0f, 1f, 0.01f));
            GameNetworkMessage.WriteIntToPacket(this.PlayerSetPrice, new CompressionInfo.Integer(0, 100000, true));
            GameNetworkMessage.WriteIntToPacket(this.MaxStorageCapacity, new CompressionInfo.Integer(0, 500, true));
        }
    }
}

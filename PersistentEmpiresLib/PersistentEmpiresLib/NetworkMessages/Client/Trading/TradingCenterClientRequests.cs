using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Network.Messages;

namespace PersistentEmpiresLib.NetworkMessages.Client.TradingCenter
{
	// Anfrage zum Kauf eines Items im Handelszentrum
	[DefineGameNetworkMessageTypeForMod(GameNetworkMessageSendType.FromClient)]
	public sealed class RequestTradingBuyItem : GameNetworkMessage
	{
		public MissionObject TradingCenter { get; private set; }
		public int ItemIndex { get; private set; }

		public RequestTradingBuyItem() { }
		public RequestTradingBuyItem(MissionObject tradingCenter, int itemIndex)
		{
			TradingCenter = tradingCenter;
			ItemIndex = itemIndex;
		}

		protected override MultiplayerMessageFilter OnGetLogFilter() => MultiplayerMessageFilter.MissionObjects;
		protected override string OnGetLogFormat() => "Trading Center Buy Request";
		protected override bool OnRead()
		{
			bool result = true;
			TradingCenter = Mission.MissionNetworkHelper.GetMissionObjectFromMissionObjectId(
				GameNetworkMessage.ReadMissionObjectIdFromPacket(ref result)
			);
			ItemIndex = GameNetworkMessage.ReadIntFromPacket(new CompressionInfo.Integer(0, 4096, true), ref result);
			return result;
		}
		protected override void OnWrite()
		{
			GameNetworkMessage.WriteMissionObjectIdToPacket(TradingCenter.Id);
			GameNetworkMessage.WriteIntToPacket(ItemIndex, new CompressionInfo.Integer(0, 4096, true));
		}
	}

	// Anfrage zu den Preisen im Handelszentrum
	[DefineGameNetworkMessageTypeForMod(GameNetworkMessageSendType.FromClient)]
	public sealed class RequestTradingPrices : GameNetworkMessage
	{
		public MissionObject TradingCenter { get; private set; }
		public int ItemIndex { get; private set; }

		public RequestTradingPrices() { }
		public RequestTradingPrices(MissionObject tradingCenter, int itemIndex)
		{
			TradingCenter = tradingCenter;
			ItemIndex = itemIndex;
		}

		protected override MultiplayerMessageFilter OnGetLogFilter() => MultiplayerMessageFilter.MissionObjects;
		protected override string OnGetLogFormat() => "Request Trading Prices";
		protected override bool OnRead()
		{
			bool result = true;
			TradingCenter = Mission.MissionNetworkHelper.GetMissionObjectFromMissionObjectId(
				GameNetworkMessage.ReadMissionObjectIdFromPacket(ref result)
			);
			ItemIndex = GameNetworkMessage.ReadIntFromPacket(new CompressionInfo.Integer(0, 4096, true), ref result);
			return result;
		}
		protected override void OnWrite()
		{
			GameNetworkMessage.WriteMissionObjectIdToPacket(TradingCenter.Id);
			GameNetworkMessage.WriteIntToPacket(ItemIndex, new CompressionInfo.Integer(0, 4096, true));
		}
	}

	// Anfrage zum Verkauf eines Items im Handelszentrum
	[DefineGameNetworkMessageTypeForMod(GameNetworkMessageSendType.FromClient)]
	public sealed class RequestTradingSellItem : GameNetworkMessage
	{
		public MissionObject TradingCenter { get; private set; }
		public int ItemIndex { get; private set; }

		public RequestTradingSellItem() { }
		public RequestTradingSellItem(MissionObject tradingCenter, int itemIndex)
		{
			TradingCenter = tradingCenter;
			ItemIndex = itemIndex;
		}

		protected override MultiplayerMessageFilter OnGetLogFilter() => MultiplayerMessageFilter.MissionObjects;
		protected override string OnGetLogFormat() => "Trading Center Sell Request";
		protected override bool OnRead()
		{
			bool result = true;
			TradingCenter = Mission.MissionNetworkHelper.GetMissionObjectFromMissionObjectId(
				GameNetworkMessage.ReadMissionObjectIdFromPacket(ref result)
			);
			ItemIndex = GameNetworkMessage.ReadIntFromPacket(new CompressionInfo.Integer(0, 4096, true), ref result);
			return result;
		}
		protected override void OnWrite()
		{
			GameNetworkMessage.WriteMissionObjectIdToPacket(TradingCenter.Id);
			GameNetworkMessage.WriteIntToPacket(ItemIndex, new CompressionInfo.Integer(0, 4096, true));
		}
	}
}

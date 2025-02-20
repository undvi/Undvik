using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Network.Messages;

namespace PersistentEmpiresLib.NetworkMessages.Client.Inventory
{
	// Anfrage, um ein Item aus dem Inventar zu droppen.
	[DefineGameNetworkMessageTypeForMod(GameNetworkMessageSendType.FromClient)]
	public sealed class RequestDropItemFromInventory : GameNetworkMessage
	{
		public string DropTag { get; set; }
		public RequestDropItemFromInventory() { }
		public RequestDropItemFromInventory(string tag)
		{
			DropTag = tag;
		}
		protected override MultiplayerMessageFilter OnGetLogFilter() => MultiplayerMessageFilter.MissionObjects;
		protected override string OnGetLogFormat() => "Request drop item";
		protected override bool OnRead()
		{
			bool result = true;
			DropTag = GameNetworkMessage.ReadStringFromPacket(ref result);
			return result;
		}
		protected override void OnWrite() =>
			GameNetworkMessage.WriteStringToPacket(DropTag);
	}

	// Anfrage, um Geld aus dem Inventar zu droppen.
	[DefineGameNetworkMessageTypeForMod(GameNetworkMessageSendType.FromClient)]
	public sealed class RequestDropMoney : GameNetworkMessage
	{
		public int Amount { get; set; }
		public RequestDropMoney() { }
		public RequestDropMoney(int amount)
		{
			Amount = amount;
		}
		protected override MultiplayerMessageFilter OnGetLogFilter() => MultiplayerMessageFilter.MissionObjects;
		protected override string OnGetLogFormat() => "Drop money";
		protected override bool OnRead()
		{
			bool result = true;
			Amount = GameNetworkMessage.ReadIntFromPacket(new CompressionInfo.Integer(0, int.MaxValue, true), ref result);
			return result;
		}
		protected override void OnWrite() =>
			GameNetworkMessage.WriteIntToPacket(Amount, new CompressionInfo.Integer(0, int.MaxValue, true));
	}

	// Anfrage für einen Inventartransfer.
	[DefineGameNetworkMessageTypeForMod(GameNetworkMessageSendType.FromClient)]
	public sealed class RequestInventoryTransfer : GameNetworkMessage
	{
		public string DraggedTag { get; set; }
		public string DroppedTag { get; set; }
		public RequestInventoryTransfer() { }
		public RequestInventoryTransfer(string draggedTag, string droppedTag)
		{
			DraggedTag = draggedTag;
			DroppedTag = droppedTag;
		}
		protected override MultiplayerMessageFilter OnGetLogFilter() => MultiplayerMessageFilter.MissionObjects;
		protected override string OnGetLogFormat() => "Player request an inventory transfer";
		protected override bool OnRead()
		{
			bool result = true;
			DraggedTag = GameNetworkMessage.ReadStringFromPacket(ref result);
			DroppedTag = GameNetworkMessage.ReadStringFromPacket(ref result);
			return result;
		}
		protected override void OnWrite()
		{
			GameNetworkMessage.WriteStringToPacket(DraggedTag);
			GameNetworkMessage.WriteStringToPacket(DroppedTag);
		}
	}

	// Anfrage, um das Inventar zu öffnen.
	[DefineGameNetworkMessageTypeForMod(GameNetworkMessageSendType.FromClient)]
	public sealed class RequestOpenInventory : GameNetworkMessage
	{
		public string InventoryId { get; set; }
		public RequestOpenInventory() { }
		public RequestOpenInventory(string inventoryId)
		{
			InventoryId = inventoryId;
		}
		protected override MultiplayerMessageFilter OnGetLogFilter() => MultiplayerMessageFilter.MissionObjects;
		protected override string OnGetLogFormat() => "Player request an inventory open";
		protected override bool OnRead()
		{
			bool result = true;
			InventoryId = GameNetworkMessage.ReadStringFromPacket(ref result);
			return result;
		}
		protected override void OnWrite() =>
			GameNetworkMessage.WriteStringToPacket(InventoryId);
	}

	// Anfrage, um den Item-Bag (z. B. bei besonderen Items) zu enthüllen.
	[DefineGameNetworkMessageTypeForMod(GameNetworkMessageSendType.FromClient)]
	public sealed class RequestRevealItemBag : GameNetworkMessage
	{
		public RequestRevealItemBag() { }
		protected override MultiplayerMessageFilter OnGetLogFilter() => MultiplayerMessageFilter.General;
		protected override string OnGetLogFormat() => "Reveal Item Bag Requested";
		protected override bool OnRead() => true;
		protected override void OnWrite() { }
	}
}

﻿using System;
using System.Collections.Generic;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Network.Messages;
using TaleWorlds.ObjectSystem;
using PersistentEmpiresLib.Data;
using PersistentEmpiresLib.Helpers;

namespace PersistentEmpiresLib.NetworkMessages.Server.InventoryAndCrafting
{
    #region Crafting Messages

    [DefineGameNetworkMessageTypeForMod(GameNetworkMessageSendType.FromServer)]
    public sealed class CraftingCompleted : GameNetworkMessage
    {
        public int PlayerID { get; private set; }
        public string ItemID { get; private set; }

        public CraftingCompleted() { }

        public CraftingCompleted(int playerId, string itemId)
        {
            this.PlayerID = playerId;
            this.ItemID = itemId;
        }

        protected override MultiplayerMessageFilter OnGetLogFilter() => MultiplayerMessageFilter.MissionObjects;
        protected override string OnGetLogFormat() => $"✅ Crafting abgeschlossen: Spieler {PlayerID} hat {ItemID} hergestellt.";
        protected override bool OnRead()
        {
            bool result = true;
            this.PlayerID = GameNetworkMessage.ReadIntFromPacket(new CompressionInfo.Integer(0, 10000, true), ref result);
            this.ItemID = GameNetworkMessage.ReadStringFromPacket(ref result);
            return result;
        }
        protected override void OnWrite()
        {
            GameNetworkMessage.WriteIntToPacket(this.PlayerID, new CompressionInfo.Integer(0, 10000, true));
            GameNetworkMessage.WriteStringToPacket(this.ItemID);
        }
    }

    [DefineGameNetworkMessageTypeForMod(GameNetworkMessageSendType.FromServer)]
    public sealed class CraftingStarted : GameNetworkMessage
    {
        public int PlayerID { get; private set; }
        public int CraftingStationID { get; private set; }
        public int CraftIndex { get; private set; }

        public CraftingStarted() { }

        public CraftingStarted(int playerId, int craftingStationId, int craftIndex)
        {
            this.PlayerID = playerId;
            this.CraftingStationID = craftingStationId;
            this.CraftIndex = craftIndex;
        }

        protected override MultiplayerMessageFilter OnGetLogFilter() => MultiplayerMessageFilter.MissionObjects;
        protected override string OnGetLogFormat() => $"🛠️ Crafting gestartet: Station {CraftingStationID}, Spieler {PlayerID}, Index {CraftIndex}";
        protected override bool OnRead()
        {
            bool result = true;
            this.PlayerID = GameNetworkMessage.ReadIntFromPacket(new CompressionInfo.Integer(0, 10000, true), ref result);
            this.CraftingStationID = GameNetworkMessage.ReadIntFromPacket(new CompressionInfo.Integer(0, 10000, true), ref result);
            this.CraftIndex = GameNetworkMessage.ReadIntFromPacket(new CompressionInfo.Integer(0, 1024, true), ref result);
            return result;
        }
        protected override void OnWrite()
        {
            GameNetworkMessage.WriteIntToPacket(this.PlayerID, new CompressionInfo.Integer(0, 10000, true));
            GameNetworkMessage.WriteIntToPacket(this.CraftingStationID, new CompressionInfo.Integer(0, 10000, true));
            GameNetworkMessage.WriteIntToPacket(this.CraftIndex, new CompressionInfo.Integer(0, 1024, true));
        }
    }

    #endregion

    #region Inventory Transfer & Inventory Management

    [DefineGameNetworkMessageTypeForMod(GameNetworkMessageSendType.FromServer)]
    public sealed class ExecuteInventoryTransfer : GameNetworkMessage
    {
        public string DraggedSlot { get; set; }
        public string DroppedSlot { get; set; }
        public ItemObject DraggedSlotItem { get; set; }
        public ItemObject DroppedSlotItem { get; set; }
        public int DraggedSlotCount { get; set; }
        public int DroppedSlotCount { get; set; }

        public ExecuteInventoryTransfer() { }

        public ExecuteInventoryTransfer(string draggedSlot, string droppedSlot, ItemObject draggedSlotItem, ItemObject droppedSlotItem, int draggedSlotCount, int droppedSlotCount)
        {
            this.DraggedSlot = draggedSlot;
            this.DroppedSlot = droppedSlot;
            this.DraggedSlotItem = draggedSlotItem;
            this.DroppedSlotItem = droppedSlotItem;
            this.DraggedSlotCount = draggedSlotCount;
            this.DroppedSlotCount = droppedSlotCount;
        }

        protected override MultiplayerMessageFilter OnGetLogFilter() => MultiplayerMessageFilter.Equipment;
        protected override string OnGetLogFormat() => "Equipment execute inventory";
        protected override bool OnRead()
        {
            bool result = true;
            this.DraggedSlot = GameNetworkMessage.ReadStringFromPacket(ref result);
            this.DroppedSlot = GameNetworkMessage.ReadStringFromPacket(ref result);
            string draggedSlotItemString = GameNetworkMessage.ReadStringFromPacket(ref result);
            string droppedSlotItemString = GameNetworkMessage.ReadStringFromPacket(ref result);
            this.DraggedSlotItem = !string.IsNullOrEmpty(draggedSlotItemString) ? MBObjectManager.Instance.GetObject<ItemObject>(draggedSlotItemString) : null;
            this.DroppedSlotItem = !string.IsNullOrEmpty(droppedSlotItemString) ? MBObjectManager.Instance.GetObject<ItemObject>(droppedSlotItemString) : null;
            this.DraggedSlotCount = GameNetworkMessage.ReadIntFromPacket(new CompressionInfo.Integer(0, 256, true), ref result);
            this.DroppedSlotCount = GameNetworkMessage.ReadIntFromPacket(new CompressionInfo.Integer(0, 256, true), ref result);
            return result;
        }
        protected override void OnWrite()
        {
            GameNetworkMessage.WriteStringToPacket(this.DraggedSlot);
            GameNetworkMessage.WriteStringToPacket(this.DroppedSlot);
            GameNetworkMessage.WriteStringToPacket(this.DraggedSlotItem != null ? this.DraggedSlotItem.StringId : "");
            GameNetworkMessage.WriteStringToPacket(this.DroppedSlotItem != null ? this.DroppedSlotItem.StringId : "");
            GameNetworkMessage.WriteIntToPacket(this.DraggedSlotCount, new CompressionInfo.Integer(0, 256, true));
            GameNetworkMessage.WriteIntToPacket(this.DroppedSlotCount, new CompressionInfo.Integer(0, 256, true));
        }
    }

    [DefineGameNetworkMessageTypeForMod(GameNetworkMessageSendType.FromServer)]
    public sealed class ForceCloseInventory : GameNetworkMessage
    {
        public ForceCloseInventory() { }
        protected override MultiplayerMessageFilter OnGetLogFilter() => MultiplayerMessageFilter.Equipment;
        protected override string OnGetLogFormat() => "Force Close Inventory";
        protected override bool OnRead() => true;
        protected override void OnWrite() { }
    }

    [DefineGameNetworkMessageTypeForMod(GameNetworkMessageSendType.FromServer)]
    public sealed class OpenCraftingStation : GameNetworkMessage
    {
        public int StationID { get; private set; }
        public Inventory PlayerInventory { get; private set; }

        public OpenCraftingStation() { }

        public OpenCraftingStation(int stationId, Inventory playerInventory)
        {
            this.StationID = stationId;
            this.PlayerInventory = playerInventory ?? throw new ArgumentNullException("❌ OpenCraftingStation: Ungültige Inventardaten übergeben!");
        }

        protected override MultiplayerMessageFilter OnGetLogFilter() => MultiplayerMessageFilter.MissionObjects;
        protected override string OnGetLogFormat() => $"📌 OpenCraftingStation: Spieler öffnet Schmiede {StationID}";
        protected override bool OnRead()
        {
            bool result = true;
            this.StationID = GameNetworkMessage.ReadIntFromPacket(new CompressionInfo.Integer(0, 10000, true), ref result);
            this.PlayerInventory = PENetworkModule.ReadInventoryPlayer(ref result);
            return result;
        }
        protected override void OnWrite()
        {
            GameNetworkMessage.WriteIntToPacket(this.StationID, new CompressionInfo.Integer(0, 10000, true));
            PENetworkModule.WriteInventoryPlayer(this.PlayerInventory);
        }
    }

    [DefineGameNetworkMessageTypeForMod(GameNetworkMessageSendType.FromServer)]
    public sealed class OpenImportExport : GameNetworkMessage
    {
        public MissionObject ImportExportEntity { get; private set; }
        public Inventory PlayerInventory { get; private set; }

        public OpenImportExport() { }
        public OpenImportExport(MissionObject importExportEntity, Inventory playerInventory)
        {
            this.ImportExportEntity = importExportEntity;
            this.PlayerInventory = playerInventory;
        }
        protected override MultiplayerMessageFilter OnGetLogFilter() => MultiplayerMessageFilter.Administration;
        protected override string OnGetLogFormat() => "Open Import Export";
        protected override bool OnRead()
        {
            bool result = true;
            this.ImportExportEntity = Mission.MissionNetworkHelper.GetMissionObjectFromMissionObjectId(GameNetworkMessage.ReadMissionObjectIdFromPacket(ref result));
            this.PlayerInventory = PENetworkModule.ReadInventoryPlayer(ref result);
            return result;
        }
        protected override void OnWrite()
        {
            GameNetworkMessage.WriteMissionObjectIdToPacket(this.ImportExportEntity.Id);
            PENetworkModule.WriteInventoryPlayer(this.PlayerInventory);
        }
    }

    [DefineGameNetworkMessageTypeForMod(GameNetworkMessageSendType.FromServer)]
    public sealed class OpenInventory : GameNetworkMessage
    {
        public string InventoryId { get; set; }
        public Inventory PlayerInventory;
        public Inventory RequestedInventory;

        public OpenInventory() { }
        public OpenInventory(string inventoryId, Inventory playerInventory, Inventory requestedInventory)
        {
            this.InventoryId = inventoryId;
            this.PlayerInventory = playerInventory;
            this.RequestedInventory = requestedInventory;
        }
        protected override MultiplayerMessageFilter OnGetLogFilter() => MultiplayerMessageFilter.Equipment;
        protected override string OnGetLogFormat() => "Open inventory";
        protected override bool OnRead()
        {
            bool result = true;
            this.InventoryId = GameNetworkMessage.ReadStringFromPacket(ref result);
            this.PlayerInventory = PENetworkModule.ReadInventoryPlayer(ref result);
            if (!string.IsNullOrEmpty(this.InventoryId))
            {
                this.RequestedInventory = PENetworkModule.ReadCustomInventory(this.InventoryId, ref result);
            }
            return result;
        }
        protected override void OnWrite()
        {
            GameNetworkMessage.WriteStringToPacket(this.InventoryId);
            PENetworkModule.WriteInventoryPlayer(this.PlayerInventory);
            if (!string.IsNullOrEmpty(this.InventoryId))
            {
                PENetworkModule.WriteCustomInventory(this.RequestedInventory);
            }
        }
    }

    #endregion
}

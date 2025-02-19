using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Network.Messages;

namespace PersistentEmpiresLib.NetworkMessages.Client.AdminAndMisc
{
    #region Inventory & User Info

    // Sendet einen Befehl, um einen Teil eines Inventars zu splitten.
    [DefineGameNetworkMessageTypeForMod(GameNetworkMessageSendType.FromClient)]
    public sealed class InventorySplitItem : GameNetworkMessage
    {
        public string ClickedTag { get; set; }
        public InventorySplitItem() { }
        public InventorySplitItem(string clickedTag)
        {
            ClickedTag = clickedTag;
        }
        protected override MultiplayerMessageFilter OnGetLogFilter() => MultiplayerMessageFilter.MissionObjects;
        protected override string OnGetLogFormat() => "Inventory Split Request";
        protected override bool OnRead()
        {
            bool result = false;
            ClickedTag = GameNetworkMessage.ReadStringFromPacket(ref result);
            return result;
        }
        protected override void OnWrite() =>
            GameNetworkMessage.WriteStringToPacket(ClickedTag);
    }

    // Lokale Chatnachricht vom Client.
    [DefineGameNetworkMessageTypeForMod(GameNetworkMessageSendType.FromClient)]
    public sealed class LocalMessage : GameNetworkMessage
    {
        public string Text { get; set; }
        public LocalMessage() { }
        public LocalMessage(string text)
        {
            Text = text;
        }
        protected override MultiplayerMessageFilter OnGetLogFilter() => MultiplayerMessageFilter.General;
        protected override string OnGetLogFormat() => "Local Chat Message";
        protected override bool OnRead()
        {
            bool result = true;
            Text = GameNetworkMessage.ReadStringFromPacket(ref result);
            return result;
        }
        protected override void OnWrite() =>
            GameNetworkMessage.WriteStringToPacket(Text);
    }

    // Sendet die Discord-ID des Spielers.
    [DefineGameNetworkMessageTypeForMod(GameNetworkMessageSendType.FromClient)]
    public sealed class MyDiscordId : GameNetworkMessage
    {
        public string Id { get; set; }
        public MyDiscordId() { }
        public MyDiscordId(string id)
        {
            Id = id;
        }
        protected override MultiplayerMessageFilter OnGetLogFilter() => MultiplayerMessageFilter.General;
        protected override string OnGetLogFormat() => "Discord ID Sent";
        protected override bool OnRead()
        {
            bool result = true;
            Id = GameNetworkMessage.ReadStringFromPacket(ref result);
            return result;
        }
        protected override void OnWrite() =>
            GameNetworkMessage.WriteStringToPacket(Id);
    }

    // Anfrage zum Betreten der Akademie (z. B. für spezielle Features).
    [DefineGameNetworkMessageTypeForMod(GameNetworkMessageSendType.FromClient)]
    public sealed class PEAcademyEnter : GameNetworkMessage
    {
        public int PlayerID { get; private set; }
        public PEAcademyEnter() { }
        public PEAcademyEnter(int playerId)
        {
            PlayerID = playerId;
        }
        protected override MultiplayerMessageFilter OnGetLogFilter() => MultiplayerMessageFilter.MissionObjects;
        protected override string OnGetLogFormat() => $"Player {PlayerID} requests to enter the Academy";
        protected override bool OnRead()
        {
            bool result = true;
            PlayerID = GameNetworkMessage.ReadIntFromPacket(new CompressionInfo.Integer(0, 10000, true), ref result);
            return result;
        }
        protected override void OnWrite() =>
            GameNetworkMessage.WriteIntToPacket(PlayerID, new CompressionInfo.Integer(0, 10000, true));
    }

    #endregion

    #region Administrative Nachrichten

    // Chatnachricht speziell für Administratoren.
    [DefineGameNetworkMessageTypeForMod(GameNetworkMessageSendType.FromClient)]
    public sealed class AdminChat : GameNetworkMessage
    {
        public string Message { get; set; }
        public AdminChat() { }
        public AdminChat(string message)
        {
            Message = message;
        }
        protected override MultiplayerMessageFilter OnGetLogFilter() => MultiplayerMessageFilter.Administration;
        protected override string OnGetLogFormat() => "Admin Chat Message";
        protected override bool OnRead()
        {
            bool result = true;
            Message = GameNetworkMessage.ReadStringFromPacket(ref result);
            return result;
        }
        protected override void OnWrite() =>
            GameNetworkMessage.WriteStringToPacket(Message);
    }

    // Enthält administrative Befehle (z. B. für Gold- oder Faction-Management).
    [DefineGameNetworkMessageTypeForMod(GameNetworkMessageSendType.FromClient)]
    public sealed class AdminPackets : GameNetworkMessage
    {
        // Hier könnten weitere administrative Befehle gruppiert werden,
        // wie z. B. RequestAdminGold, RequestAdminJoinFaction, etc.
        // Wir fassen hier beispielhaft mehrere Befehle zusammen.

        public enum AdminAction
        {
            BecomeGodlike,
            SetFactionName,
            ResetFactionBanner,
            // weitere Aktionen …
        }

        public AdminAction Action { get; private set; }
        public int Value { get; private set; }  // Beispiel: Goldbetrag oder FactionIndex
        public string StringValue { get; private set; } // Beispiel: neuer FactionName

        public AdminPackets() { }
        public AdminPackets(AdminAction action, int value, string stringValue = null)
        {
            Action = action;
            Value = value;
            StringValue = stringValue;
        }

        protected override MultiplayerMessageFilter OnGetLogFilter() => MultiplayerMessageFilter.Administration;
        protected override string OnGetLogFormat() => $"Admin Action: {Action}";
        protected override bool OnRead()
        {
            bool result = true;
            Action = (AdminAction)GameNetworkMessage.ReadIntFromPacket(new CompressionInfo.Integer(0, 10, true), ref result);
            Value = GameNetworkMessage.ReadIntFromPacket(new CompressionInfo.Integer(0, 1000000, true), ref result);
            StringValue = GameNetworkMessage.ReadStringFromPacket(ref result);
            return result;
        }
        protected override void OnWrite()
        {
            GameNetworkMessage.WriteIntToPacket((int)Action, new CompressionInfo.Integer(0, 10, true));
            GameNetworkMessage.WriteIntToPacket(Value, new CompressionInfo.Integer(0, 1000000, true));
            GameNetworkMessage.WriteStringToPacket(StringValue);
        }
    }

    // Behandelt Aktionen im Zusammenhang mit Conquest (Gebiets- oder Vasalleneinordnungen).
    [DefineGameNetworkMessageTypeForMod(GameNetworkMessageSendType.FromClient)]
    public sealed class HandleConquestAction : GameNetworkMessage
    {
        // Diese Nachricht dient als Transport für Conquest-Aktionen.
        // Die eigentliche Logik wird auf Serverseite ausgeführt.
        public int FactionId { get; private set; }
        public int TargetCastleId { get; private set; }
        public int ActionType { get; private set; }

        public HandleConquestAction() { }
        public HandleConquestAction(int factionId, int targetCastleId, int actionType)
        {
            FactionId = factionId;
            TargetCastleId = targetCastleId;
            ActionType = actionType;
        }
        protected override MultiplayerMessageFilter OnGetLogFilter() => MultiplayerMessageFilter.FactionManagement;
        protected override string OnGetLogFormat() => $"Conquest Action: Faction {FactionId}, Castle {TargetCastleId}, Action {ActionType}";
        protected override bool OnRead()
        {
            bool result = true;
            FactionId = GameNetworkMessage.ReadIntFromPacket(ref result);
            TargetCastleId = GameNetworkMessage.ReadIntFromPacket(ref result);
            ActionType = GameNetworkMessage.ReadIntFromPacket(ref result);
            return result;
        }
        protected override void OnWrite()
        {
            GameNetworkMessage.WriteIntToPacket(FactionId);
            GameNetworkMessage.WriteIntToPacket(TargetCastleId);
            GameNetworkMessage.WriteIntToPacket(ActionType);
        }
    }

    // Beispiel: Eine administrative Nachricht zur Inventarsteuerung (Hotkeys).
    [DefineGameNetworkMessageTypeForMod(GameNetworkMessageSendType.FromClient)]
    public sealed class InventoryHotkey : GameNetworkMessage
    {
        public string ClickedTag { get; set; }
        public InventoryHotkey() { }
        public InventoryHotkey(string clickedTag)
        {
            ClickedTag = clickedTag;
        }
        protected override MultiplayerMessageFilter OnGetLogFilter() => MultiplayerMessageFilter.MissionObjects;
        protected override string OnGetLogFormat() => "Inventory Hotkey Request";
        protected override bool OnRead()
        {
            bool result = false;
            ClickedTag = GameNetworkMessage.ReadStringFromPacket(ref result);
            return result;
        }
        protected override void OnWrite() =>
            GameNetworkMessage.WriteStringToPacket(ClickedTag);
    }

    #endregion

    #region Allgemeine Sonstige Nachrichten

    // Anfrage, um Batch-Voice-Daten zu senden.
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
            {
                sum += bufferLens[i];
            }
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
        protected override string OnGetLogFormat() => "Send Batch Voice";
        protected override bool OnRead()
        {
            bool result = true;
            int len = GameNetworkMessage.ReadIntFromPacket(new CompressionInfo.Integer(0, 10, true), ref result);
            BufferLens = new int[len];
            int sum = 0;
            for (int i = 0; i < len; i++)
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
            {
                GameNetworkMessage.WriteIntToPacket(BufferLens[i], new CompressionInfo.Integer(0, 1440, true));
            }
            GameNetworkMessage.WriteByteArrayToPacket(PackedBuffer, 0, PackedBuffer.Length);
        }
    }

    // Chat-Nachricht (lokal, aber über Admin bzw. allgemeine Kanäle versendbar).
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
        protected override string OnGetLogFormat() => "Shout Message";
        protected override bool OnRead()
        {
            bool result = true;
            Text = GameNetworkMessage.ReadStringFromPacket(ref result);
            return result;
        }
        protected override void OnWrite() =>
            GameNetworkMessage.WriteStringToPacket(Text);
    }

    #endregion
}

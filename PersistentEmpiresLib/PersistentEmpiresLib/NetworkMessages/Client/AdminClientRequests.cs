using System;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Network.Messages;

namespace PersistentEmpiresLib.NetworkMessages.Client.Admin
{
    // Beispiel für Admin-Anfragen. Diese Nachrichten dienen z. B. zur Änderung von Fraktionsdaten, 
    // zum temporären oder permanenten Bann von Spielern, Teleport-Anfragen usw.

    [DefineGameNetworkMessageTypeForMod(GameNetworkMessageSendType.FromClient)]
    public sealed class RequestBecameGodlike : GameNetworkMessage
    {
        public RequestBecameGodlike() { }
        protected override MultiplayerMessageFilter OnGetLogFilter() => MultiplayerMessageFilter.Administration;
        protected override string OnGetLogFormat() => "Request Became Godlike";
        protected override bool OnRead() => true;
        protected override void OnWrite() { }
    }

    [DefineGameNetworkMessageTypeForMod(GameNetworkMessageSendType.FromClient)]
    public sealed class RequestAdminGold : GameNetworkMessage
    {
        public int Gold { get; set; }
        public RequestAdminGold() { }
        public RequestAdminGold(int gold)
        {
            Gold = gold;
        }
        protected override MultiplayerMessageFilter OnGetLogFilter() => MultiplayerMessageFilter.Administration;
        protected override string OnGetLogFormat() => "Request Admin Gold";
        protected override bool OnRead()
        {
            bool result = true;
            Gold = GameNetworkMessage.ReadIntFromPacket(new CompressionInfo.Integer(0, 1000000, true), ref result);
            return result;
        }
        protected override void OnWrite() =>
            GameNetworkMessage.WriteIntToPacket(Gold, new CompressionInfo.Integer(0, 1000000, true));
    }

    [DefineGameNetworkMessageTypeForMod(GameNetworkMessageSendType.FromClient)]
    public sealed class RequestAdminJoinFaction : GameNetworkMessage
    {
        public int FactionIndex { get; set; }
        public RequestAdminJoinFaction() { }
        public RequestAdminJoinFaction(int factionIndex)
        {
            FactionIndex = factionIndex;
        }
        protected override MultiplayerMessageFilter OnGetLogFilter() => MultiplayerMessageFilter.Administration;
        protected override string OnGetLogFormat() => "Request Admin Join Faction";
        protected override bool OnRead()
        {
            bool result = true;
            FactionIndex = GameNetworkMessage.ReadIntFromPacket(new CompressionInfo.Integer(-1, 200, true), ref result);
            return result;
        }
        protected override void OnWrite() =>
            GameNetworkMessage.WriteIntToPacket(FactionIndex, new CompressionInfo.Integer(-1, 200, true));
    }

    [DefineGameNetworkMessageTypeForMod(GameNetworkMessageSendType.FromClient)]
    public sealed class RequestAdminResetFactionBanner : GameNetworkMessage
    {
        public int FactionIndex { get; set; }
        public RequestAdminResetFactionBanner() { }
        public RequestAdminResetFactionBanner(int factionIndex)
        {
            FactionIndex = factionIndex;
        }
        protected override MultiplayerMessageFilter OnGetLogFilter() => MultiplayerMessageFilter.Administration;
        protected override string OnGetLogFormat() => "Request Admin Reset Faction Banner";
        protected override bool OnRead()
        {
            bool result = true;
            FactionIndex = GameNetworkMessage.ReadIntFromPacket(new CompressionInfo.Integer(-1, 200, true), ref result);
            return result;
        }
        protected override void OnWrite() =>
            GameNetworkMessage.WriteIntToPacket(FactionIndex, new CompressionInfo.Integer(-1, 200, true));
    }

    [DefineGameNetworkMessageTypeForMod(GameNetworkMessageSendType.FromClient)]
    public sealed class RequestAdminSetFactionName : GameNetworkMessage
    {
        public int FactionIndex { get; set; }
        public string FactionName { get; set; }
        public RequestAdminSetFactionName() { }
        public RequestAdminSetFactionName(int factionIndex, string factionName)
        {
            FactionIndex = factionIndex;
            FactionName = factionName;
        }
        protected override MultiplayerMessageFilter OnGetLogFilter() => MultiplayerMessageFilter.Administration;
        protected override string OnGetLogFormat() => "Request Admin Set Faction Name";
        protected override bool OnRead()
        {
            bool result = true;
            FactionIndex = GameNetworkMessage.ReadIntFromPacket(new CompressionInfo.Integer(-1, 200, true), ref result);
            FactionName = GameNetworkMessage.ReadStringFromPacket(ref result);
            return result;
        }
        protected override void OnWrite()
        {
            GameNetworkMessage.WriteIntToPacket(FactionIndex, new CompressionInfo.Integer(-1, 200, true));
            GameNetworkMessage.WriteStringToPacket(FactionName);
        }
    }

    // Weitere Admin‑Anfragen (wie Bannen, Teleport etc.) können hier analog ergänzt werden.
}

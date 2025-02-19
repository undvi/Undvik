using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Network.Messages;

namespace PersistentEmpiresLib.NetworkMessages.Client.PlayerActions
{
    // Anfrage, um zu essen zu beginnen.
    [DefineGameNetworkMessageTypeForMod(GameNetworkMessageSendType.FromClient)]
    public sealed class RequestStartEat : GameNetworkMessage
    {
        public RequestStartEat() { }
        protected override MultiplayerMessageFilter OnGetLogFilter() => MultiplayerMessageFilter.AgentsDetailed;
        protected override string OnGetLogFormat() => "Request Start Eat";
        protected override bool OnRead() => true;
        protected override void OnWrite() { }
    }

    // Anfrage, um das Essen zu beenden.
    [DefineGameNetworkMessageTypeForMod(GameNetworkMessageSendType.FromClient)]
    public sealed class RequestStopEat : GameNetworkMessage
    {
        public RequestStopEat() { }
        protected override MultiplayerMessageFilter OnGetLogFilter() => MultiplayerMessageFilter.AgentsDetailed;
        protected override string OnGetLogFormat() => "Request Stop Eat";
        protected override bool OnRead() => true;
        protected override void OnWrite() { }
    }

    // Anfrage, um mit einem Instrument zu spielen zu beginnen.
    [DefineGameNetworkMessageTypeForMod(GameNetworkMessageSendType.FromClient)]
    public sealed class RequestStartPlaying : GameNetworkMessage
    {
        public RequestStartPlaying() { }
        protected override MultiplayerMessageFilter OnGetLogFilter() => MultiplayerMessageFilter.Mission;
        protected override string OnGetLogFormat() => "Request Start Playing";
        protected override bool OnRead() => true;
        protected override void OnWrite() { }
    }

    // Anfrage, um das Spielen zu beenden.
    [DefineGameNetworkMessageTypeForMod(GameNetworkMessageSendType.FromClient)]
    public sealed class RequestStopPlaying : GameNetworkMessage
    {
        public RequestStopPlaying() { }
        protected override MultiplayerMessageFilter OnGetLogFilter() => MultiplayerMessageFilter.Mission;
        protected override string OnGetLogFormat() => "Request Stop Playing";
        protected override bool OnRead() => true;
        protected override void OnWrite() { }
    }

    // Anfrage für Selbstmord.
    [DefineGameNetworkMessageTypeForMod(GameNetworkMessageSendType.FromClient)]
    public sealed class RequestSuicide : GameNetworkMessage
    {
        public RequestSuicide() { }
        protected override MultiplayerMessageFilter OnGetLogFilter() => MultiplayerMessageFilter.Peers;
        protected override string OnGetLogFormat() => "Request Suicide";
        protected override bool OnRead() => true;
        protected override void OnWrite() { }
    }

    // Anfrage, um die Geldtasche zu enthüllen.
    [DefineGameNetworkMessageTypeForMod(GameNetworkMessageSendType.FromClient)]
    public sealed class RequestRevealMoneyPouch : GameNetworkMessage
    {
        public RequestRevealMoneyPouch() { }
        protected override MultiplayerMessageFilter OnGetLogFilter() => MultiplayerMessageFilter.General;
        protected override string OnGetLogFormat() => "Request Reveal Money Pouch";
        protected override bool OnRead() => true;
        protected override void OnWrite() { }
    }
}

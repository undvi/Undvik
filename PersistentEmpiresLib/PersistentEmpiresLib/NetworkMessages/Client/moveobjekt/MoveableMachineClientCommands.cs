using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Network.Messages;

namespace PersistentEmpiresLib.NetworkMessages.Client.MoveableMachine
{
    #region Enums

    public enum MoveDirection
    {
        Forward,
        Backward,
        Up,
        Down
    }

    public enum TurnDirection
    {
        Left,
        Right
    }

    #endregion

    #region Start Movement Commands

    // Generischer Start-Befehl für Bewegungen (Vorwärts, Rückwärts, Hoch, Runter)
    [DefineGameNetworkMessageTypeForMod(GameNetworkMessageSendType.FromClient)]
    public sealed class StartMoveableMachineCommand : GameNetworkMessage
    {
        public MissionObject Object { get; set; }
        public MoveDirection Direction { get; set; }

        public StartMoveableMachineCommand() { }
        public StartMoveableMachineCommand(MissionObject obj, MoveDirection direction)
        {
            Object = obj;
            Direction = direction;
        }

        protected override MultiplayerMessageFilter OnGetLogFilter() => MultiplayerMessageFilter.MissionObjects;
        protected override string OnGetLogFormat() => $"Start Moving {Direction} Game Object";

        protected override bool OnRead()
        {
            bool result = true;
            Object = Mission.MissionNetworkHelper.GetMissionObjectFromMissionObjectId(GameNetworkMessage.ReadMissionObjectIdFromPacket(ref result));
            // Wir kodieren die Richtung als Integer (0 = Forward, 1 = Backward, 2 = Up, 3 = Down)
            Direction = (MoveDirection)GameNetworkMessage.ReadIntFromPacket(new CompressionInfo.Integer(0, 3, true), ref result);
            return result;
        }
        protected override void OnWrite()
        {
            GameNetworkMessage.WriteMissionObjectIdToPacket(Object.Id);
            GameNetworkMessage.WriteIntToPacket((int)Direction, new CompressionInfo.Integer(0, 3, true));
        }
    }

    #endregion

    #region Stop Movement Commands

    // Generischer Stop-Befehl für Bewegungen
    [DefineGameNetworkMessageTypeForMod(GameNetworkMessageSendType.FromClient)]
    public sealed class StopMoveableMachineCommand : GameNetworkMessage
    {
        public MissionObject Object { get; set; }
        public MoveDirection Direction { get; set; }

        public StopMoveableMachineCommand() { }
        public StopMoveableMachineCommand(MissionObject obj, MoveDirection direction)
        {
            Object = obj;
            Direction = direction;
        }

        protected override MultiplayerMessageFilter OnGetLogFilter() => MultiplayerMessageFilter.MissionObjects;
        protected override string OnGetLogFormat() => $"Stop Moving {Direction} Game Object";

        protected override bool OnRead()
        {
            bool result = true;
            Object = Mission.MissionNetworkHelper.GetMissionObjectFromMissionObjectId(GameNetworkMessage.ReadMissionObjectIdFromPacket(ref result));
            Direction = (MoveDirection)GameNetworkMessage.ReadIntFromPacket(new CompressionInfo.Integer(0, 3, true), ref result);
            return result;
        }
        protected override void OnWrite()
        {
            GameNetworkMessage.WriteMissionObjectIdToPacket(Object.Id);
            GameNetworkMessage.WriteIntToPacket((int)Direction, new CompressionInfo.Integer(0, 3, true));
        }
    }

    #endregion

    #region Start Turning Commands

    // Generischer Start-Befehl für Drehungen
    [DefineGameNetworkMessageTypeForMod(GameNetworkMessageSendType.FromClient)]
    public sealed class StartTurnableMachineCommand : GameNetworkMessage
    {
        public MissionObject Object { get; set; }
        public TurnDirection Direction { get; set; }

        public StartTurnableMachineCommand() { }
        public StartTurnableMachineCommand(MissionObject obj, TurnDirection direction)
        {
            Object = obj;
            Direction = direction;
        }

        protected override MultiplayerMessageFilter OnGetLogFilter() => MultiplayerMessageFilter.MissionObjects;
        protected override string OnGetLogFormat() => $"Start Turning {Direction} Game Object";

        protected override bool OnRead()
        {
            bool result = true;
            Object = Mission.MissionNetworkHelper.GetMissionObjectFromMissionObjectId(GameNetworkMessage.ReadMissionObjectIdFromPacket(ref result));
            Direction = (TurnDirection)GameNetworkMessage.ReadIntFromPacket(new CompressionInfo.Integer(0, 1, true), ref result);
            return result;
        }
        protected override void OnWrite()
        {
            GameNetworkMessage.WriteMissionObjectIdToPacket(Object.Id);
            GameNetworkMessage.WriteIntToPacket((int)Direction, new CompressionInfo.Integer(0, 1, true));
        }
    }

    #endregion

    #region Stop Turning Commands

    // Generischer Stop-Befehl für Drehungen
    [DefineGameNetworkMessageTypeForMod(GameNetworkMessageSendType.FromClient)]
    public sealed class StopTurnableMachineCommand : GameNetworkMessage
    {
        public MissionObject Object { get; set; }
        public TurnDirection Direction { get; set; }

        public StopTurnableMachineCommand() { }
        public StopTurnableMachineCommand(MissionObject obj, TurnDirection direction)
        {
            Object = obj;
            Direction = direction;
        }

        protected override MultiplayerMessageFilter OnGetLogFilter() => MultiplayerMessageFilter.MissionObjects;
        protected override string OnGetLogFormat() => $"Stop Turning {Direction} Game Object";

        protected override bool OnRead()
        {
            bool result = true;
            Object = Mission.MissionNetworkHelper.GetMissionObjectFromMissionObjectId(GameNetworkMessage.ReadMissionObjectIdFromPacket(ref result));
            Direction = (TurnDirection)GameNetworkMessage.ReadIntFromPacket(new CompressionInfo.Integer(0, 1, true), ref result);
            return result;
        }
        protected override void OnWrite()
        {
            GameNetworkMessage.WriteMissionObjectIdToPacket(Object.Id);
            GameNetworkMessage.WriteIntToPacket((int)Direction, new CompressionInfo.Integer(0, 1, true));
        }
    }

    #endregion
}

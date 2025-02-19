using System;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Network.Messages;
using TaleWorlds.ObjectSystem;
using PersistentEmpiresLib.SceneScripts;
using PersistentEmpiresLib.Helpers;

namespace PersistentEmpiresLib.NetworkMessages.Server.MiscUpdates
{
    #region Inventory Updates

    [DefineGameNetworkMessageTypeForMod(GameNetworkMessageSendType.FromServer)]
    public sealed class UpdateInventorySlot : GameNetworkMessage
    {
        public string Slot { get; set; }
        public ItemObject Item { get; set; }
        public int Count { get; set; }

        public UpdateInventorySlot() { }
        public UpdateInventorySlot(string slot, ItemObject item, int count)
        {
            this.Slot = slot;
            this.Item = item;
            this.Count = count;
        }
        protected override MultiplayerMessageFilter OnGetLogFilter() => MultiplayerMessageFilter.Equipment;
        protected override string OnGetLogFormat() => "Inventory update received";
        protected override bool OnRead()
        {
            bool result = true;
            this.Slot = GameNetworkMessage.ReadStringFromPacket(ref result);
            string itemId = GameNetworkMessage.ReadStringFromPacket(ref result);
            this.Item = !string.IsNullOrEmpty(itemId) ? MBObjectManager.Instance.GetObject<ItemObject>(itemId) : null;
            this.Count = GameNetworkMessage.ReadIntFromPacket(new CompressionInfo.Integer(0, 256, true), ref result);
            return result;
        }
        protected override void OnWrite()
        {
            GameNetworkMessage.WriteStringToPacket(this.Slot);
            GameNetworkMessage.WriteStringToPacket(this.Item == null ? "" : this.Item.StringId);
            GameNetworkMessage.WriteIntToPacket(this.Count, new CompressionInfo.Integer(0, 256, true));
        }
    }

    #endregion

    #region Agent Configuration

    [DefineGameNetworkMessageTypeForMod(GameNetworkMessageSendType.FromServer)]
    public sealed class AgentLabelConfig : GameNetworkMessage
    {
        public AgentLabelConfig() { }
        public AgentLabelConfig(bool enabled)
        {
            this.Enabled = enabled;
        }
        public bool Enabled = true;
        protected override MultiplayerMessageFilter OnGetLogFilter() => MultiplayerMessageFilter.All;
        protected override string OnGetLogFormat() => "AgentLabelConfig";
        protected override bool OnRead()
        {
            bool result = true;
            this.Enabled = GameNetworkMessage.ReadBoolFromPacket(ref result);
            return result;
        }
        protected override void OnWrite()
        {
            GameNetworkMessage.WriteBoolToPacket(this.Enabled);
        }
    }

    #endregion

    #region Agent Actions

    [DefineGameNetworkMessageTypeForMod(GameNetworkMessageSendType.FromServer)]
    public sealed class AgentPlayingInstrument : GameNetworkMessage
    {
        public Agent PlayerAgent;
        public int PlayingInstrumentIndex;
        public bool IsPlaying;
        public AgentPlayingInstrument() { }
        public AgentPlayingInstrument(Agent agent, int index, bool isPlaying)
        {
            this.PlayerAgent = agent;
            this.PlayingInstrumentIndex = index;
            this.IsPlaying = isPlaying;
        }
        protected override MultiplayerMessageFilter OnGetLogFilter() => MultiplayerMessageFilter.Agents;
        protected override string OnGetLogFormat() => "AgentPlayingInstrument";
        protected override bool OnRead()
        {
            bool result = true;
            this.PlayerAgent = Mission.MissionNetworkHelper.GetAgentFromIndex(GameNetworkMessage.ReadAgentIndexFromPacket(ref result));
            this.PlayingInstrumentIndex = GameNetworkMessage.ReadIntFromPacket(new CompressionInfo.Integer(0, 100, true), ref result);
            this.IsPlaying = GameNetworkMessage.ReadBoolFromPacket(ref result);
            return result;
        }
        protected override void OnWrite()
        {
            GameNetworkMessage.WriteAgentIndexToPacket(this.PlayerAgent.Index);
            GameNetworkMessage.WriteIntToPacket(this.PlayingInstrumentIndex, new CompressionInfo.Integer(0, 100, true));
            GameNetworkMessage.WriteBoolToPacket(this.IsPlaying);
        }
    }

    #endregion

    #region Agent Packets

    [DefineGameNetworkMessageTypeForMod(GameNetworkMessageSendType.FromServer)]
    public sealed class BehadeAgentPacket : GameNetworkMessage
    {
        public Agent BodyAgent;
        public Agent HeadAgent;
        public BehadeAgentPacket() { }
        public BehadeAgentPacket(Agent bodyAgent, Agent headAgent)
        {
            this.BodyAgent = bodyAgent;
            this.HeadAgent = headAgent;
        }
        protected override MultiplayerMessageFilter OnGetLogFilter() => MultiplayerMessageFilter.None;
        protected override string OnGetLogFormat() => "BehadeAgentPacket";
        protected override bool OnRead()
        {
            bool result = true;
            this.BodyAgent = Mission.MissionNetworkHelper.GetAgentFromIndex(GameNetworkMessage.ReadAgentIndexFromPacket(ref result));
            this.HeadAgent = Mission.MissionNetworkHelper.GetAgentFromIndex(GameNetworkMessage.ReadAgentIndexFromPacket(ref result));
            return result;
        }
        protected override void OnWrite()
        {
            GameNetworkMessage.WriteAgentIndexToPacket(this.BodyAgent.Index);
            GameNetworkMessage.WriteAgentIndexToPacket(this.HeadAgent.Index);
        }
    }

    #endregion

    #region Agent Animation

    [DefineGameNetworkMessageTypeForMod(GameNetworkMessageSendType.FromServer)]
    public sealed class SetAgentAnimation : GameNetworkMessage
    {
        public Agent AnimAgent;
        public string ActionId;
        public SetAgentAnimation() { }
        public SetAgentAnimation(Agent _animAgent, string _actionId)
        {
            this.AnimAgent = _animAgent;
            this.ActionId = _actionId;
        }
        protected override MultiplayerMessageFilter OnGetLogFilter() => MultiplayerMessageFilter.None;
        protected override string OnGetLogFormat() => "SetAgentAnimation";
        protected override bool OnRead()
        {
            bool result = true;
            this.AnimAgent = Mission.MissionNetworkHelper.GetAgentFromIndex(GameNetworkMessage.ReadAgentIndexFromPacket(ref result));
            this.ActionId = GameNetworkMessage.ReadStringFromPacket(ref result);
            return result;
        }
        protected override void OnWrite()
        {
            GameNetworkMessage.WriteAgentIndexToPacket(this.AnimAgent.Index);
            GameNetworkMessage.WriteStringToPacket(this.ActionId);
        }
    }

    #endregion

    #region Ladder Builder

    [DefineGameNetworkMessageTypeForMod(GameNetworkMessageSendType.FromServer)]
    public sealed class SetLadderBuilder : GameNetworkMessage
    {
        public PE_LadderBuilder LadderBuilder { get; private set; }
        public bool LadderBuilt { get; private set; }
        public SetLadderBuilder() { }
        public SetLadderBuilder(PE_LadderBuilder ladderBuilder, bool ladderBuilt)
        {
            LadderBuilder = ladderBuilder;
            LadderBuilt = ladderBuilt;
        }
        protected override MultiplayerMessageFilter OnGetLogFilter() => MultiplayerMessageFilter.MissionObjects;
        protected override string OnGetLogFormat() => $"[Ladder] ID: {LadderBuilder?.Id ?? -1} | Status: {(LadderBuilt ? "Built" : "Not Built")}";
        protected override bool OnRead()
        {
            bool result = true;
            int missionObjectId = GameNetworkMessage.ReadMissionObjectIdFromPacket(ref result);
            LadderBuilder = Mission.MissionNetworkHelper.GetMissionObjectFromMissionObjectId(missionObjectId) as PE_LadderBuilder;
            LadderBuilt = GameNetworkMessage.ReadBoolFromPacket(ref result);
            if (LadderBuilder == null)
            {
                Debug.Print($"[Error] SetLadderBuilder: Leiter-Objekt mit ID {missionObjectId} nicht gefunden!", 0xFF0000);
                result = false;
            }
            return result;
        }
        protected override void OnWrite()
        {
            GameNetworkMessage.WriteMissionObjectIdToPacket(LadderBuilder?.Id ?? -1);
            GameNetworkMessage.WriteBoolToPacket(LadderBuilt);
        }
    }

    #endregion

    #region Battering Ram Arrival

    [DefineGameNetworkMessageTypeForMod(GameNetworkMessageSendType.FromServer)]
    public sealed class SetPE_BatteringRamHasArrivedAtTarget : GameNetworkMessage
    {
        public MissionObjectId BatteringRamId { get; private set; }
        public SetPE_BatteringRamHasArrivedAtTarget() { }
        public SetPE_BatteringRamHasArrivedAtTarget(MissionObjectId batteringRamId)
        {
            this.BatteringRamId = batteringRamId;
        }
        protected override bool OnRead()
        {
            bool flag = true;
            this.BatteringRamId = GameNetworkMessage.ReadMissionObjectIdFromPacket(ref flag);
            return flag;
        }
        protected override void OnWrite()
        {
            GameNetworkMessage.WriteMissionObjectIdToPacket(this.BatteringRamId);
        }
        protected override MultiplayerMessageFilter OnGetLogFilter() => MultiplayerMessageFilter.SiegeWeaponsDetailed;
        protected override string OnGetLogFormat() => "Battering Ram with ID: " + this.BatteringRamId + " has arrived at its target.";
    }

    #endregion

    #region Shout / Local Messages

    [DefineGameNetworkMessageTypeForMod(GameNetworkMessageSendType.FromServer)]
    public sealed class ShoutMessageServer : GameNetworkMessage
    {
        public string Message;
        public NetworkCommunicator Sender;
        public ShoutMessageServer() { }
        public ShoutMessageServer(string message, NetworkCommunicator sender)
        {
            this.Sender = sender;
            this.Message = message;
        }
        protected override MultiplayerMessageFilter OnGetLogFilter() => MultiplayerMessageFilter.General;
        protected override string OnGetLogFormat() => "Local message received";
        protected override bool OnRead()
        {
            bool result = true;
            this.Sender = GameNetworkMessage.ReadNetworkPeerReferenceFromPacket(ref result);
            this.Message = GameNetworkMessage.ReadStringFromPacket(ref result);
            return result;
        }
        protected override void OnWrite()
        {
            GameNetworkMessage.WriteNetworkPeerReferenceToPacket(this.Sender);
            GameNetworkMessage.WriteStringToPacket(this.Message);
        }
    }

    #endregion

    #region Attachments

    [DefineGameNetworkMessageTypeForMod(GameNetworkMessageSendType.FromServer)]
    public sealed class SyncAttachToAgent : GameNetworkMessage
    {
        public PE_AttachToAgent AttachToAgent;
        public Agent UserAgent;
        public SyncAttachToAgent() { }
        public SyncAttachToAgent(PE_AttachToAgent attachToAgent, Agent userAgent)
        {
            this.AttachToAgent = attachToAgent;
            this.UserAgent = userAgent;
        }
        protected override MultiplayerMessageFilter OnGetLogFilter() => MultiplayerMessageFilter.MissionObjects;
        protected override string OnGetLogFormat() => "Sync Attach To Agent";
        protected override bool OnRead()
        {
            bool result = true;
            this.AttachToAgent = (PE_AttachToAgent)Mission.MissionNetworkHelper.GetMissionObjectFromMissionObjectId(GameNetworkMessage.ReadMissionObjectIdFromPacket(ref result));
            this.UserAgent = Mission.MissionNetworkHelper.GetAgentFromIndex(GameNetworkMessage.ReadAgentIndexFromPacket(ref result), true);
            return result;
        }
        protected override void OnWrite()
        {
            GameNetworkMessage.WriteMissionObjectIdToPacket(this.AttachToAgent.Id);
            GameNetworkMessage.WriteAgentIndexToPacket(this.UserAgent.Index);
        }
    }

    #endregion

    #region Gold Synchronization

    [DefineGameNetworkMessageTypeForMod(GameNetworkMessageSendType.FromServer)]
    public sealed class SyncGold : GameNetworkMessage
    {
        public int Gold;
        public SyncGold() { }
        public SyncGold(int gold)
        {
            this.Gold = gold;
        }
        protected override MultiplayerMessageFilter OnGetLogFilter() => MultiplayerMessageFilter.General;
        protected override string OnGetLogFormat() => "Sync gold";
        protected override bool OnRead()
        {
            bool result = true;
            this.Gold = GameNetworkMessage.ReadIntFromPacket(new CompressionInfo.Integer(0, Int32.MaxValue, true), ref result);
            return result;
        }
        protected override void OnWrite()
        {
            GameNetworkMessage.WriteIntToPacket(this.Gold, new CompressionInfo.Integer(0, Int32.MaxValue, true));
        }
    }

    #endregion

    #region Wounded Player Update

    [DefineGameNetworkMessageTypeForMod(GameNetworkMessageSendType.FromServer)]
    public sealed class UpdateWoundedPlayer : GameNetworkMessage
    {
        public NetworkCommunicator Player;
        public bool IsWounded;
        public UpdateWoundedPlayer() { }
        public UpdateWoundedPlayer(NetworkCommunicator player, bool isWounded)
        {
            this.Player = player;
            this.IsWounded = isWounded;
        }
        protected override MultiplayerMessageFilter OnGetLogFilter() => MultiplayerMessageFilter.Agents;
        protected override string OnGetLogFormat() => "UpdateWoundedPlayer";
        protected override bool OnRead()
        {
            bool result = true;
            this.Player = GameNetworkMessage.ReadNetworkPeerReferenceFromPacket(ref result);
            this.IsWounded = GameNetworkMessage.ReadBoolFromPacket(ref result);
            return result;
        }
        protected override void OnWrite()
        {
            GameNetworkMessage.WriteNetworkPeerReferenceToPacket(this.Player);
            GameNetworkMessage.WriteBoolToPacket(this.IsWounded);
        }
    }

    #endregion
}

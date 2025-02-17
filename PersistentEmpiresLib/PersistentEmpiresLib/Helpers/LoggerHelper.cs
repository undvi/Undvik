using PersistentEmpiresLib.Data;
using PersistentEmpiresLib.Database.DBEntities;
using PersistentEmpiresLib.Factions;
using PersistentEmpiresLib.SceneScripts;
using System;
using System.Linq;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;

namespace PersistentEmpiresLib.Helpers
{
    public static class LogAction
    {
        public static readonly string PlayerBuysStockpile = "PlayerBuysStockpile";
        public static readonly string PlayerSellsStockpile = "PlayerSellsStockpile";
        public static readonly string PlayerStockpileLimitExceeded = "PlayerStockpileLimitExceeded";
        public static readonly string PlayerStockpileTaxCalculated = "PlayerStockpileTaxCalculated";
        public static readonly string PlayerStockpileExpiredItemRemoved = "PlayerStockpileExpiredItemRemoved";

        public static readonly string LocalChat = "LocalChat";
        public static readonly string TeamChat = "TeamChat";
        public static readonly string PlayerHitToAgent = "PlayerHitAgent";
        public static readonly string PlayerKilledAnAgent = "PlayerKilledAgent";
        public static readonly string PlayerJoined = "PlayerJoined";
        public static readonly string PlayerDisconnected = "PlayerDisconnected";
        public static readonly string PlayerFactionChange = "PlayerFactionChange";
        public static readonly string FactionDeclaredWar = "FactionDeclaredWar";
        public static readonly string FactionMadePeace = "FactionMadePeace";
        public static readonly string PlayerBumpedWithHorse = "PlayerBumpedWithHorse";
    }

    public class LoggerHelper
    {
        public delegate void LogActionHandler(DBLog log);
        public static event LogActionHandler OnLogAction;

        public static void LogAnAction(NetworkCommunicator issuer, string actionType, AffectedPlayer[] affectedPlayers = null, object[] oParams = null)
        {
            affectedPlayers ??= new AffectedPlayer[] { };
            oParams ??= new object[] { };

            string logMessage = GenerateActionMessage(issuer, actionType, DateTime.UtcNow, affectedPlayers, oParams);

            DBLog dbLog = new DBLog()
            {
                ActionType = actionType,
                AffectedPlayers = new Json<AffectedPlayer[]>(affectedPlayers),
                CreatedAt = DateTime.UtcNow,
                IssuerCoordinates = GetCoordinatesOfPlayer(issuer),
                IssuerPlayerId = issuer.VirtualPlayer.ToPlayerId(),
                IssuerPlayerName = issuer.UserName,
                LogMessage = logMessage
            };

            OnLogAction?.Invoke(dbLog);
        }

        private static string GenerateActionMessage(NetworkCommunicator issuer, string actionType, DateTime dateTime, AffectedPlayer[] affectedPlayers, object[] oParams)
        {
            string playerLog = FormatLogForPlayer(issuer, dateTime);

            return actionType switch
            {
                nameof(LogAction.PlayerJoined) => $"{playerLog} joined the server.",
                nameof(LogAction.PlayerDisconnected) => $"{playerLog} disconnected from the server.",
                nameof(LogAction.LocalChat) or nameof(LogAction.TeamChat) =>
                    $"{playerLog} said \"{oParams[0] as string}\". Receivers: {AffectedPlayersToString(affectedPlayers)}",
                nameof(LogAction.PlayerHitToAgent) or nameof(LogAction.PlayerBumpedWithHorse) or nameof(LogAction.PlayerKilledAnAgent) =>
                    HandlePlayerAttackAgent(issuer, dateTime, (MissionWeapon)oParams[0], (Agent)oParams[1], actionType),
                nameof(LogAction.PlayerFactionChange) =>
                    $"{playerLog} joined faction {((Faction)oParams[0])?.name ?? "Unknown"} from {((Faction)oParams[1])?.name ?? "Unknown"}.",
                nameof(LogAction.FactionDeclaredWar) =>
                    $"{playerLog} declared war on {((Faction)oParams[0])?.name ?? "Unknown"}.",
                nameof(LogAction.FactionMadePeace) =>
                    $"{playerLog} made peace with {((Faction)oParams[0])?.name ?? "Unknown"}.",
                _ => actionType
            };
        }

        private static string HandlePlayerAttackAgent(NetworkCommunicator issuer, DateTime dateTime, MissionWeapon weapon, Agent affectedAgent, string actionType)
        {
            string attackedItem = weapon.Item?.Name?.ToString() ?? "fist";
            string warStatus = GetWarStatus(issuer, affectedAgent);
            string playerLog = FormatLogForPlayer(issuer, dateTime);
            string targetLog = FormatLogForAgent(affectedAgent, dateTime);

            return actionType switch
            {
                nameof(LogAction.PlayerHitToAgent) => $"{playerLog} attacked {targetLog} with {attackedItem}. War status: {warStatus}.",
                nameof(LogAction.PlayerBumpedWithHorse) => $"{playerLog} bumped into {targetLog} with a horse. War status: {warStatus}.",
                nameof(LogAction.PlayerKilledAnAgent) => $"{playerLog} killed {targetLog} with {attackedItem}. War status: {warStatus}.",
                _ => string.Empty
            };
        }

        private static string GetWarStatus(NetworkCommunicator issuer, Agent affectedAgent)
        {
            NetworkCommunicator affectedPeer = GetAffectedPeerFromAgent(affectedAgent);
            if (affectedPeer == null) return "Neutral";

            PersistentEmpireRepresentative issuerRepr = issuer.GetComponent<PersistentEmpireRepresentative>();
            PersistentEmpireRepresentative affectedRepr = affectedPeer.GetComponent<PersistentEmpireRepresentative>();

            if (issuerRepr?.GetFaction() == null || affectedRepr?.GetFaction() == null)
                return "Neutral";

            return issuerRepr.GetFaction().warDeclaredTo.Contains(affectedRepr.GetFactionIndex()) ||
                   affectedRepr.GetFaction().warDeclaredTo.Contains(issuerRepr.GetFactionIndex())
                ? "Enemies"
                : "Neutral";
        }

        private static string FormatLogForPlayer(NetworkCommunicator player, DateTime dateTime) =>
            $"[{player.UserName} ({GetCoordinatesOfPlayer(player)})]";

        private static string FormatLogForAgent(Agent agent, DateTime dateTime)
        {
            if (agent.IsHuman && agent.IsPlayerControlled)
                return FormatLogForPlayer(agent.MissionPeer.GetNetworkPeer(), dateTime);

            if (agent.IsMount && agent.RiderAgent?.IsHuman == true && agent.RiderAgent.IsPlayerControlled)
                return $"A Horse Ridden by {FormatLogForPlayer(agent.RiderAgent.MissionPeer.GetNetworkPeer(), dateTime)}";

            return agent.IsMount ? "A Horse" : "An Animal";
        }

        private static string GetCoordinatesOfPlayer(NetworkCommunicator player) =>
            player.ControlledAgent?.IsActive() == true
                ? $"({player.ControlledAgent.Position.X}, {player.ControlledAgent.Position.Y}, {player.ControlledAgent.Position.Z})"
                : "(?, ?, ?)";

        private static string AffectedPlayersToString(AffectedPlayer[] players) =>
            string.Join(", ", players.Select(p => p.ToString()));
    }
}

using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Network.Messages;
using TaleWorlds.Library;

namespace PersistentEmpiresLib.NetworkMessages.Server.UpgradeableBuilding
{
    #region Upgrade Building Tier

    /// <summary>
    /// Nachricht: Setzt ein Gebäude auf eine bestimmte Tier-Stufe.
    /// Roadmap:
    ///  - Tier 1 bis 3 (Holz/Stein/Eisen).
    ///  - Optionale Abhängigkeit von Fraktionsrang.
    ///  - Ggf. Bauzeit oder Sofort-Upgrade.
    /// </summary>
    [DefineGameNetworkMessageTypeForMod(GameNetworkMessageSendType.FromServer)]
    public sealed class UpgradeableBuildingSetTier : GameNetworkMessage
    {
        public int Tier { get; private set; }
        public MissionObject UpgradingObject { get; private set; }

        // Erweiterung: Optionale Felder für Ressourcenkosten oder Fraktionsinfos
        public int FactionIndex { get; private set; }
        public int RequiredWood { get; private set; }
        public int RequiredStone { get; private set; }
        public int RequiredIron { get; private set; }
        // Hier könntest du z. B. noch "GoldCost", "InfluenceCost" oder "UpgradeTime" hinzufügen.

        public UpgradeableBuildingSetTier() { }

        public UpgradeableBuildingSetTier(
            int tier,
            MissionObject upgradingObject,
            int factionIndex,
            int requiredWood,
            int requiredStone,
            int requiredIron)
        {
            Tier = tier;
            UpgradingObject = upgradingObject;
            FactionIndex = factionIndex;
            RequiredWood = requiredWood;
            RequiredStone = requiredStone;
            RequiredIron = requiredIron;
        }

        protected override MultiplayerMessageFilter OnGetLogFilter()
            => MultiplayerMessageFilter.MissionObjects;

        protected override string OnGetLogFormat()
        {
            return $"[Upgrade] Gebäude {UpgradingObject?.Id ?? -1} -> Stufe {Tier}, " +
                   $"Faction {FactionIndex}, Wood {RequiredWood}, Stone {RequiredStone}, Iron {RequiredIron}";
        }

        protected override bool OnRead()
        {
            bool result = true;

            Tier = GameNetworkMessage.ReadIntFromPacket(
                new CompressionInfo.Integer(0, 4, true), ref result);

            int missionObjectId = GameNetworkMessage.ReadMissionObjectIdFromPacket(ref result);

            FactionIndex = GameNetworkMessage.ReadIntFromPacket(
                new CompressionInfo.Integer(-1, 1000, true), ref result);
            RequiredWood = GameNetworkMessage.ReadIntFromPacket(
                new CompressionInfo.Integer(0, 999999, true), ref result);
            RequiredStone = GameNetworkMessage.ReadIntFromPacket(
                new CompressionInfo.Integer(0, 999999, true), ref result);
            RequiredIron = GameNetworkMessage.ReadIntFromPacket(
                new CompressionInfo.Integer(0, 999999, true), ref result);

            if (!result)
                return false;

            UpgradingObject = Mission.MissionNetworkHelper.GetMissionObjectFromMissionObjectId(missionObjectId);
            if (UpgradingObject == null)
            {
                Debug.Print($"[Error] UpgradeableBuildingSetTier: MissionObject mit ID {missionObjectId} nicht gefunden!", 0xFF0000);
                return false;
            }

            // Beispiel: Prüfen, ob Tier valid (0..3) oder ob FactionIndex -1 => neutral
            // => Zusätzliche Checks in Manager-Klasse realisieren

            return true;
        }

        protected override void OnWrite()
        {
            GameNetworkMessage.WriteIntToPacket(Tier,
                new CompressionInfo.Integer(0, 4, true));

            GameNetworkMessage.WriteMissionObjectIdToPacket(UpgradingObject?.Id ?? -1);

            GameNetworkMessage.WriteIntToPacket(FactionIndex,
                new CompressionInfo.Integer(-1, 1000, true));
            GameNetworkMessage.WriteIntToPacket(RequiredWood,
                new CompressionInfo.Integer(0, 999999, true));
            GameNetworkMessage.WriteIntToPacket(RequiredStone,
                new CompressionInfo.Integer(0, 999999, true));
            GameNetworkMessage.WriteIntToPacket(RequiredIron,
                new CompressionInfo.Integer(0, 999999, true));
        }
    }

    #endregion

    #region Upgrade Building State

    /// <summary>
    /// Nachricht: Startet oder stoppt den Upgrade-Prozess
    /// (Roadmap: Bauphasen, Timer, mehrere Stufen).
    /// 
    /// Hier kannst du z. B. Zeitbasiertes Bauen realisieren:
    ///  - IsUpgrading = true => Timer läuft
    ///  - OnFinish => Tier++ / Neubau
    /// </summary>
    [DefineGameNetworkMessageTypeForMod(GameNetworkMessageSendType.FromServer)]
    public sealed class UpgradeableBuildingUpgrading : GameNetworkMessage
    {
        public bool IsUpgrading { get; private set; }
        public MissionObject UpgradingObject { get; private set; }

        // Erweiterung: Bau-Fortschritt (z. B. 0..100%)
        public float ConstructionProgress { get; private set; }
        // NEU: Falls relevant, könnte man hier angeben, wie lange das Upgrade noch braucht
        public float TimeRemaining { get; private set; }

        public UpgradeableBuildingUpgrading() { }

        public UpgradeableBuildingUpgrading(bool isUpgrading,
                                            MissionObject upgradingObject,
                                            float constructionProgress,
                                            float timeRemaining)
        {
            IsUpgrading = isUpgrading;
            UpgradingObject = upgradingObject;
            ConstructionProgress = constructionProgress;
            TimeRemaining = timeRemaining;
        }

        protected override MultiplayerMessageFilter OnGetLogFilter()
            => MultiplayerMessageFilter.MissionObjects;

        protected override string OnGetLogFormat()
        {
            return $"[Upgrade] Gebäude: {UpgradingObject?.Id ?? -1} | " +
                   $"Status: {(IsUpgrading ? "Start" : "Stop")}, " +
                   $"Progress: {ConstructionProgress:F1}%, TimeLeft: {TimeRemaining:F1}s";
        }

        protected override bool OnRead()
        {
            bool result = true;
            IsUpgrading = GameNetworkMessage.ReadBoolFromPacket(ref result);
            int missionObjectId = GameNetworkMessage.ReadMissionObjectIdFromPacket(ref result);

            // Baufortschritt & Zeit
            ConstructionProgress = GameNetworkMessage.ReadFloatFromPacket(
                new CompressionInfo.Float(0f, 100f, 0.1f), ref result);
            TimeRemaining = GameNetworkMessage.ReadFloatFromPacket(
                new CompressionInfo.Float(0f, 3600f, 0.1f), ref result); // max. 1h als Bsp

            if (!result)
                return false;

            UpgradingObject = Mission.MissionNetworkHelper.GetMissionObjectFromMissionObjectId(missionObjectId);
            if (UpgradingObject == null)
            {
                Debug.Print($"[Error] UpgradeableBuildingUpgrading: MissionObject ID {missionObjectId} nicht gefunden!", 0xFF0000);
                return false;
            }

            return true;
        }

        protected override void OnWrite()
        {
            GameNetworkMessage.WriteBoolToPacket(IsUpgrading);
            GameNetworkMessage.WriteMissionObjectIdToPacket(UpgradingObject?.Id ?? -1);

            GameNetworkMessage.WriteFloatToPacket(ConstructionProgress,
                new CompressionInfo.Float(0f, 100f, 0.1f));
            GameNetworkMessage.WriteFloatToPacket(TimeRemaining,
                new CompressionInfo.Float(0f, 3600f, 0.1f));
        }
    }

    #endregion
}

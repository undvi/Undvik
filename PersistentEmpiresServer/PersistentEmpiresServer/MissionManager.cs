using System;
using System.Timers;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;
using PersistentEmpiresMod.Trading;
using PersistentEmpiresLib.PersistentEmpiresMission.MissionBehaviors;
using PersistentEmpiresServer.ServerMissions;
using PersistentEmpiresServer.SpawnBehavior;
using TaleWorlds.MountAndBlade.Multiplayer;
using TaleWorlds.MountAndBlade.Source.Missions;

namespace PersistentEmpiresServer
{
    public class MissionManager
    {
        private static Timer missionTimer;

        static MissionManager()
        {
            missionTimer = new Timer(14400000); // 4 Stunden in Millisekunden
            missionTimer.Elapsed += GenerateTradeMissions;
            missionTimer.AutoReset = true;
            missionTimer.Enabled = true;
        }

        private static void GenerateTradeMissions(object sender, ElapsedEventArgs e)
        {
            foreach (Faction faction in Campaign.Current.Factions)
            {
                if (faction.IsKingdomFaction)
                {
                    ExportTradeSystem.GenerateTradeOrder(faction);
                }
            }
        }

        [MissionMethod]
        public static void OpenPersistentEmpires(string scene)
        {
            MissionState.OpenNew("PersistentEmpires", new MissionInitializerRecord(scene), delegate (Mission missionController)
            {
                return new MissionBehavior[]
                {
                    new PersistentEmpireBehavior(),
                    new SaveSystemBehavior(), // Wichtiges Speicherverhalten
                    new DayNightCycleBehavior(),
                    MissionLobbyComponent.CreateBehavior(),
                    new DrowningBehavior(),
                    new NotAllPlayersJoinFixBehavior(),
                    new AnimalButcheringBehavior(),
                    new InformationComponent(),
                    new FactionsBehavior(),
                    new CastlesBehavior(),
                    new DoctorBehavior(),
                    new FactionPollComponent(),
                    new WoundingBehavior(),
                    new PlayerInventoryComponent(),
                    new PersistentEmpireClientBehavior(),
                    new PersistentEmpireSceneSyncBehaviors(),
                    new ImportExportComponent(),
                    new MultiplayerTimerComponent(),
                    new CraftingComponent(),
                    new MoneyPouchBehavior(),
                    new StockpileMarketComponent(),
                    new LocalChatComponent(),
                    new InstrumentsBehavior(),
                    new CombatlogBehavior(),
                    new AgentHungerBehavior(),
                    new SpawnFrameSelectionBehavior(),
                    new SpawnComponent(new PersistentEmpireSpawnFrameBehavior(), new PersistentEmpiresSpawningBehavior()),
                    new MissionLobbyEquipmentNetworkComponent(),
                    new ProximityChatComponent(),
                    new MissionBoundaryPlacer(),
                    new MissionBoundaryCrossingHandler(),
                    new MissionOptionsComponent(),
                    new MissionScoreboardComponent(new TDMScoreboardData()),
                    new MissionAgentPanicHandler(),
                    new AdminServerBehavior(),
                    new BankingComponent(),
                    new PatreonRegistryBehavior(),
                    new TradingCenterBehavior(),
                    new MoneyChestBehavior(),
                    new PickpocketingBehavior(),
                    new LockpickingBehavior(),
                    new PoisoningBehavior(),
                    new AutorestartBehavior(),
                    new AnimationBehavior(),
                    new ChatCommandSystem(),
                    new WhitelistBehavior(),
                    new AutoPayBehavior(),
                    // Integration des Handels-Systems
                    new ExportTradeSystem()
                };
            }, true, true);
        }

        public static void Initialize()
        {
            missionTimer.Start();
            ExportTradeSystem.LoadTradeOrders(); // Laden der Handelsaufträge bei Serverstart
            InformationManager.DisplayMessage(new InformationMessage("Mission Manager gestartet. Handelsmissionen werden alle 4 Stunden generiert."));
        }
    }
}

using System;
using System.Timers;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;
using PersistentEmpiresMod.Trading;
using PersistentEmpiresLib.PersistentEmpiresMission.MissionBehaviors;
using TaleWorlds.MountAndBlade.Multiplayer;
using TaleWorlds.MountAndBlade.Source.Missions;

namespace PersistentEmpiresClient
{
    public class MissionManager
    {
        private static Timer missionTimer;

        static MissionManager()
        {
            missionTimer = new Timer(14400000); // 4 Stunden in Millisekunden
            missionTimer.Elapsed += RequestTradeOrders;
            missionTimer.AutoReset = true;
            missionTimer.Enabled = true;
        }

        private static void RequestTradeOrders(object sender, ElapsedEventArgs e)
        {
            if (GameNetwork.IsClient)
            {
                InformationManager.DisplayMessage(new InformationMessage("Handelsaufträge werden aktualisiert..."));
                ExportTradeSystem.RequestTradeOrders(); // Stellt eine Anfrage an den Server
            }
        }

        [MissionMethod]
        public static void OpenPersistentEmpires(string scene)
        {
            MissionState.OpenNew("PersistentEmpires", new MissionInitializerRecord(scene), delegate (Mission missionController)
            {
                return new MissionBehavior[]
                {
                    MissionLobbyComponent.CreateBehavior(),
                    new InformationComponent(),
                    new FactionsBehavior(),
                    new CastlesBehavior(),
                    new FactionPollComponent(),
                    new WoundingBehavior(),
                    new PersistentEmpireClientBehavior(),
                    new PlayerInventoryComponent(),
                    new ImportExportComponent(),
                    new CraftingComponent(),
                    new MoneyPouchBehavior(),
                    new StockpileMarketComponent(),
                    new ProximityChatComponent(),
                    new MultiplayerTimerComponent(),
                    new MultiplayerMissionAgentVisualSpawnComponent(),
                    new PersistentEmpireSceneSyncBehaviors(),
                    new AgentHungerBehavior(),
                    new MissionLobbyEquipmentNetworkComponent(),
                    new MissionBoundaryPlacer(),
                    new MissionBoundaryCrossingHandler(),
                    new FactionUIComponent(),
                    new LocalChatComponent(),
                    new MissionOptionsComponent(),
                    new MissionScoreboardComponent(new TDMScoreboardData()),
                    new AdminClientBehavior(),
                    new BankingComponent(),
                    new PatreonRegistryBehavior(),
                    new TradingCenterBehavior(),
                    new DayNightCycleBehavior(),
                    new InstrumentsBehavior(),
                    new MoneyChestBehavior(),
                    new DecapitationBehavior(),
                    new AnimationBehavior(),
                };
            }, true, true);
        }

        public static void Initialize()
        {
            if (GameNetwork.IsClient)
            {
                missionTimer.Start();
                ExportTradeSystem.RequestTradeOrders(); // Initiale Handelsanfrage beim Start
                InformationManager.DisplayMessage(new InformationMessage("Client-Mission Manager gestartet. Handelsaufträge werden alle 4 Stunden abgefragt."));
            }
        }
    }
}

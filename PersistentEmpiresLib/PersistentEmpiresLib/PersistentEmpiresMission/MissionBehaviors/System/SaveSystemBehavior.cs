using PersistentEmpiresLib.Database.DBEntities;
using PersistentEmpiresLib.Factions;
using PersistentEmpiresLib.SceneScripts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace PersistentEmpiresLib.PersistentEmpiresMission.MissionBehaviors
{
    public class SaveSystemBehavior : MissionNetwork
    {
        public long LastSaveAt = DateTimeOffset.Now.ToUnixTimeSeconds();
        public int SaveDuration = 600;

        /* Events */
        public delegate void StartMigration();
        public static event StartMigration OnStartMigration;

        /* Spieler */
        public delegate void CreateOrSavePlayers(List<NetworkCommunicator> peers);
        public static event CreateOrSavePlayers OnCreateOrSavePlayers;

        public delegate DBPlayer CreateOrSavePlayer(NetworkCommunicator peer);
        public static event CreateOrSavePlayer OnCreateOrSavePlayer;

        public delegate DBPlayer GetOrCreatePlayer(NetworkCommunicator peer, out bool created);
        public static event GetOrCreatePlayer OnGetOrCreatePlayer;

        public delegate DBPlayer GetPlayer(string playerId);
        public static event GetPlayer OnGetPlayer;

        /* Inventar */
        public delegate DBInventory GetOrCreateInventory(string inventoryId);
        public static event GetOrCreateInventory OnGetOrCreateInventory;

        /* Fraktionen */
        public delegate DBFactions GetFaction(int factionIndex);
        public static event GetFaction OnGetFaction;

        public delegate void SetFactionTax(string factionName, float taxRate);
        public static event SetFactionTax OnSetFactionTax;

        public delegate float GetFactionTax(string factionName);
        public static event GetFactionTax OnGetFactionTax;

        /* Märkte */
        public delegate IEnumerable<DBStockpileMarket> GetAllMarketStands();
        public static event GetAllMarketStands OnGetAllMarketStands;

        public delegate DBStockpileMarket GetMarketStand(PE_StockpileMarket market);
        public static event GetMarketStand OnGetMarketStand;

        public delegate void UpsertMarketStands(List<PE_StockpileMarket> markets);
        public static event UpsertMarketStands OnUpsertMarketStands;

        /* Gebäude */
        public delegate IEnumerable<DBBuilding> GetAllBuildings();
        public static event GetAllBuildings OnGetAllBuildings;

        public delegate DBBuilding GetBuilding(int zoneId);
        public static event GetBuilding OnGetBuilding;

        public delegate DBBuilding CreateOrSaveBuilding(PEBuilding building);
        public static event CreateOrSaveBuilding OnCreateOrSaveBuilding;

        /* Einfluss */
        public delegate int GetPlayerInfluence(string playerId);
        public static event GetPlayerInfluence OnGetPlayerInfluence;

        public delegate void UpdatePlayerInfluence(string playerId, int influence);
        public static event UpdatePlayerInfluence OnUpdatePlayerInfluence;

        /* Autosave */
        public static void AutoSaveJob(List<NetworkCommunicator> peers)
        {
            Debug.Print($"[Persistent Empires Auto Save] Speichere {peers.Count} Spieler", 0, Debug.DebugColor.Blue);
            HandleCreateOrSavePlayers(peers);
        }

        public static IEnumerable<DBBuilding> HandleGetAllBuildings()
        {
            return OnGetAllBuildings?.Invoke();
        }

        public static DBBuilding HandleGetBuilding(int zoneId)
        {
            return OnGetBuilding?.Invoke(zoneId);
        }

        public static DBBuilding HandleCreateOrSaveBuilding(PEBuilding building)
        {
            return OnCreateOrSaveBuilding?.Invoke(building);
        }

        public static void HandleSetFactionTax(string factionName, float taxRate)
        {
            OnSetFactionTax?.Invoke(factionName, taxRate);
        }

        public static float HandleGetFactionTax(string factionName)
        {
            return OnGetFactionTax?.Invoke(factionName) ?? 0.05f;
        }

        public static int HandleGetPlayerInfluence(string playerId)
        {
            return OnGetPlayerInfluence?.Invoke(playerId) ?? 100;
        }

        public static void HandleUpdatePlayerInfluence(string playerId, int influence)
        {
            OnUpdatePlayerInfluence?.Invoke(playerId, influence);
        }

        public override void OnMissionResultReady(MissionResult missionResult)
        {
            base.OnMissionResultReady(missionResult);
            foreach (NetworkCommunicator peer in GameNetwork.NetworkPeers)
            {
                if (peer.ControlledAgent != null)
                {
                    HandleCreateOrSavePlayer(peer);
                }
            }
        }

        public override void OnBehaviorInitialize()
        {
            base.OnBehaviorInitialize();
            AppDomain.CurrentDomain.ProcessExit += HandleApplicationExit;

            SaveDuration = ConfigManager.GetIntConfig("AutosaveDuration", 600);
            HandleStartMigration();
        }

        public override void OnMissionTick(float dt)
        {
            base.OnMissionTick(dt);
            if (DateTimeOffset.Now.ToUnixTimeSeconds() > LastSaveAt + SaveDuration)
            {
                Task.Run(() =>
                {
                    try
                    {
                        InformationComponent.Instance.BroadcastMessage("* Autosave gestartet. Kann kurz laggen.", Colors.Blue.ToUnsignedInteger());
                        AutoSaveJob(GameNetwork.NetworkPeers.Where(x => x.ControlledAgent != null).ToList());
                        LastSaveAt = DateTimeOffset.Now.ToUnixTimeSeconds();
                    }
                    catch (Exception ex)
                    {
                        Debug.Print($"* Fehler beim Autosave! {ex.Message}", Debug.DebugColor.Red);
                        throw ex;
                    }
                });
            }
        }

        private void HandleApplicationExit(object sender, EventArgs e)
        {
            Debug.Print("! SERVER CRASH DETECTED. SPEICHERE SPIELERDATEN!");
            foreach (NetworkCommunicator peer in GameNetwork.NetworkPeers)
            {
                if (peer.ControlledAgent != null)
                {
                    HandleCreateOrSavePlayer(peer);
                }
            }
        }
    }
}

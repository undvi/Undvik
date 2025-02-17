using Dapper;
using PersistentEmpiresLib.Database.DBEntities;
using PersistentEmpiresLib.PersistentEmpiresMission.MissionBehaviors;
using PersistentEmpiresLib.SceneScripts;
using PersistentEmpiresLib.SceneScripts.Extensions;
using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Linq;
using TaleWorlds.Library;

namespace PersistentEmpiresSave.Database.Repositories
{
    public class DBMarketRepository
    {
        private static readonly string dbPath = "MarketDatabase.sqlite";

        public static void Initialize()
        {
            SaveSystemBehavior.OnGetAllStockpileMarkets += GetAllMarketStands;
            SaveSystemBehavior.OnGetStockpileMarket += GetMarketStand;
            SaveSystemBehavior.OnUpsertStockpileMarkets += UpsertMarketStands;

            InitializeDatabase();
            CheckExpiredMarketStands();
        }

        /// <summary>
        /// Erstellt die Markt- und Gebäude-Datenbank, falls sie nicht existiert.
        /// </summary>
        private static void InitializeDatabase()
        {
            using (var connection = new SQLiteConnection($"Data Source={dbPath};Version=3;"))
            {
                connection.Open();
                connection.Execute("PRAGMA foreign_keys = ON;");

                using (var command = new SQLiteCommand(connection))
                {
                    Debug.Print("[DB] Initialisiere MarketDatabase...");

                    // Marktstände-Tabelle
                    command.CommandText = @"
                        CREATE TABLE IF NOT EXISTS MarketStands (
                            StandID INTEGER PRIMARY KEY, 
                            Owner TEXT, 
                            IsOccupied BOOLEAN, 
                            IsOwned BOOLEAN,
                            RentPrice INTEGER,
                            RentTime INTEGER,
                            RentStartDate TEXT,
                            TaxRate REAL
                        );";
                    command.ExecuteNonQuery();

                    // Fraktionssteuern-Tabelle
                    command.CommandText = @"
                        CREATE TABLE IF NOT EXISTS FactionTaxes (
                            FactionName TEXT PRIMARY KEY, 
                            TaxRate REAL
                        );";
                    command.ExecuteNonQuery();

                    // Gebäude-Tabelle
                    command.CommandText = @"
                        CREATE TABLE IF NOT EXISTS Buildings (
                            ZoneID INTEGER PRIMARY KEY, 
                            Faction TEXT, 
                            BuildingOwner TEXT,
                            BuildingType TEXT, 
                            IsCompleted BOOLEAN,
                            BuildingCostGold INTEGER,
                            BuildingCostInfluence INTEGER,
                            BuildingLevel INTEGER,
                            ConstructionProgress INTEGER,
                            IsPublic BOOLEAN
                        );";
                    command.ExecuteNonQuery();
                }
            }
        }

        /// <summary>
        /// Überprüft und setzt abgelaufene Marktstände zurück.
        /// </summary>
        private static void CheckExpiredMarketStands()
        {
            using (var connection = new SQLiteConnection($"Data Source={dbPath};Version=3;"))
            {
                connection.Open();
                var expiredStands = connection.Query<int>(
                    "SELECT StandID FROM MarketStands WHERE RentStartDate IS NOT NULL AND RentTime > 0 AND (strftime('%s','now') - strftime('%s', RentStartDate)) > RentTime"
                ).ToList();

                foreach (var standId in expiredStands)
                {
                    Debug.Print($"[DB] Marktstand {standId} Mietzeit abgelaufen, wird freigegeben.");
                    connection.Execute("UPDATE MarketStands SET Owner = NULL, IsOccupied = FALSE WHERE StandID = @StandID", new { StandID = standId });
                }
            }
        }

        /// <summary>
        /// Gibt alle Marktstände aus der Datenbank zurück.
        /// </summary>
        public static IEnumerable<DBStockpileMarket> GetAllMarketStands()
        {
            Debug.Print("[DB] Lade alle Marktstände...");
            using (var connection = new SQLiteConnection($"Data Source={dbPath};Version=3;"))
            {
                return connection.Query<DBStockpileMarket>("SELECT * FROM MarketStands");
            }
        }

        /// <summary>
        /// Lädt einen einzelnen Marktstand basierend auf der `MissionObjectHash`.
        /// </summary>
        public static DBStockpileMarket GetMarketStand(PE_StockpileMarket marketStand)
        {
            Debug.Print($"[DB] Lade Marktstand ({marketStand.GetMissionObjectHash()})");
            using (var connection = new SQLiteConnection($"Data Source={dbPath};Version=3;"))
            {
                return connection.QueryFirstOrDefault<DBStockpileMarket>(
                    "SELECT * FROM MarketStands WHERE MissionObjectHash = @MissionObjectHash",
                    new { MissionObjectHash = marketStand.GetMissionObjectHash() }
                );
            }
        }

        /// <summary>
        /// Fügt Marktstände zur Datenbank hinzu oder aktualisiert bestehende.
        /// </summary>
        public static void UpsertMarketStands(List<PE_StockpileMarket> markets)
        {
            Debug.Print($"[DB] Speichere {markets.Count} Marktstände...");
            if (markets.Any())
            {
                using (var connection = new SQLiteConnection($"Data Source={dbPath};Version=3;"))
                {
                    foreach (var market in markets)
                    {
                        connection.Execute(@"
                            INSERT INTO MarketStands (MissionObjectHash, MarketItemsSerialized) 
                            VALUES (@MissionObjectHash, @MarketItemsSerialized) 
                            ON CONFLICT(MissionObjectHash) DO UPDATE 
                            SET MarketItemsSerialized = excluded.MarketItemsSerialized",
                            new { market.MissionObjectHash, market.MarketItemsSerialized });
                    }
                }
            }
        }

        /// <summary>
        /// Speichert oder aktualisiert ein Gebäude in der Datenbank.
        /// </summary>
        public static void SaveBuilding(BuildingZone building)
        {
            using (var connection = new SQLiteConnection($"Data Source={dbPath};Version=3;"))
            {
                connection.Execute(@"
                    INSERT INTO Buildings (ZoneID, Faction, BuildingOwner, BuildingType, IsCompleted, BuildingCostGold, BuildingCostInfluence, BuildingLevel, ConstructionProgress, IsPublic) 
                    VALUES (@ZoneID, @Faction, @BuildingOwner, @BuildingType, @IsCompleted, @BuildingCostGold, @BuildingCostInfluence, @BuildingLevel, @ConstructionProgress, @IsPublic)
                    ON CONFLICT(ZoneID) DO UPDATE
                    SET BuildingType = excluded.BuildingType,
                        BuildingOwner = excluded.BuildingOwner,
                        IsCompleted = excluded.IsCompleted,
                        BuildingCostGold = excluded.BuildingCostGold,
                        BuildingCostInfluence = excluded.BuildingCostInfluence,
                        BuildingLevel = excluded.BuildingLevel,
                        ConstructionProgress = excluded.ConstructionProgress,
                        IsPublic = excluded.IsPublic",
                    building);
            }
        }
    }
}

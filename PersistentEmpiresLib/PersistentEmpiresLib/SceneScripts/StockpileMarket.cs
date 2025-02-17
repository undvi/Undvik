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
        }

        private static void InitializeDatabase()
        {
            using (var connection = new SQLiteConnection($"Data Source={dbPath};Version=3;"))
            {
                connection.Open();
                using (var command = new SQLiteCommand(connection))
                {
                    Debug.Print("[DB] Initialisiere MarketDatabase...");

                    command.CommandText = @"
                        CREATE TABLE IF NOT EXISTS MarketStands (
                            StandID INTEGER PRIMARY KEY, 
                            Owner TEXT, 
                            MarketType TEXT,
                            IsOwned BOOLEAN,
                            StorageCapacity INTEGER,
                            TaxRate REAL,
                            CustomPrices TEXT
                        );";
                    command.ExecuteNonQuery();

                    command.CommandText = @"
                        CREATE TABLE IF NOT EXISTS FactionTaxes (
                            FactionName TEXT PRIMARY KEY, 
                            TaxRate REAL,
                            WeaponTax REAL,
                            ArmorTax REAL,
                            HorseTax REAL,
                            FoodTax REAL
                        );";
                    command.ExecuteNonQuery();
                }
            }
        }

        public static IEnumerable<DBStockpileMarket> GetAllMarketStands()
        {
            Debug.Print("[DB] Lade alle Marktstände...");
            using (var connection = new SQLiteConnection($"Data Source={dbPath};Version=3;"))
            {
                return connection.Query<DBStockpileMarket>("SELECT * FROM MarketStands");
            }
        }

        public static DBStockpileMarket GetMarketStand(PE_StockpileMarket marketStand)
        {
            Debug.Print($"[DB] Lade Marktstand ({marketStand.GetMissionObjectHash()})");
            using (var connection = new SQLiteConnection($"Data Source={dbPath};Version=3;"))
            {
                return connection.QueryFirstOrDefault<DBStockpileMarket>(
                    "SELECT * FROM MarketStands WHERE StandID = @StandID",
                    new { StandID = marketStand.GetMissionObjectHash() }
                );
            }
        }

        public static void UpsertMarketStands(List<PE_StockpileMarket> markets)
        {
            Debug.Print($"[DB] Speichere {markets.Count} Marktstände...");
            using (var connection = new SQLiteConnection($"Data Source={dbPath};Version=3;"))
            {
                foreach (var market in markets)
                {
                    connection.Execute(@"
                        INSERT INTO MarketStands (StandID, Owner, MarketType, IsOwned, StorageCapacity, TaxRate, CustomPrices) 
                        VALUES (@StandID, @Owner, @MarketType, @IsOwned, @StorageCapacity, @TaxRate, @CustomPrices) 
                        ON CONFLICT(StandID) DO UPDATE 
                        SET Owner = excluded.Owner, MarketType = excluded.MarketType, IsOwned = excluded.IsOwned, 
                            StorageCapacity = excluded.StorageCapacity, TaxRate = excluded.TaxRate, CustomPrices = excluded.CustomPrices",
                        new
                        {
                            StandID = market.GetMissionObjectHash(),
                            Owner = market.Owner,
                            MarketType = market.MarketType,
                            IsOwned = market.IsOwned,
                            StorageCapacity = market.StorageCapacity,
                            TaxRate = market.TaxRate,
                            CustomPrices = market.SerializePrices()
                        });
                }
            }
        }

        public static void SetFactionTax(string factionName, float generalTax, float weaponTax, float armorTax, float horseTax, float foodTax)
        {
            using (var connection = new SQLiteConnection($"Data Source={dbPath};Version=3;"))
            {
                connection.Execute(@"
                    INSERT INTO FactionTaxes (FactionName, TaxRate, WeaponTax, ArmorTax, HorseTax, FoodTax) 
                    VALUES (@FactionName, @TaxRate, @WeaponTax, @ArmorTax, @HorseTax, @FoodTax)
                    ON CONFLICT(FactionName) DO UPDATE 
                    SET TaxRate = excluded.TaxRate, WeaponTax = excluded.WeaponTax, ArmorTax = excluded.ArmorTax, 
                        HorseTax = excluded.HorseTax, FoodTax = excluded.FoodTax",
                    new { FactionName = factionName, TaxRate = generalTax, WeaponTax = weaponTax, ArmorTax = armorTax, HorseTax = horseTax, FoodTax = foodTax });
            }
        }

        public static float GetFactionTax(string factionName, string marketType)
        {
            using (var connection = new SQLiteConnection($"Data Source={dbPath};Version=3;"))
            {
                var taxes = connection.QueryFirstOrDefault<(float GeneralTax, float WeaponTax, float ArmorTax, float HorseTax, float FoodTax)>(
                    "SELECT TaxRate, WeaponTax, ArmorTax, HorseTax, FoodTax FROM FactionTaxes WHERE FactionName = @FactionName",
                    new { FactionName = factionName }
                );
                return marketType switch
                {
                    "Weapons" => taxes.WeaponTax,
                    "Armor" => taxes.ArmorTax,
                    "Horses" => taxes.HorseTax,
                    "Food" => taxes.FoodTax,
                    _ => taxes.GeneralTax
                };
            }
        }
    }
}

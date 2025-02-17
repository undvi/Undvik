using Dapper;
using PersistentEmpiresLib.Database.DBEntities;
using PersistentEmpiresLib.PersistentEmpiresMission.MissionBehaviors;
using PersistentEmpiresLib.SceneScripts;
using PersistentEmpiresLib.SceneScripts.Extensions;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.Library;

namespace PersistentEmpiresSave.Database.Repositories
{
    public class DBUpgradeableBuildingRepository
    {
        public static void Initialize()
        {
            SaveSystemBehavior.OnGetAllUpgradeableBuildings += GetAllUpgradeableBuildings;
            SaveSystemBehavior.OnGetUpgradeableBuilding += GetUpgradeableBuilding;
            SaveSystemBehavior.OnCreateOrSaveUpgradebleBuilding += CreateOrSaveUpgradeableBuilding;
        }

        private static DBUpgradeableBuilding CreateDBUpgradeableBuilding(PE_UpgradeableBuildings upgradeableBuilding)
        {
            if (upgradeableBuilding == null)
            {
                Debug.Print("[Save Module] CREATE DB UPGRADEABLE BUILDING FAILED - Building is NULL!");
                return null;
            }

            Debug.Print($"[Save Module] CREATE DB UPGRADEABLE BUILDING ({upgradeableBuilding.GetMissionObjectHash()})");

            return new DBUpgradeableBuilding
            {
                CurrentTier = upgradeableBuilding.CurrentTier,
                IsUpgrading = upgradeableBuilding.IsUpgrading,
                MissionObjectHash = upgradeableBuilding.GetMissionObjectHash()
            };
        }

        public static DBUpgradeableBuilding CreateOrSaveUpgradeableBuilding(PE_UpgradeableBuildings upgradeableBuilding)
        {
            if (upgradeableBuilding == null)
            {
                Debug.Print("[Save Module] ERROR: Trying to save a NULL upgradeable building!");
                return null;
            }

            return GetUpgradeableBuilding(upgradeableBuilding) ?? CreateUpgradeableBuilding(upgradeableBuilding);
        }

        public static DBUpgradeableBuilding CreateUpgradeableBuilding(PE_UpgradeableBuildings upgradeableBuilding)
        {
            if (upgradeableBuilding == null)
            {
                Debug.Print("[Save Module] ERROR: Trying to create a NULL upgradeable building!");
                return null;
            }

            Debug.Print($"[Save Module] CREATE UPGRADEABLE BUILDING TO DB ({upgradeableBuilding.GetMissionObjectHash()})");

            DBUpgradeableBuilding db = CreateDBUpgradeableBuilding(upgradeableBuilding);
            if (db == null) return null;

            const string insertSql = "INSERT INTO UpgradeableBuildings (MissionObjectHash, CurrentTier, IsUpgrading) VALUES (@MissionObjectHash, @CurrentTier, @IsUpgrading)";
            DBConnection.Connection.Execute(insertSql, db);

            Debug.Print($"[Save Module] CREATED UPGRADEABLE BUILDING IN DB ({upgradeableBuilding.GetMissionObjectHash()})");

            return db;
        }

        public static DBUpgradeableBuilding SaveUpgradeableBuilding(PE_UpgradeableBuildings upgradeableBuilding)
        {
            if (upgradeableBuilding == null)
            {
                Debug.Print("[Save Module] ERROR: Trying to update a NULL upgradeable building!");
                return null;
            }

            Debug.Print($"[Save Module] UPDATE UPGRADEABLE BUILDING TO DB ({upgradeableBuilding.GetMissionObjectHash()})");

            DBUpgradeableBuilding db = CreateDBUpgradeableBuilding(upgradeableBuilding);
            if (db == null) return null;

            const string updateSql = "UPDATE UpgradeableBuildings SET CurrentTier = @CurrentTier, IsUpgrading = @IsUpgrading WHERE MissionObjectHash = @MissionObjectHash";
            DBConnection.Connection.Execute(updateSql, db);

            Debug.Print($"[Save Module] UPDATED UPGRADEABLE BUILDING IN DB ({upgradeableBuilding.GetMissionObjectHash()})");

            return db;
        }

        public static IEnumerable<DBUpgradeableBuilding> GetAllUpgradeableBuildings()
        {
            Debug.Print("[Save Module] FETCHING ALL UPGRADEABLE BUILDINGS");
            return DBConnection.Connection.Query<DBUpgradeableBuilding>("SELECT * FROM UpgradeableBuildings");
        }

        public static DBUpgradeableBuilding GetUpgradeableBuilding(PE_UpgradeableBuildings upgradeableBuilding)
        {
            if (upgradeableBuilding == null)
            {
                Debug.Print("[Save Module] ERROR: Trying to get a NULL upgradeable building!");
                return null;
            }

            Debug.Print($"[Save Module] LOADING UPGRADEABLE BUILDING FROM DB ({upgradeableBuilding.GetMissionObjectHash()})");

            var result = DBConnection.Connection.Query<DBUpgradeableBuilding>(
                "SELECT * FROM UpgradeableBuildings WHERE MissionObjectHash = @MissionObjectHash",
                new { MissionObjectHash = upgradeableBuilding.GetMissionObjectHash() }
            );

            if (!result.Any())
            {
                Debug.Print($"[Save Module] NO RECORD FOUND FOR ({upgradeableBuilding.GetMissionObjectHash()})");
                return null;
            }

            Debug.Print($"[Save Module] SUCCESS: UPGRADEABLE BUILDING FOUND ({upgradeableBuilding.GetMissionObjectHash()})");
            return result.FirstOrDefault();
        }
    }
}

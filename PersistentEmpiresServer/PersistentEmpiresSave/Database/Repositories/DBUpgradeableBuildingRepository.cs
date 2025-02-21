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
            Debug.Print("[Save Module] CREATE UPGRADEABLE BUILDING TO DB ({upgradeableBuilding.GetMissionObjectHash()})");
            DBUpgradeableBuilding db = new DBUpgradeableBuilding
            {
                CurrentTier = upgradeableBuilding.CurrentTier,
                IsUpgrading = upgradeableBuilding.IsUpgrading,
                MissionObjectHash = upgradeableBuilding.GetMissionObjectHash()
            };
            const string insertSql = "INSERT INTO UpgradeableBuildings (MissionObjectHash, CurrentTier, IsUpgrading) VALUES (@MissionObjectHash, @CurrentTier, @IsUpgrading)";
            DBConnection.Connection.Execute(insertSql, db);
            return db;
        }

        public static IEnumerable<DBUpgradeableBuilding> GetAllUpgradeableBuildings()
        {
            Debug.Print("[Save Module] FETCHING ALL UPGRADEABLE BUILDINGS");
            return DBConnection.Connection.Query<DBUpgradeableBuilding>("SELECT * FROM UpgradeableBuildings");
        }
    }
}
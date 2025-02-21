// File: DBUpgradeableBuilding.cs
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PersistentEmpiresLib.Database.DBEntities
{
    [Table("UpgradeableBuildings")]
    public class DBUpgradeableBuilding
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required]
        [StringLength(128)]
        public string MissionObjectHash { get; set; }

        public bool IsUpgrading { get; set; }

        [Range(0, 3)]
        public int CurrentTier { get; set; }

        [DatabaseGenerated(DatabaseGeneratedOption.Computed)]
        public DateTime LastUpdated { get; set; } = DateTime.UtcNow;

        [Required]
        public int RequiredWood { get; set; }

        [Required]
        public int RequiredStone { get; set; }

        [Required]
        public int RequiredIron { get; set; }

        [Required]
        public string RequiredTool { get; set; }

        [Required]
        public int FactionIndex { get; set; }

        [Required]
        public int MaintenanceGold { get; set; }

        [Required]
        public int MaintenanceWood { get; set; }
    }
}
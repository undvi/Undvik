using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PersistentEmpiresLib.Database.DBEntities
{
    [Table("UpgradeableBuildings")] // Stellt sicher, dass der Tabellenname konsistent ist
    public class DBUpgradeableBuilding
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; } // Primärschlüssel (auto-increment)

        [Required]
        [StringLength(128)] // Begrenzung für Performance-Optimierung
        public string MissionObjectHash { get; set; }

        public bool IsUpgrading { get; set; }

        [Range(0, 3)] // Begrenzung, damit nur Tier 0-3 erlaubt sind
        public int CurrentTier { get; set; }

        [DatabaseGenerated(DatabaseGeneratedOption.Computed)]
        public DateTime LastUpdated { get; set; } = DateTime.UtcNow; // Automatische Aktualisierung für Logging
    }
}

namespace PersistentEmpiresLib.Database.DBEntities
{
    public class DBFactions
    {
        public int Id { get; set; }
        public int FactionIndex { get; set; }
        public string Name { get; set; }
        public string BannerKey { get; set; }
        public string LordId { get; set; }
        public long PollUnlockedAt { get; set; }
        public string Marshalls { get; set; }

        // Neue Felder für das Rangsystem
        public int Rank { get; set; } // Rang der Fraktion (1-5)
        public int Gold { get; set; } // Fraktionsvermögen
        public int MaxMembers { get; set; } // Maximale Mitgliederanzahl basierend auf Rang
    }
}

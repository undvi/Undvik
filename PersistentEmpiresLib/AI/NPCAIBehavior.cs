using System;
using System.Collections.Generic;
using Newtonsoft.Json; // Für JSON-Operationen (über NuGet-Paket Newtonsoft.Json)

namespace PersistentEmpiresLib.AI
{
    /// <summary>
    /// Mögliche Zustände, in denen sich ein NPC befinden kann.
    /// </summary>
    public enum NPCAIState
    {
        Idle,
        GatherWood,
        GuardCity,
        Travel
    }

    /// <summary>
    /// Repräsentiert das Verhalten eines NPC basierend auf einer einfachen Zustandsmaschine.
    /// </summary>
    public class NPCAIBehavior
    {
        public string NpcId { get; private set; }
        public NPCAIState CurrentState { get; private set; }
        public Dictionary<string, int> Inventory { get; private set; }

        public NPCAIBehavior(string npcId)
        {
            NpcId = npcId;
            CurrentState = NPCAIState.Idle;
            Inventory = new Dictionary<string, int>();
        }

        /// <summary>
        /// Diese Methode sollte in regelmäßigen Ticks (z. B. pro Frame oder festgelegtem Zeitintervall) aufgerufen werden.
        /// </summary>
        /// <param name="deltaTime">Die verstrichene Zeit seit dem letzten Update.</param>
        public void Update(float deltaTime)
        {
            switch (CurrentState)
            {
                case NPCAIState.Idle:
                    EvaluateNextState();
                    break;
                case NPCAIState.GatherWood:
                    PerformGatherWood(deltaTime);
                    break;
                case NPCAIState.GuardCity:
                    PerformGuardCity(deltaTime);
                    break;
                case NPCAIState.Travel:
                    PerformTravel(deltaTime);
                    break;
            }
        }

        /// <summary>
        /// Entscheidet, in welchen Zustand der NPC als Nächstes wechseln soll.
        /// Hier erfolgt eine einfache Logik, die in einer echten Anwendung erweitert werden kann.
        /// </summary>
        private void EvaluateNextState()
        {
            // Beispiel: Immer in den "GatherWood"-Zustand wechseln, wenn Idle.
            Console.WriteLine($"{NpcId}: Wechsel von Idle zu GatherWood.");
            CurrentState = NPCAIState.GatherWood;
        }

        /// <summary>
        /// Simuliert das Sammeln von Holz.
        /// </summary>
        /// <param name="deltaTime">Verstrichene Zeit zur Simulation der Arbeitsdauer.</param>
        private void PerformGatherWood(float deltaTime)
        {
            Console.WriteLine($"{NpcId}: Sammle Holz...");
            // Holz im Inventar erhöhen
            if (Inventory.ContainsKey("wood"))
                Inventory["wood"] += 1;
            else
                Inventory["wood"] = 1;

            // Beispielbedingung: Nach 5 Holzeinheiten Aufgabe abschließen und in Idle zurückkehren
            if (Inventory["wood"] >= 5)
            {
                Console.WriteLine($"{NpcId}: Holz sammeln abgeschlossen, wechsle zu Idle.");
                CurrentState = NPCAIState.Idle;
            }
        }

        /// <summary>
        /// Simuliert das Bewachen einer Stadt.
        /// </summary>
        /// <param name="deltaTime">Verstrichene Zeit zur Simulation der Aufgabe.</param>
        private void PerformGuardCity(float deltaTime)
        {
            Console.WriteLine($"{NpcId}: Bewache die Stadt...");
            // Nach Abschluss der Aufgabe wieder zu Idle wechseln
            CurrentState = NPCAIState.Idle;
        }

        /// <summary>
        /// Simuliert das Reisen.
        /// </summary>
        /// <param name="deltaTime">Verstrichene Zeit zur Simulation der Reise.</param>
        private void PerformTravel(float deltaTime)
        {
            Console.WriteLine($"{NpcId}: Reise...");
            // Nach Erreichen des Zielorts wieder zu Idle wechseln
            CurrentState = NPCAIState.Idle;
        }
    }

    /// <summary>
    /// Eine einfache Datenbank-Klasse zum Laden von NPC-Daten aus einer JSON-Struktur.
    /// </summary>
    public class AIDatabase
    {
        /// <summary>
        /// Lädt einen NPC aus einem JSON-String.
        /// </summary>
        /// <param name="json">JSON-Daten, die den NPC beschreiben.</param>
        /// <returns>Ein NPCAIBehavior-Objekt, falls erfolgreich; sonst null.</returns>
        public static NPCAIBehavior LoadNPCFromJson(string json)
        {
            try
            {
                var npcData = JsonConvert.DeserializeObject<NPCData>(json);
                var npcAI = new NPCAIBehavior(npcData.npc_id);
                // Weitere Initialisierungen können hier erfolgen, z. B. das Befüllen des Inventars
                return npcAI;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Fehler beim Laden der NPC-Daten: " + ex.Message);
                return null;
            }
        }

        /// <summary>
        /// Beispielhafte Datenstruktur für einen NPC gemäß der Roadmap.
        /// </summary>
        public class NPCData
        {
            public string npc_id { get; set; }
            public string npc_type { get; set; }
            public string faction_id { get; set; }
            public string current_task { get; set; }
            public Location location { get; set; }
            public Dictionary<string, int> inventory { get; set; }
            public Schedule schedule { get; set; }
            public string state { get; set; }
        }

        public class Location
        {
            public string scene { get; set; }
            public Position position { get; set; }
        }

        public class Position
        {
            public int x { get; set; }
            public int y { get; set; }
        }

        public class Schedule
        {
            public string work_start { get; set; }
            public string work_end { get; set; }
        }
    }
}

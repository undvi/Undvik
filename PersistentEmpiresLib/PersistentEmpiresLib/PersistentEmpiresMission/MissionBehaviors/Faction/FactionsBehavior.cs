using PersistentEmpiresLib.Factions;
using PersistentEmpiresLib.Helpers;
using PersistentEmpiresLib.NetworkMessages.Client;
using PersistentEmpiresLib.NetworkMessages.Server;
using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace PersistentEmpiresLib.PersistentEmpiresMission.MissionBehaviors
{
    public class FactionsBehavior : MissionNetwork
    {
        public bool CanDeclareWar(Faction faction, Player player)
        {
            return player.IsNoble() && player.FactionRank >= 3 && faction.IsAtPeace();
        }

        public void DeclareWar(Faction declaringFaction, Faction targetFaction, Player player)
        {
            if (!CanDeclareWar(declaringFaction, player))
            {
                Console.WriteLine("Nur Adelige mit Rang 3 oder höher können den Krieg erklären!");
                return;
            }
            if (!StartWarVote(declaringFaction, targetFaction))
            {
                Console.WriteLine("Die Fraktion hat gegen den Krieg gestimmt!");
                return;
            }
            declaringFaction.SetWarStatus(targetFaction, true);
            Console.WriteLine($"{declaringFaction.Name} hat {targetFaction.Name} den Krieg erklärt!");
        }

        public bool StartWarVote(Faction declaringFaction, Faction targetFaction)
        {
            // Simpler Mehrheitsentscheidungsmechanismus
            int votesForWar = 0;
            int votesAgainstWar = 0;
            foreach (var member in declaringFaction.Members)
            {
                if (member.FactionRank >= 2) // Nur Adelige dürfen abstimmen
                {
                    if (member.WantsWar(targetFaction))
                        votesForWar++;
                    else
                        votesAgainstWar++;
                }
            }
            return votesForWar > votesAgainstWar;
        }
    }
}

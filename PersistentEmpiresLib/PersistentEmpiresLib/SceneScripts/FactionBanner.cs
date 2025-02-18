using PersistentEmpiresLib.Factions;
using PersistentEmpiresLib.Helpers;
using System;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;

namespace PersistentEmpiresLib.SceneScripts
{
    public class PEFactionBanner : MissionObject
    {
        public int FactionIndex { get; private set; }
        public string FactionName { get; private set; }
        public string BannerKey { get; private set; }

        public void InitializeBanner(int factionIndex, string factionName)
        {
            this.FactionIndex = factionIndex;
            this.FactionName = factionName;

            Faction faction = FactionManager.GetFactionByIndex(factionIndex);
            if (faction != null)
            {
                SetBannerKey(faction.BannerKey);
            }
            else
            {
                SetBannerKey("11.45.126.4345.4345.764.764.1.0.0.462.0.13.512.512.764.764.1.0.0"); // Default Banner
            }
        }

        public string GetBannerKey()
        {
            return this.BannerKey;
        }

        public void SetBannerKey(string newBannerKey)
        {
            if (string.IsNullOrEmpty(newBannerKey) || newBannerKey.Length < 10)
            {
                InformationManager.DisplayMessage(new InformationMessage($"⚠️ Ungültiger Banner-Key für Fraktion {FactionName}!"));
                return;
            }

            this.BannerKey = newBannerKey;
            BannerRenderer.RequestRenderBanner(new Banner(this.BannerKey), base.GameEntity);

            InformationManager.DisplayMessage(new InformationMessage($"✅ Banner für {FactionName} aktualisiert!"));
        }

        public void UpdateBannerFromFaction()
        {
            Faction faction = FactionManager.GetFactionByIndex(this.FactionIndex);
            if (faction != null && faction.BannerKey != this.BannerKey)
            {
                SetBannerKey(faction.BannerKey);
            }
        }

        public void UpdateBannerForVassals()
        {
            Faction faction = FactionManager.GetFactionByIndex(this.FactionIndex);
            if (faction != null && faction.LeaderFaction != null)
            {
                SetBannerKey(faction.LeaderFaction.BannerKey);
                InformationManager.DisplayMessage(new InformationMessage($"🏳️ Vasallen-Banner für {FactionName} übernommen!"));
            }
        }
    }
}

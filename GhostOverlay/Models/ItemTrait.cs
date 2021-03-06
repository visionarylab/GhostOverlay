﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GhostSharper.Models;

namespace GhostOverlay.Models
{
    class ItemTrait
    {
        private static List<DestinyTraitCategoryDefinition> allTraitCategoryDefinitions;
        
        /*private static readonly Dictionary<string, string> CustomTraitNames = new Dictionary<string, string>
        {
            {"inventory_filtering.quest", "All Quests"},
            {"inventory_filtering.bounty", "Bounties"},
            {"inventory_filtering.quest.featured", "Featured Quests"},
        };

        private static readonly Dictionary<string, Uri> CustomIcons = new Dictionary<string, Uri>
        {
            {"inventory_filtering.bounty", new Uri("ms-appx:///Assets/QuestTraitIcons/bounties.png")},
            {"inventory_filtering.quest", new Uri("ms-appx:///Assets/QuestTraitIcons/quests.png")},

            {"quest.new_light", new Uri("ms-appx:///Assets/QuestTraitIcons/new_light.png")},
            {"inventory_filtering.quest.featured", new Uri("ms-appx:///Assets/QuestTraitIcons/new_light.png")},
            {"quest.current_release", new Uri("ms-appx:///Assets/QuestTraitIcons/current_release.png")},
            {"quest.seasonal", new Uri("ms-appx:///Assets/QuestTraitIcons/seasonal.png")},
            {"quest.playlists", new Uri("ms-appx:///Assets/QuestTraitIcons/playlists.png")},
            {"quest.exotic", new Uri("ms-appx:///Assets/QuestTraitIcons/exotic.png")},
            {"quest.past", new Uri("ms-appx:///Assets/QuestTraitIcons/past.png")}
        };*/

        public string TraitId;
        public DestinyTraitDefinition Definition;
        public DestinyTraitCategoryDefinition TraitCategoryDefinition;

        public bool HasIcon => GetTraitConfigForTraitId(TraitId)?.Icon != null;
        public Uri IconUri => HasIcon ? GetTraitConfigForTraitId(TraitId).Icon : CommonHelpers.LocalFallbackIconUri;

        public string Name
        {
            get
            {
                var name = Definition?.DisplayProperties?.Name ?? "";
                var nearlyName = name == "" ? TraitId : name;

                var traitConfig = GetTraitConfigForTraitId(TraitId);
                return traitConfig?.Name ?? nearlyName;
            }
        }

        public async Task<DestinyTraitDefinition> PopulateDefinition()
        {
            if (allTraitCategoryDefinitions == null)
            {
                allTraitCategoryDefinitions = await Definitions.GetAllTraitCategory();
            }

            TraitCategoryDefinition = allTraitCategoryDefinitions.FirstOrDefault(v => v.TraitIds.Contains(TraitId));

            if (TraitCategoryDefinition == null)
            {
                return default;
            }

            var index = TraitCategoryDefinition.TraitIds.IndexOf(TraitId);
            var traitHash = TraitCategoryDefinition.TraitHashes[index];

            Definition = await Definitions.GetTrait(traitHash);

            return Definition;
        }

        private static TraitConfig GetTraitConfigForTraitId(string traitId)
        {
            if (AppState.RemoteConfig?.TraitData?.ContainsKey(traitId) ?? false)
            {
                return AppState.RemoteConfig.TraitData[traitId];
            }

            return default;
        }
    }
}

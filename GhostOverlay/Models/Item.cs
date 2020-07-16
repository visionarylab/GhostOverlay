﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading.Tasks;
using GhostSharper.Models;

namespace GhostOverlay.Models
{
    public class Item : ITrackable
    {
        public static long PersuitsBucketHash = 1345459588;
        public static string QuestTraitId = "inventory_filtering.quest";
        public static string BountyTraitId = "inventory_filtering.bounty";

        public long ItemHash = 0;
        public long ItemInstanceId = 0;
        public TrackedEntry TrackedEntry { get; set; }

        public DestinyInventoryItemDefinition Definition = new DestinyInventoryItemDefinition();
        public DestinyDisplayPropertiesDefinition DisplayProperties => Definition.DisplayProperties;
        public List<Objective> Objectives { get; set; }
        public Character OwnerCharacter;
        public bool IsCompleted => Objectives?.TrueForAll(v => v.Progress.Complete) ?? false;

        public bool ShowDescription =>
            !IsCompleted && ((TrackedEntry?.ShowDescription ?? true) || AppState.Data.ShowDescriptions.Value);

        public string GroupByKey => OwnerCharacter.ClassName;

        public string Title =>
            Definition?.SetData?.QuestLineName ?? Definition?.DisplayProperties?.Name ?? "No name";

        public Uri ImageUri => new Uri($"https://www.bungie.net{Definition?.DisplayProperties?.Icon ?? "/img/misc/missing_icon_d2.png"}");

        public string SortValue {
            get
            {
                var value = 100;

                if (IsCompleted)
                    value += 100;

                return value.ToString();
            }
        }

        public string Subtitle => Definition?.ItemTypeDisplayName ?? "Pursuit";

        public async Task<DestinyInventoryItemDefinition> PopulateDefinition()
        {
            Definition = await Definitions.GetInventoryItem(ItemHash);

            return Definition;
        }

        public static async Task<Item> ItemFromItemComponent(DestinyItemComponent item, DestinyProfileResponse profile, Character ownerCharacter = default)
        {
            var uninstancedObjectivesData =
                ownerCharacter == null
                ?  new Dictionary<string, DestinyItemObjectivesComponent>()
                : profile.CharacterUninstancedItemComponents[ownerCharacter.CharacterComponent.CharacterId.ToString()].Objectives.Data;

            var objectives = new List<DestinyObjectiveProgress>();
            var itemInstanceId = item.ItemInstanceId.ToString();
            var itemHash = item.ItemHash.ToString();

            if (profile.ItemComponents.Objectives.Data.ContainsKey(itemInstanceId))
            {
                objectives.AddRange(profile.ItemComponents.Objectives.Data[itemInstanceId]?.Objectives);
            }

            if (item.ItemInstanceId.Equals(0) && uninstancedObjectivesData.ContainsKey(itemHash))
            {
                objectives.AddRange(uninstancedObjectivesData[itemHash].Objectives);
            }

            if (objectives.Count == 0)
            {
                return new Item();
            }

            var bounty = new Item()
            {
                ItemHash = item.ItemHash,
                ItemInstanceId = item.ItemInstanceId,
                OwnerCharacter = ownerCharacter,
                Objectives = new List<Objective>()
            };
            
            await bounty.PopulateDefinition();

            foreach (var destinyQuestsDestinyObjectiveProgress in objectives)
            {
                var obj = new Objective { Progress = destinyQuestsDestinyObjectiveProgress };
                await obj.PopulateDefinition();
                bounty.Objectives.Add(obj);
            }

            return bounty;
        }

        public static async Task<List<Item>> ItemsFromProfile(DestinyProfileResponse profile, Character activeCharacter)
        {
            var bounties = new List<Item>();

            foreach (var inventoryKv in profile.CharacterInventories.Data)
            {
                var characterId = inventoryKv.Key;
                var inventory = inventoryKv.Value;

                if (characterId != activeCharacter.CharacterId.ToString())
                {
                    continue;
                }

                var character = new Character { CharacterComponent = profile.Characters.Data[characterId] };
                await character.PopulateDefinition();

                foreach (var inventoryItem in inventory.Items)
                {
                    if (inventoryItem.BucketHash != PersuitsBucketHash)
                        continue;

                    var bounty = await ItemFromItemComponent(inventoryItem, profile, character);

                    if (bounty.Objectives?.Count > 0)
                    {
                        bounties.Add(bounty);
                    }
                }
            }

            //foreach (var inventoryItem in profile.ProfileInventory.Data.Items)
            //{
            //    var bounty = await ItemFromItemComponent(inventoryItem, profile);

            //    if (bounty.Objectives?.Count > 0 && (addCompletedBounties || !bounty.IsCompleted))
            //    {
            //        bounties.Add(bounty);
            //    }
            //}

            return bounties;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public virtual void NotifyPropertyChanged(string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}

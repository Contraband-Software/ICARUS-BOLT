using Resources.Firmware;
using Resources.Modules;
using Resources.System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using System.IO;
using Helpers;
using System;

namespace ProgressionV2
{
    public static class LootGen
    {
        private struct LootCandidate
        {
            public AbstractUpgradeAsset Asset;
            public int Tier;
            public float Rarity;
            public float PointValue;
        }


        public static List<ItemData> GenerateLoot(
            int maxLoot,
            float maxPointBudget,
            float biasMeanPoint = 0.0f,
            float biasSigma = 1.0f,
            float biasStrength = 1.0f
            )
        {
            List<LootCandidate> candidates = GenerateLootCandidates();
            var weightMap = CalculateWeights(candidates, biasMeanPoint, biasSigma, biasStrength);
            foreach (var kvp in weightMap)
            {
                LootCandidate c = kvp.Key;
                float weight = kvp.Value;
            }

            List<LootCandidate> chosen = new List<LootCandidate>();
            HashSet<AbstractUpgradeAsset> pickedAssets = new HashSet<AbstractUpgradeAsset>();
            float remainingBudget = maxPointBudget;
            for(int i = 0; i < maxLoot && remainingBudget > 0; i++)
            {
                // filter out items that cost too much or share an Asset with a chosen item
                var filtered = weightMap
                    .Where(kvp => kvp.Key.PointValue <= remainingBudget
                                  && !pickedAssets.Contains(kvp.Key.Asset))
                    .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);

                if (filtered.Count == 0)
                    break;
                LootCandidate pick = PickWeightedRandom(filtered);
                chosen.Add(pick);
                pickedAssets.Add(pick.Asset);
                remainingBudget -= pick.PointValue;
            }

            List<ItemData> output = new List<ItemData>();

            foreach(LootCandidate loot in chosen)
            {
                
                if(loot.Asset is FirmwareUpgradeAsset)
                {
                    FirmwareData f = new FirmwareData();
                    f.firmwareId = loot.Asset.Id;
                    f.isActive = false;
                    f.slotId = -1;
                    f.tier = loot.Tier;
                    output.Add(f);
                }
                else if(loot.Asset is ModuleUpgradeAsset)
                {
                    ModuleData m = new ModuleData();
                    m.moduleId = loot.Asset.Id;
                    m.isActive = false;
                    m.slotId = -1;
                    output.Add(m);
                }
            }

            return output;
        }

        private static List<LootCandidate> GenerateLootCandidates()
        {
            List<LootCandidate> candidates = new List<LootCandidate>();
            foreach (FirmwareUpgradeAsset firmwareAsset in ItemStore.AllFirmwareAssets)
            {
                candidates.AddRange(MakeCandidatesForFirmwareTiers(firmwareAsset));
            }
            foreach (ModuleUpgradeAsset moduleAsset in ItemStore.AllModuleAssets)
            {
                candidates.Add(new LootCandidate
                {
                    Asset = moduleAsset,
                    Tier = 1,
                    Rarity = moduleAsset.GetRarity(),
                    PointValue = moduleAsset.GetPointValue()
                });
            }

            return candidates;
        }

        /// <summary>
        /// Roulette wheel style selection
        /// </summary>
        /// <param name="weightMap"></param>
        /// <returns></returns>
        private static LootCandidate PickWeightedRandom(Dictionary<LootCandidate, float> weightMap)
        {
            float total = weightMap.Values.Sum();           // ≈1 but avoids rounding issue
            float r = UnityEngine.Random.value * total;     // scale by total
            float cumulative = 0f;
            foreach (var kvp in weightMap)
            {
                cumulative += kvp.Value;
                if (r <= cumulative)
                    return kvp.Key;
            }
            return weightMap.Keys.Last(); // now this should basically never be hit
        }

        private static List<LootCandidate> MakeCandidatesForFirmwareTiers(
            FirmwareUpgradeAsset firmwareUpgradeAsset)
        {
            List<LootCandidate> candidates = new List<LootCandidate>();
            for(int tier = 1; tier <= firmwareUpgradeAsset.MaxTier; tier++)
            {
                candidates.Add(new LootCandidate
                {
                    Asset = firmwareUpgradeAsset,
                    Tier = tier,
                    Rarity = firmwareUpgradeAsset.GetRarity(tier),
                    PointValue = firmwareUpgradeAsset.GetPointValue(tier)
                });
            }

            return candidates;
        }

        // - candidates: your list of LootCandidate (has Rarity and PointValue)
        // - meanPoint: target mean (μ) in same units as PointValue (can be non-integer)
        // - sigma: standard deviation (σ). Must be > 0. Larger sigma = wider bump.
        // - strength: multiplier at mean. 1.0 = no bias, >1 favors items near mean.
        private static Dictionary<LootCandidate, float> CalculateWeights(
            List<LootCandidate> candidates,
            float biasMeanPoint = 0f,
            float biasSigma = 1.0f,
            float biasStrength = 1.0f)
        {
            // safety
            if (candidates == null || candidates.Count == 0)
                return new Dictionary<LootCandidate, float>();

            biasSigma = Mathf.Max(1e-4f, biasSigma); // avoid divide-by-zero
            biasStrength = Mathf.Max(0f, biasStrength); // don't allow negative strength

            // Precompute g(x) = exp(-0.5 * ((x-mean)/sigma)^2)
            // and multiplier = 1 + (strength - 1) * g(x)
            var rawScores = new List<float>(candidates.Count);
            float totalRaw = 0f;

            for (int i = 0; i < candidates.Count; i++)
            {
                var c = candidates[i];

                // base weight from rarity (higher rarity -> rarer -> lower weight)
                float baseWeight = 1f / c.Rarity;

                // gaussian kernel (peak at meanPoint)
                float z = (c.PointValue - biasMeanPoint) / biasSigma;
                float g = Mathf.Exp(-0.5f * z * z); // in (0,1], g(mean)=1

                // map to multiplier so that multiplier(mean) == strength
                float multiplier = 1f + (biasStrength - 1f) * g;

                float raw = baseWeight * multiplier;
                rawScores.Add(raw);
                totalRaw += raw;
            }

            // Normalize
            var dict = new Dictionary<LootCandidate, float>(candidates.Count);
            if (totalRaw <= 0f)
            {
                // fallback: evenly distribute
                float equal = 1f / candidates.Count;
                foreach (var c in candidates) dict[c] = equal;
                return dict;
            }

            for (int i = 0; i < candidates.Count; i++)
            {
                dict[candidates[i]] = rawScores[i] / totalRaw;
            }
            return dict;
        }

        #region DEBUGGING
        public static void LootRarityReport(
            string outputFilePath,
            float biasMeanPoint = 0.0f,
            float biasSigma = 1.0f,
            float biasStrength = 1.0f) 
        {
            List<LootCandidate> candidates = GenerateLootCandidates();
            var weightMap = CalculateWeights(candidates, biasMeanPoint, biasSigma, biasStrength);

            StringBuilder sb = new StringBuilder();
            sb.AppendLine("Item,Value,DropChance");

            var ordered = weightMap
                .OrderBy(kvp => kvp.Key.Asset is ModuleUpgradeAsset ? 1 : 0) // firmwares first
                .ThenBy(kvp => kvp.Key.Asset.FullName)
                .ThenBy(kvp => kvp.Key.Tier);

            foreach (var kvp in ordered)
            {
                LootCandidate c = kvp.Key;
                float percent = kvp.Value * 100f;

                string name = (c.Asset is ModuleUpgradeAsset)
                    ? c.Asset.FullName
                    : $"{c.Asset.FullName} Tier {RomanNumerals.ToRoman(c.Tier)}";

                sb.AppendLine($"{name},{c.Asset.GetPointValue(c.Tier)},{percent:0.#####}%");
            }

            File.WriteAllText(outputFilePath, sb.ToString());

            Debug.Log($"Loot rarity report written to: {outputFilePath}");
        }

        public static void SimulateLootDrops(
            int bursts,
            int lootPerBurst,
            int maxPointsPerBurst,
            string outputFilePath,
            float biasMeanPoint = 0.0f,
            float biasSigma = 1.0f,
            float biasStrength = 1.0f)
        {
            StringBuilder sb = new StringBuilder();
            Dictionary<LootKey, int> tally = new();
            List<string> burstLines = new();

            // --- Simulate ---
            for (int b = 1; b <= bursts; b++)
            {
                List<ItemData> loot = GenerateLoot(
                    lootPerBurst,
                    maxPointsPerBurst,
                    biasMeanPoint,
                    biasSigma,
                    biasStrength);

                foreach (var item in loot)
                {
                    AbstractUpgradeAsset asset = ItemStore.GetAsset(item);
                    if (asset == null) continue;

                    LootKey key = (item is FirmwareData fw)
                        ? new LootKey(asset, fw.tier)
                        : new LootKey(asset);

                    if (!tally.ContainsKey(key))
                        tally[key] = 0;
                    tally[key]++;

                    burstLines.Add($"{b},{key}");
                }
            }

            int totalDropped = tally.Values.Sum();

            // --- Tally Section ---
            sb.AppendLine("Item,Tally,PercentOfLoot");
            foreach (var entry in tally
                .OrderBy(kvp => kvp.Key.Asset is ModuleUpgradeAsset ? 1 : 0) // firmwares first
                .ThenBy(kvp => kvp.Key.Asset.FullName)
                .ThenBy(kvp => kvp.Key.Tier ?? 0))
            {
                float percent = totalDropped > 0 ? (entry.Value / (float)totalDropped) * 100f : 0f;
                sb.AppendLine($"{entry.Key},{entry.Value},{percent:F5}%");
            }

            // --- Summary ---
            sb.AppendLine();
            sb.AppendLine("Category,Tally,PercentOfLoot");

            // Group firmwares by tier
            var firmwareGroups = tally
                .Where(kvp => kvp.Key.Asset is FirmwareUpgradeAsset)
                .GroupBy(kvp => kvp.Key.Tier ?? 0)
                .OrderBy(g => g.Key);

            foreach (var group in firmwareGroups)
            {
                int count = group.Sum(g => g.Value);
                float percent = totalDropped > 0 ? (count / (float)totalDropped) * 100f : 0f;
                sb.AppendLine($"Firmware Tier {group.Key},{count},{percent:F5}%");
            }

            // Group modules
            int moduleCount = tally
                .Where(kvp => kvp.Key.Asset is ModuleUpgradeAsset)
                .Sum(kvp => kvp.Value);

            float modulePercent = totalDropped > 0 ? (moduleCount / (float)totalDropped) * 100f : 0f;
            sb.AppendLine($"Modules,{moduleCount},{modulePercent:F5}%");

            // --- Each burst ---
            sb.AppendLine();
            sb.AppendLine("Burst,Item");
            foreach (var line in burstLines)
                sb.AppendLine(line);

            File.WriteAllText(outputFilePath, sb.ToString());
            Debug.Log($"Loot simulation report written to: {outputFilePath}");
        }
        // --- struct to uniquely identify loot entries ---
        struct LootKey : IEquatable<LootKey>
        {
            public AbstractUpgradeAsset Asset;
            public int? Tier; // null for modules

            public LootKey(AbstractUpgradeAsset asset, int? tier = null)
            {
                Asset = asset;
                Tier = tier;
            }

            public bool Equals(LootKey other)
                => Asset == other.Asset && Tier == other.Tier;

            public override bool Equals(object obj)
                => obj is LootKey other && Equals(other);

            public override int GetHashCode()
            {
                unchecked
                {
                    return ((Asset != null ? Asset.GetHashCode() : 0) * 397)
                         ^ (Tier.HasValue ? Tier.Value : 0);
                }
            }

            public override string ToString()
            {
                return Tier.HasValue
                    ? $"{Asset.FullName} Tier {RomanNumerals.ToRoman(Tier.Value)}"
                    : Asset.FullName;
            }
        }

        #endregion
    }
}

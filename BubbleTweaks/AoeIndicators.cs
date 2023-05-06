using BubbleTweaks.Utilities;
using HarmonyLib;
using Kingmaker.Armies.TacticalCombat;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.PubSubSystem;
using Kingmaker.TurnBasedMode;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.Utility;
using Kingmaker.Visual.Decals;
using Newtonsoft.Json;
using Owlcat.Runtime.Core.Math;
using Owlcat.Runtime.Core.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TurnBased.Controllers;
using UnityEngine;

namespace BubbleTweaks {

    [HarmonyPatch]
    static class AoeIndicatorPatches {
        [HarmonyPatch(typeof(AreaEffectEntityData), nameof(AreaEffectEntityData.OnPostLoad)), HarmonyPostfix]
        public static void AreaEffectEntityData_OnPostLoad(AreaEffectEntityData __instance) {
            AoeAreaIndicators.Instance.ToCreate.Add(__instance);

        }


        [HarmonyPatch(typeof(PathVisualizer), nameof(PathVisualizer.Clear)), HarmonyPostfix]
        public static void PathVisualiser_Clear() {
            AoeAreaIndicators.Instance.UpdateHighlightedAreas(null);
        }

        [HarmonyPatch(typeof(PathVisualizer), nameof(PathVisualizer.DecimateLine)), HarmonyPostfix]
        public static void PathVisualiser_DecimateLine(List<PathVisualizer.VisualPathElement> path) {
            AoeAreaIndicators.Instance.UpdateHighlightedAreas(path);
        }
    }

    class AoeAreaIndicators : IAreaEffectHandler, IAreaActivationHandler {

        public readonly List<AreaEffectEntityData> ToCreate = new();

        private readonly Dictionary<AreaEffectEntityData, AreaMarker> markers = new();
        private readonly Dictionary<string, CircleColors> abilityColors = new();
        private readonly HashSet<string> doNotIndicate = new();

        private bool configReloadRequested = true;
        private GameObject CirclePrefab => SelectionManagerBase.Instance?.m_SelectionMarkPrefab?.m_SpellSelectionRange?.gameObject;

        private class AreaMarker {
            public Material Material;
            public bool Highlighted;
            public AreaEffectEntityData Area;
        };

        public void HandleAreaEffectDestroyed(AreaEffectEntityData entityData) {
            markers.Remove(entityData);
        }


        private Color GetColor(bool highlighted, AreaEffectEntityData area) {

            var colors = abilityColors.Get(area.Blueprint.AssetGuid.ToString(), defaultColors);

            if (highlighted)
                return colors.HighlightColor;
            else
                return colors.NormalColor;
        }

        public void HandleAreaEffectSpawned(AreaEffectEntityData entityData) {
            ReloadConfig();

            if (CirclePrefab == null) return;
            if (!entityData.IsInGame) return;
            if (TacticalCombatHelper.IsActive) return;
            if (doNotIndicate.Contains(entityData.Blueprint.AssetGuid.ToString())) return;
            if (!abilityColors.ContainsKey(entityData.Blueprint.AssetGuid.ToString())) return;
            if (markers.ContainsKey(entityData)) return;

            if (entityData.Blueprint.Shape == AreaEffectShape.Cylinder) {
                var markerObj = GameObject.Instantiate(CirclePrefab, entityData.View.transform.position, Quaternion.identity, entityData.View.transform);

                markerObj.DestroyChildrenImmediate("SpellRangeBack");
                var material = markerObj.GetComponentInChildren<MeshRenderer>().material;
                material.color = GetColor(false, entityData);
                var decal = markerObj.GetComponentInChildren<GUIDecal>();
                decal.SetSpellRangeScale(entityData.Blueprint.Size.Meters * 2 + 0.25f, 0, 0);
                decal.SetRangeColor(false, true, 1);

                markers.Add(entityData, new AreaMarker {
                    Material = material,
                    Area = entityData,
                    Highlighted = false,
                });
            }
        }

        public void OnAreaActivated() {
            markers.Clear();

            foreach (var entity in ToCreate)
                HandleAreaEffectSpawned(entity);
            ToCreate.Clear();
        }

        private static bool CheckLineCircleIntersection(Vector2 start, Vector2 end, Vector2 center, float radius) {
            return (end - center).sqrMagnitude <= radius * radius || VectorMath.SqrDistancePointSegment(start, end, center) <= radius * radius;
        }


        internal void UpdateHighlightedAreas(List<PathVisualizer.VisualPathElement> path) {
            ReloadConfig();

            Dictionary<AreaEffectEntityData, (bool stop, bool start)> actions = new();
            foreach (var marker in markers.Values.Where(m => m.Highlighted))
                actions[marker.Area] = (stop: true, start: false);

            if (path != null && path.Count > 1) {
                Vector2 prev = path[0].Position.To2D();

                var unit = CombatController.Mount ?? CombatController.Rider;

                for (int i = 1; i < path.Count; i++) {
                    Vector2 next = path[i].Position.To2D();
                    foreach (var marker in markers) {
                        if (CheckLineCircleIntersection(prev, next, marker.Value.Area.View.transform.position.To2D(), marker.Key.Blueprint.Size.Meters + unit.Corpulence)) {
                            if (actions.TryGetValue(marker.Key, out var action))
                                actions[marker.Key] = (false, action.start);
                            else
                                actions[marker.Key] = (false, true);
                        }
                    }
                }
            }

            foreach (var kv in actions) {
                var area = kv.Key;
                var action = kv.Value;
                if (action.stop) {
                    markers[area].Material.color = GetColor(false, area);
                    markers[area].Highlighted = false;
                } else if (action.start) {
                    markers[area].Material.color = GetColor(true, area);
                    markers[area].Highlighted = true;
                }
            }
        }

        private CircleColors defaultColors = new() {
            NormalColor = new(.0f, .1f, 1.0f, 1.0f),
            HighlightColor = new(1f, .2f, 0, 1.0f),
        };

        private DateTime lastTimeConfigLoaded = DateTime.MinValue;

        private AoeAreaIndicators() {
            ReloadConfig();
        }

        private void ReloadConfig() {
            try {
                if (!configReloadRequested) return;

                var path = Path.Combine(Main.ModPath, "aoe_indicators.json");
                var lastTimeConfigModified = File.GetLastWriteTimeUtc(path);

                if (lastTimeConfigModified == lastTimeConfigLoaded) return;

                lastTimeConfigLoaded = lastTimeConfigModified;

                using StreamReader reader = File.OpenText(path);
                using var jReader = new JsonTextReader(reader);

                var config = new JsonSerializer().Deserialize<AoeIndicatorsConfig>(jReader);

                configReloadRequested = config.reload;

                CircleColors good = defaultColors;
                CircleColors bad = defaultColors;

                abilityColors.Clear();
                doNotIndicate.Clear();

                foreach (var c in config.colors) {
                    CircleColors cols = new() {
                        HighlightColor = c.hi != null ? new Color(c.hi[0], c.hi[1], c.hi[2]) : Color.magenta,
                        NormalColor = c.normal != null ? new Color(c.normal[0], c.normal[1], c.normal[2]) : Color.magenta,
                        InheritFrom = c.type,
                    };

                    if (c.ability == "__default_bad__")
                        bad = cols;
                    else if (c.ability == "__default_good__")
                        good = cols;
                    else
                        abilityColors[c.ability] = cols;
                }

                foreach (var kv in abilityColors) {
                    var id = kv.Key;
                    var c = kv.Value;
                    if (c.InheritFrom == "bad") {
                        c.NormalColor = bad.NormalColor;
                        c.HighlightColor = bad.HighlightColor;
                    } else if (c.InheritFrom == "good") {
                        c.NormalColor = good.NormalColor;
                        c.HighlightColor = good.HighlightColor;
                    } else if (c.InheritFrom == "skip") {
                        doNotIndicate.Add(id);
                    }

                }

            } catch (Exception e) {
                Main.Error(e, "blah");
            }
        }

        private static AoeAreaIndicators _Instance;

        public static AoeAreaIndicators Instance => _Instance ??= new();

        internal class CircleColors {
            public Color NormalColor;
            public Color HighlightColor;
            public string InheritFrom;
        }

        public class CircleColorsConfig {
            public string ability { get; set; }
            public float[] hi { get; set; }
            public float[] normal { get; set; }
            public string type { get; set; }
        }

        internal class AoeIndicatorsConfig {
            public bool reload { get; set; }
            public CircleColorsConfig[] colors { get; set; }

        }


    }

}

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace BubbleTweaks {
    public static class BundleManger {
        private static Dictionary<string, GameObject> _objects = new Dictionary<string, GameObject>();
        private static Dictionary<string, Sprite> _sprites = new Dictionary<string, Sprite>();
        public static Dictionary<string, Mesh> Meshes = new();

        public static void RemoveBundle(string loadAss, bool unloadAll = false) {
            AssetBundle bundle;
            if (bundle = AssetBundle.GetAllLoadedAssetBundles().FirstOrDefault(x => x.name == loadAss))
                bundle.Unload(unloadAll);
            if (unloadAll) {
                _objects.Clear();
                _sprites.Clear();
            }
        }

        public static UnityEngine.Object[] Assets;

        public static void AddBundle(string loadAss) {
            try {
                AssetBundle bundle;
                GameObject prefab;
                Sprite sprite;

                RemoveBundle(loadAss, true);

                bundle = AssetBundle.LoadFromFile(Main.ModPath + loadAss);
                if (!bundle) throw new Exception($"Failed to load AssetBundle! {Main.ModPath + loadAss}");

                Assets = bundle.LoadAllAssets();

                //foreach (var obj in Assets) {
                //    Main.Log($"Found asset <{obj.name}> of type [{obj.GetType()}]");
                //}

                foreach (var obj in Assets.OfType<GameObject>())
                    _objects[obj.name] = obj;
                foreach (var obj in Assets.OfType<Mesh>())
                    Meshes[obj.name] = obj;
                //foreach (string b in bundle.GetAllAssetNames()) {
                //    Main.Log($"asset => {b}");
                //    if (b.EndsWith(".prefab")) {
                //        Main.Log($"Loading prefab: {b}");
                //        if (!_objects.ContainsKey(Path.GetFileNameWithoutExtension(b))) {
                //            if ((prefab = bundle.LoadAsset<GameObject>(b)) != null) {
                //                prefab.SetActive(false);
                //                _objects.Add(prefab.name, prefab);
                //            } else
                //                Main.Error($"Failed to load the prefab: {b}");
                //        } else
                //            Main.Error($"Asset {b} already loaded.");
                //    }
                //    if (b.EndsWith(".obj")) {
                //        Main.Log($"Loading mesh: {b}");
                //        var mesh = bundle.LoadAsset<Mesh>(b);
                //        if (mesh == null) {
                //            Main.Error("Failed to load mesh...");
                //        } else {
                //        }
                //        Meshes[mesh.name] = mesh;
                //    }
                //    //if (b.EndsWith(".png")) {
                //    //    Main.Log($"Loading sprite: {b}");
                //    //    if (!_sprites.ContainsKey(Path.GetFileNameWithoutExtension(b))) {
                //    //        if ((sprite = bundle.LoadAsset<Sprite>(b)) != null) {
                //    //            _sprites.Add(sprite.name, sprite);
                //    //        } else
                //    //            Main.Error($"Failed to load the prefab: {b}");
                //    //    } else
                //    //        Main.Error($"Asset {b} already loaded.");
                //    //}
                //}

                RemoveBundle(loadAss);
            } catch (Exception ex) {
                Main.Error(ex, "LOADING ASSET");
            }
        }

        public static bool IsLoaded(string asset) {
            return _objects.ContainsKey(asset);
        }

        public static Dictionary<string, GameObject> LoadedPrefabs { get { return _objects; } }
        public static Dictionary<string, Sprite> LoadedSprites { get { return _sprites; } }
    }
}

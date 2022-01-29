using Kingmaker;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace BubbleTweaks {
    static class BubblePrintsThinger {

        private static Type[] ctor = Array.Empty<Type>();
        private static Dictionary<string, Dictionary<string, string>> defaults = new();

        private static void DoThing(Type type) {
            if (defaults.ContainsKey(type.FullName))
                return;

            if (type.IsAbstract || type.IsInterface)
                return;
            if (type.GetConstructor(ctor) == null) {
                return;
            }

            var proto = Activator.CreateInstance(type);

            var map = new Dictionary<string, string>();
            defaults[type.FullName] = map;

            foreach (var f in type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)) {
                var val = f.GetValue(proto);
                if (val == null)
                    map[f.Name] = "<null>";
                else if (val.GetType().IsArray) {
                    map[f.Name] = $"[{((Array)val).Length}]";
                }
                else if (val.GetType().IsPrimitive)
                    map[f.Name] = val.ToString();
                else
                    DoThing(val.GetType());
            }

        }

        public static void DoThing() {

            List<(string Guid, string FullName)> types = File.ReadAllLines(@"D:\bp_types.txt").Select(l => {
                var c = l.Split(' ');
                return (c[0], c[1]);
            }).ToList();

            Main.Log("Doing thing?");

            var wrath = Assembly.GetAssembly(typeof(Game));

            foreach (var t in types) {
                DoThing(wrath.GetType(t.FullName));
            }


            Main.Log("Doing thing?");

            File.WriteAllText(@"D:\bp_defaults.json", JsonConvert.SerializeObject(defaults, Formatting.Indented));

        }
    }
}

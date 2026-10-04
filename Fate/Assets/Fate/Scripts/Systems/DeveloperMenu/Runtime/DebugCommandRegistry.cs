using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace Fate.Systems.DeveloperMenu
{
    /// <summary>A single invokable entry discovered by <see cref="DebugCommandRegistry"/>.</summary>
    public readonly struct DebugCommandEntry
    {
        public readonly string Label;
        public readonly Action Invoke;

        public DebugCommandEntry(string label, Action invoke)
        {
            Label = label;
            Invoke = invoke;
        }
    }

    /// <summary>
    /// Scans loaded assemblies for methods tagged <see cref="DebugCommandAttribute"/> and exposes
    /// them as invokable <see cref="DebugCommandEntry"/> entries, so <see cref="DeveloperMenuController"/>
    /// can list them without each command needing to register itself anywhere by hand. Built lazily
    /// on first access and cached for the life of the process.
    /// </summary>
    public static class DebugCommandRegistry
    {
        private const BindingFlags MethodFlags =
            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance | BindingFlags.DeclaredOnly;

        private static List<DebugCommandEntry> _commands;

        public static IReadOnlyList<DebugCommandEntry> Commands => _commands ??= BuildCommands();

        private static List<DebugCommandEntry> BuildCommands()
        {
            var commands = new List<DebugCommandEntry>();

            foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                Type[] types;
                try
                {
                    types = assembly.GetTypes();
                }
                catch (ReflectionTypeLoadException ex)
                {
                    types = ex.Types.Where(t => t != null).ToArray();
                }

                foreach (Type type in types)
                {
                    foreach (MethodInfo method in type.GetMethods(MethodFlags))
                    {
                        var attribute = method.GetCustomAttribute<DebugCommandAttribute>();
                        if (attribute == null)
                            continue;

                        if (TryBuildInvoker(type, method, out Action invoke))
                            commands.Add(new DebugCommandEntry(attribute.Label ?? method.Name, invoke));
                    }
                }
            }

            return commands;
        }

        private static bool TryBuildInvoker(Type type, MethodInfo method, out Action invoke)
        {
            if (method.GetParameters().Length > 0)
            {
                Debug.LogWarning($"{nameof(DebugCommandAttribute)} on {type.FullName}.{method.Name} ignored - debug commands must be parameterless.");
                invoke = null;
                return false;
            }

            if (method.IsStatic)
            {
                invoke = () => InvokeSafely(method, null, type);
                return true;
            }

            PropertyInfo instanceProperty = type.GetProperty("Instance", BindingFlags.Public | BindingFlags.Static);
            if (instanceProperty == null)
            {
                Debug.LogWarning($"{nameof(DebugCommandAttribute)} on {type.FullName}.{method.Name} ignored - instance methods need a public static \"Instance\" property to call them on.");
                invoke = null;
                return false;
            }

            invoke = () => InvokeSafely(method, instanceProperty.GetValue(null), type);
            return true;
        }

        private static void InvokeSafely(MethodInfo method, object target, Type type)
        {
            try
            {
                method.Invoke(target, null);
            }
            catch (Exception ex)
            {
                Debug.LogError($"Debug command {type.FullName}.{method.Name} threw: {ex}");
            }
        }
    }
}

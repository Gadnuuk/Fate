using System;

namespace Fate.Systems.DeveloperMenu
{
    /// <summary>
    /// Marks a parameterless method as a developer-menu command (see <see cref="DebugCommandRegistry"/>).
    /// Works on static methods, or instance methods on a type that exposes a public static
    /// "Instance" property (e.g. <see cref="Utilities.PersistentSingleton{T}"/> subclasses) -
    /// the registry resolves the target instance through that property at invoke time.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    public class DebugCommandAttribute : Attribute
    {
        /// <summary>Button text in the developer menu. Defaults to the method name when null.</summary>
        public string Label { get; }

        public DebugCommandAttribute(string label = null)
        {
            Label = label;
        }
    }
}

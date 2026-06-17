namespace Jattac.Libraries.QBuilder.Helpers
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Reflection;
    using System.Runtime.CompilerServices;
    using Jattac.Libraries.QBuilder.Attributes;

    /// <summary>
    /// Reflects the public instance properties of a type and categorises them as
    /// key, ignored, or regular — based on QBuilder attributes.
    /// Results are cached per Type for performance.
    /// </summary>
    internal static class PocoReflector
    {
        // ── Cached descriptor (shape of the type, no values) ──────────────────────────

        internal sealed class PocoPropertyDescriptor
        {
            internal string PropertyName { get; }
            internal string ColumnName { get; }
            internal bool IsKey { get; }
            internal bool IsIgnored { get; }
            // Compiled getter: avoids MethodInfo.Invoke on every call after first reflection.
            internal Func<object, object> GetValue { get; }

            internal PocoPropertyDescriptor(
                string propertyName,
                string columnName,
                bool isKey,
                bool isIgnored,
                Func<object, object> getValue)
            {
                PropertyName = propertyName;
                ColumnName = columnName;
                IsKey = isKey;
                IsIgnored = isIgnored;
                GetValue = getValue;
            }
        }

        // ── Per-call result (descriptor + the value from a specific instance) ─────────

        internal sealed class PocoProperty
        {
            internal string PropertyName { get; }
            internal string ColumnName { get; }
            internal bool IsKey { get; }
            internal bool IsIgnored { get; }
            internal object Value { get; }

            internal PocoProperty(PocoPropertyDescriptor descriptor, object value)
            {
                PropertyName = descriptor.PropertyName;
                ColumnName = descriptor.ColumnName;
                IsKey = descriptor.IsKey;
                IsIgnored = descriptor.IsIgnored;
                Value = value;
            }
        }

        // ── Cache ────────────────────────────────────────────────────────────────────

        private static readonly ConcurrentDictionary<Type, IReadOnlyList<PocoPropertyDescriptor>> _cache
            = new ConcurrentDictionary<Type, IReadOnlyList<PocoPropertyDescriptor>>();

        // ── Public API ───────────────────────────────────────────────────────────────

        /// <summary>
        /// Returns the properties of <typeparamref name="T"/> with values populated from
        /// <paramref name="instance"/>. Descriptors are cached; values are read on every call.
        /// </summary>
        internal static IReadOnlyList<PocoProperty> GetProperties<T>(T instance)
        {
            var descriptors = _cache.GetOrAdd(typeof(T), BuildDescriptors);
            var result = new List<PocoProperty>(descriptors.Count);
            foreach (var d in descriptors)
                result.Add(new PocoProperty(d, d.GetValue(instance)));
            return result;
        }

        /// <summary>
        /// Returns true if <paramref name="t"/> is a C# anonymous type.
        /// Anonymous types: compiler-generated, sealed, not public, name contains "AnonymousType".
        /// This heuristic is reliable for all current C# compiler versions.
        /// </summary>
        internal static bool IsAnonymousType(Type t) =>
            t.GetCustomAttributes(typeof(CompilerGeneratedAttribute), false).Length > 0
            && t.IsSealed
            && !t.IsPublic
            && t.Name.Contains("AnonymousType");

        // ── Private ──────────────────────────────────────────────────────────────────

        private static IReadOnlyList<PocoPropertyDescriptor> BuildDescriptors(Type type)
        {
            var props = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);
            var descriptors = new List<PocoPropertyDescriptor>(props.Length);

            foreach (var prop in props)
            {
                if (!prop.CanRead) continue;

                var isIgnored = prop.GetCustomAttribute<QIgnoreAttribute>() != null;
                var isKey = prop.GetCustomAttribute<QKeyAttribute>() != null;
                var colAttr = prop.GetCustomAttribute<QColumnAttribute>();
                var colName = colAttr != null ? colAttr.Name : prop.Name;

                // Capture prop in a local variable to avoid closure-capture-loop bug.
                var capturedProp = prop;
                Func<object, object> getter = instance => capturedProp.GetValue(instance);

                descriptors.Add(new PocoPropertyDescriptor(
                    prop.Name,
                    colName,
                    isKey,
                    isIgnored,
                    getter));
            }

            return descriptors;
        }
    }
}

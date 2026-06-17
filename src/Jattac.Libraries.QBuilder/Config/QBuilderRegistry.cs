namespace Jattac.Libraries.QBuilder.Config
{
    using System;
    using System.Collections.Concurrent;

    internal static class QBuilderRegistry
    {
        private static QBuilderOptions _default = new QBuilderOptions();
        private static readonly ConcurrentDictionary<string, QBuilderOptions> _named = new ConcurrentDictionary<string, QBuilderOptions>(StringComparer.OrdinalIgnoreCase);

        internal static void SetDefault(QBuilderOptions options) => _default = options;

        internal static void Register(string name, QBuilderOptions options) => _named[name] = options;

        internal static QBuilderOptions GetDefault() => _default;

        internal static QBuilderOptions Get(string name)
        {
            if (_named.TryGetValue(name, out var options))
            {
                return options;
            }
            throw new InvalidOperationException(
                $"No QBuilder configuration named '{name}' was found. Register it with QBuilderConfig.Configure(\"{name}\", ...).");
        }

        internal static void Reset()
        {
            _default = new QBuilderOptions();
            _named.Clear();
        }
    }
}

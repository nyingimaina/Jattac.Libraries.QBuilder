namespace Jattac.Libraries.QBuilder.Config
{
    using System;
    using Jattac.Libraries.QBuilder.Helpers;

    /// <summary>
    /// Static configuration API for <see cref="QBuilder"/> — modelled after <c>IHttpClientFactory</c>.
    /// Set a global default at application startup and optionally register named configurations
    /// for different databases or schemas.
    /// </summary>
    /// <example>
    /// <code>
    /// // Program.cs / Startup.cs
    /// QBuilderConfig.ConfigureDefault(opt =>
    /// {
    ///     opt.Dialect = Dialect.SqlServer;
    ///     opt.TableNameResolver = t => "dbo." + t.Name;
    /// });
    ///
    /// QBuilderConfig.Configure("mysql", opt =>
    /// {
    ///     opt.Dialect = Dialect.MySql;
    ///     opt.TableNameResolver = t => t.Name.ToLower();
    /// });
    ///
    /// // Usage
    /// var q = Q.New();           // uses default (SqlServer)
    /// var q = Q.New("mysql");    // uses named MySql config
    /// </code>
    /// </example>
    public static class QBuilderConfig
    {
        /// <summary>
        /// Sets the application-wide default configuration.
        /// <c>Q.New()</c> and <c>Q.New(parameterize: false)</c> use this config.
        /// </summary>
        public static void ConfigureDefault(Action<QBuilderOptions> configure)
        {
            Guard.NotNull(configure, nameof(configure));
            var options = new QBuilderOptions();
            configure(options);
            QBuilderRegistry.SetDefault(options);
        }

        /// <summary>
        /// Registers a named configuration.
        /// <c>Q.New("name")</c> resolves to this config.
        /// </summary>
        public static void Configure(string name, Action<QBuilderOptions> configure)
        {
            Guard.NotNull(name, nameof(name));
            Guard.NotNull(configure, nameof(configure));
            var options = new QBuilderOptions();
            configure(options);
            QBuilderRegistry.Register(name, options);
        }

        /// <summary>
        /// Resets all configuration to library defaults. Intended for use in tests.
        /// </summary>
        public static void Reset() => QBuilderRegistry.Reset();
    }
}

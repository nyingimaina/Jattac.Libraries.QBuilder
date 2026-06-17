namespace Jattac.QBuilderTests.Models
{
    using System;
    using Jattac.Libraries.QBuilder.Attributes;

    // ── Unit-test models (no SQLite schema constraint; used for SQL string assertions) ──

    internal class UserWithKey
    {
        [QKey]
        public Guid Id { get; set; }
        public string Name { get; set; }
        public bool IsActive { get; set; }
        public DateTime? DeletedAt { get; set; }
    }

    internal class UserWithIgnore
    {
        [QKey]
        public Guid Id { get; set; }
        public string Name { get; set; }

        [QIgnore]
        public bool IsActive { get; set; }
    }

    internal class UserWithColumnAlias
    {
        [QKey]
        public Guid Id { get; set; }

        [QColumn("user_name")]
        public string Name { get; set; }
    }

    internal class UserWithMultiKey
    {
        [QKey]
        public Guid TenantId { get; set; }
        [QKey]
        public Guid UserId { get; set; }
        public string Name { get; set; }
    }

    // No [QKey] — used to test the no-key guard.
    internal class UserNoKey
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
    }

    // Both [QKey] and [QIgnore] — QIgnore wins.
    internal class UserWithKeyAndIgnore
    {
        [QKey]
        [QIgnore]
        public Guid Id { get; set; }
        public string Name { get; set; }
    }

    // Minimal model for bulk insert unit tests (predictable param counts).
    internal class SimpleProduct
    {
        [QKey]
        public int Id { get; set; }
        public string Name { get; set; }
        public decimal Price { get; set; }
    }

    // ── Integration-test models (string IDs to match SQLite TEXT schema) ──

    internal class UserWithKeyDb
    {
        [QKey]
        public string Id { get; set; }
        public string Name { get; set; }
        public int IsActive { get; set; }
        public string DeletedAt { get; set; }
    }

    internal class UserWithIgnoreDb
    {
        [QKey]
        public string Id { get; set; }
        public string Name { get; set; }

        [QIgnore]
        public int IsActive { get; set; }
    }

    internal class UserWithAliasDb
    {
        [QKey]
        public string Id { get; set; }

        [QColumn("user_name")]
        public string Name { get; set; }
    }

    internal class UserWithMultiKeyDb
    {
        [QKey]
        public string TenantId { get; set; }
        [QKey]
        public string UserId { get; set; }
        public string Name { get; set; }
    }

    // For bulk insert integration tests — matches the existing Product table schema.
    internal class SimpleProductDb
    {
        [QKey]
        public string Id { get; set; }
        public string Name { get; set; }
        public decimal Price { get; set; }
    }
}

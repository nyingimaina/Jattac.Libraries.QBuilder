namespace Jattac.QBuilderTests.Integration
{
    using System;
    using System.Data;
    using Dapper;
    using Microsoft.Data.Sqlite;

    /// <summary>
    /// Base for integration tests that execute QBuilder-generated SQL against an in-memory SQLite database.
    /// Each test class gets a fresh connection; the schema is created per-instance.
    /// </summary>
    public abstract class IntegrationTestBase : IDisposable
    {
        protected IDbConnection Db { get; }

        protected IntegrationTestBase()
        {
            Db = new SqliteConnection("Data Source=:memory:");
            Db.Open();
            CreateSchema();
        }

        private void CreateSchema()
        {
            Db.Execute(@"
                CREATE TABLE User (
                    Id   TEXT NOT NULL PRIMARY KEY,
                    Name TEXT NOT NULL,
                    IsActive INTEGER NOT NULL DEFAULT 1,
                    DeletedAt TEXT NULL
                );

                CREATE TABLE [Order] (
                    Id        TEXT NOT NULL PRIMARY KEY,
                    UserId    TEXT NOT NULL,
                    Amount    REAL NOT NULL,
                    Status    TEXT NOT NULL,
                    DeletedAt TEXT NULL
                );

                CREATE TABLE Product (
                    Id    TEXT NOT NULL PRIMARY KEY,
                    Name  TEXT NOT NULL,
                    Price REAL NOT NULL
                );
            ");
        }

        protected void SeedUsers(params (string id, string name, bool isActive, string deletedAt)[] rows)
        {
            foreach (var (id, name, isActive, deletedAt) in rows)
            {
                Db.Execute(
                    "INSERT INTO User (Id, Name, IsActive, DeletedAt) VALUES (@id, @name, @isActive, @deletedAt)",
                    new { id, name, isActive = isActive ? 1 : 0, deletedAt });
            }
        }

        protected void SeedOrders(params (string id, string userId, decimal amount, string status, string deletedAt)[] rows)
        {
            foreach (var (id, userId, amount, status, deletedAt) in rows)
            {
                Db.Execute(
                    "INSERT INTO [Order] (Id, UserId, Amount, Status, DeletedAt) VALUES (@id, @userId, @amount, @status, @deletedAt)",
                    new { id, userId, amount, status, deletedAt });
            }
        }

        public void Dispose() => Db?.Dispose();
    }
}

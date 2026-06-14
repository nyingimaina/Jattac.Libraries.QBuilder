namespace Jattac.Libraries.QBuilder
{
    using System;

    public class TableNameAliaser
    {
        public TableNameAliaser(Func<Type, string> tableNameResolver)
        {
            TableNameResolver = tableNameResolver;
        }

        public Func<Type, string> TableNameResolver { get; }

        /// <summary>
        /// Returns a table alias, stripping any schema prefix first.
        /// <c>"dbo.Users"</c> → <c>"tUsers"</c>.
        /// </summary>
        public string GetTableAlias(string tableName, bool shouldAliasTableName = true)
        {
            if (shouldAliasTableName == false)
            {
                return string.Empty;
            }

            var dotIndex = tableName.LastIndexOf('.');
            var baseName = dotIndex >= 0 ? tableName.Substring(dotIndex + 1) : tableName;
            return $"t{baseName}";
        }

        public string GetTableAlias<TTable>()
        {
            var tableName = TableNameResolver(typeof(TTable));
            return GetTableAlias(tableName, true);
        }
    }
}
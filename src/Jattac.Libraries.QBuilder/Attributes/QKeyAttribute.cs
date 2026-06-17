namespace Jattac.Libraries.QBuilder.Attributes
{
    using System;

    /// <summary>
    /// Marks a property as the primary key of the table.
    /// When <c>FromObject</c> is called:
    /// - On INSERT: the property is inserted as a normal column.
    /// - On UPDATE: the property value is placed in the WHERE clause, not the SET clause.
    /// - On DELETE: the property value is placed in the WHERE clause.
    /// To skip a database-generated key from INSERT, use <see cref="QIgnoreAttribute"/> instead.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property, Inherited = true, AllowMultiple = false)]
    public sealed class QKeyAttribute : Attribute
    {
    }
}

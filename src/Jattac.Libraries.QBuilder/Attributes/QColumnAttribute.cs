namespace Jattac.Libraries.QBuilder.Attributes
{
    using System;

    /// <summary>
    /// Overrides the SQL column name used by <c>FromObject</c>.
    /// When not present, the property name is used as-is.
    /// IMPORTANT: This attribute only affects the <c>FromObject</c> (reflection) path.
    /// The expression-based fluent methods (.Value(u => u.Name, ...), .Set(...), etc.)
    /// always use the C# property name and are unaffected by this attribute.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property, Inherited = true, AllowMultiple = false)]
    public sealed class QColumnAttribute : Attribute
    {
        public string Name { get; }

        public QColumnAttribute(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Column name cannot be null or whitespace.", nameof(name));
            Name = name;
        }
    }
}

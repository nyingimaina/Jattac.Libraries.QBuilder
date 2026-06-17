namespace Jattac.Libraries.QBuilder.Attributes
{
    using System;

    /// <summary>
    /// Instructs <c>FromObject</c> to skip this property entirely.
    /// The property will not appear in INSERT columns, SET clauses, or WHERE conditions.
    /// If a property is decorated with both <see cref="QIgnoreAttribute"/> and
    /// <see cref="QKeyAttribute"/>, it is still fully ignored.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property, Inherited = true, AllowMultiple = false)]
    public sealed class QIgnoreAttribute : Attribute
    {
    }
}

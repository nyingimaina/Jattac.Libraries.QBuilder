namespace Jattac.Libraries.QBuilder.Helpers
{
    using System;

    internal static class Guard
    {
        // Builder state invariant — throw when the caller has used the builder incorrectly.
        internal static void Against(bool condition, string message)
        {
            if (condition) throw new InvalidOperationException(message);
        }

        // Null argument — throw when a required argument is null.
        internal static void NotNull(object value, string message)
        {
            if (value is null) throw new ArgumentNullException(message);
        }

        // Range argument — throw when a numeric argument is outside acceptable bounds.
        internal static void Range(bool valid, string paramName, string message)
        {
            if (!valid) throw new ArgumentOutOfRangeException(paramName, message);
        }
    }
}

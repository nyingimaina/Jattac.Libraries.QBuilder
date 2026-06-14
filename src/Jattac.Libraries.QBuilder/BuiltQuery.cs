using System.Collections.Generic;

namespace Jattac.Libraries.QBuilder
{
    public class BuiltQuery
    {
        public string ParameterizedSql { get; set; } = string.Empty;

        public Dictionary<string, object> Parameters { get; set; } = new Dictionary<string, object>();
    }
}
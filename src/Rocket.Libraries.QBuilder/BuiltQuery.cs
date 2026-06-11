using System.Collections.Generic;

namespace Rocket.Libraries.Qurious
{
    public class BuiltQuery
    {
        public string ParameterizedSql { get; set; } = string.Empty;

        public Dictionary<string, object> Parameters { get; set; } = new Dictionary<string, object>();
    }
}
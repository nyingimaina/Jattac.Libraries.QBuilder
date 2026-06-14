using System;

namespace Jattac.Libraries.QBuilder
{
    public class WhereDescription
    {
        public string Clause { get; set; }

        internal string Conjunction { get; set; }

        internal Guid ParenthesesId {get; set;}
    }
}
using System;
using System.Collections.Generic;
using System.Linq;

namespace Jattac.Libraries.QBuilder.Helpers
{
    public class UniqueValueResolver<TValue> : IDisposable
    {
        public void Dispose()
        {
            
        }

        public List<TValue> GetUnique(List<TValue> list)
        {
            return list.GroupBy(a => a)
                .Select(a => a.Key)
                .ToList();
        }
    }
}
using System;
using System.Collections.Generic;
using System.Linq;
using Jattac.Libraries.QBuilder.Helpers;
using Xunit;

namespace Lattice.Services.Foundation.Tests.Shared
{
    public class UniqueValueResolverTests
    {
        [Fact]
        public void Resolve()
        {
            var alpha = Guid.NewGuid();
            var alphaCopy = alpha;
            var beta = Guid.NewGuid();

            var list = new List<Guid>{
                alpha,
                alphaCopy,
                beta
            };

            Assert.Equal(3,list.Count);
            Assert.Equal(2,list.Count(a => a == alpha));
            
            using(var uniqueValueResolver = new UniqueValueResolver<Guid>())
            {
                var resolvedList = uniqueValueResolver.GetUnique(list);
                Assert.Equal(2,resolvedList.Count);
                Assert.Equal(1,resolvedList.Count(a => a == alpha));
            }
            

        }
    }
}
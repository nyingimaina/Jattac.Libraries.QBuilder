using System;

namespace Jattac.QBuilderTests.Models
{
    internal class User
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public bool IsActive { get; set; }
        public DateTime? DeletedAt { get; set; }
    }
}
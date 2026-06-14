using System;

namespace Jattac.QBuilderTests.Models
{
    internal class Order
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public decimal Amount { get; set; }
        public string Status { get; set; }
        public DateTime? DeletedAt { get; set; }
    }
}

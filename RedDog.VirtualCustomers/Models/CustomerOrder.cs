using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace RedDog.VirtualCustomers.Models
{
    public class CustomerOrder
    {
        [JsonPropertyName("storeId")]
        public string StoreId { get; set; } = string.Empty;

        [JsonPropertyName("firstName")]
        public string FirstName { get; set; } = string.Empty;

        [JsonPropertyName("lastName")]
        public string LastName { get; set; } = string.Empty;

        [JsonPropertyName("loyaltyId")]
        public string LoyaltyId { get; set; } = string.Empty;

        [JsonPropertyName("orderItems")]
        public List<CustomerOrderItem> OrderItems { get; set; } = new();
    }
}

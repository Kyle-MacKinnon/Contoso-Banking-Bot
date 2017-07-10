using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Contoso_Banking_Bot
{
    [Serializable]
    public class Account
	{
		[JsonProperty(PropertyName = "Id")]
		public string ID { get; set; }

		[JsonProperty(PropertyName = "type")]
		public string Type { get; set; }

		[JsonProperty(PropertyName = "number")]
		public string Number { get; set; }

		[JsonProperty(PropertyName = "name")]
		public string Name { get; set; }

		[JsonProperty(PropertyName = "balance")]
		public decimal Balance { get; set; }

		[JsonProperty(PropertyName = "availableBalance")]
		public decimal AvailableBalance { get; set; }

        [JsonProperty(PropertyName = "owner")]
        public int Owner { get; set; }
    }
}
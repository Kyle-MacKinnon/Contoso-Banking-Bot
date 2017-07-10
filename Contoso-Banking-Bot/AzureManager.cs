using Microsoft.WindowsAzure.MobileServices;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Contoso_Banking_Bot
{
	public sealed class AzureManager
	{
		private static readonly AzureManager instance = instance = new AzureManager();
		private MobileServiceClient client;

		private AzureManager()
		{
			this.client = new MobileServiceClient("http://contoso-database.azurewebsites.net");
		}

		public MobileServiceClient AzureClient
		{
			get { return client; }
		}

		public static AzureManager AzureManagerInstance
		{
			get
			{
				return instance;
			}
		}
	}
}
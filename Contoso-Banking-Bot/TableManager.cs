using Microsoft.WindowsAzure.MobileServices;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Contoso_Banking_Bot
{
    public sealed class TableManager
    {
        private static readonly TableManager instance = new TableManager();

        public MobileServiceClient TableClient                      { get; set; }
        public IMobileServiceTable<Account> AccountTable            { get; set; }
        public IMobileServiceTable<Person> PersonTable              { get; set; }
        public IMobileServiceTable<Transaction> TransactionTable    { get; set; }
        public IMobileServiceTable<Payee> PayeeTable                { get; set; }

        private TableManager()
        {
            // Instantiate tables
            TableClient = AzureManager.AzureManagerInstance.AzureClient;

            AccountTable     = TableClient.GetTable<Account>();
            PersonTable      = TableClient.GetTable<Person>();
            TransactionTable = TableClient.GetTable<Transaction>();
            PayeeTable       = TableClient.GetTable<Payee>();
        }

        public static TableManager Instance
        {
            get
            {
                return instance;
            }
        }
    }
}
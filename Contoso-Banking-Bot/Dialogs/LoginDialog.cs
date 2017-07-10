

using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Luis;
using Microsoft.Bot.Builder.Luis.Models;
using Microsoft.Bot.Connector;
using System;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Contoso_Banking_Bot
{
    [LuisModel("4db07800-945f-4416-9544-4384fc1cbb35", "8ada123cda7c4bb692a4fe33625f61aa")]
    [Serializable]
    public class LoginDialog : LuisDialog<object>
    {
        [LuisIntent("")]
        public async Task None(IDialogContext context, LuisResult result)
        {
            await context.PostAsync("Before we get started could you please enter your access number?");
            context.Wait(MessageReceived);
        }

        [LuisIntent("Greeting")]
        public async Task Greeting(IDialogContext context, LuisResult result)
        {
            await context.PostAsync("## ![Contoso Logo](http://i.imgur.com/aiENnQp.png') Welcome to Contoso Bank\n\nHey there! Please type in your **_access number_** and we can get started.");
            context.Wait(MessageReceived);
        }

        [LuisIntent("Login")]
        public async Task Login(IDialogContext context, LuisResult result)
        {
            // Must provide a number entity
            var numberEntities = result.Entities.Where(e => e.Type == "builtin.number");

            if (numberEntities.Count() > 0)
            {
                // Number provided must be a valid 6 digit integer
                var accessNumber = numberEntities.First().Entity;

                if(accessNumber.Length != 6)
                {
                    await context.PostAsync("I didn't recognize that access number, could you please try again?");
                    context.Wait(MessageReceived);
                    return;
                }

                int accessNumberInt = 0;

                try
                {
                    accessNumberInt = Int32.Parse(accessNumber);
                }

                catch (AmbiguousMatchException)
                {
                    await context.PostAsync($"That access number isn't in our system. Please check to make sure it's written correctly.");
                    context.Wait(MessageReceived);
                    return;
                }

                // Is this person in our database?
                var customerList = await TableManager.Instance.PersonTable.Where(p => p.AccessNumber == accessNumberInt).ToListAsync();

                if (customerList.Count == 1)
                {
                    // Log in message
                    var customer = customerList.First();
                    await context.PostAsync($"Great to see you again {customer.FirstName}. How can I help you today?");

                    // Get all transactions
                    var transactions = await TableManager.Instance.TransactionTable.Where(t => t.Owner == accessNumberInt).ToListAsync();

                    if (transactions.Count > 0)
                    {
                        var reply = new StringBuilder();

                        reply.Append("## Recent Payments To You\n\n");
                        reply.Append("");

                        int count = 1;

                        foreach (Transaction t in transactions)
                        {
                            reply.Append($"* **{t.Amount.ToString("C")}** at {t.CreatedAt.ToLongDateString()} {t.CreatedAt.ToShortTimeString()}");
                            await TableManager.Instance.TransactionTable.DeleteAsync(t);
                            count++;
                        }
                        await context.PostAsync(reply.ToString());
                    }

                    // Open new conversation with more options
                    var bankingDialog = new BankingDialog();

                    bankingDialog.customer = customer;
                    context.Call(bankingDialog, Handled);

                    return;
                }
            }
            // The information provided was not valid
            await context.PostAsync("I didn't recognize that access number, could you please try again?");
            context.Wait(MessageReceived);
        }

        private async Task Handled(IDialogContext context, IAwaitable<bool> result)
        {
            var messageHandled = await result;
            context.Wait(MessageReceived);
        }
    }
}
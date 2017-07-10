using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Luis;
using Microsoft.Bot.Builder.Luis.Models;
using Microsoft.Bot.Connector;
using System;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Contoso_Banking_Bot
{
    [LuisModel("4db07800-945f-4416-9544-4384fc1cbb35", "8ada123cda7c4bb692a4fe33625f61aa")]
    [Serializable]
    public class BankingDialog : LuisDialog<bool>
    {

        // The person who's accessing this dialog
        public Person customer { get; set; }

        [LuisIntent("")]
        public async Task None(IDialogContext context, LuisResult result)
        {
            await context.PostAsync("Sorry but I couldn't understand that.");
            context.Wait(MessageReceived);
        }

        [LuisIntent("Login")]
        public async Task Login(IDialogContext context, LuisResult result)
        {
            await context.PostAsync("That's alright, you have already entered your access number.");
            context.Wait(MessageReceived);
        }

        [LuisIntent("Greeting")]
        public async Task Greeting(IDialogContext context, LuisResult result)
        {
            await context.PostAsync("Hey, I'm still here.");
            context.Wait(MessageReceived);
        }

        [LuisIntent("SeeBalance")]
        public async Task SeeBalance(IDialogContext context, LuisResult result)
        {
            var accounts = await TableManager.Instance.AccountTable.Where(a => a.Owner == customer.AccessNumber).ToListAsync();

            if(accounts.Count() > 0)
            {
                var balance = accounts.Sum(a => a.AvailableBalance);
                await context.PostAsync($"## Your Balance\n\nYour total balance across all accounts is **_{balance.ToString("C")}_**\n\n_- You can ask me about your accounts for more detail._");
            }

            context.Wait(MessageReceived);
        }

        [LuisIntent("SeeAccounts")]
        public async Task SeeAccounts(IDialogContext context, LuisResult result)
        {
            var accounts = await TableManager.Instance.AccountTable.Where(a => a.Owner == customer.AccessNumber).ToListAsync();

            if (accounts.Count > 0)
            {
                var reply = new StringBuilder();

                reply.Append("## Your Accounts\n\n");
                reply.Append("");

                int count = 1;

                foreach (Account a in accounts)
                {
                    reply.Append($"* **{a.Type}**: {a.Number}   \n-- Balance: **_{a.Balance.ToString("C")}_**   \n-- Available Balance: **_{a.AvailableBalance.ToString("C")}_**\n\n  \n");
                    count++;
                }
                await context.PostAsync(reply.ToString());
            }

            else
            {
                await context.PostAsync("You don't have any accounts at the moment. Give us a call and we can get you set up with one.");
            }

            context.Wait(MessageReceived);
        }

        [LuisIntent("SeePayees")]
        public async Task SeePayees(IDialogContext context, LuisResult result)
        {
            var payees = await TableManager.Instance.PayeeTable.Where(a => a.Owner == customer.AccessNumber).ToListAsync();

            if (payees.Count() > 0)
            {
                var reply = new StringBuilder();

                reply.Append("## Your Payees\n\n");
                reply.Append("");

                int count = 1;

                foreach (Payee p in payees)
                {
                    reply.Append($"* **{p.Name}**: {p.Account}  \n");
                    count++;
                }
                await context.PostAsync(reply.ToString());
            }
            context.Wait(MessageReceived);
        }

        [LuisIntent("PaySomeone")]
        public async Task PaySomeone(IDialogContext context, LuisResult result)
        {
            var payDialog = new PaySomeoneDialog();

            EntityRecommendation personName = null;
            EntityRecommendation amount = null;

            foreach (EntityRecommendation e in result.Entities)
            {
                if(e.Type == "PersonName")
                {
                    personName = e;
                }

                else if(e.Type == "builtin.currency")
                {
                    amount = e; ;
                }
            }

            // If a person was specified
            if (personName != null)
            {
                // Can we actually find that person in our payee list?
                var matchingPayees = await TableManager.Instance.PayeeTable.Where(p => p.Owner == customer.AccessNumber && p.Name == personName.Entity).ToListAsync();

                if (matchingPayees.Count > 0)
                {
                    // We know what account we're sending too then. Set the target.
                    var accountList = await TableManager.Instance.AccountTable.Where(a => a.Number == matchingPayees.First().Account).ToListAsync();
                    payDialog.Target = accountList.First();

                }

                else
                {
                    // We can't so we need to ask if they would like to add a new payee
                    await context.PostAsync($"I couldn't find that name in your list of Payees.");
                    context.Wait(MessageReceived);
                    return;
                }
            }

            // If an amount was specified
            if (amount != null)
            {
                // How much money was specified?
                decimal decAmount = 0;

                try
                {
                    decAmount = decimal.Parse(amount.Entity.Replace("$", string.Empty), NumberStyles.AllowCurrencySymbol | NumberStyles.AllowThousands | NumberStyles.AllowDecimalPoint);
                }

                catch (AmbiguousMatchException)
                {
                    await context.PostAsync($"The amount of money you specified wasn't in a format I can understand. Please use $X.XX where X is a number.");
                    context.Wait(MessageReceived);
                    return;
                }

                // Must be larger than zero
                if (decAmount <= 0)
                {
                    await context.PostAsync($"The amount of money you specified wasn't in a format I can understand. Please use $X.XX where X is a number.");
                    context.Wait(MessageReceived);
                    return;
                }

                // At this point we have a payee and an amount to pay them
                payDialog.Amount = decAmount;

            }

            // All values are valid, continue with the pay dialog.
            await context.PostAsync("Which account would you like to pay from?");
       
            var accounts = await TableManager.Instance.AccountTable.Where(a => a.Owner == customer.AccessNumber).ToListAsync();

            if (accounts.Count > 0)
            {
                var reply = new StringBuilder();

                reply.Append("## Send From?\n\n");
                reply.Append("");

                int count = 1;

                foreach (Account a in accounts)
                {
                    reply.Append($"* **{count}**: {a.Type}  \n-- Available Balance: **_{a.AvailableBalance.ToString("C")}_**\n\n  \n");
                    count++;
                }
                await context.PostAsync(reply.ToString());
            }

            payDialog.Origin = customer;
            payDialog.payee = personName.Entity;
            context.Call(payDialog, Handled);
            return;
        }

        private async Task Handled(IDialogContext context, IAwaitable<bool> result)
        {
            var messageHandled = await result;
            await context.PostAsync($"Is there anything else I can do for you {customer.FirstName}?");
            context.Wait(MessageReceived);
        }

        [LuisIntent("Reset")]
        public async Task Reset(IDialogContext context, LuisResult result)
        {
            await context.PostAsync($"See you later {customer.FirstName}!");
            context.Done(true);
        }
    }
}
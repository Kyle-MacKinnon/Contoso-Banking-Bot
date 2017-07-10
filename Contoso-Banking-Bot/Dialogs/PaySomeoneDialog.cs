using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Luis;
using Microsoft.Bot.Builder.Luis.Models;
using Microsoft.Bot.Connector;
using Microsoft.WindowsAzure.MobileServices;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace Contoso_Banking_Bot
{
    [LuisModel("4db07800-945f-4416-9544-4384fc1cbb35", "8ada123cda7c4bb692a4fe33625f61aa")]
    [Serializable]
    public class PaySomeoneDialog : LuisDialog<bool>
    {
        public Person Origin    { get; set; }
        public Account Target   { get; set; }
        public string payee     { get; set; }
        public decimal Amount   { get; set; }
        public int from = 0;

        [LuisIntent("")]
        public async Task None(IDialogContext context, LuisResult result)
        {
            foreach (EntityRecommendation e in result.Entities)
            {
                if(e.Type == "builtin.number")
                {
                    try
                    {
                        from = Int32.Parse(e.Entity);
                    }

                    catch(AmbiguousMatchException)
                    {
                        break;
                    }
                }
            }

            var accounts = await TableManager.Instance.AccountTable.Where(a => a.Owner == Origin.AccessNumber).ToListAsync();
            Account sendFrom = null;

            if (accounts.Count > 0)
            {
                if (accounts.Count > from)
                {
                    sendFrom = accounts.ElementAt(from - 1);

                    if(sendFrom.AvailableBalance > Amount)
                    {
                        Target.Balance += Amount;
                        Target.AvailableBalance += Amount;
                        sendFrom.AvailableBalance -= Amount;
                        sendFrom.Balance -= Amount;

                        await context.PostAsync($"Sent {Amount.ToString("C")} to {payee}");
                        await TableManager.Instance.AccountTable.UpdateAsync(sendFrom);
                        await TableManager.Instance.AccountTable.UpdateAsync(Target);

                        var newTransaction = new Transaction();
                        newTransaction.Amount = Amount;
                        newTransaction.Destination = Target.Number;
                        newTransaction.Owner = Target.Owner;

                        await TableManager.Instance.TransactionTable.InsertAsync(newTransaction);

                        context.Done(true);
                        return;
                    }

                    else
                    {
                        await context.PostAsync($"There's not enough money in account {from}. Please choose another account.");
                        context.Wait(MessageReceived);
                        return;
                    }
                }
            }
            await context.PostAsync($"Please specify which account you want to send from. Example: Account 1");
            context.Wait(MessageReceived);
        }

        [LuisIntent("Reset")]
        public async Task Reset(IDialogContext context, LuisResult result)
        {
            await context.PostAsync("Goodbye");
            context.Done(true);
        }
    }
}
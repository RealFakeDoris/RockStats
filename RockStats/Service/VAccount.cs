/*
Copyright (c) 2020 - 0xCD7F82BcFa333B4072A11Bd0B1da95c9b5f9E869 m/44'/60'/0'/0/0

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
 */

using Nethereum.Util;
using Raven.Client.Documents.Indexes;
using Raven.Client.Documents.Linq;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using Vidyano.Service.RavenDB;
using Vidyano.Service.Repository;

namespace RockStats.Service
{
    /// <summary>
    /// A view object for the VAccount_Details index.
    /// </summary>
    [Mapped]
    public class VAccount
    {
        /// <summary>
        /// The Id of the account.
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// The full blockchain address.
        /// </summary>
        public string Address { get; set; }

        /// <summary>
        /// The Reddit user that owns this address.
        /// </summary>
        public string Redditor { get; set; }

        /// <summary>
        /// The Reddit user avatar url.
        /// </summary>
        public string Avatar { get; set; }

        /// <summary>
        /// The Reddit user flairs.
        /// </summary>
        public string Flairs { get; set; }

        /// <summary>
        /// The amount of ROCK tokens currently held.
        /// </summary>
        public string Balance { get; set; }

        /// <summary>
        /// The amount of ROCK tokens sent until now.
        /// </summary>
        public string Sent { get; set; }

        /// <summary>
        /// The amount of ROCK transactions sent.
        /// </summary>
        public int TxsOut { get; set; }

        /// <summary>
        /// The amount of ROCK transactions received.
        /// </summary>
        public int TxsIn { get; set; }

        /// <summary>
        /// Is the account owned by a moderator of the community.
        /// </summary>
        public bool Moderator { get; set; }
    }

    public class VAccountActions : PersistentObjectActions<RockStatsContext, VAccount>
    {
        public VAccountActions(RockStatsContext context)
            : base(context)
        {
        }

        /// <summary>
        /// Load the Account PersistentObject when trying to load a VAccount PersistentObject.
        /// </summary>
        public override void OnPreviewLoad(PreviewLoadArgs e)
        {
            e.Reroute("Account");

            base.OnPreviewLoad(e);
        }
    }

    /// <summary>
    /// Index for calculating the current account details for use on the leaderboard.
    /// </summary>
    public class VAccounts_Details : AbstractMultiMapIndexCreationTask<VAccount>
    {
        public VAccounts_Details()
        {
            // Selects all transactions to calculate the amount of ROCKs sent.
            AddMap<Transaction>(senders => from tx in senders
                                           let sender = LoadDocument<Account>(tx.Sender)
                                           let amount = !string.IsNullOrEmpty(tx.Amount) ? tx.Amount : "0"
                                           select new VAccount
                                           {
                                               Id = sender.Id ?? "accounts/000000000000000000000000000000000000dead",
                                               Address = sender.Address,
                                               Redditor = sender.Owner,
                                               Avatar = sender.Avatar,
                                               Flairs = sender.Flairs,
                                               Balance = "-" + amount,
                                               Sent = tx.Receiver != "accounts/000000000000000000000000000000000000dead" ? amount : "0",
                                               Moderator = sender.Moderator.HasValue ? sender.Moderator.Value : false,
                                               TxsIn = 0,
                                               TxsOut = 1
                                           }); ;

            // Selects all transactions to calculate the ROCK balance.
            AddMap<Transaction>(receivers => from tx in receivers
                                             let receiver = LoadDocument<Account>(tx.Receiver)
                                             let amount = !string.IsNullOrEmpty(tx.Amount) ? tx.Amount : "0"
                                             select new VAccount
                                             {
                                                 Id = receiver.Id ?? "accounts/000000000000000000000000000000000000dead",
                                                 Address = receiver.Address,
                                                 Redditor = receiver.Owner,
                                                 Avatar = receiver.Avatar,
                                                 Flairs = receiver.Flairs,
                                                 Balance = amount,
                                                 Sent = "0",
                                                 Moderator = receiver.Moderator.HasValue ? receiver.Moderator.Value : false,
                                                 TxsIn = 1,
                                                 TxsOut = 0
                                             });

            // Reduce all mapped transactions from above, grouped by the Redditor that owns them.
            Reduce = results =>
                from result in results
                group result by result.Redditor
                into g
                let balance = g.Aggregate("0", (a, b) => BigDecimal.Add(BigDecimal.Parse(a), BigDecimal.Parse(b.Balance)).ToString())
                let sent = g.Aggregate("0", (a, b) => BigDecimal.Add(BigDecimal.Parse(a), BigDecimal.Parse(b.Sent)).ToString())
                select new VAccount
                {
                    Id = g.First().Id,
                    Address = g.First().Address,
                    Redditor = g.Key,
                    Avatar = g.First().Avatar,
                    Flairs = g.First().Flairs,
                    Balance = !string.IsNullOrEmpty(balance) ? balance : "0",
                    Sent = !string.IsNullOrEmpty(sent) ? sent : "0",
                    Moderator = g.First().Moderator,
                    TxsIn = g.Sum(t => t.TxsIn),
                    TxsOut = g.Sum(t => t.TxsOut)
                };

            AdditionalSources = new Dictionary<string, string>
            {
                { "BigDecimal", Properties.Resources.BigDecimal }
            };
        }
    }

    partial class RockStatsContext
    {
        public IRavenQueryable<VAccount> VAccounts => Query<VAccount, VAccounts_Details>().Where(a => a.Id != "accounts/000000000000000000000000000000000000dead").AsNoTracking();
    }
}
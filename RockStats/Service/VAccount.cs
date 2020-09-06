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

using Raven.Client.Documents.Indexes;
using Raven.Client.Documents.Linq;
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
        public decimal Balance { get; set; }

        /// <summary>
        /// The amount of ROCK tokens sent until now.
        /// </summary>
        public decimal Sent { get; set; }

        /// <summary>
        /// Is the account owned by a moderator of the community.
        /// </summary>
        public bool Moderator { get; set; }
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
                                           select new VAccount
                                           {
                                               Address = sender.Address,
                                               Redditor = sender.Owner,
                                               Avatar = sender.Avatar,
                                               Flairs = sender.Flairs,
                                               Balance = (decimal.Parse(tx.Amount) / 1000000000000000000M) * -1,
                                               Sent = tx.Receiver != "accounts/000000000000000000000000000000000000dead" ? decimal.Parse(tx.Amount) / 1000000000000000000M : 0M,
                                               Moderator = sender.Moderator.HasValue ? sender.Moderator.Value : false
                                           });

            // Selects all transactions to calculate the ROCK balance.
            AddMap<Transaction>(receivers => from tx in receivers
                                             let receiver = LoadDocument<Account>(tx.Receiver)
                                             select new VAccount
                                             {
                                                 Address = receiver.Address,
                                                 Redditor = receiver.Owner,
                                                 Avatar = receiver.Avatar,
                                                 Flairs = receiver.Flairs,
                                                 Balance = decimal.Parse(tx.Amount) / 1000000000000000000M,
                                                 Sent = 0M,
                                                 Moderator = receiver.Moderator.HasValue ? receiver.Moderator.Value : false
                                             });

            // Reduce all mapped transactions from above, grouped by the Redditor that owns them.
            Reduce = results =>
                from result in results
                group result by result.Redditor
                into g
                select new VAccount
                {
                    Address = g.First().Address,
                    Redditor = g.Key,
                    Avatar = g.First().Avatar,
                    Flairs = g.First().Flairs,
                    Balance = g.Sum(t => t.Balance),
                    Sent = g.Sum(t => t.Sent),
                    Moderator = g.First().Moderator
                };
        }
    }

    partial class RockStatsContext
    {
        public IRavenQueryable<VAccount> VAccounts => Query<VAccount, VAccounts_Details>().Where(a => !a.Moderator).AsNoTracking();
    }
}
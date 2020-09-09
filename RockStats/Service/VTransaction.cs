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
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Vidyano.Service.RavenDB;

namespace RockStats.Service
{
    /// <summary>
    /// A view object for the VTransaction_Details index.
    /// </summary>
    public class VTransaction
    {
        public string Id { get; set; }

        /// <summary>
        /// The timestamp of the transaction.
        /// </summary>
        public long Timestamp { get; set; }

        /// <summary>
        /// The transaction hash
        /// </summary>
        public string Hash { get; set; }

        /// <summary>
        /// The Reddit user that sent the transaction.
        /// </summary>
        public string Sender { get; set; }

        /// <summary>
        /// The Reddit user that received the transaction.
        /// </summary>
        public string Receiver { get; set; }

        /// <summary>
        /// The amount of ROCK tokens sent in this transaction.
        /// </summary>
        public string Amount { get; set; }

        /// <summary>
        /// Optional transaction metadata
        /// </summary>
        public string Metadata { get; set; }
    }

    public class VTransaction_Details : AbstractIndexCreationTask<Transaction>
    {
        public VTransaction_Details()
        {
            Map = transactions =>
                from tx in transactions
                let sender = LoadDocument<Account>(tx.Sender)
                let receiver = LoadDocument<Account>(tx.Receiver)
                select new VTransaction
                {
                    Timestamp = (long)tx.Timestamp.Subtract(new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)).TotalSeconds,
                    Hash = tx.Hash,
                    Sender = sender != null ? sender.Owner : "0x" + tx.Sender.Split(new[] { '/' }, 2)[1],
                    Receiver = receiver != null ? receiver.Owner : "0x" + tx.Receiver.Split(new[] { '/' }, 2)[1],
                    Amount = tx.Amount,
                    Metadata = tx.Metadata
                };

            StoreAllFields(FieldStorage.Yes);
        }
    }

    partial class RockStatsContext
    {
        public IRavenQueryable<VTransaction> VTransactions => Query<VTransaction, VTransaction_Details>().AsNoTracking();
    }
}
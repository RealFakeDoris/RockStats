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

using Raven.Client.Documents.Linq;
using System;
using Vidyano.Service.Repository;

namespace RockStats.Service
{
    /// <summary>
    /// Database Transaction object.
    /// </summary>
    public class Transaction
    {
        /// <summary>
        /// The id of the transaction in the form of blocks/[BLOCKNUMBER]/txs/accounts/[SENDER_ADDRESS]/accounts/[RECEIVER_ADDRESS]
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// The timestamp of the transaction.
        /// </summary>
        public DateTime Timestamp { get; set; }

        /// <summary>
        /// A reference to the block that contains the transaction.
        /// </summary>
        [Reference(typeof(Block))]
        public string Block { get; set; }

        /// <summary>
        /// A reference to the account that send the transaction.
        /// </summary>
        [Reference(typeof(Account))]
        public string Sender { get; set; }

        /// <summary>
        /// A reference to the account the received the transaction.
        /// </summary>
        [Reference(typeof(Account))]
        public string Receiver { get; set; }

        /// <summary>
        /// The amount of tokens sent.
        /// </summary>
        public string Amount { get; set; }
    }

    partial class RockStatsContext
    {
        public IRavenQueryable<Transaction> BlockTransactions => base.Query<Transaction>();
    }
}
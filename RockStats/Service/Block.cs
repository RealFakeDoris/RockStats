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

using Raven.Client.Documents;
using Raven.Client.Documents.Indexes;
using Raven.Client.Documents.Linq;
using Raven.Client.Documents.Queries.Facets;
using Raven.Client.Documents.Session;
using RockStats.Watcher;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Vidyano.Service.Repository;

namespace RockStats.Service
{
    /// <summary>
    /// Database Block object.
    /// </summary>
    public class Block
    {
        /// <summary>
        /// The id of the block in the form of blocks/1262000
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// The block's height.
        /// </summary>
        public long Number { get; set; }

        /// <summary>
        /// The OMG Network synchronization task.
        /// </summary>
        /// <param name="job">The Job object with additional parameters.</param>
        /// <param name="session">The RavenDB session object.</param>
        /// <returns>A task that can be awaited until the job is finished.</returns>
        public static async Task Sync(Job job, IAsyncDocumentSession session)
        {
            // Check if the watcher is online.
            var status = await WatcherClient.Status();
            if (!status.Success)
                return;

            // Use the Blocks_View index to get the latest block number.
            var queryable = session.Query<Block, Blocks_View>();
            var aggregate = queryable.AggregateBy(new Facet
            {
                Aggregations = new Dictionary<FacetAggregation, string>
                    {
                        { FacetAggregation.Max, "Number" },
                    },
                DisplayFieldName = "Max_Number",
            });

            var result = await aggregate.ExecuteAsync();
            var currentHeight = (long)result["Max_Number"].Values[0].Max;

            // Gets the ROCK currency address.
            var rock = job.Parameters.First(p => p.Name == "Rock").Value;

            using var bulkInsert = session.Advanced.DocumentStore.BulkInsert();
            for (var height = currentHeight; height <= status.Data.Last_Mined_Child_Block_Number; height += 1000)
            {
                // Create the new Block.
                var block = new Block
                {
                    Id = $"blocks/{height}",
                    Number = height
                };

                // Store the new Block.
                await bulkInsert.StoreAsync(block);

                // Get the transactions for this Block.
                var txList = await WatcherClient.BlockTransactionList(height);
                if (txList == null)
                    break;

                foreach (var tx in txList.Transactions)
                {
                    // Does this transaction include a ROCK transfer?
                    var input = tx.Inputs.FirstOrDefault(t => t.Currency == rock);
                    if (input == null)
                        continue;

                    // Store the receiver ROCK transactions that are different from the sender.
                    foreach (var receiver in tx.Outputs.Where(t => t.Currency == rock && t.Owner != input.Owner))
                    {
                        await bulkInsert.StoreAsync(new Transaction
                        {
                            Id = $"blocks/{height}/txs/accounts/{input.Owner[2..]}/accounts/{receiver.Owner[2..]}",
                            Timestamp = tx.Block.Timestamp.FromUnixTime(),
                            Block = block.Id,
                            Sender = $"accounts/{input.Owner[2..]}",
                            Receiver = $"accounts/{receiver.Owner[2..]}",
                            Amount = receiver.Amount
                        });
                    }
                }
            }
        }
    }

    // This index view object is used to project the blocknumbers in the database.
    public class Blocks_View : AbstractIndexCreationTask<Block>
    {
        public class Result
        {
            public long Number { get; set; }
        }

        public Blocks_View()
        {
            Map = blocks => from block in blocks
                            select new Result
                            {
                                Number = block.Number
                            };
        }
    }

    partial class RockStatsContext
    {
        public IRavenQueryable<Block> Blocks => base.Query<Block>();
    }
}
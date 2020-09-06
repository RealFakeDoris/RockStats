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

using Newtonsoft.Json;
using System;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace RockStats.Watcher
{
    public static class WatcherClient
    {
        /// <summary>
        /// Keep a static instance of the HttpClient, as recommended.
        /// </summary>
        private static HttpClient client = new HttpClient
        {
            BaseAddress = new Uri("https://watcher-info.mainnet.v1.omg.network")
            //BaseAddress = new Uri("http://localhost:7534")
        };

        static WatcherClient()
        {
            client.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));
        }

        /// <summary>
        /// Retrieves network statistics.
        /// </summary>
        /// <returns>The network statistics.</returns>
        public static Task<Status> Status()
        {
            return Post<Status>("status.get");
        }

        /// <summary>
        /// Gets a list of transactions for given block.
        /// </summary>
        /// <param name="blockNumber">The number of the block for which to get the transactions.</param>
        /// <returns>The list of transaction for this block.</returns>
        public static async Task<BlockTransactionList> BlockTransactionList(long blockNumber)
        {
            return await PostPaged<BlockTransactionList>("transaction.all", new BlockTransactionListArgument
            {
                BlockNumber = blockNumber,
                Limit = 50
            }, (list1, list2) =>
            {
                if (list1 != null)
                    list1.Transactions = list1.Transactions.Concat(list2.Transactions).ToArray();

                return (list1 ?? list2, list2.Transactions.Length < 50);
            });
        }

        /// <summary>
        /// Helper function to keep querying the next pages in case the limit for a single request is reached.
        /// </summary>
        static async Task<T> PostPaged<T>(string path, WatcherRequestBase requestArguments, Func<T, T, (T result, bool done)> merger)
            where T: WatcherResponseBase
        {
            requestArguments.Page = 1;
            T endResult = null;
            do
            {
                var result = await Post<T>(path, requestArguments);
                if (!result.Success)
                    break;

                var mergeResult = merger(endResult, result);
                endResult = mergeResult.result;
                if (mergeResult.done)
                    break;

                requestArguments.Page++;
            }
            while (true);

            return endResult;
        }

        /// <summary>
        /// Helper function to make the REST call.
        /// </summary>
        static async Task<T> Post<T>(string path, WatcherRequestBase requestArguments = null)
        {
            var input = requestArguments == null ? "" : JsonConvert.SerializeObject(requestArguments);

            var result = await client.PostAsync(path, new StringContent(input, Encoding.UTF8, "application/json")).ConfigureAwait(false);
            var content = await result.Content.ReadAsStringAsync().ConfigureAwait(false);
            return JsonConvert.DeserializeObject<T>(content);
        }
    }
}
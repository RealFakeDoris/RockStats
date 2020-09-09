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

namespace RockStats.Watcher
{
    public class BlockTransactionListArgument: WatcherRequestBase
    {
        [JsonProperty("blknum")]
        public long BlockNumber { get; set; }
    }

    public class BlockTransactionList: WatcherResponseBase
    {
        [JsonProperty("data")]
        public BlockTransaction[] Transactions { get; set; }
    }

    public class BlockTransaction
    {
        [JsonProperty("txindex")]
        public int Index { get; set; }

        [JsonProperty("txhash")]
        public string Hash { get; set; }

        public BlockTransactionBlock Block { get; set; }

        public BlockTransactionIO[] Inputs { get; set; }
        
        public BlockTransactionIO[] Outputs { get; set; }

        public string Metadata { get; set; }
    }

    public class BlockTransactionBlock
    {
        public long Timestamp { get; set; }
    }

    public class BlockTransactionIO
    {
        [JsonProperty("oindex")]
        public int Index { get; set; }

        public string Amount { get; set; }
        
        public string Currency { get; set; }

        public string Owner { get; set; }

        [JsonProperty("inserted_at")]
        public string InsertedAt { get; set; }

    }
}
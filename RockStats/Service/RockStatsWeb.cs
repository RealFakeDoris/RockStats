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

using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Vidyano.Service;
using Vidyano.Service.Repository;

namespace RockStats.Service
{
    public class RockStatsWeb : CustomApiController
    {
        /// <summary>
        /// View object to return the account data to 3rd party websites.
        /// </summary>
        class Account
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
        }

        class Balance: Account
        {
            /// <summary>
            /// The current ROCK balance for this account.
            /// </summary>
            public decimal Amount { get; set; }
        }

        class Sent : Account
        {
            /// <summary>
            /// The amount of ROCKs sent from this account.
            /// </summary>
            public decimal Amount { get; set; }
        }

        /// <summary>
        /// API call to get the accounts sorted by their current balance.
        /// </summary>
        [HttpPost]
        public IActionResult ByBalance(ApiArgs args)
        {
            if (args.AccessToken != Manager.Current.GetSetting("ThirdParty_API_Key"))
                return BadRequest();

            var context = args.GetContext<RockStatsContext>();
            var accounts = context.VAccounts.ToArray().Select(a => new Balance
            {
                Redditor = a.Redditor,
                Flairs = a.Flairs,
                Address = a.Address,
                Avatar = a.Avatar,
                Amount = a.Balance
            }).ToArray();

            return Ok(JsonConvert.SerializeObject(accounts));
        }

        /// <summary>
        /// API call to get the accounts sorted by their amount of ROCKs sent.
        /// </summary>
        [HttpPost]
        public IActionResult BySent(ApiArgs args)
        {
            if (args.AccessToken != Manager.Current.GetSetting("ThirdParty_API_Key"))
                return BadRequest();

            var context = args.GetContext<RockStatsContext>();
            var accounts = context.VAccounts.ToArray().Select(a => new Sent
            {
                Redditor = a.Redditor,
                Flairs = a.Flairs,
                Address = a.Address,
                Avatar = a.Avatar,
                Amount = a.Sent
            }).ToArray();

            return Ok(JsonConvert.SerializeObject(accounts));
        }
    }
}
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
using Raven.Client.Documents.Linq;
using Raven.Client.Documents.Session;
using Reddit.Controllers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Vidyano.Core.Services;
using Vidyano.Service.Repository;

namespace RockStats.Service
{
    /// <summary>
    /// Database Account object.
    /// </summary>
    public class Account
    {
        /// <summary>
        /// The regular expression to get the address from the Reddit post.
        /// </summary>
        private static readonly Regex AddressRegEx = new Regex("(0x[0-9a-fA-F]{40})+");

        /// <summary>
        /// The id of the account in the form of accounts/CD7F82BcFa333B4072A11Bd0B1da95c9b5f9E869
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// The full blockchain address.
        /// </summary>
        public string Address { get; set; }

        /// <summary>
        /// The Reddit user that owns this address.
        /// </summary>
        public string Owner { get; set; }

        /// <summary>
        /// The Reddit user short id for getting details.
        /// </summary>
        public string OwnerId { get; set; }

        /// <summary>
        /// The Reddit user avatar url.
        /// </summary>
        public string Avatar { get; set; }

        /// <summary>
        /// The Reddit user flairs.
        /// </summary>
        public string Flairs { get; set; }

        /// <summary>
        /// Indicator for flagging community moderators.
        /// </summary>
        public bool? Moderator { get; set; }

        /// <summary>
        /// The Reddit account synchronization task.
        /// </summary>
        /// <param name="job">The Job object with additional parameters.</param>
        /// <param name="session">The RavenDB session object.</param>
        /// <returns>A task that can be awaited until the job is finished.</returns>
        public static async Task Sync(Job job, IAsyncDocumentSession session)
        {
            var client = new Reddit.RedditClient(Manager.Current.GetSetting("RedditAppId"), Manager.Current.GetSetting("RedditAppRefreshToken"));

            // Get the Reddit post.
            var rocks = client.Subreddit("OMGNetwork").Post($"t3_{job.Parameters.First(p => p.Name == "Post").Value}");

            // Get the comments from the Reddit post.
            var comments = rocks.Comments.GetComments(depth: 0, limit: 10000);

            var commentsByAddress = new Dictionary<string, Comment>();
            foreach (var comment in comments.Where(c => !String.IsNullOrEmpty(c.Body)))
            {
                // Extract all addresses from the Reddit post.
                var matches = AddressRegEx.Matches(comment.Body).Select(m => m.Value).ToArray();
                foreach (var address in matches)
                {
                    if (!commentsByAddress.TryAdd(address, comment) && commentsByAddress[address].Created > comment.Created)
                    {
                        // If the address was found twice, set the owner to the comment that first mentioned it.
                        commentsByAddress[address] = comment;
                    }
                }
            }

            // Transform the addresses into database account id's and load those accounts.
            var addressIds = commentsByAddress.Keys.Select(a => $"accounts/{a[2..]}");
            var accounts = await session.LoadAsync<Account>(addressIds);
            
            // Bulk insert all new addresses.
            using var bulkInsert = session.Advanced.DocumentStore.BulkInsert();
            foreach (var account in accounts.Where(a => a.Value == null))
            {
                var address = $"0x{account.Key[9..]}";
                var comment = commentsByAddress[address];

                bulkInsert.Store(new Account
                {
                    Id = account.Key,
                    Address = address,
                    Owner = comment.Author,
                    OwnerId = comment.Listing.AuthorFullname,
                    Flairs = comment.Listing.AuthorFlairText
                });
            }

            // Update the avatar and flairs for the accounts that already existed.
            foreach (var account in accounts.Where(a => a.Value != null))
            {
                account.Value.Flairs = commentsByAddress[account.Value.Address].Listing.AuthorFlairText;

                // If we didn't get the avatar for this user, get it now.
                if (account.Value.Avatar == null)
                {
                    try
                    {
                        var userData = client.Models.Users.UserDataByAccountIds(account.Value.OwnerId);
                        account.Value.Avatar = userData.First().Value.ProfileImg;
                    }
                    catch (Exception e)
                    {
                        // The user might have been deleted. Reddit returns 404 in that case.
                        ServiceLocator.GetService<IExceptionService>().Log(new Exception($"Unable to get account user data ({account.Key})", e));
                    }
                }
            }

            await session.SaveChangesAsync();
        }
    }

    partial class RockStatsContext
    {
        public IRavenQueryable<Account> Accounts => Query<Account>();
    }
}
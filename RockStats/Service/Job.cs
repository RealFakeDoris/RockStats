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
using Raven.Client.Documents.Subscriptions;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using Vidyano.Core.Extensions;
using Vidyano.Core.Services;
using Vidyano.Service.Repository;

namespace RockStats.Service
{
    /// <summary>
    /// Database Job object.
    /// </summary>
    public class Job
    {
        /// <summary>
        /// The id of the job in the form of jobs/ddf959a2dfd748afa08278d344a223ed
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// The name of the Job.
        /// </summary>
        [Required, MaxLength(255)]
        public string Name { get; set; }

        /// <summary>
        /// The interval to trigger a refresh, in seconds.
        /// </summary>
        [Required]
        public int RefreshTimeInSeconds { get; set; }

        /// <summary>
        /// Additional parameters for this job.
        /// </summary>
        public IList<JobParameter> Parameters { get; set; } = new List<JobParameter>();

        /// <summary>
        /// The Job run task.
        /// </summary>
        /// <param name="batch">The batch of jobs that were triggered by the subscription.</param>
        /// <returns>A task that can be awaited until the work is finished.</returns>
        public static async Task Run(SubscriptionBatch<Job> batch)
        {
            using var session = batch.OpenAsyncSession();

            try
            {
                foreach (var item in batch.Items)
                {
                    var job = item.Result;

                    try
                    {
                        // Delegate the task to the corresponding Sync function.
                        if (job.Name == "Reddit")
                            await Account.Sync(job, session);
                        else if (job.Name == "OMGNetwork")
                            await Block.Sync(job, session);

                        // Create or update the last run parameter.
                        var lastRun = job.Parameters.FirstOrDefault(p => p.Name == "LastRun");
                        if (lastRun == null)
                            job.Parameters.Add(new JobParameter { Name = "LastRun" });

                        lastRun.Value = $"{DateTime.Now.ToString("MM/dd/yyyy HH:mm")}";
                    }
                    catch(Exception e)
                    {
                        ServiceLocator.GetService<IExceptionService>().Log(e);
                    }
                    finally
                    {
                        // Set the Refresh metadata to trigger a refresh on the database after x seconds.
                        var metadata = session.Advanced.GetMetadataFor(job);
                        metadata[Raven.Client.Constants.Documents.Metadata.Refresh] = DateTime.UtcNow.AddSeconds(job.RefreshTimeInSeconds);
                    }
                }
            }
            finally
            {
                await session.SaveChangesAsync();
            }
        }
    }

    public class JobActions : PersistentObjectActions<RockStatsContext, Job>
    {
        public JobActions(RockStatsContext context)
            : base(context)
        {
        }

        /// <summary>
        /// Persists the job to the database.
        /// </summary>
        /// <param name="obj">The persistent object.</param>
        /// <param name="job">The job database object.</param>
        protected override void PersistToContext(PersistentObject obj, Job job)
        {
            // Additional logic to update, delete or create job parameters.
            var parameters = obj.GetAttribute("Parameters") as PersistentObjectAttributeAsDetail;
            parameters.DeletedObjects.Run(po =>
            {
                var pe = job.Parameters.FirstOrDefault(p => p.Name == (string)po.GetAttributeValue("Name"));
                if (pe != null)
                    job.Parameters.Remove(pe);
            });

            parameters.Objects.Run(po =>
            {
                if (po.IsNew)
                {
                    job.Parameters.Add(new JobParameter
                    {
                        Name = (string)po.GetAttributeValue("Name"),
                        Value = (string)po.GetAttributeValue("Value")
                    });
                }
                else
                {
                    var pe = job.Parameters.FirstOrDefault(p => p.Name == (string)po.GetAttributeValue("Name"));
                    if (pe != null)
                        pe.Value = (string)po.GetAttributeValue("Value");
                }
            });

            base.PersistToContext(obj, job);

            // Set the initial Refresh metadata to trigger a refresh on the database after x seconds.
            var metadata = Context.Session.Advanced.GetMetadataFor(job);
            metadata[Raven.Client.Constants.Documents.Metadata.Refresh] = DateTime.UtcNow.AddSeconds(job.RefreshTimeInSeconds);

            Context.Session.SaveChanges();
        }
    }

    partial class RockStatsContext
    {
        public IRavenQueryable<Job> Jobs => base.Query<Job>();
    }
}
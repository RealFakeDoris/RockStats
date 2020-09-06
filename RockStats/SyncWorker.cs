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
using Raven.Client.Documents.Subscriptions;
using Raven.Client.Exceptions.Database;
using Raven.Client.Exceptions.Documents.Subscriptions;
using Raven.Client.Exceptions.Security;
using System;
using System.Threading.Tasks;
using Vidyano.Core.Services;

namespace RockStats
{
    public static class SyncWorker
    {
        public static async void Run<T>(DocumentStore store, string name, Func<SubscriptionBatch<T>, Task> action, TimeSpan? maxDownTime = null, TimeSpan? retryWaitTime = null)
            where T: class
        {
            // Run the rest of this function in a seperate task.
            await Task.Delay(1);

            while (true)
            {
                var options = new SubscriptionWorkerOptions(name)
                {
                    MaxErroneousPeriod = maxDownTime ?? TimeSpan.FromMinutes(1),
                    TimeToWaitBeforeConnectionRetry = retryWaitTime ?? TimeSpan.FromSeconds(30)
                };

                var subscriptionWorker = store.Subscriptions.GetSubscriptionWorker<T>(options);

                try
                {
                    // here we are able to be informed of any exception that happens during processing
                    subscriptionWorker.OnSubscriptionConnectionRetry += exception =>
                    {
                        if (!(exception is SubscriptionDoesNotBelongToNodeException))
                            ServiceLocator.GetService<IExceptionService>().Log(exception);
                    };

                    await subscriptionWorker.Run(action); //, cancellationToken);

                    // Run will complete normally if you have disposed the subscription
                    return;
                }
                catch (Exception e)
                {
                    if (!(e is SubscriptionInUseException))
                        ServiceLocator.GetService<IExceptionService>().Log(e);

                    if (e is DatabaseDoesNotExistException ||
                        e is SubscriptionDoesNotExistException ||
                        e is SubscriptionInvalidStateException ||
                        e is AuthorizationException)
                        throw; // not recoverable


                    if (e is SubscriptionClosedException)
                        // closed explicitly by admin, probably
                        return;

                    if (e is SubscriberErrorException se)
                    {
                        // for UnsupportedCompanyException type, we want to throw an exception, otherwise
                        // we continue processing
                        //if (se.InnerException != null && se.InnerException is UnsupportedCompanyException)
                        //{
                        //    throw;
                        //}

                        continue;
                    }

                    // handle this depending on subscription
                    // open strategy (discussed later)
                    if (e is SubscriptionInUseException)
                        continue;

                    return;
                }
                finally
                {
                    await subscriptionWorker.DisposeAsync();
                }
            }
        }
    }
}
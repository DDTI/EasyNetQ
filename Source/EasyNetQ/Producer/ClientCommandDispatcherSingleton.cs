using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using EasyNetQ.Internals;
using RabbitMQ.Client;

namespace EasyNetQ.Producer
{
    public class ClientCommandDispatcherSingleton : IClientCommandDispatcher
    {
        private const int queueSize = 1;
        private readonly CancellationTokenSource cancellation = new CancellationTokenSource();
        private readonly IPersistentChannel persistentChannel;
        private readonly BlockingCollection<Action> queue = new BlockingCollection<Action>(queueSize);

        public ClientCommandDispatcherSingleton(
            ConnectionConfiguration configuration,
            IPersistentConnection connection,
            IPersistentChannelFactory persistentChannelFactory)
        {
            Preconditions.CheckNotNull(configuration, "configuration");
            Preconditions.CheckNotNull(connection, "connection");
            Preconditions.CheckNotNull(persistentChannelFactory, "persistentChannelFactory");

            persistentChannel = persistentChannelFactory.CreatePersistentChannel(connection);

            StartDispatcherThread(configuration);
        }

        public T Invoke<T>(Func<IModel, T> channelAction)
        {
            try
            {
                return InvokeAsync(channelAction).Result;
            }
            catch (AggregateException e)
            {
                throw e.InnerException;
            }
        }

        public void Invoke(Action<IModel> channelAction)
        {
            try
            {
                InvokeAsync(channelAction).Wait();
            }
            catch (AggregateException e)
            {
                throw e.InnerException;
            }
        }

        public Task<T> InvokeAsync<T>(Func<IModel, T> channelAction)
        {
            Preconditions.CheckNotNull(channelAction, "channelAction");

            var tcs = new TaskCompletionSource<T>();
			// Set the start time used for calculating the message timeout to be the time when it was added to the queue, 
			// instead of the time when it is pulled out of the queue when the includeQueTimeInTimeout option is used in the connection string. 
			DateTime startTime = DateTime.UtcNow; 

            try
            {
                queue.Add(() =>
                {
                    if (cancellation.IsCancellationRequested)
                    {
                        tcs.TrySetCanceledSafe();
                        return;
                    }
                    try
                    {
                        persistentChannel.InvokeChannelAction(channel => tcs.TrySetResultSafe(channelAction(channel)),startTime);
                    }
                    catch (Exception e)
                    {
                        tcs.TrySetExceptionSafe(e);
                    }
                }, cancellation.Token);
            }
            catch (OperationCanceledException)
            {
                tcs.TrySetCanceled();
            }
            return tcs.Task;
        }

        public Task InvokeAsync(Action<IModel> channelAction)
        {
            Preconditions.CheckNotNull(channelAction, "channelAction");

            return InvokeAsync(x =>
            {
                channelAction(x);
                return new NoContentStruct();
            });
        }

        public void Dispose()
        {
            cancellation.Cancel();
            persistentChannel.Dispose();
        }

        private void StartDispatcherThread(ConnectionConfiguration configuration)
        {
            var thread = new Thread(() =>
            {
                while (!cancellation.IsCancellationRequested)
                {
                    try
                    {
                        var channelAction = queue.Take(cancellation.Token);
                        channelAction();
                    }
                    catch (OperationCanceledException)
                    {
                        break;
                    }
                }
            }) {Name = "Client Command Dispatcher Thread", IsBackground = configuration.UseBackgroundThreads};
            thread.Start();
        }

        private struct NoContentStruct
        {
        }
    }
}
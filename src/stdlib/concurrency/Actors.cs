using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Ouroboros.Stdlib.Concurrency
{
    /// <summary>
    /// Actor-based concurrency model
    /// </summary>
    public abstract class Actor<TMessage>
    {
        private readonly Channel<TMessage> mailbox;
        private readonly CancellationTokenSource cts = new();
        private Task? processingTask;
        private readonly ActorContext context;
        protected readonly ActorSystem system;

        public ActorId Id { get; }
        public ActorState State { get; private set; } = ActorState.Created;
        public string Name { get; }

        protected Actor(ActorSystem system, string? name = null, int mailboxCapacity = 1000)
        {
            this.system = system;
            Id = ActorId.NewId();
            Name = name ?? $"Actor-{Id}";
            mailbox = new Channel<TMessage>(mailboxCapacity);
            context = new ActorContext(this, system);
        }

        /// <summary>
        /// Start the actor
        /// </summary>
        public void Start()
        {
            if (State != ActorState.Created)
                throw new InvalidOperationException($"Actor {Name} is already started");

            State = ActorState.Running;
            processingTask = Task.Run(ProcessMessages);
            OnStart();
        }

        /// <summary>
        /// Stop the actor
        /// </summary>
        public async Task StopAsync()
        {
            if (State != ActorState.Running)
                return;

            State = ActorState.Stopping;
            mailbox.Close();
            cts.Cancel();

            if (processingTask != null)
                await processingTask;

            State = ActorState.Stopped;
            OnStop();
        }

        /// <summary>
        /// Send a message to this actor
        /// </summary>
        public async Task<bool> SendAsync(TMessage message)
        {
            if (State != ActorState.Running)
                return false;

            try
            {
                return await mailbox.SendAsync(message);
            }
            catch (ChannelClosedException)
            {
                return false;
            }
        }

        /// <summary>
        /// Tell - fire and forget message send
        /// </summary>
        public void Tell(TMessage message)
        {
            if (State == ActorState.Running)
            {
                _ = SendAsync(message);
            }
        }

        /// <summary>
        /// Handle incoming message
        /// </summary>
        protected abstract Task HandleAsync(TMessage message);

        /// <summary>
        /// Called when actor starts
        /// </summary>
        protected virtual void OnStart() { }

        /// <summary>
        /// Called when actor stops
        /// </summary>
        protected virtual void OnStop() { }

        /// <summary>
        /// Called when unhandled exception occurs
        /// </summary>
        protected virtual void OnError(Exception exception)
        {
            Console.Error.WriteLine($"Actor {Name} error: {exception}");
        }

        private async Task ProcessMessages()
        {
            try
            {
                await foreach (var message in mailbox.GetAsyncEnumerable(cts.Token))
                {
                    try
                    {
                        await HandleAsync(message);
                    }
                    catch (Exception ex)
                    {
                        OnError(ex);
                    }
                }
            }
            catch (OperationCanceledException)
            {
                // Expected when stopping
            }
        }

        protected ActorContext Context => context;
    }

    /// <summary>
    /// Actor that can respond to requests
    /// </summary>
    public abstract class RequestResponseActor<TRequest, TResponse> : Actor<ActorMessage<TRequest, TResponse>>
    {
        protected RequestResponseActor(ActorSystem system, string? name = null, int mailboxCapacity = 1000)
            : base(system, name, mailboxCapacity)
        {
        }

        protected override async Task HandleAsync(ActorMessage<TRequest, TResponse> message)
        {
            try
            {
                var response = await ProcessRequestAsync(message.Request);
                message.SetResponse(response);
            }
            catch (Exception ex)
            {
                message.SetError(ex);
            }
        }

        protected abstract Task<TResponse> ProcessRequestAsync(TRequest request);

        /// <summary>
        /// Ask - request with response
        /// </summary>
        public async Task<TResponse> AskAsync(TRequest request, TimeSpan? timeout = null)
        {
            var message = new ActorMessage<TRequest, TResponse>(request);
            
            if (!await SendAsync(message))
                throw new ActorException("Failed to send message to actor");

            if (timeout.HasValue)
            {
                using var cts = new CancellationTokenSource(timeout.Value);
                return await message.GetResponseAsync(cts.Token);
            }

            return await message.GetResponseAsync();
        }
    }

    /// <summary>
    /// Actor message with response
    /// </summary>
    public class ActorMessage<TRequest, TResponse>
    {
        private readonly TaskCompletionSource<TResponse> tcs = new();
        
        public TRequest Request { get; }
        
        public ActorMessage(TRequest request)
        {
            Request = request;
        }

        internal void SetResponse(TResponse response)
        {
            tcs.SetResult(response);
        }

        internal void SetError(Exception exception)
        {
            tcs.SetException(exception);
        }

        public Task<TResponse> GetResponseAsync(CancellationToken cancellationToken = default)
        {
            using (cancellationToken.Register(() => tcs.TrySetCanceled()))
            {
                return tcs.Task;
            }
        }
    }

    /// <summary>
    /// Actor system for managing actors
    /// </summary>
    public class ActorSystem
    {
        private readonly ConcurrentDictionary<ActorId, object> actors = new();
        private readonly ConcurrentDictionary<string, ActorId> namedActors = new();
        private readonly CancellationTokenSource shutdownCts = new();

        public string Name { get; }
        public bool IsShuttingDown => shutdownCts.Token.IsCancellationRequested;

        public ActorSystem(string name = "default")
        {
            Name = name;
        }

        /// <summary>
        /// Create and register an actor
        /// </summary>
        public TActor CreateActor<TActor>(Func<ActorSystem, TActor> factory, string? name = null)
            where TActor : class
        {
            var actor = factory(this);
            
            if (actor is IActorBase actorBase)
            {
                actors[actorBase.Id] = actor;
                
                if (name != null)
                {
                    namedActors[name] = actorBase.Id;
                }
                
                actorBase.Start();
            }

            return actor;
        }

        /// <summary>
        /// Get actor by ID
        /// </summary>
        public TActor? GetActor<TActor>(ActorId id) where TActor : class
        {
            return actors.TryGetValue(id, out var actor) ? actor as TActor : null;
        }

        /// <summary>
        /// Get actor by name
        /// </summary>
        public TActor? GetActor<TActor>(string name) where TActor : class
        {
            if (namedActors.TryGetValue(name, out var id))
            {
                return GetActor<TActor>(id);
            }
            return null;
        }

        /// <summary>
        /// Stop an actor
        /// </summary>
        public async Task StopActorAsync(ActorId id)
        {
            if (actors.TryRemove(id, out var actor) && actor is IActorBase actorBase)
            {
                await actorBase.StopAsync();
            }
        }

        /// <summary>
        /// Shutdown the actor system
        /// </summary>
        public async Task ShutdownAsync()
        {
            shutdownCts.Cancel();
            
            var stopTasks = new List<Task>();
            foreach (var actor in actors.Values)
            {
                if (actor is IActorBase actorBase)
                {
                    stopTasks.Add(actorBase.StopAsync());
                }
            }

            await Task.WhenAll(stopTasks);
            actors.Clear();
            namedActors.Clear();
        }
    }

    /// <summary>
    /// Actor context for accessing system services
    /// </summary>
    public class ActorContext
    {
        private readonly IActorBase actor;
        private readonly ActorSystem system;

        internal ActorContext(IActorBase actor, ActorSystem system)
        {
            this.actor = actor;
            this.system = system;
        }

        public ActorId Self => actor.Id;
        public ActorSystem System => system;

        /// <summary>
        /// Create a child actor
        /// </summary>
        public TActor CreateChild<TActor>(Func<ActorSystem, TActor> factory, string? name = null)
            where TActor : class
        {
            var childName = name != null ? $"{actor.Name}/{name}" : null;
            return system.CreateActor(factory, childName);
        }

        /// <summary>
        /// Schedule a message to self
        /// </summary>
        public void ScheduleSelf<TMessage>(TMessage message, TimeSpan delay)
        {
            Task.Delay(delay).ContinueWith(_ =>
            {
                if (actor is Actor<TMessage> typedActor)
                {
                    typedActor.Tell(message);
                }
            });
        }
    }

    /// <summary>
    /// Actor ID
    /// </summary>
    public struct ActorId : IEquatable<ActorId>
    {
        private readonly Guid value;

        private ActorId(Guid value)
        {
            this.value = value;
        }

        public static ActorId NewId() => new(Guid.NewGuid());

        public static ActorId Parse(string id) => new(Guid.Parse(id));

        public override string ToString() => value.ToString();

        public bool Equals(ActorId other) => value.Equals(other.value);

        public override bool Equals(object? obj) => obj is ActorId other && Equals(other);

        public override int GetHashCode() => value.GetHashCode();

        public static bool operator ==(ActorId left, ActorId right) => left.Equals(right);

        public static bool operator !=(ActorId left, ActorId right) => !left.Equals(right);
    }

    /// <summary>
    /// Actor state
    /// </summary>
    public enum ActorState
    {
        Created,
        Running,
        Stopping,
        Stopped,
        Failed
    }

    /// <summary>
    /// Actor base interface
    /// </summary>
    internal interface IActorBase
    {
        ActorId Id { get; }
        string Name { get; }
        ActorState State { get; }
        void Start();
        Task StopAsync();
    }

    /// <summary>
    /// Actor exception
    /// </summary>
    public class ActorException : Exception
    {
        public ActorException(string message) : base(message) { }
        public ActorException(string message, Exception inner) : base(message, inner) { }
    }

    /// <summary>
    /// Supervisor actor for fault tolerance
    /// </summary>
    public abstract class SupervisorActor<TMessage> : Actor<TMessage>
    {
        private readonly Dictionary<ActorId, IActorBase> children = new();
        private readonly SupervisorStrategy strategy;

        protected SupervisorActor(ActorSystem system, SupervisorStrategy strategy, string? name = null)
            : base(system, name)
        {
            this.strategy = strategy;
        }

        protected TActor CreateChildActor<TActor>(Func<ActorSystem, TActor> factory, string? name = null)
            where TActor : class, IActorBase
        {
            var child = Context.CreateChild(factory, name);
            if (child is IActorBase actorBase)
            {
                children[actorBase.Id] = actorBase;
            }
            return child;
        }

        protected async Task RestartChild(ActorId childId)
        {
            if (children.TryGetValue(childId, out var child))
            {
                await child.StopAsync();
                child.Start();
            }
        }

        protected async Task StopChild(ActorId childId)
        {
            if (children.TryGetValue(childId, out var child))
            {
                children.Remove(childId);
                await child.StopAsync();
            }
        }

        protected override void OnError(Exception exception)
        {
            // Apply supervisor strategy
            switch (strategy)
            {
                case SupervisorStrategy.Restart:
                    // Restart the failed child
                    break;
                case SupervisorStrategy.Stop:
                    // Stop the failed child
                    break;
                case SupervisorStrategy.Escalate:
                    // Escalate to parent
                    base.OnError(exception);
                    break;
                case SupervisorStrategy.Resume:
                    // Resume processing
                    break;
            }
        }
    }

    /// <summary>
    /// Supervisor strategy
    /// </summary>
    public enum SupervisorStrategy
    {
        Resume,
        Restart,
        Stop,
        Escalate
    }

    /// <summary>
    /// Router actor for load balancing
    /// </summary>
    public class RouterActor<TMessage> : Actor<TMessage>
    {
        private readonly List<IActorBase> routees = new();
        private readonly RoutingStrategy strategy;
        private int currentIndex = 0;

        public RouterActor(ActorSystem system, RoutingStrategy strategy, int routeeCount, 
            Func<ActorSystem, Actor<TMessage>> routeeFactory, string? name = null)
            : base(system, name)
        {
            this.strategy = strategy;

            for (int i = 0; i < routeeCount; i++)
            {
                var routee = routeeFactory(system);
                routees.Add(routee);
                routee.Start();
            }
        }

        protected override async Task HandleAsync(TMessage message)
        {
            var target = SelectRoutee();
            if (target is Actor<TMessage> actor)
            {
                await actor.SendAsync(message);
            }
        }

        private IActorBase SelectRoutee()
        {
            return strategy switch
            {
                RoutingStrategy.RoundRobin => routees[Interlocked.Increment(ref currentIndex) % routees.Count],
                RoutingStrategy.Random => routees[Random.Shared.Next(routees.Count)],
                RoutingStrategy.Broadcast => throw new NotSupportedException("Use BroadcastAsync for broadcast"),
                _ => routees[0]
            };
        }

        public async Task BroadcastAsync(TMessage message)
        {
            var tasks = new List<Task>();
            foreach (var routee in routees)
            {
                if (routee is Actor<TMessage> actor)
                {
                    tasks.Add(actor.SendAsync(message));
                }
            }
            await Task.WhenAll(tasks);
        }
    }

    /// <summary>
    /// Routing strategy
    /// </summary>
    public enum RoutingStrategy
    {
        RoundRobin,
        Random,
        Broadcast,
        ConsistentHash
    }
} 
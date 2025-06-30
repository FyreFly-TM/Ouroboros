using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Ouroboros.Core.Actors
{
    /// <summary>
    /// Actor system for Ouroboros - provides Erlang-style actors with supervision
    /// Enables distributed computing with fault tolerance and message passing
    /// </summary>
    public class ActorSystem
    {
        private readonly string systemName;
        private readonly ConcurrentDictionary<string, ActorRef> actors = new();
        private readonly ConcurrentDictionary<string, Supervisor> supervisors = new();
        private readonly MessageRouter messageRouter;
        private readonly TaskScheduler scheduler;
        private readonly CancellationTokenSource shutdownToken = new();
        
        public ActorSystem(string name)
        {
            systemName = name;
            messageRouter = new MessageRouter(this);
            scheduler = TaskScheduler.Default;
        }
        
        /// <summary>
        /// Spawn a new actor
        /// </summary>
        public ActorRef Spawn<T>(Func<T> actorFactory, string? name = null) where T : IActor
        {
            var actorId = name ?? Guid.NewGuid().ToString();
            var actor = actorFactory();
            var actorRef = new ActorRef(actorId, this);
            var actorCell = new ActorCell(actor, actorRef, this);
            
            actors[actorId] = actorRef;
            actorRef.SetCell(actorCell);
            
            // Start the actor
            actorCell.Start();
            
            return actorRef;
        }
        
        /// <summary>
        /// Spawn a supervised actor
        /// </summary>
        public Supervisor SpawnSupervisor<T>() where T : ISupervisor, new()
        {
            var supervisorId = Guid.NewGuid().ToString();
            var supervisor = new T();
            var supervisorInstance = new Supervisor(supervisorId, supervisor, this);
            
            supervisors[supervisorId] = supervisorInstance;
            supervisorInstance.Start();
            
            return supervisorInstance;
        }
        
        /// <summary>
        /// Send a message to an actor
        /// </summary>
        public void Tell(string actorId, object message)
        {
            if (actors.TryGetValue(actorId, out var actorRef))
            {
                actorRef.Tell(message);
            }
        }
        
        /// <summary>
        /// Send a message and wait for reply
        /// </summary>
        public async Task<T> Ask<T>(string actorId, object message, TimeSpan timeout)
        {
            if (actors.TryGetValue(actorId, out var actorRef))
            {
                return await actorRef.Ask<T>(message, timeout);
            }
            throw new ActorNotFoundException(actorId);
        }
        
        /// <summary>
        /// Shutdown the actor system
        /// </summary>
        public async Task Shutdown()
        {
            shutdownToken.Cancel();
            
            // Stop all supervisors first
            var supervisorTasks = new List<Task>();
            foreach (var supervisor in supervisors.Values)
            {
                supervisorTasks.Add(supervisor.Stop());
            }
            await Task.WhenAll(supervisorTasks);
            
            // Stop all remaining actors
            var actorTasks = new List<Task>();
            foreach (var actorRef in actors.Values)
            {
                if (actorRef.Cell != null)
                {
                    actorTasks.Add(actorRef.Cell.Stop());
                }
            }
            await Task.WhenAll(actorTasks);
            
            actors.Clear();
            supervisors.Clear();
        }
        
        internal void RemoveActor(string actorId)
        {
            actors.TryRemove(actorId, out _);
        }
        
        internal TaskScheduler GetScheduler() => scheduler;
        internal CancellationToken GetShutdownToken() => shutdownToken.Token;
    }
    
    /// <summary>
    /// Reference to an actor
    /// </summary>
    public class ActorRef
    {
        public string Id { get; }
        internal ActorCell? Cell { get; private set; }
        private readonly ActorSystem system;
        
        internal ActorRef(string id, ActorSystem system)
        {
            Id = id;
            this.system = system;
        }
        
        internal void SetCell(ActorCell cell)
        {
            Cell = cell;
        }
        
        /// <summary>
        /// Send a fire-and-forget message
        /// </summary>
        public void Tell(object message)
        {
            Cell?.Enqueue(new Envelope(message, null));
        }
        
        /// <summary>
        /// Send a message and wait for reply
        /// </summary>
        public async Task<T> Ask<T>(object message, TimeSpan timeout)
        {
            var tcs = new TaskCompletionSource<T>();
            var timeoutCts = new CancellationTokenSource(timeout);
            
            timeoutCts.Token.Register(() => 
                tcs.TrySetException(new TimeoutException("Ask timeout")));
            
            var envelope = new Envelope(message, reply => 
            {
                if (reply is T typedReply)
                    tcs.TrySetResult(typedReply);
                else
                    tcs.TrySetException(new InvalidCastException($"Expected {typeof(T)}, got {reply?.GetType()}"));
            });
            
            Cell?.Enqueue(envelope);
            return await tcs.Task;
        }
        
        public override string ToString() => $"ActorRef({Id})";
    }
    
    /// <summary>
    /// Actor cell - contains the actor instance and manages its lifecycle
    /// </summary>
    internal class ActorCell
    {
        private readonly IActor actor;
        private readonly ActorRef actorRef;
        private readonly ActorSystem system;
        private readonly ConcurrentQueue<Envelope> mailbox = new();
        private readonly SemaphoreSlim messageAvailable = new(0);
        private Task? processingTask;
        private readonly CancellationTokenSource stopToken = new();
        
        public ActorCell(IActor actor, ActorRef actorRef, ActorSystem system)
        {
            this.actor = actor;
            this.actorRef = actorRef;
            this.system = system;
        }
        
        public void Start()
        {
            processingTask = Task.Factory.StartNew(
                ProcessMessages,
                stopToken.Token,
                TaskCreationOptions.LongRunning,
                system.GetScheduler());
        }
        
        public void Enqueue(Envelope envelope)
        {
            mailbox.Enqueue(envelope);
            messageAvailable.Release();
        }
        
        public async Task Stop()
        {
            stopToken.Cancel();
            messageAvailable.Release(); // Wake up processing loop
            
            if (processingTask != null)
            {
                await processingTask;
            }
        }
        
        private async Task ProcessMessages()
        {
            try
            {
                while (!stopToken.Token.IsCancellationRequested)
                {
                    await messageAvailable.WaitAsync(stopToken.Token);
                    
                    while (mailbox.TryDequeue(out var envelope))
                    {
                        try
                        {
                            var response = await actor.Receive(envelope.Message);
                            envelope.ReplyTo?.Invoke(response);
                        }
                        catch (Exception ex)
                        {
                            // Handle actor failure - notify supervisor
                            envelope.ReplyTo?.Invoke(new ActorException(ex));
                        }
                    }
                }
            }
            catch (OperationCanceledException)
            {
                // Normal shutdown
            }
            finally
            {
                system.RemoveActor(actorRef.Id);
            }
        }
    }
    
    /// <summary>
    /// Message envelope containing message and reply callback
    /// </summary>
    internal class Envelope
    {
        public object Message { get; }
        public Action<object>? ReplyTo { get; }
        
        public Envelope(object message, Action<object>? replyTo = null)
        {
            Message = message;
            ReplyTo = replyTo;
        }
    }
    
    /// <summary>
    /// Base interface for actors
    /// </summary>
    public interface IActor
    {
        Task<object> Receive(object message);
    }
    
    /// <summary>
    /// Supervisor interface for fault tolerance
    /// </summary>
    public interface ISupervisor
    {
        SupervisionStrategy Strategy { get; }
        int MaxRestarts { get; }
        TimeSpan TimeWindow { get; }
        IEnumerable<ChildSpec> GetChildSpecs();
        SupervisorAction HandleFailure(string childId, Exception exception);
    }
    
    /// <summary>
    /// Supervisor implementation
    /// </summary>
    public class Supervisor
    {
        private readonly string id;
        private readonly ISupervisor supervisorLogic;
        private readonly ActorSystem system;
                 private readonly ConcurrentDictionary<string, ActorRef> children = new();
         private readonly ConcurrentDictionary<string, ChildSpec> childSpecs = new();
         private readonly ConcurrentDictionary<string, int> restartCounts = new();
         private readonly ConcurrentDictionary<string, DateTime> lastRestart = new();
        
        public Supervisor(string id, ISupervisor supervisorLogic, ActorSystem system)
        {
            this.id = id;
            this.supervisorLogic = supervisorLogic;
            this.system = system;
        }
        
        public void Start()
        {
            foreach (var childSpec in supervisorLogic.GetChildSpecs())
            {
                StartChild(childSpec);
            }
        }
        
        public async Task Stop()
        {
            var stopTasks = new List<Task>();
            foreach (var child in children.Values)
            {
                if (child.Cell != null)
                {
                    stopTasks.Add(child.Cell.Stop());
                }
            }
            await Task.WhenAll(stopTasks);
        }
        
        public ActorRef? GetChild(string childId)
        {
            return children.TryGetValue(childId, out var child) ? child : null;
        }
        
        private void StartChild(ChildSpec childSpec)
        {
            try
            {
                var actorRef = system.Spawn(childSpec.StartFunction, childSpec.Id);
                children[childSpec.Id] = actorRef;
                childSpecs[childSpec.Id] = childSpec;
            }
            catch (Exception ex)
            {
                HandleChildFailure(childSpec.Id, ex);
            }
        }
        
        private void HandleChildFailure(string childId, Exception exception)
        {
            var action = supervisorLogic.HandleFailure(childId, exception);
            
            switch (action)
            {
                case SupervisorAction.Restart:
                    RestartChild(childId);
                    break;
                case SupervisorAction.Stop:
                    StopChild(childId);
                    break;
                case SupervisorAction.Escalate:
                    // Escalate to parent supervisor
                    throw new SupervisorException($"Child {childId} failed", exception);
            }
        }
        
        private void RestartChild(string childId)
        {
            if (!childSpecs.TryGetValue(childId, out var childSpec))
                return;
            
            // Check restart limits
            var now = DateTime.UtcNow;
            if (lastRestart.TryGetValue(childId, out var lastTime))
            {
                if (now - lastTime < supervisorLogic.TimeWindow)
                {
                    restartCounts[childId] = restartCounts.GetValueOrDefault(childId, 0) + 1;
                    if (restartCounts[childId] > supervisorLogic.MaxRestarts)
                    {
                        StopChild(childId);
                        return;
                    }
                }
                else
                {
                    restartCounts[childId] = 0;
                }
            }
            
            lastRestart[childId] = now;
            
            // Stop old child and start new one
            if (children.TryGetValue(childId, out var oldChild))
            {
                oldChild.Cell?.Stop();
            }
            
            StartChild(childSpec);
        }
        
                 private void StopChild(string childId)
         {
             if (children.TryRemove(childId, out var child))
             {
                 child.Cell?.Stop();
             }
             childSpecs.TryRemove(childId, out _);
             restartCounts.TryRemove(childId, out _);
             lastRestart.TryRemove(childId, out _);
         }
    }
    
    /// <summary>
    /// Child specification for supervisor
    /// </summary>
    public class ChildSpec
    {
        public string Id { get; set; } = "";
        public Func<IActor> StartFunction { get; set; } = () => new NullActor();
        public RestartPolicy Restart { get; set; } = RestartPolicy.Permanent;
        public TimeSpan Shutdown { get; set; } = TimeSpan.FromSeconds(5);
    }
    
    /// <summary>
    /// Supervision strategy
    /// </summary>
    public enum SupervisionStrategy
    {
        OneForOne,      // Restart only failed child
        OneForAll,      // Restart all children when one fails
        RestForOne      // Restart failed child and all started after it
    }
    
    /// <summary>
    /// Restart policy
    /// </summary>
    public enum RestartPolicy
    {
        Permanent,      // Always restart
        Temporary,      // Never restart
        Transient       // Restart only on abnormal termination
    }
    
    /// <summary>
    /// Supervisor action on child failure
    /// </summary>
    public enum SupervisorAction
    {
        Restart,        // Restart the failed child
        Stop,           // Stop the failed child
        Escalate        // Escalate to parent supervisor
    }
    
    /// <summary>
    /// Message router for distributed actors
    /// </summary>
    internal class MessageRouter
    {
        private readonly ActorSystem system;
        private readonly Dictionary<string, INodeConnector> nodeConnectors = new();
        
        public MessageRouter(ActorSystem system)
        {
            this.system = system;
        }
        
        public void RouteMessage(string actorPath, object message)
        {
            if (IsLocalActor(actorPath))
            {
                // Route to local actor
                var actorId = ExtractActorId(actorPath);
                system.Tell(actorId, message);
            }
            else
            {
                // Route to remote actor
                var nodeId = ExtractNodeId(actorPath);
                if (nodeConnectors.TryGetValue(nodeId, out var connector))
                {
                    connector.SendMessage(actorPath, message);
                }
            }
        }
        
        private bool IsLocalActor(string actorPath)
        {
            return !actorPath.Contains("@");
        }
        
        private string ExtractActorId(string actorPath)
        {
            return actorPath.Split('@')[0];
        }
        
        private string ExtractNodeId(string actorPath)
        {
            var parts = actorPath.Split('@');
            return parts.Length > 1 ? parts[1] : "";
        }
    }
    
    /// <summary>
    /// Node connector for distributed communication
    /// </summary>
    internal interface INodeConnector
    {
        void SendMessage(string actorPath, object message);
    }
    
    /// <summary>
    /// Channels for CSP-style communication
    /// </summary>
    public class Channel<T>
    {
        private readonly ConcurrentQueue<T> queue;
        private readonly SemaphoreSlim semaphore;
        private readonly int capacity;
        private volatile bool closed = false;
        
        public Channel(int capacity = 0)
        {
            this.capacity = capacity;
            queue = new ConcurrentQueue<T>();
            semaphore = new SemaphoreSlim(capacity == 0 ? int.MaxValue : capacity);
        }
        
        public async Task<bool> Send(T item)
        {
            if (closed) return false;
            
            await semaphore.WaitAsync();
            if (closed)
            {
                semaphore.Release();
                return false;
            }
            
            queue.Enqueue(item);
            return true;
        }
        
        public bool TryReceive(out T? item)
        {
            if (queue.TryDequeue(out item))
            {
                semaphore.Release();
                return true;
            }
            
            item = default;
            return false;
        }
        
        public async Task<(bool Success, T? Item)> ReceiveAsync()
        {
            while (!closed)
            {
                if (TryReceive(out var item))
                {
                    return (true, item);
                }
                
                await Task.Delay(1); // Simple polling - real implementation would use events
            }
            
            return (false, default);
        }
        
        public void Close()
        {
            closed = true;
        }
        
        public static Channel<T> New(int capacity) => new(capacity);
    }
    
    /// <summary>
    /// Null actor implementation for default cases
    /// </summary>
    internal class NullActor : IActor
    {
        public Task<object> Receive(object message)
        {
            // Simply return the message back (echo behavior)
            return Task.FromResult(message);
        }
    }
    
    /// <summary>
    /// Exception types
    /// </summary>
    public class ActorNotFoundException : Exception
    {
        public ActorNotFoundException(string actorId) : base($"Actor not found: {actorId}") { }
    }
    
    public class ActorException : Exception
    {
        public ActorException(Exception innerException) : base("Actor execution failed", innerException) { }
    }
    
    public class SupervisorException : Exception
    {
        public SupervisorException(string message, Exception innerException) : base(message, innerException) { }
    }
} 
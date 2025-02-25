// Copyright (c) Charlie Poole, Rob Prouse and Contributors. MIT License - see LICENSE.txt

using System;
using System.Collections.Generic;
using System.Threading;

namespace NUnit.Framework.Internal.Execution
{
    /// <summary>
    /// ParallelWorkItemDispatcher handles execution of work items by
    /// queuing them for worker threads to process.
    /// </summary>
    public class ParallelWorkItemDispatcher : IWorkItemDispatcher
    {
        private static readonly Logger Log = InternalTrace.GetLogger("Dispatcher");

        private const int WAIT_FOR_FORCED_TERMINATION = 5000;

        private WorkItem? _topLevelWorkItem;
        private readonly Stack<WorkItem> _savedWorkItems = new Stack<WorkItem>();

        private readonly List<CompositeWorkItem> _activeWorkItems = new List<CompositeWorkItem>();

        #region Events

        /// <summary>
        /// Event raised whenever a shift is starting.
        /// </summary>
        public event ShiftChangeEventHandler? ShiftStarting;

        /// <summary>
        /// Event raised whenever a shift has ended.
        /// </summary>
        public event ShiftChangeEventHandler? ShiftFinished;

        #endregion

        #region Constructor

        /// <summary>
        /// Construct a ParallelWorkItemDispatcher
        /// </summary>
        /// <param name="levelOfParallelism">Number of workers to use</param>
        public ParallelWorkItemDispatcher(int levelOfParallelism)
        {
            Log.Info("Initializing with {0} workers", levelOfParallelism);

            LevelOfParallelism = levelOfParallelism;

            InitializeShifts();
        }

        private void InitializeShifts()
        {
            foreach (var shift in Shifts)
                shift.EndOfShift += OnEndOfShift;

            // Assign queues to shifts
            ParallelShift.AddQueue(ParallelQueue);
            ParallelShift.AddQueue(ParallelSTAQueue);
            NonParallelShift.AddQueue(NonParallelQueue);
            NonParallelSTAShift.AddQueue(NonParallelSTAQueue);

            // Create workers and assign to shifts and queues
            // TODO: Avoid creating all the workers till needed
            for (int i = 1; i <= LevelOfParallelism; i++)
            {
                string name = $"ParallelWorker#{i}";
                ParallelShift.Assign(new TestWorker(ParallelQueue, name));
            }

            ParallelShift.Assign(new TestWorker(ParallelSTAQueue, "ParallelSTAWorker"));

            var worker = new TestWorker(NonParallelQueue, "NonParallelWorker");
            worker.Busy += OnStartNonParallelWorkItem;
            NonParallelShift.Assign(worker);

            worker = new TestWorker(NonParallelSTAQueue, "NonParallelSTAWorker");
            worker.Busy += OnStartNonParallelWorkItem;
            NonParallelSTAShift.Assign(worker);
        }

        private void OnStartNonParallelWorkItem(TestWorker worker, WorkItem work)
        {
            // This captures the startup of TestFixtures and SetUpFixtures,
            // but not their teardown items, which are not composite items
            if (work.IsolateChildTests)
                IsolateQueues(work);
        }

#endregion

        #region Properties

        /// <summary>
        /// Number of parallel worker threads
        /// </summary>
        public int LevelOfParallelism { get; }

        /// <summary>
        /// Enumerates all the shifts supported by the dispatcher
        /// </summary>
        public IEnumerable<WorkShift> Shifts
        {
            get
            {
                yield return ParallelShift;
                yield return NonParallelShift;
                yield return NonParallelSTAShift;
            }
        }

        /// <summary>
        /// Enumerates all the Queues supported by the dispatcher
        /// </summary>
        public IEnumerable<WorkItemQueue> Queues
        {
            get
            {
                yield return ParallelQueue;
                yield return ParallelSTAQueue;
                yield return NonParallelQueue;
                yield return NonParallelSTAQueue;
            }
        }

        // WorkShifts - Dispatcher processes tests in three non-overlapping shifts.
        // See comment in Workshift.cs for a more detailed explanation.
        private WorkShift ParallelShift { get; } = new WorkShift("Parallel");
        private WorkShift NonParallelShift { get; } = new WorkShift("NonParallel");
        private WorkShift NonParallelSTAShift { get; } = new WorkShift("NonParallelSTA");

        // WorkItemQueues
        private WorkItemQueue ParallelQueue { get; } = new WorkItemQueue("ParallelQueue", true, ApartmentState.MTA);
        private WorkItemQueue ParallelSTAQueue { get; } = new WorkItemQueue("ParallelSTAQueue", true, ApartmentState.STA);
        private WorkItemQueue NonParallelQueue { get; } = new WorkItemQueue("NonParallelQueue", false, ApartmentState.MTA);
        private WorkItemQueue NonParallelSTAQueue { get; } = new WorkItemQueue("NonParallelSTAQueue", false, ApartmentState.STA);

        #endregion

        #region IWorkItemDispatcher Members

        /// <summary>
        /// Start execution, setting the top level work,
        /// enqueuing it and starting a shift to execute it.
        /// </summary>
        public void Start(WorkItem topLevelWorkItem)
        {
            _topLevelWorkItem = topLevelWorkItem;

            Dispatch(topLevelWorkItem, InitialExecutionStrategy(topLevelWorkItem));

            var shift = SelectNextShift();

            if (shift is not null)
            {
                ShiftStarting?.Invoke(shift);
                shift.Start();
            }
        }

        // Initial strategy for the top level item is solely determined
        // by the ParallelScope of that item. While other approaches are
        // possible, this one gives the user a predictable result.
        private static ParallelExecutionStrategy InitialExecutionStrategy(WorkItem workItem)
        {
            return workItem.ParallelScope == ParallelScope.Default || workItem.ParallelScope == ParallelScope.None
                ? ParallelExecutionStrategy.NonParallel
                : ParallelExecutionStrategy.Parallel;
        }

        /// <summary>
        /// Dispatch a single work item for execution. The first
        /// work item dispatched is saved as the top-level
        /// work item and used when stopping the run.
        /// </summary>
        /// <param name="work">The item to dispatch</param>
        public void Dispatch(WorkItem work)
        {
            Dispatch(work, work.ExecutionStrategy);
        }

        // Separate method so it can be used by Start
        private void Dispatch(WorkItem work, ParallelExecutionStrategy strategy)
        {
            Log.Debug("Using {0} strategy for {1}", strategy, work.Name);

            // Currently, we only track CompositeWorkItems - this could be expanded
            if (work is CompositeWorkItem composite)
            {
                lock (_activeWorkItems)
                {
                    _activeWorkItems.Add(composite);
                    composite.Completed += OnWorkItemCompletion;
                }
            }

            // The attribute SingleThreaded may not be applied to the content in time, so here as a workaround, check for the flag again
            if (work.Context.IsSingleThreaded)
            {
                work.Execute();
            }
            else
            {
                switch (strategy)
                {
                    default:
                    case ParallelExecutionStrategy.Direct:
                        work.Execute();
                        break;
                    case ParallelExecutionStrategy.Parallel:
                        if (work.TargetApartment == ApartmentState.STA)
                            ParallelSTAQueue.Enqueue(work);
                        else
                            ParallelQueue.Enqueue(work);
                        break;
                    case ParallelExecutionStrategy.NonParallel:
                        if (work.TargetApartment == ApartmentState.STA)
                            NonParallelSTAQueue.Enqueue(work);
                        else
                            NonParallelQueue.Enqueue(work);
                        break;
                }
            }
        }

        /// <summary>
        /// Cancel the ongoing run completely.
        /// If no run is in process, the call has no effect.
        /// </summary>
        public void CancelRun(bool force)
        {
            if (_topLevelWorkItem is null)
            {
                throw new InvalidOperationException("Called Cancel without Start");
            }

            foreach (var shift in Shifts)
                shift.Cancel(force);

            if (force)
            {
                SpinWait.SpinUntil(() => _topLevelWorkItem.State == WorkItemState.Complete, WAIT_FOR_FORCED_TERMINATION);

                // Notify termination of any remaining in-process suites
                lock (_activeWorkItems)
                {
                    int index = _activeWorkItems.Count;

                    while (index > 0)
                    {
                        var work = _activeWorkItems[--index];

                        if (work.State == WorkItemState.Running)
                            new CompositeWorkItem.OneTimeTearDownWorkItem(work).WorkItemCancelled();
                    }
                }
            }
        }

        private readonly object _queueLock = new object();
        private int _isolationLevel = 0;

        /// <summary>
        /// Save the state of the queues and create a new isolated set
        /// </summary>
        internal void IsolateQueues(WorkItem work)
        {
            if (_topLevelWorkItem is null)
            {
                throw new InvalidOperationException("Called IsolateQueues without Start");
            }

            Log.Info("Saving Queue State for {0}", work.Name);
            lock (_queueLock)
            {
                foreach (WorkItemQueue queue in Queues)
                    queue.Save();

                _savedWorkItems.Push(_topLevelWorkItem);
                _topLevelWorkItem = work;

                _isolationLevel++;
            }
        }

        /// <summary>
        /// Try to remove isolated queues and restore old ones
        /// </summary>
        private void TryRestoreQueues()
        {
            // Keep lock until we can remove for both methods
            lock (_queueLock)
            {
                if (_isolationLevel <= 0)
                {
                    Log.Debug("Ignoring call to restore Queue State");
                    return;
                }

                Log.Info("Restoring Queue State");

                foreach (WorkItemQueue queue in Queues)
                    queue.Restore();
                _topLevelWorkItem = _savedWorkItems.Pop();

                _isolationLevel--;
            }
        }

        #endregion

        #region Helper Methods

        private void OnWorkItemCompletion(object? sender, EventArgs args)
        {
            if (sender is CompositeWorkItem work)
            {
                lock (_activeWorkItems)
                {
                    _activeWorkItems.Remove(work);
                    work.Completed -= OnWorkItemCompletion;
                }
            }
        }

        private void OnEndOfShift(WorkShift endingShift)
        {
            ShiftFinished?.Invoke(endingShift);

            while (true)
            {
                // Shift has ended but all work may not yet be done
                while (_topLevelWorkItem!.State != WorkItemState.Complete)
                {
                    // This will return null if all queues are empty.
                    WorkShift? nextShift = SelectNextShift();
                    if (nextShift is not null)
                    {
                        ShiftStarting?.Invoke(nextShift);
                        nextShift.Start();
                        return;
                    }
                }

                // If the shift has ended for an isolated queue, restore
                // the queues and keep trying. Otherwise, we are done.
                if (_isolationLevel > 0)
                    TryRestoreQueues();
                else
                    break;
            }

            // All done - shutdown all shifts
            foreach (var shift in Shifts)
                shift.ShutDown();
        }

        private WorkShift? SelectNextShift()
        {
            foreach (var shift in Shifts)
            {
                if (shift.HasWork)
                    return shift;
            }

            return null;
        }

        #endregion
    }
}

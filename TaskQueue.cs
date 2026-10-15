using Serilog;
using System.Collections.Concurrent;

namespace FileSync
{
    public static class TaskQueue
    {
        private static readonly BlockingCollection<Action> _queue = new();
        private static Thread? _worker;
        private static readonly object _locker = new object();

        public static void Start()
        {
            lock (_locker)
            {
                if (_worker != null) return;

                _worker = new Thread(ProcessQueue)
                {
                    IsBackground = true,
                    Name = "TaskQueueWorker"
                };
                _worker.Start();
            }
        }

        public static bool Enqueue(Action task)
        {
            if (!_queue.IsAddingCompleted)
            {
                _queue.Add(task);
                return true;
            }
            else
            {
                Log.Error("TaskQueue: failed to add task. adding to queue completed");
                return false;
            }
        }

        public static Task StopAsync()
        {
            if (!_queue.IsAddingCompleted)
                _queue.CompleteAdding();

            if (_worker == null)
                return Task.CompletedTask;

            return Task.Run(() => _worker.Join());
        }

        public static int Count() => _queue.Count;

        private static void ProcessQueue()
        {
            foreach (var task in _queue.GetConsumingEnumerable())
            {
                try
                {
                    task();
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "TaskQueue: task failed");
                }
            }
        }
    }
}

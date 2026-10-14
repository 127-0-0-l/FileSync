using FileSync.Interfaces;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FileSync
{
    public static class TaskQueue
    {
        private static readonly BlockingCollection<Action> queue = new();
        private static Thread? worker;
        private static IOutputService outputService;
        private static readonly object locker = new object();

        public static void Start(IOutputService outputService)
        {
            lock (locker)
            {
                if (worker != null) return;

                TaskQueue.outputService = outputService;

                worker = new Thread(ProcessQueue)
                {
                    IsBackground = true,
                    Name = "TaskQueueWorker"
                };
                worker.Start();
            }
        }

        public static void Enqueue(Action task)
        {
            if (!queue.IsAddingCompleted)
                queue.Add(task);
        }

        public static Task StopAsync()
        {
            queue.CompleteAdding();

            if (worker == null)
                return Task.CompletedTask;

            return Task.Run(() => worker.Join());
        }

        private static void ProcessQueue()
        {
            foreach (var task in queue.GetConsumingEnumerable())
            {
                try
                {
                    task();
                }
                catch (Exception ex)
                {
                    outputService.WriteLine(ex.Message);
                }
            }
        }
    }
}

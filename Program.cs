using System;
using System.Threading;
using System.Threading.Tasks;

namespace DelayedEventManager
{
    class Program
    {
        readonly static TaskManager<object> manager = new TaskManager<object>();
        static void RandomAdd(int id)
        {
            Random random = new Random();
            while (!manager.Closed)
            {
                Thread.Sleep(random.Next(10, 1000));
                long delay = random.Next(10, 1000);
                manager.Add(delay, null);
            }
        }
        static void Main(string[] args)
        {
            Console.WriteLine("Hello World!");
            manager.Start();
            Task t = Task.Run(() =>
            {
                // This is not best practice for stopping
                while (!manager.Output.IsCompleted)
                {
                    object delta = manager.Output.Take();
                    // do something
                }
            });
            if (!manager.Closed)
            {
                for (int i = 0; i < 8; i++)
                {
                    int ii = i;
                    Task.Run(() =>
                    {
                        RandomAdd(ii);
                    });
                }
            }
            Thread.Sleep(100000);
            manager.Close();
            t.Wait();
        }
    }
}

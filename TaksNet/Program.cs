using Serilog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Configuration;

namespace TaksNet
{
    class Program
    {
        public static volatile int Skip = 0;
        static object locker = new object();
        public static volatile int LineCount;

        static void Main(string[] args)
        {
            var threadsCount = int.Parse(ConfigurationManager.AppSettings.Get("ThreadsCount"));

            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .WriteTo.Console()
                .WriteTo.File("myapp.txt")
                .CreateLogger();

            Log.Information("App started");

            LineCount = File.ReadLines(@"L:\Code\TaskNet\TaksNet\TaksNet\bin\Debug\task.txt", Encoding.Default).Count();


            for (int i = 0; i < threadsCount; i++)
            {
                Thread myThread = new Thread(Count);
                myThread.Name = "Поток " + i.ToString();
                myThread.Start();
            }

            //string[] AllLines = File.ReadAllLines(@"L:\Code\TaskNet\TaksNet\TaksNet\bin\Debug\task.txt");
            //Parallel.For(0, AllLines.Length,  x =>
            //{
            //   // DoStuff(AllLines[x]);
            //    //whatever you need to do
            //});

            //var line = File.ReadLines(@"L:\Code\TaskNet\TaksNet\TaksNet\bin\Debug\task.txt", Encoding.Default).Skip(Skip).Take(1).First();

            //using (var stream = File.Open(@"L:\Code\TaskNet\TaksNet\TaksNet\bin\Debug\task.txt", FileMode.Open, FileAccess.Read, FileShare.Read))
            //{

            //}


            Console.ReadLine();
        }

        public static void Count()
        {
            while (LineCount != 0)
            {
                lock (locker)
                {
                    if (LineCount != 0)
                    {
                        var line = File.ReadLines(@"L:\Code\TaskNet\TaksNet\TaksNet\bin\Debug\task.txt", Encoding.Default).Skip(Skip).Take(1).First();
                        Console.WriteLine(line);
                        var t = Thread.CurrentThread.ManagedThreadId;
                        Console.WriteLine(t);
                        LineCount--;
                        Skip++;
                    }
                }
            }
        }
    }
}

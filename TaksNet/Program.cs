using Serilog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Configuration;
using TaksNet.Models;
using Newtonsoft.Json;
using System.Runtime.InteropServices;
using System.Data.Entity.Migrations;
using Serilog.Events;

namespace TaksNet
{
    class Program
    {
        static volatile int LineCount;
        static volatile int Skip = 0;
        static object locker = new object();
        static CancellationTokenSource cts;
        static readonly string PathToFile = @"..\..\TestData.txt";

        static void Main(string[] args)
        {
            cts = new CancellationTokenSource();
            var threadsCount = int.Parse(ConfigurationManager.AppSettings.Get("ThreadsCount"));

            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .WriteTo.Console()
                .WriteTo.File($"log_file.txt")
                .CreateLogger();

            LineCount = File.ReadLines(PathToFile, Encoding.Default).Count();

            var tasks = new List<Task>();

            for (int i = 0; i < threadsCount; i++)
            {
                var LastTask = new Task(() => ProcessFileLine(), cts.Token);
                LastTask.Start();
                tasks.Add(LastTask);
            }

            //запуск Таска ждущего команды остановки потоков и завершения работы приложения
            var shutdownTask = new Task(() => WaitShutDownCommand(), cts.Token);
            shutdownTask.Start();

            Task.WaitAll(tasks.ToArray());

            Console.ReadLine();
        }

        public static void ProcessFileLine()
        {
            while (LineCount != 0)
            {
                lock (locker)
                {
                    if (LineCount != 0)
                    {
                        var emp = GetEmployeeFromFile();
                        DbProcessEmployee(emp);

                        LineCount--;
                        Skip++;
                    }
                }
            }
        }


        public static Employee GetEmployeeFromFile()
        {
            var line = File.ReadLines(PathToFile, Encoding.Default).Skip(Skip).Take(1).First();
            var employee = JsonConvert.DeserializeObject<Employee>(line);
            return employee;
        }


        public static void DbProcessEmployee(Employee emp)
        {
            using (var db = new EmployeeContext())
            {
                var employeeDbRecord = db.Employees.Where(c => c.Id == emp.Id).FirstOrDefault();
                if (employeeDbRecord == null)
                {
                    db.Employees.Add(emp);
                    Log.Information($"Added new record with Id = {emp.Id} by threadId = {Thread.CurrentThread.ManagedThreadId}");
                }
                else
                {
                    //логика обновления записи
                    var compare = DateTimeOffset.Compare(emp.ChangeDate, employeeDbRecord.ChangeDate);
                    if (compare > 0)
                    {
                        db.Employees.AddOrUpdate(emp);
                        Log.Information($"Record with Id = {emp.Id} has updated by threadId = {Thread.CurrentThread.ManagedThreadId}");
                    }
                }

                db.SaveChanges();
            }
        }

        public static void WaitShutDownCommand()
        {
            while (true)
            {
                if (Console.ReadKey(true).Key == ConsoleKey.Escape)
                {
                    if (cts != null)
                    {
                        cts.Cancel();
                        Log.Information("Tasks are canceled!");
                    }
                    Log.Information("Shutdown application...");
                    Environment.Exit(0);
                }
            }
        }
    }
}

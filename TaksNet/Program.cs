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
        public static volatile int Skip = 0;
        static object locker = new object();
        public static volatile int LineCount;

        static void Main(string[] args)
        {
            //using (var db = new EmployeeContext())
            //{
            //    var e = new Employee()
            //    {
            //        Id = Guid.NewGuid(),
            //        ChangeDate = DateTimeOffset.Now,
            //        City = "C",
            //        Email = "asd",
            //        FullName = "abc",
            //        Phone = "111"
            //    };

            //    db.Employees.Add(e);
            //    db.SaveChanges();
            //}

            var threadsCount = int.Parse(ConfigurationManager.AppSettings.Get("ThreadsCount"));

            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Debug()
                //.MinimumLevel.Override("Microsoft", LogEventLevel.Information)
                .WriteTo.Console()
                .WriteTo.File("log_.txt")
                .CreateLogger();

            LineCount = File.ReadLines(@"L:\Code\task.txt", Encoding.Default).Count();

            var tasks = new List<Task>();

            for (int i = 0; i < threadsCount; i++)
            {
                var LastTask = new Task(() => Count());
                LastTask.Start();
                tasks.Add(LastTask);
            }


            Task.WaitAll(tasks.ToArray());

            //for (int i = 0; i < threadsCount; i++)
            //{
            //    Thread myThread = new Thread(Count);
            //    myThread.Name = "Поток " + i.ToString();
            //    myThread.Start();
            //}




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
                        var line = File.ReadLines(@"L:\Code\task.txt", Encoding.Default).Skip(Skip).Take(1).First();
                        var emp = JsonConvert.DeserializeObject<Employee>(line);
                        using (var db = new EmployeeContext())
                        {
                            var emp1 = db.Employees.Where(c => c.Id == emp.Id).FirstOrDefault();
                            if (emp1 == null)
                            {
                                db.Employees.Add(emp);
                                Log.Information($"Added new record with Id = {emp.Id}");
                            }
                            else
                            {
                                var d = DateTimeOffset.Compare(emp.ChangeDate, emp1.ChangeDate);
                                if (d > 0)
                                {
                                    //emp1 = emp;
                                    db.Employees.AddOrUpdate(emp);
                                    Log.Information($"Record with Id = {emp.Id} has updated");
                                    //db.Entry(emp1).CurrentValues.SetValues(emp);
                                }
                            }

                            db.SaveChanges();
                        }
                        //Log.Information($"Employee with {emp.Id} added to db by threadId = {Thread.CurrentThread.ManagedThreadId}");

                        //Console.WriteLine(line);
                        //var t = Thread.CurrentThread.ManagedThreadId;
                        //Console.WriteLine(t);
                        LineCount--;
                        Skip++;
                    }
                }
            }
        }
    }
}

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Npgsql;
using System.Configuration;

namespace TestPostgreSQLPerf
{
    class Program
    {
        static StatisticsModule sm;
        private static List<Task> tasks;
        private static string selectquery = "select a_value from table_a where thread_id=@id and a_value=@value;";
        private static string updatequery = "update table_a set a_value=@value where thread_id=@id and a_value=@value;";
        private static int maxTask = 5000;
        private static string CONNECTION_STRING;

        static void Main(string[] args)
        {
            if (args.Any())
                maxTask = Int32.Parse(args[0]);

            Initialize();

            LogColumnsHeader();

            CreateAndRunAllTasks();

            Task.WaitAll(tasks.ToArray());

            logInfo();

            Console.WriteLine();

            sm.ExportStatistics();

        }


        private static void CreateAndRunAllTasks()
        {
            for (int i = 0; i < maxTask; i++)
            {
                sm.IncreaseTotal() ;
                Task task = new Task(new Action(RunAction));
                tasks.Add(task);
                task.Start();
            }

        }

        private static void LogColumnsHeader()
        {
            Console.WriteLine("Rung\tFinshd\tTotal\tTimeout\tError\tExClient");
            logInfo();
        }

        private static void Initialize()
        {
            CONNECTION_STRING = ConfigurationManager.ConnectionStrings["constring"].ConnectionString;
            sm = new StatisticsModule();
            tasks = new List<Task>();
        }

        static async void RunAction()
        {
            var random = new Random();
            DateTime start = DateTime.Now;
            sm.IncreaseRunning();
            logInfo();
            using (Npgsql.NpgsqlConnection connection = new Npgsql.NpgsqlConnection(CONNECTION_STRING))
            {
                try
                {
                    connection.Open();

                    int threadId = random.Next(1, 10);
                    int value = random.Next(1, 100000);

                    await ExecuteDBAction(connection, threadId, value);

                    sm.AddDurationNow(start);

                }
                catch (TimeoutException ex)
                {
                    sm.AddTimeOut(ex.Message);
                }
                catch (Npgsql.NpgsqlException ex)
                {

                    if (ex.Code == "53300")
                    {
                        sm.AddTooManyClients(ex.Message);
                        //too many clients
                    }
                    else
                    {
                        sm.AddError(ex.Message);
                    }
                }
                catch (Exception ex)
                {
                    sm.AddError(ex.Message);
                }
            }
            sm.IncreaseRunningFinished();
        }


        private static async Task ExecuteDBAction(Npgsql.NpgsqlConnection connection, int threadId, int value)
        {
            using (DbCommand command = new Npgsql.NpgsqlCommand(selectquery, connection))
            {
                command.Parameters.Add(new Npgsql.NpgsqlParameter("id", threadId));
                command.Parameters.Add(new Npgsql.NpgsqlParameter("value", value));
                var result = await command.ExecuteScalarAsync();

            }

            Thread.Sleep(new Random().Next(50, 250));

            using (DbCommand command = new Npgsql.NpgsqlCommand(updatequery, connection))
            {
                command.Parameters.Add(new Npgsql.NpgsqlParameter("id", threadId));
                command.Parameters.Add(new Npgsql.NpgsqlParameter("value", value));
                var result = await command.ExecuteScalarAsync();

            }
        }



        private static void logInfo()
        {
            sm.LogInfo();
        }
    }
}

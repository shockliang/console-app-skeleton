using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BetterConsoleTables;
using McMaster.Extensions.CommandLineUtils;

namespace console_lab
{
    class Program
    {
        private static readonly string PROMPT_TITLE = "App> ";
        private static CancellationTokenSource watchingCts;
        private static CancellationTokenSource mainCts;
        private static CancellationTokenSource keyEventCts;
        private static CommandLineApplication app;
        private static bool isTerminated = false;

        static void Main(string[] args)
        {
            Console.TreatControlCAsInput = true;

            app = new CommandLineApplication();
            app.HelpOption();

            app.Command("exit", exitCmd =>
            {
                exitCmd.OnExecute(() =>
                {
                    System.Console.WriteLine("Bye!");
                    mainCts?.Cancel();
                    watchingCts?.Cancel();
                    keyEventCts?.Cancel();
                    isTerminated = true;

                    return 1;
                });

                exitCmd.Description = "Exit app.";
            });

            app.Command("watch", watchCmd =>
            {
                watchCmd.OnExecute(() =>
                {
                    mainCts.Cancel();
                    StartingWatchLog();
                    StartingListenKeyEvent();
                    return 1;
                });

                watchCmd.Description= "Watch something";
            });

            StartingMainLoop();

            // Keeping main thread executing. To down the cpu usage using Sleep.
            while (!isTerminated)
            {
                Thread.Sleep(1000);
            }
        }

        private static void StartingListenKeyEvent()
        {
            // System.Console.WriteLine("Strating listen key event");
            keyEventCts = new CancellationTokenSource();
            var keyEventToken = keyEventCts.Token;
            Task.Factory.StartNew(() =>
            {
                while (!keyEventToken.IsCancellationRequested)
                {
                    var keyInfo = Console.ReadKey();
                    if (IsTerminateWatchLog(keyInfo))
                    {
                        watchingCts?.Cancel();
                        CleanScreen();
                        StartingMainLoop();
                        break;
                    }
                }
            }, keyEventToken);
        }
        private static bool IsTerminateWatchLog(ConsoleKeyInfo keyInfo)
            => keyInfo.Key == ConsoleKey.C && keyInfo.Modifiers == ConsoleModifiers.Control;

        private static void StartingMainLoop()
        {
            // System.Console.WriteLine("Strating main loop");

            keyEventCts?.Cancel();
            mainCts = new CancellationTokenSource();
            var mainToken = mainCts.Token;
            Task.Factory.StartNew(() =>
            {
                while (!mainToken.IsCancellationRequested)
                {
                    try
                    {
                        string input = ReadLine.Read(PROMPT_TITLE);
                        if (!string.IsNullOrEmpty(input) || !string.IsNullOrWhiteSpace(input))
                        {
                            ReadLine.AddHistory(input);
                            app.Execute(input.Split(' ').ToArray());
                        }
                    }
                    catch
                    {

                    }
                }
            }, mainToken);
        }

        private static void StartingWatchLog()
        {
            watchingCts = new CancellationTokenSource();
            var watchToken = watchingCts.Token;
            Task.Factory.StartNew(async () =>
            {
                while (!watchToken.IsCancellationRequested)
                {
                    CleanScreen();

                    RenderingTables();
                    Console.SetCursorPosition(0, 19);

                    await Task.Delay(1000);
                }

            }, watchToken);
        }

        private static void RenderingTables()
        {

            var rand = new Random();
            int num1 = rand.Next(), num2 = rand.Next(), num3 = rand.Next();

            Table table = new Table("One", "Two", "Three")
                                .AddRow(num1++, num2++, num3++)
                                .AddRow("Short", "item", "Here")
                                .AddRow("Longer items go here", "stuff stuff", "stuff");
            table.Config = TableConfiguration.Unicode();

            var table2 = new Table("Log")
                .AddRow($"{DateTime.Now} log something 1")
                .AddRow($"{DateTime.Now} log something 2")
                .AddRow($"{DateTime.Now} log something 3")
                .AddRow($"{DateTime.Now} log something 4")
                .AddRow($"{DateTime.Now} log something 5 ver long ........");
            table2.Config = TableConfiguration.Unicode();
            var tables = new ConsoleTables(table, table2);
            System.Console.WriteLine(tables.ToString());

            System.Console.WriteLine("Press CTRL+C to terminate watch log.");
        }

        private static void CleanScreen()
        {
            Console.SetCursorPosition(0, 0);
            Console.Clear();
        }
    }
}

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
        private static CommandOption watchCmd;
        private static Func<int> appExecuteFunc = OnAppExecute;
        private static bool isTerminated = false;

        static void Main(string[] args)
        {
            Console.TreatControlCAsInput = true;

            app = new CommandLineApplication();
            app.HelpOption();
            watchCmd = app.Option("-w|--watch <TARGET>", "The watch targets", CommandOptionType.MultipleValue);

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

            app.OnExecute(appExecuteFunc);

            StartingMainLoop();

            // Keeping main thread executing. To down the cpu usage using Sleep.
            while (!isTerminated)
            {
                Thread.Sleep(1000);
            }
        }

        private static int OnAppExecute()
        {
            if (watchCmd.HasValue())
            {
                mainCts.Cancel();
                StartingWatchLog();
                StartingListenKeyEvent();
            }

            return 1;
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
                    Console.SetCursorPosition(0, 0);
                    Console.Clear();

                    RenderTables();
                    Console.SetCursorPosition(0, 19);

                    await Task.Delay(1000);
                }

            }, watchToken);
        }

        private static void RenderTables()
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
                .AddRow($"{DateTime.Now} log something 5 ver long ......................");
            table2.Config = TableConfiguration.Unicode();
            System.Console.WriteLine(table.ToString());
            System.Console.WriteLine(table2.ToString());
            System.Console.WriteLine("Press CTRL+C to terminate watch log.");
        }

        public static void ClearCurrentConsoleLine()
        {
            if (Console.IsOutputRedirected) return;

            int currentLineCursor = Console.CursorTop;
            Console.SetCursorPosition(0, Console.CursorTop);
            Console.Write(new string(' ', Console.WindowWidth));
            Console.SetCursorPosition(0, currentLineCursor);
            Console.Out.Flush();
        }
    }

    public class ConsoleSpinner
    {
        int counter;
        public void Turn()
        {
            counter++;
            switch (counter % 4)
            {
                case 0: Console.Write("/"); counter = 0; break;
                case 1: Console.Write("-"); break;
                case 2: Console.Write("\\"); break;
                case 3: Console.Write("|"); break;
            }
            Thread.Sleep(100);
            Console.SetCursorPosition(Console.CursorLeft - 1, Console.CursorTop);
        }
    }
}

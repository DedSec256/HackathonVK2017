using System;
using System.Collections.Generic;
using System.Text;
using VkNet.Model;
using System.Threading;
using VkNet.Model.RequestParams;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using BOT_Platform.Kernel;
using BOT_Platform.Kernel.Bots;
using BOT_Platform.Kernel.CIO;
using BOT_Platform.Kernel.Settings;
using BOT_Platform.Kernel.Standart;

namespace BOT_Platform.Kernel
{
    public class StandartCommands
    {
        private const string BOTS_NAMESPACE = "BOT_Platform.Kernel.Bots";
        void AddMyCommandInPlatform()
        {
            CommandsCenter.AddConsoleCommand("help|?", new MyCommandStruct("Показывает список всех комманд",CShowCommands));
            CommandsCenter.AddConsoleCommand("exit", new MyCommandStruct("Закрыть приложение", CExit));
            CommandsCenter.AddConsoleCommand("cls" , new MyCommandStruct("Очистить консоль", ClearConsole));
            CommandsCenter.AddConsoleCommand("log", new MyCommandStruct("Открывает лог", СLog));
            CommandsCenter.AddConsoleCommand("open", new MyCommandStruct("Открывает директорию с ботом", COpen));
            CommandsCenter.AddConsoleCommand("settings", new MyCommandStruct("Открывает файл настроек", CSettings));
            CommandsCenter.AddConsoleCommand("class", new MyCommandStruct("Показывает текущий список Bot-классов", Classes));
            CommandsCenter.AddConsoleCommand("!", new MyCommandStruct("Тест бота", CDebugCommand));
            CommandsCenter.AddConsoleCommand("restart", new MyCommandStruct("Перезапуск системы", CRestart));
            CommandsCenter.AddConsoleCommand("bots", new MyCommandStruct("Список ботов", CBots));
            CommandsCenter.AddConsoleCommand("debug", new MyCommandStruct("Активирует режим отладки", CDebug));
            CommandsCenter.AddConsoleCommand("undebug", new MyCommandStruct("Деактивирует режим отладки", CDebug));
            CommandsCenter.AddConsoleCommand("_n", new MyCommandStruct("", CNeo, true));
        }

        private void CNeo(Message message, string args, Bot bot)
        {
           Console.ForegroundColor = ConsoleColor.Green;
           Console.WriteLine("Wake up, Neo...");
           Console.ResetColor();
        }

        private void COpen(Message message, string args, Bot bot)
        {
            if (!BotMain.Bots.ContainsKey(args))
            {
                BotConsole.Write($"Бот {args} отсутствует в системе.\n", MessageType.Warning);
                return;
            }
            Process.Start(BotMain.Bots[args].Directory);
        }

        private void CDebugCommand(Message message, string args, Bot bot)
        {
            string[] param = args.Split(new char[] {','}, 2, StringSplitOptions.RemoveEmptyEntries);

            if (!BotMain.Bots.ContainsKey(param[0]))
            {
                BotConsole.Write($"Бот {param[0]} отсутствует в системе.\n", MessageType.Warning);
                return;
            }
            BotMain.Bots[param[0]].DebugExecuteCommand(param[1]);
        }

        private void CBots(Message message, string args, Bot Bot)
        {
            StringBuilder sB = new StringBuilder();
            sB.AppendLine("---------------------------------------------------------------------");
            sB.AppendLine($"Список загруженных ботов ({BotMain.Bots.Values.Count} бота(ов)) :");
            foreach (var bot in BotMain.Bots.Values)
            {
                                    sB.Append($"[{bot.Name}] - ");
                                    sB.Append($"AppId: [{bot.GetSettings().Convert<VkSettings>().ApplicationId}], ");
                                    sB.Append($"Тип: {bot.GetType().Name}, ");
                                    sB.Append($"Базовый тип: {bot.GetType().BaseType.Name}, ");
                                    sB.AppendLine($"Статус IsDebug: {bot.GetSettings().GetIsDebug().ToString().ToUpper()} ");
            }
            sB.AppendLine("---------------------------------------------------------------------");
            BotConsole.Write(sB.ToString(), MessageType.Default);
        }

        private void Classes(Message message, string args, Bot Bot)
        {
            Type[] typelist = Assembly.GetExecutingAssembly().GetTypes()
                .Where(t => t.Namespace == BOTS_NAMESPACE)
                .ToArray();

            StringBuilder sB = new StringBuilder();
            sB.AppendLine("---------------------------------------------------------------------");
            sB.AppendLine("Список текущих Bot-классов: ");
            foreach (Type type in typelist)
            {
                sB.AppendLine($"- {{{type.Name}}} Base = {{{type.BaseType.Name}}}");
            }
            sB.AppendLine("---------------------------------------------------------------------");

            BotConsole.Write(sB.ToString(), MessageType.Default);
        }

        private void CSettings(Message message, string args, Bot bot)
        {
            if (bot == null && String.IsNullOrEmpty(args)) return;

            if (BotMain.Bots.ContainsKey(args))
                Process.Start(Path.Combine(BotMain.Bots[args].Directory, PlatformSettings.PATH));
            else BotConsole.Write($"Бот {args} отсутствует в системе.\n", MessageType.Warning);
        }

        private void СLog(Message message, string args, Bot bot)
        {
            if (bot == null && String.IsNullOrEmpty(args)) return;

            if (BotMain.Bots.ContainsKey(args))
                Process.Start(Path.Combine(BotMain.Bots[args].Directory, Log.logFile));
            else BotConsole.Write($"Бот {args} отсутствует в системе.\n", MessageType.Warning);
        }

        public StandartCommands()
        {
            AddMyCommandInPlatform();
        }

        void SetDebug(Bot Bot)
        {
            void ThreadDebug(Bot bot)
            {
                bot.GetSettings().SetIsDebug(true);
                BotConsole.Write(
                    "---------------------" +
                    $"Решим отладки бота [{bot.Name}] ВКЛЮЧЁН(ON)" +
                    "---------------------", MessageType.System);
            }
            if (Bot != null)
            {
               ThreadDebug(Bot);
            }
            else
            {
                foreach (var bot in BotMain.Bots.Values)
                {
                    ThreadDebug(bot);
                }
            }

        }
        void SetUndebug(Bot Bot)
        {
            void UndebugThread(Bot bot)
            {
                bot.GetSettings().SetIsDebug(false);
                Thread abortThread = bot.botThread;

                bot.botThread = new Thread(bot.BotWork)
                {
                    Priority = ThreadPriority.AboveNormal,
                    Name = bot.Name
                };

                bot.botThread.Start();
                if (abortThread != null)
                {
                    BotConsole.Write($"Перезапуск потока botThread в [{bot.Name}]", MessageType.System);
                    BotConsole.Write(
                        "---------------------" +
                        $"Решим отладки бота [{bot.Name}] ВЫКЛЮЧЕН(OFF)" +
                        "---------------------", MessageType.System);
                    abortThread.Abort($"Перезапуск потока botThread в [{bot.Name}]");
                }
            }

            if (Bot != null)
            {
                UndebugThread(Bot);
            }
            else
            {
                foreach (var bot in BotMain.Bots.Values)
                {
                    UndebugThread(bot);
                }
            }
        }
        void CDebug(Message message, string args, Bot Bot)
        {

            Bot = String.IsNullOrEmpty(args) ? Bot : 
                (BotMain.Bots.ContainsKey(args) ? BotMain.Bots[args] 
                : throw new ArgumentException($"Бот {args} отсутствует в системе."));

            if (message.Body == "debug")
            {
                SetDebug(Bot);
            }
            
            else if (message.Body == "undebug")
            {
                SetUndebug(Bot);
            }
        }
        void CRestart(Message message, string args, Bot bot)
        {
            Thread needToAbortThread = BotMain.consoleThread;
            BotMain.consoleThread = new Thread(BotMain.ConsoleCommander)
            {
                Priority = ThreadPriority.Highest
            };
            BotMain.consoleThread.Start();
            if (needToAbortThread != null)
            {
                BotConsole.Write("Платформа была перезапущена!", MessageType.Info);
                needToAbortThread.Abort("Платформа была перезапущена!");
            }
            
        }

        void CShowCommands(Message message, string args, Bot bot)
        {
            
        }
        void CExit(Message message, string args, Bot bot)
        {
            Environment.Exit(1);
        }
        void ClearConsole(Message message, string args, Bot bot)
        {
            Console.Clear();
        }

    }
}
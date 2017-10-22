#define PARALLEL

/*
 * TODO:
 * 2) Добавить бан-лист для каждой группы
 * 7) Синхонизированный вывод
 * 8) Проверить чат, в частности, как работает уведомление о недобавлении (там return) + свой чат для каждого бота
 * 12) ОГРАНИЧЕНИЕ В ЧАТ НА СПЛИТ
 * 14) Ответ на непрочитанные
 */
using System;
using System.Collections.Generic;
using System.Diagnostics.Eventing.Reader;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Remoting.Messaging;
using System.Threading;
using BOT_Platform.Kernel.Bots;
using BOT_Platform.Kernel.CIO;
using BOT_Platform.Kernel.Interfaces;
using VkNet;
using System.Text;
using System.Windows.Forms;
using BOT_Platform.Kernel.Settings;
using BOT_Platform.Kernel.Standart;

namespace BOT_Platform.Kernel
{
    public static partial class BotMain
    {
        const string DevNamespace = "MyFunctions";   /* Пространоство имён, содержащее только
                                                      * пользовательские функции
                                                      */
        public const string UsersBots = @"Bots\UsersBots";
        public const string GroupsBots = @"Bots\GroupsBots";
        public const string OtherBots = @"Bots\OtherBots";

        private const string BotClasses = @"BOT_Platform.Kernel.Bots";

        public const string MainBot = "MainBot";

        /// <summary>
        /// Словарь всех ботов в системе
        /// </summary>
        public static volatile Dictionary<string, Bot> Bots;

        internal static Thread consoleThread;

        static void Main()
        {
            InitalizeConsole();
            Run();
        }

        public static void Run()
        {
            BotConsole.Write("---------------------------------------------------------------------\n" +
                             $"BOT_Platfrom v{Assembly.GetExecutingAssembly().GetName().Version}\n" +
                             "---------------------------------------------------------------------", MessageType.System);

            BotConsole.Write("[Инициализация консоли...]", MessageType.System);
            /* Подключаем стандартный модуль с базовыми командами */
            StandartCommands sC = new StandartCommands();

            /* Запускаем обратчик команд */
            CommandsCenter.ConsoleCommand("restart", null, null);
        }

        private static void InitalizeConsole()
        {
            Console.OutputEncoding = Encoding.UTF8;
            BotConsole.SetWriter((string text, MessageType type) =>
            {
                if (type == MessageType.Error) Console.ForegroundColor = ConsoleColor.Red;
                else if (type == MessageType.Warning) Console.ForegroundColor = ConsoleColor.Yellow;
                else if (type == MessageType.Info) Console.ForegroundColor = ConsoleColor.Green;
                else if (type == MessageType.System) Console.ForegroundColor = ConsoleColor.Cyan;
                Console.WriteLine(text);
                Console.ResetColor();
            });

            BotConsole.SetReader(Console.ReadLine, true);
            BotConsole.SetNotifyer((string caption, string text) =>
            {
                MessageBox.Show(text, caption, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                BotConsole.Write($"{caption}: {text}", MessageType.Warning);
            });
        }

        public static event EventHandler ClearCommands;
        /// <summary>
        /// Функция - обработчик команд, поступающих в консоль
        /// </summary>
        internal static void ConsoleCommander()
        { 
            /* Считываем настройки бота из файла настроек */
            BotConsole.Write("[Загружаются параметры платформы...]", MessageType.System);

            if(Bots != null)
                foreach (var bot in Bots.Values)
                {
                    bot.Abort();
                }
            Bots = new Dictionary<string, Bot>();

            string s = Log.logFile;  //заглушка
            CommandsCenter.ConsoleCommand("start", null, null); //заглушка
            ClearCommands.Invoke(new object(), null);

            /* Подключаем модули */
            ExecuteModules();
            /* Подключаем ботов */
            ExecuteBots();

            /* Создаём поток обработки ботом поступающих сообщений */
            CommandsCenter.ConsoleCommand("undebug", null, null);
            /* Обрабатываем команды, поступающие в консоль */
            BotConsole.StartReading();

        }

        static VkBot.CommandInfo GetCommandFromMessage(string consoleMessage)
        {
            int index = consoleMessage.IndexOf('(');

            string command = consoleMessage;
            string param = null;

            if (index != -1)
            {
                command = consoleMessage.Substring(0, index);
                param = consoleMessage.Substring(index + 1);
                int ind = param.LastIndexOf(')');
                if (ind == -1)
                {
                    param += ')';
                    param = param.Remove(param.Length - 1);
                }
                else param = param.Remove(ind);
            }
            Functions.RemoveSpaces(ref command);

            if (!String.IsNullOrEmpty(command) && 
                Char.IsUpper(command[0])) command = Char.ToLower(command[0]) + command.Substring(1);
            return new VkBot.CommandInfo(command, param);
        }

        public static bool AddSystem(DirectoryInfo dirInfo)
        {
            bool res = false;
            if (!Directory.Exists(Path.Combine(dirInfo.FullName, @"Data")))
                Directory.CreateDirectory(Path.Combine(dirInfo.FullName, @"Data"));
            if (!Directory.Exists(Path.Combine(dirInfo.FullName, @"Data\System")))
                Directory.CreateDirectory(Path.Combine(dirInfo.FullName, @"Data\System"));

            if (!File.Exists(Path.Combine(dirInfo.FullName, $@"{Log.logFile}")))
                File.Create(Path.Combine(dirInfo.FullName, $@"{Log.logFile}")).Close();
            if (!File.Exists(Path.Combine(dirInfo.FullName, $@"{Banlist.FILENAME}")))
                File.Create(Path.Combine(dirInfo.FullName, $@"{Banlist.FILENAME}")).Close();

            if (!File.Exists(Path.Combine(dirInfo.FullName, $@"{PlatformSettings.PATH}")))
            {
                File.Create(Path.Combine(dirInfo.FullName, $@"{PlatformSettings.PATH}")).Close();
                res = true;
            }
            return res;
        }

        private static void ExecuteBots()
        {
            if (!Directory.Exists(UsersBots)) Directory.CreateDirectory(UsersBots);
            if (!Directory.Exists(GroupsBots)) Directory.CreateDirectory(GroupsBots);
            if (!Directory.Exists(OtherBots)) Directory.CreateDirectory(OtherBots);

            try
            {
                BotConsole.Write("[Подключение UserBots...]", MessageType.System);
                foreach (DirectoryInfo dirInfo in (new DirectoryInfo(Path.Combine(Environment.CurrentDirectory, $"{UsersBots}")))
                    .GetDirectories())
                {
                    if (AddSystem(dirInfo))
                        File.WriteAllText(Path.Combine(dirInfo.FullName, $@"{PlatformSettings.PATH}"),
                            Properties.Settings.Default.SettingsTemplateUserBot);
                    UserBot bot = new UserBot(dirInfo.Name, dirInfo.FullName);
                    if (bot.InitalizeBot()) Bots.Add(dirInfo.Name, bot);
                }
                BotConsole.Write("Done.", MessageType.Info);
                BotConsole.Write("[Подключение GroupsBots...]", MessageType.System);
                foreach (DirectoryInfo dirInfo in (new DirectoryInfo(Path.Combine(Environment.CurrentDirectory, $"{GroupsBots}")))
                    .GetDirectories())
                {
                    if (AddSystem(dirInfo))
                        File.WriteAllText(Path.Combine(dirInfo.FullName, $@"{PlatformSettings.PATH}"),
                            Properties.Settings.Default.SettingsTemplateGroupBot);
                    GroupBot bot = new GroupBot(dirInfo.Name, dirInfo.FullName);
                    if (bot.InitalizeBot()) Bots.Add(dirInfo.Name, bot);
                }
                BotConsole.Write("Done.", MessageType.Info);
                BotConsole.Write("[Подключение OthersBots...]", MessageType.System);
                foreach (DirectoryInfo dirInfo in (new DirectoryInfo(Path.Combine(Environment.CurrentDirectory, $"{OtherBots}")))
                    .GetDirectories())
                {
                    try
                    {
                        if (AddSystem(dirInfo))
                            File.WriteAllText(Path.Combine(dirInfo.FullName, $@"{PlatformSettings.PATH}"),
                                Properties.Settings.Default.SettingsTemplateGroupBot);

                        Type BotType = null;

                        BotType = Type.GetType(
                            $"{BotClasses}.{dirInfo.Name.Substring(1, dirInfo.Name.IndexOf(']') - 1)}",
                            false, true);

                        if (BotType != null)
                        {
                            ConstructorInfo ci = BotType.GetConstructor(
                                new Type[] {typeof(string), typeof(string)});

                            object bot = Activator.CreateInstance(type: BotType);

                            ci.Invoke(bot,
                                new object[]
                                {
                                    dirInfo.Name.Substring(dirInfo.Name.IndexOf(']') + 1).Replace(" ", ""),
                                    dirInfo.FullName
                                });
                            if ((bot as Bot).InitalizeBot()) Bots.Add(
                                Functions.RemoveSpaces(dirInfo.Name.Substring(dirInfo.Name.IndexOf(']')+1)), (Bot) bot);
                        }
                        else
                        {
                            BotConsole.Write("---------------------------------------------------------------------\n" +
                            $"[REFLECTION ERROR] Класс " +
                                              $"{dirInfo.Name.Substring(1, dirInfo.Name.IndexOf(']') - 1)} не найден\n" +
                            "---------------------------------------------------------------------", MessageType.Error);
                        }
                    }
                    catch (Exception ex)
                    {
                        BotConsole.Write("---------------------------------------------------------------------\n" +
                        $"[ERROR] Ошибка в записи {dirInfo.Name}: {ex.Message}\n" +
                        "---------------------------------------------------------------------", MessageType.Error);
                    }
                }
                BotConsole.Write("Done.", MessageType.Info);
            }
            catch {}
            if (!Bots.ContainsKey(MainBot))
            {
                BotConsole.Write("---------------------------------------------------------------------\n" +
                $"[FATAL ERROR] Не удалось подключить {MainBot}, многие функции могут быть недоступны.\n" +
                "---------------------------------------------------------------------", MessageType.Error);
            }
        }

        /// <summary>
        /// Функция, подключающая все модули бота
        /// </summary>
        static void ExecuteModules()
        {
            BotConsole.Write("[Подключение модулей...]", MessageType.System);

            /* Подключаем модули, создавая обьекты их классов */
            Type[] typelist = Assembly.GetExecutingAssembly().GetTypes().Where(t => t.Namespace == DevNamespace).ToArray();
            foreach (Type type in typelist)
            {
                Activator.CreateInstance(Type.GetType(type.FullName));
                BotConsole.Write("Подключение " + type.FullName + "...", MessageType.Default);
            }

            BotConsole.Write("Модули подключены.", MessageType.Info);
        }

    }
}


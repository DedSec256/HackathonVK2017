 using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using BOT_Platform.Kernel.CIO;
using BOT_Platform.Kernel.Interfaces;
using BOT_Platform.Kernel.Settings;
using BOT_Platform.Kernel.Standart;
using VkNet;
using VkNet.Model;

namespace BOT_Platform.Kernel.Bots
{
    public static class BotExtension
    {
        public static T Convert<T>(this Bot bot) where T : Bot
        {
            return (bot as T);
        }
        public static T Convert<T>(this PlatformSettings sett) where T : PlatformSettings
        {
            return (sett as T);
        }
    }

    public abstract class Bot
    {
        /// <summary>
        /// Имя бота в системе
        /// </summary>
        public string Name { get; private set; }
        /// <summary>
        /// Физический полный пусть к боту
        /// </summary>
        public string Directory { get; private set; }
        /// <summary>
        /// </summary>
        /// <param name="name">Имя бота в системе</param>
        /// <param name="directory">Физический полный путь к боту</param>
        public Bot(string name, string directory)
        {
            Name = name;
            Directory = directory;
        }

        /// <summary>
        /// Загрузка параметров бота из файла настроек,
        /// указанного по физическому пути бота
        /// </summary>
        /// <returns></returns>
        public abstract bool InitalizeBot();
        /// <summary>
        /// Поток бота
        /// </summary>
        public Thread botThread;
        /// <summary>
        /// Настройки бота
        /// </summary>
        protected volatile PlatformSettings platformSett; /* Здесь хранятся все настройки бота */
        public PlatformSettings GetSettings()
        {
            return platformSett;
        }

        /// <summary>
        /// Определяет, каким образом будет обрабатываться команда в DebugMode бота
        /// </summary>
        /// <param name="consoleCommand">команда</param>
        public abstract void DebugExecuteCommand(string consoleCommand);

        public abstract void BotWork();
        public virtual void Abort()
        {
            botThread.Abort();
        }
    }
    public abstract class VkBot : Bot
    {
        /// <summary>
        /// Получить список пользователей с правами
        /// администратора для этого бота
        /// </summary>
        public long[] GetAdmins
        {
            get
            {
                return (platformSett as VkSettings).Admins;
            }
        }
        /// <summary>
        /// Обьект для работы с VK API
        /// (Обьект самого приложения VK.NET)
        /// </summary>
        protected volatile VkApi _app;    
        /// <summary>
        /// Возвращает обьект для работы с VK API
        /// </summary>
        /// <returns></returns>
        public VkApi GetApi()
        {
            return _app;
        }
        /// <summary>
        /// Возвращает настройки бота
        /// </summary>
        /// <returns></returns>
        
        protected int currentTimerTime = 0;
        protected  List<Message> lastMessages;

        protected object lockObject = new object();

        public VkBot(string name, string directory) : base(name, directory)
        {
            _app = new VkApi();
        }

        volatile public Dictionary<long?, (string[] request, Action<Message, string, Bot> correctAction, Action<Message, string, Bot> notcorrAction)> DialogWaitings
            = new Dictionary<long?, (string[] request, Action<Message, string, Bot> correctAction, Action<Message, string, Bot> notcorrAction)>();

        /// <summary>
        /// Пытается подключиться к vk.com в случае потери соединения и перезагружает бота в случае необходимости
        /// </summary>
        protected void TryToRestartSystem()
        {
            if(!platformSett.GetIsDebug())
                BotConsole.Write($"[NET_INFO {Name} " + DateTime.Now.ToLongTimeString() + "] Попытка подключиться к vk.com...", MessageType.Default);
            var answer = ConnectivityChecker.CheckConnection("vk.com");
            if (!platformSett.GetIsDebug())
                BotConsole.Write($"[NET_INFO {Name} " + DateTime.Now.ToLongTimeString() + "] " + answer.info, MessageType.Warning);
            if (!answer.status)
            {
                do
                {
                    Thread.Sleep(millisecondsTimeout: 30000);
                    answer = ConnectivityChecker.CheckConnection("vk.com");

                } while (!answer.status);

                if (!platformSett.GetIsDebug())
                {
                    BotConsole.Write($"[NET_INFO {Name} " + DateTime.Now.ToLongTimeString() +
                                      "] Попытка подключиться к vk.com...", MessageType.Default);
                    BotConsole.Write($"[NET_INFO {Name} " + DateTime.Now.ToLongTimeString() + "] " + answer.info, MessageType.Warning);
                }

                CommandsCenter.ConsoleCommand("undebug", null, this);
            }
        }
        /// <summary>
        /// Определяет, каким образом будет обрабатываться команда
        /// </summary>
        /// <param name="mesGetObj">Список последних сообщений, полученных ботом</param>
        protected virtual void ExecuteCommand(MessagesGetObject mesGetObj)
        {
            Parallel.ForEach(mesGetObj.Messages, Message =>
                {
                    if (String.IsNullOrEmpty(Message.Body)) return;

                    string botName = FindBotNameInMessage(Message);

                    //if (Message.ChatId != null && botName == "[NOT_FOUND]")
                    if (botName == "[NOT_FOUND]")
                        return;

                    lock (lockObject)
                        if (Functions.ContainsMessage(Message, lastMessages)) return;

                    CommandInfo comInfo = GetCommandFromMessage(Message, botName);

                    lock (lockObject)
                    {
                        if (lastMessages.Count >= (platformSett as VkSettings).MesRemembCount)
                        {
                            for (int j = 0; j < lastMessages.Count / 2; j++) lastMessages.RemoveAt(0);
                        }
                        lastMessages.Add(Message);
                    }
                    string temp = Message.Body;
                    Message.Body = comInfo.Command;
                    Message.Title = temp;
                    CommandsCenter.TryCommand(Message,
                        comInfo.Param, this);
                    Message.Body = temp;
                }
            );
            Thread.Sleep((platformSett as VkSettings).Delay);
        }
        /// <summary>
        /// Определяет, каким образом будет обрабатываться команда в DebugMode бота
        /// </summary>
        /// <param name="consoleCommand">команда</param>
        public override void DebugExecuteCommand(string consoleCommand)
        {
            Task.Run(() =>
                {
                    Message Message = new Message { Body = consoleCommand };

                    if (String.IsNullOrEmpty(Message.Body)) return;

                    string botName = FindBotNameInMessage(Message);

                    if (botName == "[NOT_FOUND]")
                        return;

                    CommandInfo comInfo = GetCommandFromMessage(Message, botName);

                    string temp = Message.Body;
                    Message.Body = comInfo.Command;
                    Message.Title = temp;
                    CommandsCenter.TryCommand(Message,
                        comInfo.Param, this);
                    Message.Body = temp;
                }
            );
        }
        public struct CommandInfo
        {
            public string Command { get; private set; }
            public string Param { get; private set; }

            public CommandInfo(string command, string param)
            {
                this.Command = command;
                this.Param = param;
            }
        }
        protected CommandInfo GetCommandFromMessage(Message message, string botName)
        {
            int index = message.Body.IndexOf('(');
            int botNameIndex = message.Body.IndexOf(botName + ",");

            string command = message.Body;
            string param = null;

            if (index != -1)
            {
                command = message.Body.Substring(0, index);
                param = message.Body.Substring(index + 1);
                int ind = param.LastIndexOf(')');
                if (ind == -1)
                {
                    param += ')';
                    param = param.Remove(param.Length - 1);
                }
                else param = param.Remove(ind);
            }
            if (message.ChatId != null || (botNameIndex >= 0 && botNameIndex < command.Length))
                command = command.Substring(botNameIndex + botName.Length + (1));

            Functions.RemoveSpaces(ref command);

            if (Char.IsUpper(command[0])) command = Char.ToLower(command[0]) + command.Substring(1);
            return new CommandInfo(command, param);
        }
        protected string FindBotNameInMessage(Message message)
        {
            for (int y = 0; y < (platformSett as VkSettings).BotName.Length; y++)
            {
                /* Если входящее сообщение содержит обращение к боту - "бот, ..." */
                int botNameIndex = message.Body.IndexOf((platformSett as VkSettings).BotName[y] + ",");
                if (botNameIndex != -1)
                {
                    return (platformSett as VkSettings).BotName[y];
                }
            }
            return "[NOT_FOUND]";
        }
        protected string FindBotNameInMessage(string message)
        {
            for (int y = 0; y < (platformSett as VkSettings).BotName.Length; y++)
            {
                /* Если входящее сообщение содержит обращение к боту - "бот, ..." */
                int botNameIndex = message.IndexOf((platformSett as VkSettings).BotName[y] + ",");
                if (botNameIndex != -1)
                {
                    return (platformSett as VkSettings).BotName[y];
                }
            }
            return "[NOT_FOUND]";
        }
    }
    public class UserBot : VkBot
    {
        public UserBot(string name, string directory) : base(name, directory)
        {
        }
        public override bool InitalizeBot()
        {
            try
            {
                platformSett = new VkUserSettings(Path.Combine(Directory, $@"{VkUserSettings.PATH}"));
            }
            catch (Exception ex)
            {
                BotConsole.Write("---------------------------------------------------------------------\n" +
                                 $"[ERROR][{Name}]:\n" + ex.Message + "\n" +
                                 "---------------------------------------------------------------------", MessageType.Error);
                return false;
            }
            return true;
        }

        /// <summary>
        /// Функция - обработчик сообщений, поступающих боту
        /// </summary>
        public override void BotWork()
        {

            /* Подключаемся к VK, запускаем бота */
            try
            {
                BotConsole.Write($"[Запуск бота {Name}...]", MessageType.Default);
                _app.Authorize((platformSett as VkUserSettings).AuthParams);
                BotConsole.Write($"Бот {Name} запущен.", MessageType.Info);
            }
            catch (VkNet.Exception.CaptchaNeededException ex)
            {
                try
                {
                    BotConsole.Write($"Введите капчу " + ex.Img.AbsoluteUri, MessageType.Default);
                    string res = Console.ReadLine();
                      VkUserSettings sett = (platformSett as VkUserSettings);
                    var AuthParams = sett.AuthParams;
                        AuthParams.CaptchaSid = ex.Sid;
                        AuthParams.CaptchaKey = res;


                    _app.Authorize(AuthParams);
                    BotConsole.Write($"Бот {Name} запущен.", MessageType.Info);
                }
                catch { }
            }
            catch (Exception ex)
            {
                BotConsole.Write($"[ERROR][{Name}]:\n" + ex.Message + "\n", MessageType.Error);
                CommandsCenter.ConsoleCommand("debug", null, this);
                Task.Run(() => TryToRestartSystem());
                return;
            }
            lastMessages = new List<Message>();
            currentTimerTime = Environment.TickCount;

            while (true)
            {
                MessagesGetObject messages;
                try
                {
                    /* Получаем нужное количество сообщений для обработки согласно настройкам бота*/
                    if (platformSett.GetIsDebug() == false) messages = _app.Messages.Get((platformSett as VkUserSettings).MesGetParams);
                    else
                    {
                        return;
                    }
                    this.ExecuteCommand(messages);
                    Thread.Sleep((platformSett as VkUserSettings).Delay);
                }
                catch (Exception ex)
                {
                    BotConsole.Write($"[ERROR][{Name} " + DateTime.Now.ToLongTimeString() + "]:\n" + ex.Message + "\n", MessageType.Error);
                    if (ex.Message == "User authorization failed: access_token has expired.")
                    {
                        this._app.RefreshToken();
                        BotConsole.Write($"[ERROR][{Name} " + DateTime.Now.ToLongTimeString() + "]: Токен обновлён.\n", MessageType.Error);
                    }
                    else TryToRestartSystem();

                    Thread.Sleep((platformSett as VkUserSettings).Delay);
                    continue;
                }
            }

        }
    }
    public class GroupBot : VkBot
    {
        public GroupBot(string name, string directory) : base(name, directory)
        {
        }

        public override bool InitalizeBot()
        {
            try
            {
                platformSett = new VkGroupSettings(Path.Combine(Directory, $@"{VkGroupSettings.PATH}"));
            }
            catch (Exception ex)
            {
                BotConsole.Write("---------------------------------------------------------------------\n" +
                $"[ERROR][{Name}]:\n" + ex.Message + "\n" +
                "---------------------------------------------------------------------", MessageType.Error);
                return false;
            }
            return true;
        }

        public override void BotWork()
        {
            /* Подключаемся к VK, запускаем бота */
            try
            {
                BotConsole.Write($"[Запуск бота {Name}...]", MessageType.Default);
                _app.Authorize((platformSett as VkGroupSettings).Token, null, 0);
            }
            catch (Exception ex)
            {
                return;
            }

            if (!ConnectivityChecker.CheckConnection("vk.com").status)
            {
                BotConsole.Write($"[ERROR][{Name}]:\n" + "Ошибк подключения к vk.com" + "\n", MessageType.Error);
                CommandsCenter.ConsoleCommand("debug", null, this);
                Task.Run(() => TryToRestartSystem());
                return;
            }

            BotConsole.Write($"Бот {Name} запущен.", MessageType.Info);


            lastMessages = new List<Message>();
            currentTimerTime = Environment.TickCount;

            while (true)
            {
                MessagesGetObject mesGetObj;
                try
                {
                    /* Получаем нужное количество сообщений для обработки согласно настройкам бота*/
                    if (platformSett.GetIsDebug() == false) mesGetObj = _app.Messages.Get((platformSett as VkGroupSettings).MesGetParams);
                    else
                    {
                        return;
                    }
                    ExecuteCommand(mesGetObj);
                    Thread.Sleep((platformSett as VkGroupSettings).Delay);
                }
                catch (Exception ex)
                {
                    BotConsole.Write($"[ERROR][{Name} " + DateTime.Now.ToLongTimeString() + "]:\n" + ex.Message + "\n", MessageType.Error);

                    TryToRestartSystem();
                    Thread.Sleep((platformSett as VkGroupSettings).Delay);
                    continue;
                }
            }
        }
        protected override void ExecuteCommand(MessagesGetObject mesGetObj)
        {
            Parallel.ForEach(mesGetObj.Messages, Message =>
                {
                    if (String.IsNullOrEmpty(Message.Body)) return;

                    string botName = FindBotNameInMessage(Message);

                    if (Message.ChatId != null && botName == "[NOT_FOUND]")
                        return;

                    lock (lockObject)
                        if (Functions.ContainsMessage(Message, lastMessages)) return;

                    VkBot.CommandInfo comInfo = GetCommandFromMessage(Message, botName);

                    lock (lockObject)
                    {
                        if (lastMessages.Count >= (platformSett as VkGroupSettings).MesRemembCount)
                        {
                            for (int j = 0; j < lastMessages.Count / 2; j++) lastMessages.RemoveAt(0);
                        }
                        lastMessages.Add(Message);
                    }
                    string temp = Message.Body;
                    Message.Body = comInfo.Command;
                    Message.Title = temp;
                    CommandsCenter.TryCommand(Message,
                        comInfo.Param, this);
                    Message.Body = temp;
                }
            );
            Thread.Sleep((platformSett as VkGroupSettings).Delay);
        }

    }


}

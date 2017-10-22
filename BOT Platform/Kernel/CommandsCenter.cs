using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BOT_Platform.Kernel.Bots;
using BOT_Platform.Kernel.CIO;
using BOT_Platform.Kernel.Interfaces;
using BOT_Platform.Kernel.Settings;
using MyFunctions.Exceptions;
using VkNet.Model;

namespace BOT_Platform.Kernel.Standart
{
    public struct MyCommandStruct
    {
        readonly string description;
        readonly CommandsCenter.Function MyFunction;
        public string Description => this.description;
        public CommandsCenter.Function Function => this.MyFunction;

        public bool Hidden { get; set; }

        public MyCommandStruct(string desc, CommandsCenter.Function func, bool isHidden = false)
        {
            this.Hidden = isHidden;
            this.description = desc;
            this.MyFunction = new CommandsCenter.Function(func);
        }
    }

    public static partial class CommandsCenter
    {
            static CommandsCenter()
            {
                BotMain.ClearCommands += BOT_API_ClearCommands;
            }

        public delegate void Function(Message message, string args, Bot bot);

        static SortedDictionary<string, MyCommandStruct> commandList =
            new SortedDictionary<string, MyCommandStruct>();

        static SortedDictionary<string, MyCommandStruct> consoleCommandList =
            new SortedDictionary<string, MyCommandStruct>();

        static Dictionary<string, string> banList = new Dictionary<string, string>();

        public static void AddConsoleCommand(string command, MyCommandStruct mcs)
        {
            string[] commands = command.Split(new char[] { '|' }, StringSplitOptions.RemoveEmptyEntries);
            for (int i = 0; i < commands.Length; i++)
            {
                Functions.RemoveSpaces(ref commands[i]);
                MyCommandStruct mcsCopy = mcs;
                if (i > 0)
                {
                    mcs.Hidden = true;
                }
                if (!consoleCommandList.ContainsKey(commands[i]))
                {
                    consoleCommandList.Add(commands[i], mcsCopy);
                }
                else BotConsole.Write("Не удалось добавить команду \\" + commands[i] + ".\n" +
                                      "Команда уже определена.\n", MessageType.Warning);
            }
        }

        public static void TryBanUser(Message message, string id, string description, Bot bot)
        {
            if (banList.ContainsKey(id))
            {
                BotConsole.Write("---------------------------------------------------------------------\n" +
                "[ERROR] Пользователь https://vk.com/id" + id + " уже был забанен" +
                "---------------------------------------------------------------------", MessageType.Error);
                Functions.SendMessage(bot, message, "[ERROR] Пользователь https://vk.com/id" + id + " уже был забанен",
                                      message.ChatId != null);
            }

            else
            {
                banList.Add(id, description);
                Functions.SendMessage(bot, message, "Пользователь https://vk.com/id" + id + " ЗАбанен!",
                                      message.ChatId != null);
            }
        }
        public static void TryUnBanUser(Message message, string id, Bot bot)
        {
            if (banList.ContainsKey(id))
            {
                banList.Remove(id);
                Functions.SendMessage(bot, message, "Пользователь https://vk.com/id" + id + " был РАЗбанен!",
                                      message.ChatId != null);
            }

            else
            {
                BotConsole.Write("---------------------------------------------------------------------\n" +
                "[ERROR] Пользователь https://vk.com/id" + id + " не был ЗАбанен\n" +
                "---------------------------------------------------------------------", MessageType.Error);
                Functions.SendMessage(bot, message, "[ERROR] Пользователь https://vk.com/id" + id + " не был ЗАбанен",
                                      message.ChatId != null);
            }
        }

        private static void BOT_API_ClearCommands(object sender, EventArgs e)
        {
            commandList.Clear();
            banList.Clear();
        }

        public static void ConsoleCommand(string command, string args, Bot bot)
        {
            if (!String.IsNullOrEmpty(command) && consoleCommandList.ContainsKey(command)) 
                consoleCommandList[command].Function(new Message() { Body = command}, args, bot);
        }

        public static void TryCommand(Message message, string args, Bot bot)
        {
            //забей
            if (banList.ContainsKey(message.UserId.ToString()))
            {
                Functions.SendMessage(bot, message, banList[message.UserId.ToString()],
                                          message.ChatId != null);
                return;
            }
            if (bot.GetSettings().Convert<VkSettings>().AllowedСommands.Contains(message.Body))
            {
                try
                {
                    commandList[message.Body].Function(message, args, bot);
                }
                catch (BotPlatformException ex)
                {
                    WriteErrorInfo(message, ex, bot);
                    Functions.SendMessage(bot, message, ex.Message + "\n\n" +
                                                        $"Для получения справки по команде напишите {bot.GetSettings().Convert<VkSettings>().BotName[0]}, {message.Body}",
                        message.ChatId != null);
                }
                catch (Exception ex)
                {
                    WriteErrorInfo(message, ex, bot);
                    if (ex.InnerException is BotPlatformException)
                    {
                        Functions.SendMessage(bot, message, ex.InnerException.Message + "\n\n" +
                                                            $"Для получения справки по команде напишите {bot.GetSettings().Convert<VkSettings>().BotName[0]}, {message.Body}",
                            message.ChatId != null);
                    }
                    else 
                    {
                        Functions.SendMessage(bot, message, "Произошла ошибка при выполнении команды ¯\\_(ツ)_/¯.\n" +
                                                            "Убедитесь, что параметры переданы правильно "+
                                                            "или повторите запрос позже.\n\n" +

                                                            $"Для получения справки по команде напишите {message.Body}",
                            message.ChatId != null);
                    }

                }
            }

            else // Если команда не найдена
            {
              if(message.UserId != bot.Convert<VkBot>().GetApi().UserId)
                    Functions.SendMessage(bot, message, "Команда \"" + message.Body +$"\" не найдена.\n\nСписок доступных команд:\n " +
                                                        $"{GetCommandList(bot)}", 
                                    message.ChatId != null);
            }
        }
    
        public static void TryAddCommand(string command, MyCommandStruct mcs)
        {
            string[] commands = command.Split(new char[] {'|'}, StringSplitOptions.RemoveEmptyEntries);
            for (int i = 0; i < commands.Length; i++)
                {
                    Functions.RemoveSpaces(ref commands[i]);
                    MyCommandStruct mcsCopy = mcs;
                    if (i > 0)
                    {
                        mcs.Hidden = true;
                    }
                    if (!commandList.ContainsKey(commands[i]))
                    {
                        commandList.Add(commands[i], mcsCopy);
                    }
                    else BotConsole.Write("Не удалось добавить команду \\" + commands[i] + ".\n" +
                                          "Команда уже определена.\n", MessageType.Warning);
            }
        }

        public static string GetCommandList(Bot bot)
        {
            StringBuilder sB = new StringBuilder();
            
                foreach (string key in bot.GetSettings().Convert<VkSettings>().AllowedСommands)
                {
                    MyCommandStruct mcs = commandList[key];

                    if (mcs.Hidden != true)
                        sB.AppendLine("⦿ " + key + " — " + mcs.Description);
                }

            return sB.ToString();
        }

        public static List<string> Commands
        {
            get => commandList.Keys.ToList();
        }

        static void WriteErrorInfo(Message message, Exception ex, Bot bot)
        {
            string text = "---------------------------------------------------------------------\n" +
                          $"[ERROR { DateTime.Now.ToLongTimeString()}]\n" +
                          $"От: https://vk.com/id{message.UserId}\n" +
                          $"Команда: \"" + message.Title + "\"\n" + ex.Message + "\n" +
                          "[STACK_TRACE] " + ex.StackTrace + "\n" +
                          "---------------------------------------------------------------------\n";
            Parallel.Invoke(() =>
            {
                BotConsole.Write(text, MessageType.Error);
            },()=>
            {
                Log.WriteLog(text, bot.Directory);
            });
        }
    }
}

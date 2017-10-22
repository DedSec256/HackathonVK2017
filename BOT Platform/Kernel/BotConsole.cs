using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using BOT_Platform.Kernel.Bots;
using BOT_Platform.Kernel.Interfaces;
using BOT_Platform.Kernel.Standart;

namespace BOT_Platform.Kernel.CIO /* CIO - Console In-Out */
{
    public enum  MessageType 
    {
        Error,
        Info,
        Warning,
        System,
        Default
    }

    public static class BotConsole
    {
        private static bool alwaysRead;
        static BotConsole()
        {
            Writer = null;
            Reader = null;
            Notifyer = null;
        }

        public static void Read()
        {
            void ExecuteCommand(string newCommand)
            {
                try
                {
                    VkBot.CommandInfo info = GetCommandFromMessage(newCommand);
                    if (!String.IsNullOrEmpty(info.Command) ||
                        !String.IsNullOrEmpty(info.Param))
                        CommandsCenter.ConsoleCommand(info.Command, info.Param, null);
                }
                catch (Exception ex)
                {
                    BotConsole.Write("[ERROR][SYSTEM " + DateTime.Now.ToLongTimeString() + "]:\n" + ex.Message + "\n",
                        MessageType.Error);
                }
            }

            if (alwaysRead)
                while (true)
                {
                    ExecuteCommand(Reader());
                }
            else ExecuteCommand(Reader());
        }

        private static Action<string, MessageType> Writer;
        private static Action<string, string> Notifyer;
        private static Func<string> Reader;

        public static void SetWriter(Action<string, MessageType> _writer)
        {
            Writer = _writer;
        }

        public static void SetReader(Func<string> _reader, bool _alwaysRead = false)
        {
            Reader = _reader;
            alwaysRead = _alwaysRead;
        }

        public static void SetNotifyer(Action<string, string> _notifyer)
        {
            Notifyer = _notifyer;
        }

        private static object lockObj = new object();
        public static void Write(string text, MessageType type = MessageType.Default)
        {
            lock(lockObj) Writer(text, type);
        }

        internal static void StartReading()
        {
            if(alwaysRead) Read();
        }

        public static void Notify(string caption, string text)
        {
            Notifyer(caption, text);
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
    }
}

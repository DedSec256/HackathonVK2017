using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using BOT_Platform.Kernel.CIO;
using BOT_Platform.Kernel.Interfaces;
using BOT_Platform.Kernel.Settings;
using BOT_Platform.Kernel.Standart;
using MyFunctions;
using VkNet.Model;

namespace BOT_Platform.Kernel.Bots
{
    class Tinkoff : UserBot
    {

        public Tinkoff(string name, string directory) : base(name, directory)
        {
            lastMessages = new List<Message>();
            currentTimerTime = Environment.TickCount;
        }

        public Tinkoff() : base(null, null)
        {
            lastMessages = new List<Message>();
            currentTimerTime = Environment.TickCount;
        }

        /* обрабатывает поступающие команды */
        protected override void ExecuteCommand(MessagesGetObject mesGetObj)
        {

            foreach (var Message in mesGetObj.Messages)
                {
                #region Чтобы не отвечал на одно и то же сообщение
                if (String.IsNullOrEmpty(Message.Body)) return;

                    lock (lockObject)
                        if (Functions.ContainsMessage(Message, lastMessages)) return;

                    lock (lockObject)
                    {
                        if (lastMessages.Count >= (platformSett as VkUserSettings).MesRemembCount)
                        {
                            for (int j = 0; j < lastMessages.Count / 2; j++) lastMessages.RemoveAt(0);
                        }
                        lastMessages.Add(Message);
                    }
                #endregion //не трогать

                    string[] args = Message.Body.Split(new[] {' '}, 2, StringSplitOptions.RemoveEmptyEntries);
       
                    Message message = new Message();
                    message.CopyFrom(Message);
                    message.Body = args[0];


                    if (DialogWaitings.ContainsKey(message.UserId))
                    {
                        if (DialogWaitings[message.UserId].request.Contains(message.Body))
                        {
                            DialogWaitings[message.UserId].correctAction(message, args.Length > 1 ? args[1] : null, this);
                            DialogWaitings.Remove(message.UserId);
                        }
                        else
                        {
                            DialogWaitings[message.UserId].notcorrAction(message, args.Length > 1 ? args[1] : null, this);
                        }
                    } /* забей */
                    else CommandsCenter.TryCommand(message, args.Length > 1 ? args[1] : null , this);
            }
        }

        /*
         * DialogWaitings.Add(id пользователя, (new []{Эудалить, инфо}, () => 
         * {
         * CommandsCenter.TryCommand(message, args.Length > 1 ? args[1] : null , this)
         * }, 
         * 
         * () => {
         *  если нет такой команды
         * }
         * ));
         * */


        public void ConfirmWaiting(Message message, long code, TinkoffCommands.Seller seller)
        {
            if (!DialogWaitings.ContainsKey(seller.OwnerId))
            {
                Action<Message, string, Bot> corAction = (mess, args, bot) =>
                {

                    string name = $"Shop{seller.GroupId}";

                    DirectoryInfo dirInfo = 
                        System.IO.Directory.CreateDirectory(Path.Combine(BotMain.OtherBots, "[GroupShopBot] " + name));


                    BotMain.AddSystem(dirInfo);
                    File.WriteAllText(Path.Combine(dirInfo.FullName, $@"{PlatformSettings.PATH}"),
                        BOT_Platform.Properties.Settings.Default.SettingsTemplateGroupBot.Replace("[TOKEN]",
                            "[TOKEN] " + seller.Token));
                    GroupShopBot shopBot = new GroupShopBot(dirInfo.Name, dirInfo.FullName);
                    if (shopBot.InitalizeBot())
                    {
                        BotMain.Bots.Add(dirInfo.Name, shopBot);
                        shopBot.OwnerId = mess.UserId.Value;
                        CommandsCenter.ConsoleCommand($"undebug", null, shopBot);
                    }

                    Functions.SendMessage(this, message, "✅ Проверка пройдена. Сообщество успешно зарегистрировано.\n" +
                                                         "Введите команду \'реквизиты\', чтобы продолжить",
                        message.ChatId != null);

                    DialogWaitings.Remove(seller.GroupId);

                };

                Action<Message, string, Bot> uncAct = (mess, args, bot) =>
                {
                    Functions.SendMessage(this, mess, "❌ Неверный код",
                        message.ChatId != null);
                };


                DialogWaitings.Add(seller.OwnerId, (new []{code.ToString()}, corAction, uncAct));
            }
            System.Timers.Timer timer = new System.Timers.Timer(60000);
            timer.Elapsed += (sender, args) =>
            {
                if (DialogWaitings.ContainsKey(seller.GroupId))
                {
                    Functions.SendMessage(this, message, "❌ Проверка не пройдена: истекло время ожидания подтверждения", message.ChatId != null);
                    DialogWaitings.Remove(seller.GroupId);
                    MyFunctions.TinkoffCommands.Sellers.Remove(seller.OwnerId.Value);
                    timer.Stop();
                }

            };
            timer.Start();
        }

        public bool Confirm(Message message, TinkoffCommands.Seller seller)
        {
            Random rnd = new Random();

            //Получить случайное число (в диапазоне от 0 до 10)
            int value = rnd.Next(10000, 99999);
            Functions.SendMessage(this, message, "Введите проверочный ключ (только цифры, указанные в личном сообщении) ", message.ChatId != null);

            Message toGroup = new Message();
            toGroup.CopyFrom(message);
            toGroup.UserId = seller.GroupId;

            Functions.SendMessage(this, toGroup, "Ваш проверочный ключ: " + value, message.ChatId != null);
            ConfirmWaiting(message, value, seller);
            return true;
        }

    }
}

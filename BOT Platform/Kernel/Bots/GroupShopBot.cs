using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BOT_Platform.Kernel.Interfaces;
using BOT_Platform.Kernel.Settings;
using BOT_Platform.Kernel.Standart;
using VkNet.Model;
using System.Collections;
using MyFunctions;

namespace BOT_Platform.Kernel.Bots
{
    class GroupShopBot : GroupBot
    {
        public Dictionary<long, Customer> Baskets 
            = new Dictionary<long, Customer>();

        public GroupShopBot(string name, string directory) : base(name, directory)
        {
        }
        public GroupShopBot() : base(null, null)
        {

        }

        public long OwnerId { get; set; }

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
                    if (lastMessages.Count >= (platformSett as VkGroupSettings).MesRemembCount)
                    {
                        for (int j = 0; j < lastMessages.Count / 2; j++) lastMessages.RemoveAt(0);
                    }
                    lastMessages.Add(Message);
                }

                #endregion



                //Перед обработкой сообщения проверяем, содержит ли оно товар. Если содержит, то добавляем в корзину, оповещаем пользователя и больше не обрабатываем это сообщение
                var Markets = Message.Attachments.Where(t => t.Instance is Market);
                if (!this.Baskets.ContainsKey(Message.UserId.Value))
                {
                    this.Baskets.Add(Message.UserId.Value, new Customer(null));
                    //TinkoffCommands.Sellers[OwnerId]));
                }
                if (Markets.Count() != 0)
                {
  
                    var Market = Markets.Select(t => t.Instance as Market).First();
                    this.Baskets[Message.UserId.Value].Products.Add(new Product(Market,
                        // TinkoffCommands.Sellers[OwnerId].Products[Market.Id.ToString()].TinkoffProduct));
                        new RequestsManager.RequestJsons.Product_JSON()));

                    Functions.SendMessage(this, Message, "✅ Товар \"" + Market.Title + "\" добавлен в вашу корзину.\n" +
                                                         "Напишите команду \'оформить\', чтобы получить счёт и реквизиты оплаты", Message.ChatId != null);
                    continue;
                }


                //Обработка сообщения если в нем нет товара
                string[] args = Message.Body.Split(new[] { ' ' }, 2, StringSplitOptions.RemoveEmptyEntries);
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
                }

                else CommandsCenter.TryCommand(message, args.Length > 1 ? args[1] : null, this);
            }
        }

    }

}

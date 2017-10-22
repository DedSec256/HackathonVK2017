using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using BOT_Platform;
using BOT_Platform.Kernel;
using VkNet.Model;
using VkNet.Model.RequestParams;
using BOT_Platform.Kernel.Bots;
using BOT_Platform.Kernel.CIO;
using BOT_Platform.Kernel.Interfaces;
using BOT_Platform.Kernel.Settings;
using BOT_Platform.Kernel.Standart;
using Newtonsoft.Json;
using System.Threading;

namespace MyFunctions
{
    /* для главного бота */
    public class TinkoffCommands : IMyCommands
    {
        public TinkoffCommands()
        {
            AddMyCommandInPlatform();
        }

        public void AddMyCommandInPlatform()
        {
            CommandsCenter.TryAddCommand("регистрация", new MyCommandStruct("зарегистрировать сообщество в системе", Register));
           // CommandsCenter.TryAddCommand("данные", new MyCommandStruct("описание", Data));
            CommandsCenter.TryAddCommand("добавить", new MyCommandStruct("связать продукт группы с продуктом Tinkoff", AddProduct)); //добавить продукт
           // CommandsCenter.TryAddCommand("инфо", new MyCommandStruct("описание", Scheta));
            CommandsCenter.TryAddCommand("продукты", new MyCommandStruct("описание", Products));
            CommandsCenter.TryAddCommand("убрать", new MyCommandStruct("убрать связь между продуктом группы и продуктом Tinkoff", DeleteProduct));
        }

        private void Products(Message message, string args, Bot bot)
        {
        }

        private void DeleteProduct(Message message, string args, Bot bot)
        {
            if (!Sellers.ContainsKey(message.UserId.Value))
            {
                Functions.SendMessage(bot, message, "❌ Вы ещё не зарегистрировали сообщество.", message.ChatId != null);
                return;
            }
        }
        private void AddProduct(Message message, string args, Bot bot)
        {
            if (!Sellers.ContainsKey(message.UserId.Value))
            {
                Functions.SendMessage(bot, message, "❌ Вы ещё не зарегистрировали сообщество.", message.ChatId != null);
                return;
            }
            string[] param = args.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
            for(int i = 0; i< param.Length; i++)
            {
                try
                {
                    string[] arg = Functions.RemoveSpaces(param[i])
                        .Split(new[] {'-'}, StringSplitOptions.RemoveEmptyEntries);
                    var seller = Sellers[message.UserId.Value];

                    Functions.RemoveSpaces(ref arg[0]); // id товара в вк
                    Functions.RemoveSpaces(ref arg[1]);

                    if (!seller.Products.ContainsKey(arg[0]))
                        seller.Products.Add(arg[0], new Product((bot as Tinkoff).GetApi().Markets.GetById(new[] {arg[1]})[0],
                                                                          seller.requestManager.GetAllProducts().result.Where(res => res.sku == arg[1]).First()  ));
                    Thread.Sleep(200); /*CHANGE TO REAL PRODUCT BY ID*/
                }
                catch { }
            }
            Functions.SendMessage(bot, message, "✅ Продукты успешно синхронизированы.", message.ChatId != null);
        }
        private void Data(Message message, string args, Bot bot)
        {
            //string param = args.Split(new[] {','}, StringSplitOptions.RemoveEmptyEntries);

        }
 
        private void Register(Message message, string args, Bot bot)
        {
                if (NeedCommandInfo(message, args, bot)) return;

                string[] arg = args.Split(new char[] {','}, 2, StringSplitOptions.RemoveEmptyEntries);

                Seller seller = new Seller()
                {
                    GroupId = Functions.GetGroupId(arg[0], bot),
                    Token = arg[1],
                    OwnerId = message.UserId
                };
            if (Sellers.ContainsKey(seller.OwnerId.Value))
            {
                if (seller.Token == Sellers[seller.OwnerId.Value].Token)
                {
                    Functions.SendMessage(bot, message, "❌ Вы уже зарегистрировали данное сообщество.");
                    return;
                }
                Sellers[seller.OwnerId.Value] = seller;
            }
            else Sellers.Add(seller.OwnerId.Value, seller);

            bot.Convert<Tinkoff>().Confirm(message, seller);
        }

        private bool CheckINN(string v)
        {
            if (v.Length != 12) return false;
            foreach (var c in v)
            {
                if (!Char.IsDigit(c)) return false;
            }
            return true;
        }

        public bool NeedCommandInfo(Message message, string args, Bot bot)
        {
            if (String.IsNullOrEmpty(args))
            {
                switch (message.Body)
                {
                    case "регистрация":
                        Functions.SendMessage(bot, message, "Напишите следующую команду:\n" +
                                                            "регистрация {id группы}, {токен доступа группы}");
                        break;
                    default:
                        break;

                }
                return true;
            }
            return false;

        }

        public static Dictionary<long, Seller> Sellers = new Dictionary<long, Seller>();
        public class Seller
        {
            public Requisites Requisites;
            public RequestsManager requestManager;
            public long GroupId { get; set; }
            public long? OwnerId { get; set; }
            public string Token { get; set; }
            public Dictionary<string, Product> Products = new Dictionary<string, Product>();

            public void Create()
            {
                requestManager =
                    new RequestsManager(
                        "0c/e+6M/oLT2B9HoIdQGaPPoqmzz9pRxhzNHHrd/H+E38zKxPCX5RqFOxrcDnXqgpGIYjFVvppSk468MK6F6vg==",
                        "user31", Requisites.inn);
            }
        }

        public class Requisites
        {
            public string fio;
            public string acct; //счёт
            public string inn;
            public string legal_address;
            public string org_kpp;
            public string bank_name;
            public string bank_location;
            public string banc_bic;
            public string kor_acct;

            public static Requisites TryParse(string text)
            {
                string[] args = text.Split(new[]
                {
                    "ФИО:", "СЧЁТ:", "ИНН:", "АДРЕС ЮРИДИЧЕСКОЙ ОРГАНИЗАЦИИ ИЛИ ЛИЦА:",
                    "КПП ОРГАНИЗАЦИИ:", "НАЗВАНИЕ БАНКА:", "ЛОКАЦИЯ БАНКА:", "БИК БАНКА:","КОР СЧЁТ:"
                }, 9, StringSplitOptions.RemoveEmptyEntries);

                return new Requisites()
                {
                    fio = args[0].RemoveSpaces(),
                    acct = args[1].RemoveSpaces(),
                    inn = args[2].RemoveSpaces(),
                    legal_address = args[3].RemoveSpaces(),
                    org_kpp = args[4].RemoveSpaces(),
                    bank_name = args[5].RemoveSpaces(),
                    bank_location = args[6].RemoveSpaces(),
                    banc_bic = args[7].RemoveSpaces(),
                    kor_acct = args[8].RemoveSpaces()
                };
            }
        }

    }
}

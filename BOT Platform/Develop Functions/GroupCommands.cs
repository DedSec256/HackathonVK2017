using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using BOT_Platform.Kernel.Bots;
using BOT_Platform.Kernel.CIO;
using BOT_Platform.Kernel.Interfaces;
using BOT_Platform.Kernel.Standart;
using VkNet.Model;
using VkNet.Model.Attachments;
using BOT_Platform.Kernel;
using static BOT_Platform.Kernel.RequestsManager.RequestJsons;

namespace MyFunctions
{
    class GroupCommands: IMyCommands
    {
        public GroupCommands()
        {
            AddMyCommandInPlatform();
        }
        public void AddMyCommandInPlatform()
        {
            //CommandsCenter.TryAddCommand("контакты", new MyCommandStruct("получить контакты (e-mail и телефон) по выставленному счёту", Delete));
            CommandsCenter.TryAddCommand("удалить", new MyCommandStruct("удалить товар из корзины", Delete));
            CommandsCenter.TryAddCommand("корзина", new MyCommandStruct("вывести список товаров в корзине", ShowItemsInBasket));
            CommandsCenter.TryAddCommand("оформить", new MyCommandStruct("оформить заказ", Order));
            CommandsCenter.TryAddCommand("реквизиты", new MyCommandStruct("заполнить реквизиты", Requisites));
            CommandsCenter.TryAddCommand("отменить", new MyCommandStruct("отменить заказ", CancelOrder));
            //CommandsCenter.TryAddCommand("", new MyCommandStruct("описание", Scheta));
        }

        string[] Requisite = new String[]
        {
            "ФИО: - \n", 
            "СЧЁТ: - \n", 
            "ИНН: - \n", 
            "АДРЕС ЮРИДИЧЕСКОЙ ОРГАНИЗАЦИИ ИЛИ ЛИЦА: - \n", 
            "КПП ОРГАНИЗАЦИИ: - \n", 
            "НАЗВАНИЕ БАНКА: - \n", 
            "ЛОКАЦИЯ БАНКА: - \n", 
            "БИК БАНКА: - \n",
            "КОР СЧЁТ: - "

        };

        private void Requisites(Message message, string args, Bot bot)
        {
            StringBuilder sB = new StringBuilder();
            foreach (var str in Requisite)
            {
                sB.Append(str);
            }
            if (bot is Tinkoff && !TinkoffCommands.Sellers.ContainsKey(message.UserId.Value))
            {
                    Functions.SendMessage(bot, message, "❌ Вы ещё не зарегистрировали сообщество.", message.ChatId != null);
                    return;
            }

            string text = "(Ленивый сервис) Скопируйте данное сообщение, затем замените прочерки на ваши данные:\n\n"
                          + sB.ToString();
            Functions.SendMessage(bot, message, text, message.ChatId != null);

            (bot as VkBot).DialogWaitings.Add(message.UserId, (new []{ Requisite[0].Replace(" - \n", ""), "отмена"}, 
                (mess, arg, Bot) =>
                {
                    if (mess.Body == "отмена")
                    {
                        Functions.SendMessage(bot, message, "Напишите команду \'реквизиты\', когда захотите заполнить форму.", mess.ChatId != null);
                        return;
                    }

                    if (Bot is GroupShopBot)
                        try
                        {
                            (Bot as GroupShopBot).Baskets[mess.UserId.Value].Requisites =
                                TinkoffCommands.Requisites.TryParse(mess.Body + " " + arg);
                        }
                        catch
                        {
                            Functions.SendMessage(bot, mess, "❌ Заполненная вами форма имеет неправильный вид!\nУбедитесь, что заполнены все пункты.", mess.ChatId != null);
                            return;
                        }
                    else
                    {
                        if (TinkoffCommands.Sellers.ContainsKey(mess.UserId.Value))
                        {
                            try
                            {
                                TinkoffCommands.Sellers[mess.UserId.Value].Requisites =
                                    TinkoffCommands.Requisites.TryParse(mess.Body + " " + arg);
                                TinkoffCommands.Sellers[mess.UserId.Value].Create();
                            }
                            catch
                            {
                                Functions.SendMessage(bot, mess, "❌ Заполненная вами форма имеет неправильный вид!\nУбедитесь, что заполнены все пункты.", mess.ChatId != null);
                                return; 
                            }
                        }
                    }
                    (Bot as VkBot).DialogWaitings.Remove(mess.UserId.Value);
                    Functions.SendMessage(bot, mess, "✅ Реквизиты сохранены.", mess.ChatId != null);
                }
            , 
                (mess, arg, Bot) =>
                {
                    Functions.SendMessage(bot, mess, "❌ Неверный формат реквизитов. Напишите команду \'отмена\', если хотите заполнить форму позже.", mess.ChatId != null);
                    //(Bot as GroupShopBot).DialogWaitings.Remove(mess.UserId.Value);
                }
            ));
        }

        private void CancelOrder(Message message, string args, Bot bot)
        {
            //
            //RequestsManager.DeleteInvoice((bot as GroupShopBot).Baskets[message.UserId.Value].customer.Invoice.result.id);
        }

        private void Order(Message message, string args, Bot bot)
        {
            GroupShopBot shopBot = bot as GroupShopBot;
            if (!shopBot.Baskets.ContainsKey(message.UserId.Value) 
                || shopBot.Baskets[message.UserId.Value].Products.Count == 0)
            {
                Functions.SendMessage(bot, message, "❌ Ваша корзина пуста. Выберите их в товарах группы и нажмите \"Написать продавцу\", чтобы оформить заказ", message.ChatId != null);
                return;
            }
            if(shopBot.Baskets[message.UserId.Value].Requisites == null)
            {
                Functions.SendMessage(bot, message, "Вы не поделились с нами реквизитами :c\nНапишите команду реквизиты", message.ChatId != null);
                return;
            }
            Functions.SendMessage(bot, message, "✅ Оформляем Ваш заказ...", message.ChatId != null);
            //Customer.Products = new List<Products>();
        }
        private void ShowItemsInBasket(Message message, string args, Bot bot)
        {
            if (!bot.Convert<GroupShopBot>().Baskets.ContainsKey(message.UserId.Value) ||
                bot.Convert<GroupShopBot>().Baskets[message.UserId.Value].Products.Count == 0)
            {
                Functions.SendMessage(bot, message, "Ваша корзина пуста.", message.ChatId != null);
                return;
            }

            string res = GetMarkets(message.UserId.Value, bot);
            Functions.SendMessage(bot, message, "Ваш список товаров:\n" + res, message.ChatId != null);
        }
        //список всех товаров в магазине
        private string GetMarkets(long userID, Bot bot)
        {
            Market[] bask = (bot as GroupShopBot).Baskets[userID].Products.Select(t => t.VKProduct).ToArray();
            StringBuilder sB = new StringBuilder();
            for (int i = 1; i <= (bot as GroupShopBot).Baskets[userID].Products.Count; i++)
            {
                sB.AppendLine(i + $") {bask[i - 1].Title}. Цена: {bask[i - 1].Price.Text}");
            }
            return sB.ToString();
        }
        private void Delete(Message message, string args, Bot bot)
        {
            if (NeedCommandInfo(message, args, bot)) return;

            if (!(bot as GroupShopBot).Baskets.ContainsKey(message.UserId.Value) || (bot as GroupShopBot).Baskets[message.UserId.Value].Products.Count == 0)
            {
                Functions.SendMessage(bot, message, "Ваша корзина пуста.", message.ChatId != null);
            }

            else //Работает для одного числа
            {
                StringBuilder sB = new StringBuilder();
                string[] param = args.Split(new[] {','}, StringSplitOptions.RemoveEmptyEntries);
                for (int i = 0; i < param.Length; i++) Functions.RemoveSpaces(ref param[i]);

                GroupShopBot b = (bot as GroupShopBot);
                var newUserMarkets = b.Baskets[message.UserId.Value].Products.Where((t, i) => !param.Contains((i+1).ToString())).ToList();
                b.Baskets[message.UserId.Value].Products = newUserMarkets;

                Functions.SendMessage(bot, message, "✅ Выбраные товары удалены из корзины", message.ChatId != null);

            }
        }


        public bool NeedCommandInfo(Message message, string args, Bot bot)
        {
            if (string.IsNullOrEmpty(args))
            {
                switch (message.Body)
                {
                    case "удалить":
                        string res = GetMarkets(message.UserId.Value, bot);
                        Functions.SendMessage(bot, message,
                            "Ваш список товаров:\n" + res + "\n\nВыберите позиции, которые хотите удалить из заказа.",
                            message.ChatId != null);
                        break;
                }
                return true;
            }
            return false;
        }

    }


    public class Customer
    {
        public Customer(TinkoffCommands.Seller seller)
        {
            Seller = seller;
        }
        public Customer() { }

        public List<Product> Products = new List<Product>();
        long CustomerId;
  

        private TinkoffCommands.Seller Seller;
        public InvoiceCreationResult_JSON Invoice;
        public TinkoffCommands.Requisites Requisites { get; set; } = null;
        public void Order()
        {

            // RequestsManager.RequestJsons.RequestResult_JSON delPos = req.DeletePosition("02a7086e-338b-4f7a-8189-14f964727732", "1");

            /*
            RequestsManager.RequestJsons.Position_JSON position = new RequestsManager.RequestJsons.Position_JSON();
            position.name = "jepa";
            position.productId = 2;
            position.price = 10;
            position.sku = "4258";
            position.unit = "g";
            position.amount = 10;
            position.vat = "18";*/

            /*
            RequestsManager.RequestJsons.AllPositions_JSON allPos =
                req.GetAllPositions("02a7086e-338b-4f7a-8189-14f964727732");
            RequestsManager.RequestJsons.RequestResultPos_JSON addPos =
                req.AddOrChangePosition("02a7086e-338b-4f7a-8189-14f964727732", 2, position);
            RequestsManager.RequestJsons.AllPositions_JSON allPos1 =
                req.GetAllPositions("02a7086e-338b-4f7a-8189-14f964727732");
            RequestsManager.RequestJsons.RequestResult_JSON delPos =
                req.DeletePosition("02a7086e-338b-4f7a-8189-14f964727732", 2);
            RequestsManager.RequestJsons.AllPositions_JSON allPos3 =
                req.GetAllPositions("02a7086e-338b-4f7a-8189-14f964727732");*/

            RequestsManager.RequestJsons.InvoiceCreationResult_JSON invoiceCreate =
                new RequestsManager.RequestJsons.InvoiceCreationResult_JSON();
            invoiceCreate.seller = new RequestsManager.RequestJsons.Partner_JSON();

            invoiceCreate.seller.inn = Seller.Requisites.inn;
            invoiceCreate.seller.account = Seller.Requisites.acct;
            invoiceCreate.seller.bank = new RequestsManager.RequestJsons.Bank_JSON();

            invoiceCreate.buyer = new RequestsManager.RequestJsons.Partner_JSON();
            invoiceCreate.buyer.inn = Requisites.inn;
            invoiceCreate.buyer.bank = new RequestsManager.RequestJsons.Bank_JSON();
            invoiceCreate.buyer.bank.name = Requisites.bank_name;
            invoiceCreate.buyer.bank.location = Requisites.bank_location;
            invoiceCreate.buyer.bank.bic = Requisites.banc_bic;
            invoiceCreate.buyer.bank.corrAccount = Requisites.kor_acct;

            invoiceCreate.payment = new RequestsManager.RequestJsons.Payment_JSON();
            invoiceCreate.payment.status = "DRAFT";
            invoiceCreate.payment.sum = 24; ////////////////////////////////////////

            invoiceCreate.number = 123;
            invoiceCreate.status = "DRAFT";

            invoiceCreate.modifications = new RequestsManager.RequestJsons.Modifications_JSON();
            invoiceCreate.modifications.createdAt = "2017-02-17T16:27:43.955+03:00";
            invoiceCreate.modifications.createdBy = Seller.Requisites.inn;

            Invoice = Seller.requestManager.CreateInvoice(invoiceCreate).result;

            AddMarkets();

            Seller.requestManager.SendInvoice(Invoice.id);

        }

        private void AddMarkets()
        {
            int i = 1;
            foreach (var product in Products)
            {

                Position_JSON pos = new Position_JSON()
                {
                    amount = 1,
                    invoiceId = Invoice.id,
                    name = product.VKProduct.Title,
                    price = (int) product.VKProduct.Price.Amount / 100,
                    productId = i,
                    sku = product.TinkoffProduct.sku,
                    unit = product.TinkoffProduct.unit,
                    vat = product.TinkoffProduct.vat
                };
                RequestsManager.RequestJsons.RequestResultPos_JSON addPos =
                    Seller.requestManager.AddOrChangePosition(Invoice.id, i++ , pos);


            }
        }


    }
    public class Product
    {
        public Product(Market VKProduct, Product_JSON TinkoffProduct)
        {
            this.VKProduct = VKProduct;
            this.TinkoffProduct = TinkoffProduct;
        }
        public Product() { }

        public Market VKProduct;
        public Product_JSON TinkoffProduct;
    }
}

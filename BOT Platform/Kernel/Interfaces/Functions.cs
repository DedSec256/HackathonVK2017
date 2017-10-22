using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using MyFunctions.Exceptions;
using VkNet.Model;
using VkNet.Model.Attachments;
using VkNet.Model.RequestParams;
using BOT_Platform.Kernel.Bots;
using BOT_Platform.Kernel.CIO;
using Newtonsoft.Json;

namespace BOT_Platform.Kernel.Interfaces
{
    public static class Functions
    {
        /// <summary>
        /// Удаляет пробелы из начала и конца строки
        /// </summary>
        /// <param name="str">Ссылка на строку, в которой необходимо убрать пробелы</param>
        public static void RemoveSpaces(ref string str)
        {
            //удаляем пробелы с начала
            while (!string.IsNullOrEmpty(str) && str[0] == ' ')
            {
                str = str.Remove(0, 1);
            }

            if (string.IsNullOrEmpty(str)) return;
            //удаляем пробелы с конца
            int j;
            for (j = str.Length - 1; str[j] == ' '; j--) { }
            if (j != str.Length - 1) str = str.Remove(j + 1);

        }

        /// <summary>
        /// Удаляет пробелы из начала и конца строки
        /// </summary>
        /// <param name="str">Строка, в которой необходимо убрать пробелы</param>
        /// <returns>Строка без пробелов в начале и конце</returns>
        public static string RemoveSpaces(string str)
        {
            //удаляем пробелы с начала
            while (!string.IsNullOrEmpty(str) && str[0] == ' ')
            {
                str = str.Remove(0, 1);
            }

            if (string.IsNullOrEmpty(str)) return str;
            //удаляем пробелы с конца
            int j;
            for (j = str.Length - 1; str[j] == ' '; j--) { }
            if (j != str.Length - 1) str = str.Remove(j + 1);

            return str;
        }
        /// <summary>
        /// Отправляет сообщение с вложениями в чат
        /// </summary>
        /// <param name="bot">Бот, который должен отправить сообщение</param>
        /// <param name="message">Сообщение, в котором находятся UserId и ChatId</param>
        /// <param name="m">Параметры сообщения, содержащие вложения</param>
        /// <param name="body">Текст сообщения</param>
        /// <param name="isChat">Если true и ChatId != null - бот отправляет сообщение в чат</param>
        /// <param name="needAttachments">(Необязательный параметр) Только для модуля SpeechTheText</param>
        public static void SendMessage(Bot bot, Message message, MessagesSendParams m, 
            string body, bool isChat = false, bool needAttachments = false)
        {

            m.Message = body;
            if (isChat == true)
            {
                m.ChatId = message.ChatId;
                if (message.Id != null) m.ForwardMessages = new long[1] { message.Id.Value };
                //m.Attachments = needAttachments ? GetAttachments(message) : null;
            }
            else m.UserId = message.UserId;

            if (bot.GetSettings().GetIsDebug() == false)
            {
                if (bot is GroupBot && BotMain.Bots.ContainsKey(BotMain.MainBot))
                {
                    try
                    {
                        bot.Convert<VkBot>().GetApi().Messages.Send(m);
                    }
                    catch
                    {
                        SendMessage(BotMain.Bots[BotMain.MainBot], message, m, body, isChat, needAttachments);
                    }
                }
                else bot.Convert<VkBot>().GetApi().Messages.Send(m);
            }
            else BotConsole.Write($"(ответ){bot.Name}: " +  m.Message, MessageType.Info);
        }
        /// <summary>
        /// Получить все вложения в сообщении
        /// </summary>
        /// <param name="m"></param>
        /// <returns>Список вложений</returns>
        private static List<MediaAttachment> GetAttachments(Message m)
        {
            List<MediaAttachment> mediaAttachments = new List<MediaAttachment>();
            foreach (var attach in m.Attachments)
            {
                MediaAttachment mAt;
                if (attach.Instance is Photo)
                {
                    mAt = (attach.Instance as Photo);
                    mediaAttachments.Add(mAt);
                }
                else if (attach.Instance is Audio)
                {
                    mAt = (attach.Instance as Audio);
                    mediaAttachments.Add(mAt);
                }
                else if (attach.Instance is Document)
                {
                    mAt = (attach.Instance as Document);
                    mediaAttachments.Add(mAt);
                }
                else if (attach.Instance is Sticker)
                {
                    mAt = (attach.Instance as Sticker);
                    mediaAttachments.Add(mAt);
                }
                else if (attach.Instance is Video)
                {
                    mAt = (attach.Instance as Video);
                    mediaAttachments.Add(mAt);
                }
                else if (attach.Instance is Link)
                {
                    mAt = (attach.Instance as Link);
                    mediaAttachments.Add(mAt);
                }
                else if (attach.Instance is Poll)
                {
                    mAt = (attach.Instance as Poll);
                    mediaAttachments.Add(mAt);
                }
                else if (attach.Instance is WallReply)
                {
                    mAt = (attach.Instance as WallReply);
                    mediaAttachments.Add(mAt);
                }

            }
            return mediaAttachments;
        }
        /// <summary>
        /// Отправляет сообщение в чат
        /// </summary>
        /// <param name="bot">Бот, который должен отправить сообщение</param>
        /// <param name="m">Сообщение, в котором находятся UserId и ChatId</param>
        /// <param name="message">Текст сообщения</param>
        /// <param name="isChat">Если true и ChatId != null - бот отправляет сообщение в чат</param>
        /// <param name="needAttachments">(Необязательный параметр) Только для модуля SpeechTheText</param>
        public static void SendMessage(Bot bot, Message m,
            string message, bool isChat = false, bool needAttachments = false)
        {

            MessagesSendParams msp = new MessagesSendParams()
            { 
                Message = message,
                Attachments = needAttachments ? GetAttachments(m) : null
            };

            if (isChat)
            {
                msp.ChatId = m.ChatId;
                if(m.Id != null) msp.ForwardMessages = new long[1] { m.Id.Value };
            }
            else msp.UserId = m.UserId;

            if (bot.GetSettings().GetIsDebug() == false)
            {
                if (bot is GroupBot && BotMain.Bots.ContainsKey(BotMain.MainBot))
                {
                    try
                    {
                        bot.Convert<VkBot>().GetApi().Messages.Send(msp);
                    }
                    catch
                    {
                        SendMessage(BotMain.Bots[BotMain.MainBot], m, message, isChat, needAttachments);
                    }
                }
                else bot.Convert<VkBot>().GetApi().Messages.Send(msp);
            }
            else BotConsole.Write($"(ответ){bot.Name}: " + msp.Message, MessageType.Info);
        }
        /// <summary>
        /// Содержится ли данное сообщение в списке
        /// </summary>
        /// <param name="containMes">Сообщение, которое нужно проверить</param>
        /// <param name="Messages">Список сообщений</param>
        /// <returns>true - если сообщение обнаружено в списке</returns>
        public static bool ContainsMessage(Message containMes, IEnumerable<Message> Messages)
        {
            bool contains = false;
            foreach (Message m in Messages)
            {
                if (m.Date == containMes.Date &&
                   m.UserId == containMes.UserId &&
                   m.Body == containMes.Body)
                {
                    contains = true;
                    break;
                }
            }
            return contains; 
        }
        /// <summary>
        /// Получить id пользователя по ссылке на его страницу 
        /// </summary>
        /// <param name="url">Ссылка на пользователя</param>
        /// <param name="bot">С какого бота проверить ссылку (работает только для UserBot)
        /// В противном случае поиск id выполнит бот по умолчанию
        /// </param>
        /// <returns>id пользователя</returns>
        public static string GetUserId(string url, Bot bot)
        {
            string GetUrlWithApi(string _url)
            {

                if (bot is GroupBot)
                {
                    if (BotMain.Bots.ContainsKey(BotMain.MainBot))
                        _url = BotMain.Bots[BotMain.MainBot].Convert<VkBot>().GetApi().Users.Get(_url).Id.ToString();
                    else
                        throw new WrongParamsException(
                            "В данный момент бот не может отправлять сообщения по короткой ссылке пользователя." +
                            "\nПожалуйста, укажите вместо ссылки численное значение без \"id\"");
                }

                else _url = bot.Convert<VkBot>().GetApi().Users.Get(_url).Id.ToString();
                return _url;
            }

            url = url.Replace(" ", "");
            int index = url.ToString().LastIndexOf('/');
            if (index != -1) url = url.ToString().Substring(index + 1);

            Regex reg = new Regex("[a-z|A-Z]");
            bool regexIsFoundInFull = reg.IsMatch(url);

            //Если нашёл "id"
            index = url.ToString().IndexOf("id");
            if (index == 0)
            {
                if (!reg.IsMatch(url.ToString().Substring(2)))
                {
                    url = url.ToString().Substring(2);
                }
                else
                {
                    url = GetUrlWithApi(url);
                }
            }

            else if (reg.IsMatch(url))
            {
                url = GetUrlWithApi(url);
            }
            return url;
        }
        /// <summary>
        /// Заменяет все служебные символы в url на их коды
        /// </summary>
        /// <param name="s">Ссылка на url </param>
        public static void CheckURL(ref string s)
        {
           s = s.Replace(" ", "%20")
                .Replace("&", "%26")
                .Replace("#", "%23")
                .Replace("\\", "%26%23092%3B")
                .Replace(",", "%2C")
                .Replace("/", "%2F");
        }
        /// <summary>
        /// Заменяет все служебные символы в url на их коды
        /// </summary>
        /// <param name="s">Ссылка на url </param>
        /// <returns>Строка с заменёнными служебными символами</returns>
        public static string CheckURL(string s)
        {
            return s.Replace(" ", "%20")
                 .Replace("&", "%26")
                 .Replace("#", "%23")
                 .Replace("\\", "%26%23092%3B")
                 .Replace(",", "%2C")
                 .Replace("/", "%2F");
        }
        /// <summary>
        /// Загрузить изображение во вложения сообщения
        /// </summary>
        /// <param name="photoName">полный путь к файлу изображения</param>
        /// <param name="bot">бот, который должен выполнить загрузку</param>
        /// <returns>Экземпляр загруженного изображения</returns>
        public static Photo UploadImageInMessage(string photoName, Bot bot)
        {
            var uploadServer = bot.Convert<VkBot>().GetApi().Photo.GetMessagesUploadServer();
            // Загрузить фотографию.
            var wc = new WebClient();
            var responseImg = Encoding.ASCII.GetString(wc.UploadFile(uploadServer.UploadUrl, photoName));
            // Сохранить загруженную фотографию
            return bot.Convert<VkBot>().GetApi().Photo.SaveMessagesPhoto(responseImg)[0];
        }
        /// <summary>
        /// Загрузить документ во вложения сообщения
        /// </summary>
        /// <param name="filename">Полный путь к файлу документа</param>
        /// <param name="docName">Название документа в ВК</param>
        /// <param name="bot">бот, который должен выполнить загрузку</param>
        /// <returns>Экземпляр документа</returns>
        public static Document UploadDocumentInMessage(string filename, string docName, Bot bot)
        {
            UploadServerInfo uploadServer;

            bot = bot is GroupBot && BotMain.Bots.ContainsKey(BotMain.MainBot) ?
                BotMain.Bots[BotMain.MainBot] : bot;

            if (bot is GroupBot) uploadServer = bot.Convert<VkBot>().GetApi().Docs.GetWallUploadServer();
            else uploadServer = bot.Convert<VkBot>().GetApi().Docs.GetUploadServer();
            // Загрузить документ.
            var wc = new WebClient();
            var responseDoc = Encoding.ASCII.GetString(wc.UploadFile(uploadServer.UploadUrl, filename));
            // Сохранить загруженный документ
            return bot.Convert<VkBot>().GetApi().Docs.Save(responseDoc, docName)[0];
        }

        public static Photo UploadPhotoToWall( string photoName, Bot bot)
        {
            var uploadServer = bot.Convert<VkBot>().GetApi().Photo.GetWallUploadServer();
            // Загрузить фотографию.
            var wc = new WebClient();
            var responseImg = Encoding.ASCII.GetString(wc.UploadFile(uploadServer.UploadUrl, photoName));
            // Сохранить загруженную фотографию
            return bot.Convert<VkBot>().GetApi().Photo.SaveWallPhoto(responseImg, 0000)[0];
        }

        public static T GetFromJSON<T>(string uri)
        {
            using (WebClient client = new WebClient())
            {
                string response;

                response = client.DownloadString(uri);
                try
                {
                    var data = JsonConvert.DeserializeObject<T>(response);
                    return data;
                }
                catch (Exception ex)
                {
                    BotConsole.Write(ex.Message, MessageType.Error);
                    return default(T);
                }
            }
        }

        public static long GetGroupId(string uri, Bot bot)
        {
            Functions.RemoveSpaces(ref uri);
            int index = uri.LastIndexOf("/");
            if (index != -1) uri = uri.Substring(index + 1);

            return -bot.Convert<VkBot>().GetApi().Groups.GetById(uri).Id;
        }
    }

    public static class MessageExtension
    {
        public static void CopyFrom(this Message mess, Message copyMessage)
        {
            mess.ChatId = copyMessage.ChatId;
            mess.UserId = copyMessage.UserId;
            mess.FromId = copyMessage.FromId;
            mess.Body = copyMessage.Body;
            mess.Id = copyMessage.Id;
            mess.OwnerId = copyMessage.OwnerId;
            mess.Attachments = copyMessage.Attachments;
        }

        public static string RemoveSpaces(this string text)
        {
            return Functions.RemoveSpaces(text).Replace("\n","");
        }
    }
}

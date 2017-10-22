using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;
using BOT_Platform.FileManager;
using BOT_Platform.Kernel.CIO;

namespace BOT_Platform
{
    class Banlist
    {
        public const string FILENAME = "Data\\System\\banlist.txt";
        private string fullPath;
        private string BotName;

        private SerializableDictionary<long, string> BanList;
        public Banlist(string botDirPath, string botName)
        {
            BotName = botName;
            fullPath = Path.Combine(botDirPath, FILENAME);
            
            Deserialize();
            if (BanList == null) BanList = new SerializableDictionary<long, string>();
        }

        void SaveBanList()
        {
            if (BanList is null)
            {
                BotConsole.Write($"[{BotName}][BANLIST_ERROR]:\n" +
                                  @"Ошибка при сохранении бан-листа. Список пуст.", MessageType.Error);
                return;
            }

            Serialize();
        }

        void Serialize()
        {
            FileStream fs = new FileStream(fullPath, FileMode.Create);

            XmlSerializer serializer = new XmlSerializer(typeof(SerializableDictionary<long, string>));
            try
            {
                serializer.Serialize(fs, BanList);
            }
            catch (SerializationException e)
            {
                BotConsole.Write($"[{BotName}][BANLIST_ERROR]:\n" +
                                  @"Ошибка при сериализации: " + e.Message, MessageType.Error);
            }
            finally
            {
                fs.Close();
            }
        }

        void Deserialize()
        {

            FileStream fs = new FileStream(fullPath, FileMode.Open);
            try
            {
                XmlSerializer deserializer = new XmlSerializer(typeof(SerializableDictionary<long, string>));

                BanList?.Clear();
                BanList = (SerializableDictionary<long, string>)deserializer.Deserialize(fs);
            }
            catch (SerializationException e)
            {
                BotConsole.Write($"[{BotName}][BANLIST_ERROR]:\n" +
                                  @"Ошибка при десериализации: " + e.Message, MessageType.Error);
            }
            finally
            {
                fs.Close();
            }

        }

        public void Add()
        {
            
        }

        public bool Contains(long id)
        {
            return BanList.ContainsKey(id);
        }
    }
}

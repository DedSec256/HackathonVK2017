using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using VkNet;
using VkNet.Enums.Filters;
using BOT_Platform.Kernel.CIO;
using BOT_Platform.Kernel.Interfaces;
using BOT_Platform.Kernel.Standart;
using VkNet.Model.RequestParams;

namespace BOT_Platform.Kernel.Settings
{
    public abstract class PlatformSettings
    {
        protected const char COMMENTS = '$';
        protected static string DATA_FILENAME;
        public static string PATH = @"Data\System\settings.ini";

        protected string SettingsProcessing(FileInfo data)
        {
            if (!data.Exists) throw new FileLoadException($"Отсутствует файл настроек {DATA_FILENAME}!", "Ошибка");
            StringBuilder dataLine = new StringBuilder();
            using (StreamReader reader = data.OpenText())
            {

                string tempText = reader.ReadToEnd();
                #region Обработка настроек
                bool findComments = false;
                for (int i = 0; i < tempText.Length; i++)
                {
                    if (tempText[i] == '\r' ||
                        tempText[i] == ' ' ||
                        tempText[i] == '\n' ||
                        tempText[i] == '\t') continue;

                    if (tempText[i] == COMMENTS)
                    {
                        findComments = !findComments;
                        continue;
                    }
                    if (findComments == true) continue;

                    dataLine.Append(tempText[i]);
                }
                #endregion

                if (String.IsNullOrEmpty(dataLine.ToString()) == true)
                {
                    throw new FileLoadException($"Файл настроек {DATA_FILENAME} пуст!", "Ошибка");
                }
            }
            return dataLine.ToString();
        }

        protected abstract void ReadSettings();
        public PlatformSettings(string filename)
        {
            DATA_FILENAME = filename;
            ReadSettings();
        }
        protected bool isDebug;
        public bool GetIsDebug()
        {
            return isDebug;
        }
        public void SetIsDebug(bool value)
        {
            isDebug = value;
        }
    }

    public abstract class VkSettings : PlatformSettings
    {
        protected List<string> allowedСommands;
        protected MessagesGetParams mesParams;
        protected ApiAuthParams apiParams;
        protected string[] botName;
        protected Int16 mesRemeberCount;
        protected Int16 delay;
        protected Dictionary<long, bool> adminList;
        protected ulong AppId;
   
        public VkSettings(string filename) : base(filename)
        {
        }
        protected void GetAllowedCommands(string data)
        {
            allowedСommands = new List<string>();

            string[] GetNeededCommands(string text)
            {
                string[] str = text.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                for (int i = 0; i < str.Length; i++)
                {
                    str[i] = Functions.RemoveSpaces(str[i]);
                }
                return str;
            }

            if (data.Contains("ALL")) allowedСommands = CommandsCenter.Commands;
            else if (data.Contains("EXCEPT"))
            {
                data = data.Replace("EXCEPT", "").Replace(":", "");
                string[] keys = GetNeededCommands(data);
                allowedСommands = CommandsCenter.Commands.Where(t => keys.Contains(t) == false).ToList();
            }
            else if (data.Contains("ONLY"))
            {
                data = data.Replace("ONLY", "").Replace(":", "");
                string[] keys = GetNeededCommands(data);

                for (int i = 0; i < keys.Length; i++)
                {
                    if (CommandsCenter.Commands.Contains(keys[i])) allowedСommands.Add(keys[i]);
                    else BotConsole.Write($"[WARNING] Команда \\{keys[i]} не содержится в списке команд платформы (файл {DATA_FILENAME})", MessageType.Warning);
                }
            }
            else throw new ArgumentException($"[ERROR] Ошибка в записи доступа к командам. Файл {DATA_FILENAME}");
        }

        public ApiAuthParams AuthParams
        {
            get => apiParams;
            set => apiParams = value;
        }
        public MessagesGetParams MesGetParams => this.mesParams;
        public Int16 Delay => this.delay;
        public String[] BotName => this.botName;
        public long[] Admins => this.adminList.Keys.ToArray();
        public Int16 MesRemembCount => this.mesRemeberCount;
        public ulong ApplicationId => AppId;
        public List<string> AllowedСommands { get => allowedСommands; }
    }

    public class VkUserSettings : VkSettings
    {
        public VkUserSettings(string filename) : base(filename)
        {
        }
        protected override void ReadSettings()
        {
            FileInfo data = new FileInfo(DATA_FILENAME);
            string dataLine = SettingsProcessing(data);

            string[] splitSettings = {
                "[LOGIN]","[PASSWORD]","[APP_ID]","[PAUSE]",
                "[MCOUNT]", "[MTOFF]", "[BNAME]","[BMESMEM]", "[ADMIN_LIST]", "[ALLOWED_COM]"
            };
            string[] appParams = dataLine.Split(splitSettings,
                StringSplitOptions.RemoveEmptyEntries);

            if (appParams.Length != splitSettings.Length) throw new Exception($"Ошибка при считывании настроек {DATA_FILENAME}.\n" +
                                                                              "Убедитесь, что они записаны верно.");
            Parallel.Invoke(
                () =>
                {
                    this.apiParams = new ApiAuthParams()
                    {
                        Login = appParams[0],
                        Password = appParams[1],
                        ApplicationId = Convert.ToUInt32(appParams[2]),
                        Settings = VkNet.Enums.Filters.Settings.All

                    };
                },
                () =>
                {
                    AppId = Convert.ToUInt32(appParams[2]);

                    this.mesParams = new MessagesGetParams()
                    {
                        Count = Convert.ToUInt16(appParams[3]),
                        Out = 0,
                        TimeOffset = Convert.ToUInt32(appParams[4]) //15
                    };


                    this.delay = Convert.ToInt16(appParams[5]);
                    this.mesRemeberCount = Convert.ToInt16(appParams[7]);
                },
                () =>
                {
                    this.botName = appParams[6].Split(',');
                    Parallel.For(0, botName.Length, i =>
                    {
                        botName[i] = Functions.RemoveSpaces(botName[i]);
                    });
                },
                () =>
                {
                    adminList = new Dictionary<long, bool>();
                    string[] admins = appParams[8].Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                    Parallel.For(0, admins.Length, i =>
                    {
                        adminList.Add(Convert.ToInt32(admins[i]), true);
                    });
                },
                () =>
                {
                    GetAllowedCommands(appParams[9]);
                    
                }
            );
            this.SetIsDebug(true);
        }
    }
    public class VkGroupSettings : VkSettings
    {
        public VkGroupSettings(string filename) : base(filename)
        { }
        public string Token { get; set; }
        protected override void ReadSettings()
        {
            FileInfo data = new FileInfo(DATA_FILENAME);
            string dataLine = SettingsProcessing(data);
                string[] splitSettings = {
                    "[TOKEN]", "[APP_ID]","[PAUSE]",
                    "[MCOUNT]", "[MTOFF]", "[BNAME]", "[BMESMEM]", "[ADMIN_LIST]", "[ALLOWED_COM]"
                };
                string[] appParams = dataLine.Split(splitSettings,
                    StringSplitOptions.RemoveEmptyEntries);

                if (appParams.Length != splitSettings.Length) throw new Exception($"Ошибка при считывании настроек {DATA_FILENAME}.\n" +
                                                                                  "Убедитесь, что они записаны верно.");

            Parallel.Invoke(
                () =>
                {
                    Token = appParams[0];
                    AppId = Convert.ToUInt32(appParams[1]);
                    this.delay = Convert.ToInt16(appParams[2]);

                    this.mesParams = new MessagesGetParams()
                    {
                        Count = Convert.ToUInt16(appParams[3]),
                        Out = 0,
                        TimeOffset = Convert.ToUInt32(appParams[4])
                    };
                },
                () =>
                {
                    this.botName = appParams[5].Split(',');
                    Parallel.For(0, botName.Length, i =>
                    {
                        botName[i] = Functions.RemoveSpaces(botName[i]);
                    });
                },
                () =>
                {
                    this.mesRemeberCount = Convert.ToInt16(appParams[6]);

                    apiParams = new ApiAuthParams()
                    {
                        Settings = VkNet.Enums.Filters.Settings.All
                    };
                },
                () =>
                {
                    adminList = new Dictionary<long, bool>();
                    string[] admins = appParams[7].Split(new char[] {','}, StringSplitOptions.RemoveEmptyEntries);
                    Parallel.For(0, admins.Length, i =>
                    {
                        adminList.Add(Convert.ToInt32(admins[i]), true);
                    });
                },
                () =>
                {
                    GetAllowedCommands(appParams[8]);
                }
            );
            this.SetIsDebug(true);
        }
    }


}

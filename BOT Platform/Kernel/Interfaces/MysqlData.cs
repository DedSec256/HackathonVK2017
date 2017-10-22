using System;
using System.Data;
using System.Reflection;
using System.Windows.Forms;
using MySql.Data.MySqlClient;

namespace BOT_Platform.Kernel.Interfaces
{
    public class MysqlData
    {
       private static MySqlConnectionStringBuilder mysqlCSB;
        // Тут будут храниться данные авторизации
        private const string TABLE_NAME = "Bot";

       private static void InitMysqlData()
       {
           mysqlCSB = new MySqlConnectionStringBuilder
           {
               Server = "db.radiushost.net",
               Database = "yan1506_789",
               UserID = "yan1506_123",
               Password = "Qwertyuiop[]123"
           };
           // IP сервера
           // название базы данных
           // логин
           // пароль
       }

        /// <summary>
        ///     Функция, выполняющая MySql-запросы
        /// </summary>
        /// <param name="request">
        ///     MySql-запрос в виде строки
        /// </param>
        /// <returns>
        ///     Возвращает таблицу с результатом запроса.
        /// </returns>
        private static DataTable MysqlCommand(string request)
        {
            using (MySqlConnection connection = new MySqlConnection())
            {
                connection.ConnectionString = MysqlData.mysqlCSB.ConnectionString;

                MySqlCommand command = new MySqlCommand(request, connection);

                try
                {
                    connection.Open();
                    using (MySqlDataReader dataReader = command.ExecuteReader())
                    {
                        if (dataReader.HasRows == true)
                        {
                            DataTable table = new DataTable();
                            table.Load(dataReader);
                            return table;
                        }
                    }
                }

                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message, "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Stop);
                    return null;
                }
            }
            return null;
        }

        public static (bool status, string decription) CheckIsNewestVersion()
        {
            InitMysqlData();
            DataTable table = MysqlCommand($"SELECT * FROM {TABLE_NAME};");
            if(table == null || table.Rows.Count < 1 || table.Rows[0].ItemArray.Length != 2) throw new Exception("Ошибка MySql. Проверка наличия обновления бот-платформы невозможна.");

            bool answer = table.Rows[table.Rows.Count - 1].ItemArray[0].ToString()
                              .Replace(" ", "") ==
                          Assembly.GetExecutingAssembly().GetName().Version.ToString()
                ? true
                : false;

            return (answer, (!answer ? table.Rows[table.Rows.Count - 1].ItemArray[table.Rows[0].ItemArray.Length - 1].ToString() : null));

        }
    }
}

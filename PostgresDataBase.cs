
namespace Bot
{
    internal class PostgresDataBase
    {
        private string URL { get; set; }
        public string GetURL { get { return URL; } }
        private bool IsConnected { get; set; }
        private bool Connected { get { return IsConnected; } }
        private Npgsql.NpgsqlConnection? Connection { get; set; }
        private Npgsql.NpgsqlCommand? Command { get; set; }
        /// <summary>
        /// Создание экземпляра класса для работы с СУБД
        /// </summary>
        /// <param name="connection">Созданное подключение к СУБД</param>
        /// <exception cref="ArgumentNullException"></exception>
        public PostgresDataBase(Npgsql.NpgsqlConnection? connection)
        {
            if (connection is null)
                throw new ArgumentNullException(nameof(connection));

            Connection = connection;
            Command = new Npgsql.NpgsqlCommand();
            Command.Connection = connection;
        }
        /// <summary>
        /// Попытка подключения
        /// </summary>
        public void TryConnect()
        {
            try
            {
                Connection.Open();

                IsConnected = true;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                IsConnected = false;
            }
        }
        /// <summary>
        /// Сохранение данных документа в БД
        /// </summary>
        /// <param name="DocURL"></param>
        public void SaveDocumentLink(string? DocURL)
        {
            Command.CommandText = $"INSERT INTO WorkerDocument (WORKER_ID,DOCUMENT_LINK) VALUES ({CheckWorkerQuantity()},'{DocURL}');";

            Command.ExecuteNonQuery();
        }
        private int CheckWorkerQuantity()
        {
            Command.CommandText = "";
            Command.CommandText = "SELECT * FROM Employee;";

            int quantity = 0;

            var reader = Command.ExecuteReader();

            if (reader.HasRows)
            {
                while (reader.Read())
                {
                    if (reader.IsOnRow)
                        ++quantity;
                }
            }

            reader.Close();

            return quantity;
        }
        /// <summary>
        /// Установка запроса к БД
        /// </summary>
        /// <param name="query">Строка запроса</param>
        /// <returns>Коллекция объектов, соответствующих строке запроса</returns>
        public List<object> SetQuery(string query)
        {

            Command.CommandText = query;

            List<object> values = new List<object>();

            int ordinal = 0;

            var reader = Command.ExecuteReader();

            if (reader.HasRows)
            {
                while (reader.Read())
                {
                    values.Add(reader.GetValue(ordinal));
                }

                reader.Close();

                return values;
            }

            reader.Close();

            return null;

        }
        /// <summary>
        /// Получение имени таблицы в БД из Телеграм кнопок
        /// </summary>
        /// <param name="type">Нажатая кнопка</param>
        /// <returns>Кнопка в виде строки</returns>
        public static string GetTableNameByCommand(CommandType type)
        {
            switch (type)
            {
                case CommandType.JOB:
                    return "Jobs";
                case CommandType.WORKS:
                    return "Works";
                case CommandType.DOCUMENTS:
                    return "WorkerDocument";
                default:
                    return "???";
            }
        }


    }
}

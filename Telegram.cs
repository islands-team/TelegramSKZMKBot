using Telegram.Bot.Args;
using Telegram.Bot.Types;
using Telegram.Bot;
using Telegram.Bot.Types.ReplyMarkups;
using Npgsql;
using System.Reflection.Metadata.Ecma335;
using System.Collections.Generic;
using Google.Apis.Drive.v2.Data;

namespace Bot
{
    enum CommandType
    {
        DOCUMENTS,
        START_BOT,
        JOB,
        WORKS,
        BACK,
        UNKNOWN_COMMAND
    }

    class TelegramBot : IDisposable
    {
        private readonly string BOT_TOKEN;
        private bool disposedValue;
        private List<object> ListOfJob { get; set; }
        private List<Telegram.Bot.Types.User> Users { get; set; }
        private IReplyMarkup MenuItems { get; set; }
        private static TelegramBotClient? BotClient { get; set; }
        public PostgresDataBase DataBase { get; set; }
        public Document Document { get; set; }
#pragma warning disable CS0618 
        /// <summary>
        /// Создание экземпляра Telegram Bot 
        /// </summary>
        /// <param name="token">Токен бота для подключения, который можно взять по ссылке: <see cref="https://t.me/BotFather"/></param>
        /// <param name="connection">Экземпляр подключенной СУБД к проекту.</param>
        public TelegramBot(string token, NpgsqlConnection connection)
        {
            BOT_TOKEN = token;
            ListOfJob = new List<object>();
            BotClient = new TelegramBotClient(BOT_TOKEN);
            DataBase = new PostgresDataBase(connection);
            Users = new List<Telegram.Bot.Types.User>();
            BotClient.OnMessage += OnMessageHandler;
            MenuItems = GetGeneralButtons();
        }
        private async void OnMessageHandler(object? sender, MessageEventArgs messageEvent)
        {
            if (messageEvent.Message == null || messageEvent.Message.Text == "")
                return;

            Console.WriteLine($"{messageEvent.Message.Chat.Username}: {messageEvent.Message.Text}");

            await BotSendMessage(messageEvent.Message.Text, messageEvent.Message.Chat.Id, messageEvent.Message.Chat.Username, messageEvent.Message.MessageId);

            Users.Add(new Telegram.Bot.Types.User { Username = messageEvent.Message.Chat.Username });

        }
        private async Task BotSendMessage(string text, long chatID,string username, int? messageID = null)
        {
            if (BotClient == null)
                return;


            if (!Users.Contains(new Telegram.Bot.Types.User { Username = username}))
            {
                var Identify = GetCommandFromString(text);
                if (Identify == CommandType.UNKNOWN_COMMAND || Identify == CommandType.START_BOT || Identify == CommandType.BACK || !text.Contains("Документы", StringComparison.CurrentCultureIgnoreCase) || !text.Contains("Работа", StringComparison.CurrentCultureIgnoreCase))
                    await BotClient.SendTextMessageAsync(chatID, $"[{new string('-', 40)}]", replyMarkup: MenuItems);
                if (ListOfJob.Contains(text.Trim()) && ListOfJob.Count > 0 && !text.Contains("Документы", StringComparison.CurrentCultureIgnoreCase) && !text.Contains("Работа", StringComparison.CurrentCultureIgnoreCase))
                    await BotClient.SendTextMessageAsync(chatID, $"[{new string('-', 40)}]", replyMarkup: MenuItems);
                if (text.Contains("Регистрация", StringComparison.CurrentCultureIgnoreCase)
                    && !text.Contains("Документы", StringComparison.CurrentCultureIgnoreCase) && !text.Contains("Работа", StringComparison.CurrentCultureIgnoreCase))
                {
                    await BotClient.SendTextMessageAsync(chatID, string.Join('\n', DocumentsTemplate()));
                }
                if (text.Contains("Резюме", StringComparison.CurrentCultureIgnoreCase) && !text.Contains("Документы", StringComparison.CurrentCultureIgnoreCase) && !text.Contains("Работа", StringComparison.CurrentCultureIgnoreCase))
                {
                    MenuItems = JobList().Result;
                    await BotClient.SendTextMessageAsync(chatID, "Вы успешно зарегистрировались.", replyMarkup: MenuItems);
                }
            }
            else
            {
                var Identify = GetCommandFromString(text);
                if (Identify == CommandType.UNKNOWN_COMMAND || Identify == CommandType.START_BOT || Identify == CommandType.BACK )
                    await BotClient.SendTextMessageAsync(chatID, $"[{new string('-', 40)}]", replyMarkup: GetGeneralButtons());
                //if (ListOfJob.Contains(text.Trim()) && ListOfJob.Count > 0)
                //    await BotClient.SendTextMessageAsync(chatID, $"[{new string('-', 40)}]", replyMarkup: GetGeneralButtons());
                if (text.Contains("Регистрация", StringComparison.CurrentCultureIgnoreCase))
                {
                    await BotClient.SendTextMessageAsync(chatID, string.Join('\n', DocumentsTemplate()));
                }
                if (text.Contains("Резюме", StringComparison.CurrentCultureIgnoreCase) )
                {
                    MenuItems = JobList().Result;
                    await BotClient.SendTextMessageAsync(chatID, "Вы успешно зарегистрировались.", replyMarkup: JobList().Result);
                }
            }
        }
        private async Task<IReplyMarkup> JobList()
        {
            try
            {
                string Table = "Jobs";

                ListOfJob = DataBase.SetQuery($"SELECT * FROM {Table};");

                List<List<KeyboardButton>> Keys = new List<List<KeyboardButton>>();

                foreach (var item in ListOfJob)
                {
                    Keys.Add(new List<KeyboardButton> { new KeyboardButton { Text = $"{item}" } });
                }

                return new ReplyKeyboardMarkup { Keyboard = Keys };
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return null;
            }

        }
        private string[] DocumentsTemplate()
        {
            return new string[] {"\n\tЗаполните резюме по следующему шаблону:\n\t\n\t[-Резюме-]\n\t1.ФИО\n2.Возраст\n3.Гражданство\n" +
                "4.Прежнее место работы\n5.О себе\n"};
        }
        private IReplyMarkup GetGeneralButtons()
        {
            return new ReplyKeyboardMarkup
            {
                Keyboard = new List<List<KeyboardButton>>
                {
                    new List<KeyboardButton> {
                        new KeyboardButton { Text = " - "}
                    },
                    new List<KeyboardButton> {
                        new KeyboardButton { Text = GetDataFromCommands(CommandType.JOB)}
                    }
                    ,new List<KeyboardButton> {
                        new KeyboardButton {Text = " - " }
                    },
                }
            };
        }
        /// <summary>
        /// Сигнал к началу принятия сообщений ботом
        /// </summary>
        public void ReceiveResponse()
        {
            try
            {

                BotClient.StartReceiving(cancellationToken: StopToken);
                Console.ReadLine();
                BotClient.StopReceiving();

            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }
        /// <summary>
        /// Остановка чтения сообщений.
        /// </summary>
        public async void Stop()
        {
            try
            {
                await BotClient.CloseAsync();
                this.Dispose();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }
        /// <summary>
        /// Получение информации о активном экземпляре бота.
        /// </summary>
        /// <returns>Класс User, представляющий аккаунт телеграм.</returns>
        public async Task<Telegram.Bot.Types.User> GetBotInfo()
        {
            return await BotClient.GetMeAsync();
        }
        private CommandType GetCommandFromString(string command)
        {
            switch (command.Trim().ToLower())
            {
                case "/start":
                    return CommandType.START_BOT;
                case "вакансии":
                    return CommandType.JOB;
                default:
                    return CommandType.UNKNOWN_COMMAND;
            }
        }
        private string GetDataFromCommands(CommandType command)
        {
            switch (command)
            {
                case CommandType.DOCUMENTS:
                    return "Документы";
                case CommandType.START_BOT:
                    return "Здравствуйте!";
                case CommandType.WORKS:
                    return "Текущие проекты";
                case CommandType.JOB:
                    return "Регистрация";
                case CommandType.BACK:
                    return "Назад";
                default:
                    return "Назад";
            }
        }
        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    //Освобождение управляемых ресурсов
                }
                disposedValue = true;
            }
        }
        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}


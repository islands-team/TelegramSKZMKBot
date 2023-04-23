

using Npgsql;

namespace Bot
{
    internal class Program
    {
        static async Task Main(string[] args)
        {

            NpgsqlConnection connection = new NpgsqlConnection($"" +
                $"Host=localhost;" +
                $"Port=5432;" +
                $"Username=postgres;" +
                $"Password=Ldthm12345;" +
                $"Database=Data;");

            try
            {
                connection.Open();


                Document document = new Document(CreateDocFile(), new Dictionary<object, object> { { "UserName", "Alex" }, { "UserAge", 44 } });

                if (document.TrySetActivate())
                {
                    document.ReplaceWithPlaceHolders();
                }

                var url = document.UploadDocument();

                using (TelegramBot bot = new TelegramBot("6218921935:AAFNgyjf02TuUdF2DYGtTB4YIFSr34e1iic", connection))
                {
                    var info = await bot.GetBotInfo();

                    Console.WriteLine($"Бот {info.Username} запущен...\nID: {info.Id}");

                    bot.DataBase = new PostgresDataBase(connection);

                    bot.DataBase.SetQuery($"INSERT INTO Employee (full_name,company_name) VALUES ('{"Alex"}','{"zz"}');");

                    bot.DataBase.SaveDocumentLink(url);

                    while (true)
                    {
                        bot.ReceiveResponse();
                    }

                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        static string CreateDocFile()
        {
            string path = @"C:\Users\denbe\Desktop\TemplateDoc\";

            int FileQuantity = Directory.GetFiles(path).Length;

            string copiedFile = @$"C:\Users\denbe\Desktop\TemplateDoc\SpecifiedFile{FileQuantity}.docx";
            File.Copy(@"C:\Users\denbe\Desktop\TemplateDoc\Template.docx", copiedFile);

            return copiedFile;
        }
    }
}

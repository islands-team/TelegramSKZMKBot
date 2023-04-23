using Google.Apis.Auth.OAuth2;
using Google.Apis.Drive.v2.Data;
using Google.Apis.Drive.v2;
using Google.Apis.Util.Store;
using Microsoft.Office.Interop.Word;


namespace Bot
{
    interface IDcoument
    {
        /// <summary>
        /// Попытка подключения к docx файлу
        /// </summary>
        /// <param name="doc">Экземпляр класса Document для попытки подключения (не обязательно, если создан экземпляр класса, реализующего интерфейс IDcoument)</param>
        /// <returns></returns>
        public bool TrySetActivate(Microsoft.Office.Interop.Word.Document? doc = null);
        /// <summary>
        /// Подстановка данных вместо служебных мест
        /// </summary>
        /// <param name="values">Заменяемые значения (необязательно, если переданы в конструктор класса)</param>
        public void ReplaceWithPlaceHolders(Dictionary<object, object>? values = null);
    }

    internal class Document : IDcoument
    {
        private Microsoft.Office.Interop.Word.Document WordDoc { get; set; }
        private Microsoft.Office.Interop.Word.Application Application { get; set; }
        private Microsoft.Office.Interop.Word.Bookmarks Bookmarks { get; set; }
        /// <summary>
        /// Путь до текущего документа
        /// </summary>
        public string DocumentPath { get; }
        private Dictionary<object,object>? PlaceHolders { get; set; }
        /// <summary>
        /// Создание экземпляра класса Document
        /// </summary>
        /// <param name="path"></param>
        /// <param name="placeholders"></param>
        /// <exception cref="ArgumentNullException"></exception>
        public Document(string? path, Dictionary<object, object>? placeholders)
        {
            if (path is null || placeholders is null)
                throw new ArgumentNullException(nameof(path));

            Application = new Microsoft.Office.Interop.Word.Application();
            PlaceHolders = placeholders;
            WordDoc = Application.Documents.Open(path);
            DocumentPath = WordDoc.Path;
        }
        public void ReplaceWithPlaceHolders(Dictionary<object, object>? values = null)
        {
            if (values is not null)
                PlaceHolders = values;

            foreach(var Item in PlaceHolders)
            {
                Bookmarks[Item.Key].Range.Text = $"{Item.Value}";
            }

            WordDoc.Save();

        }
        public bool TrySetActivate(Microsoft.Office.Interop.Word.Document? doc = null)
        {
            if (doc is not null)
                WordDoc = doc;

            try
            {
                WordDoc.Activate();
                Bookmarks = WordDoc.Bookmarks;
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return false;
            }
        }
        /// <summary>
        /// Загрузка документа на Google Drive
        /// </summary>
        /// <returns>URL ссылка на файл для дальнейшего использования</returns>
        public string? UploadDocument()
        {
            var credential = GoogleWebAuthorizationBroker.AuthorizeAsync(new ClientSecrets
            {
                ClientId = "242091063942-7u142uuvs64ujjj00jb2t2v9e9f0n3f5.apps.googleusercontent.com",
                ClientSecret = "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"
            }, new string[] { DriveService.Scope.Drive, DriveService.Scope.DriveFile }, "statzenko890@gmail.com"
            , CancellationToken.None,
            new FileDataStore("Desc")).Result;

            DriveService service = new DriveService(new Google.Apis.Services.BaseClientService.Initializer { HttpClientInitializer = credential });


            var driveFile = new Google.Apis.Drive.v2.Data.File();
            driveFile.OriginalFilename = WordDoc.Name;
            driveFile.Description = WordDoc.Email is null ? "None E-mail": WordDoc.Email.ToString();
            driveFile.MimeType = "application/msword";
            driveFile.Parents = new List<ParentReference> { new ParentReference { Id = "1itfHvTgcEjorbIr0IxU-YrKNQRVzWfie" } };


            var request = service.Files.Insert(driveFile);
            request.Fields = "id";

            var response = request.Execute();


            if (response is null)
                return null;
            response.DownloadUrl = $"https://drive.google.com/uc?export=download&id={response.Id}";


            return response.DownloadUrl;
        }
        /// <summary>
        /// Закрытие документа
        /// </summary>
        public void Stop()
        {
            WordDoc.Close();
        }
    }
}

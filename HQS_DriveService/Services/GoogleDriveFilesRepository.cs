using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Download;
using Google.Apis.Drive.v3;
using Google.Apis.Services;
using Google.Apis.Util.Store;
using System.IO;
using System.Threading;
using HQS_DriveService.Models;
using System.Threading.Tasks;
using System.Security.Cryptography.X509Certificates;

namespace HQS_DriveService.Services
{
    public class GoogleDriveFilesRepository
    {
        //defined scope.
        public static string[] Scopes = { DriveService.Scope.Drive };

        //create Drive API service.
        //public static DriveService GetService()
        //{
        //    //get Credentials from client_secret.json file 
        //    UserCredential credential;
        //    var credentialFilePath = Path.Combine(HttpContext.Current.Server.MapPath("~/"), "client_secret.json");
        //    using (var stream = new FileStream(credentialFilePath, FileMode.Open, FileAccess.Read))
        //    {
        //        var filePath = Path.Combine(HttpContext.Current.Server.MapPath("~/"), "DriveServiceCredentials.json");

        //        credential = GoogleWebAuthorizationBroker.AuthorizeAsync(
        //            GoogleClientSecrets.Load(stream).Secrets,
        //            Scopes,
        //            "user",
        //            CancellationToken.None,
        //            new FileDataStore(filePath, true)).Result;
        //    }

        //    //create Drive API service.
        //    DriveService service = new DriveService(new BaseClientService.Initializer()
        //    {
        //        HttpClientInitializer = credential,
        //        ApplicationName = "GoogleDriveRestAPI-v3",
        //    });
        //    return service;
        //}



        public static DriveService GetService()
        {
            //get Credentials from client_secret.json file 
            // UserCredential credential;
            

            var credentialFilePath = Environment.GetEnvironmentVariable("CREDENTIAL_FILE_PATH");

            var certificate = new X509Certificate2(credentialFilePath, "notasecret", X509KeyStorageFlags.Exportable);

            string serviceAccountEmail = Environment.GetEnvironmentVariable("SERVICE_ACCOUNT_EMAIL");

            try
            {
                var credential = new ServiceAccountCredential(
                    new ServiceAccountCredential.Initializer(serviceAccountEmail)
                    {
                        Scopes = Scopes
                    }.FromCertificate(certificate));


                //create Drive API service.
                var service = new DriveService(new BaseClientService.Initializer()
                {
                    HttpClientInitializer = credential,
                    ApplicationName = "GoogleDriveRestAPI-v3",
                });
                return service;
            }
            catch (Exception)
            {
                return null;
            }

            //using (var stream = new FileStream(credentialFilePath, FileMode.Open, FileAccess.Read))
            //{
            //    var filePath = Path.Combine(HttpContext.Current.Server.MapPath("~/"), "DriveServiceCredentials.json");

            //    credential = GoogleWebAuthorizationBroker.AuthorizeAsync(
            //        GoogleClientSecrets.Load(stream).Secrets,
            //        Scopes,
            //        "user",
            //        CancellationToken.None,
            //        new FileDataStore(filePath, true)).Result;
            //}

            ////create Drive API service.
            //DriveService service = new DriveService(new BaseClientService.Initializer()
            //{
            //    HttpClientInitializer = credential,
            //    ApplicationName = "GoogleDriveRestAPI-v3",
           // });
           
        }

        //file Upload to the Google Drive.
        public static async Task<object> FileUploadAsync(HttpPostedFile file)
        {
            if (file != null && file.ContentLength > 0)
            {
                DriveService service = GetService();
                var subPath = "~/GoogleDriveFiles";
                bool exists = System.IO.Directory.Exists(HttpContext.Current.Server.MapPath(subPath));

                if (!exists)
                    System.IO.Directory.CreateDirectory(HttpContext.Current.Server.MapPath(subPath));


                string path = Path.Combine(HttpContext.Current.Server.MapPath(subPath),
                Path.GetFileName(file.FileName));
                file.SaveAs(path);

                var FileMetaData = new Google.Apis.Drive.v3.Data.File();
                FileMetaData.Name = Path.GetFileName(file.FileName);
                FileMetaData.MimeType = MimeMapping.GetMimeMapping(path);

                FilesResource.CreateMediaUpload request;
                Google.Apis.Drive.v3.Data.File output;
                using (var stream = new System.IO.FileStream(path, System.IO.FileMode.Open))
                {
                    request = service.Files.Create(FileMetaData, stream, FileMetaData.MimeType);
                    request.Fields = "id";
                    await request.UploadAsync();
                    output = request.ResponseBody;
                    
                }
                File.Delete(path);
                return output.Id;
            }
            return null;
        }

        //Download file from Google Drive by fileId.
        public static string DownloadGoogleFile(string fileId)
        {
            DriveService service = GetService();

            string FolderPath = System.Web.HttpContext.Current.Server.MapPath("/GoogleDriveFiles/");
            FilesResource.GetRequest request = service.Files.Get(fileId);

            string FileName = request.Execute().Name;
            string FilePath = System.IO.Path.Combine(FolderPath, FileName);

            MemoryStream stream1 = new MemoryStream();

            // Add a handler which will be notified on progress changes.
            // It will notify on each chunk download and when the
            // download is completed or failed.
            request.MediaDownloader.ProgressChanged += (Google.Apis.Download.IDownloadProgress progress) =>
            {
                switch (progress.Status)
                {
                    case DownloadStatus.Downloading:
                        {
                            Console.WriteLine(progress.BytesDownloaded);
                            break;
                        }
                    case DownloadStatus.Completed:
                        {
                            Console.WriteLine("Download complete.");
                            SaveStream(stream1, FilePath);
                            break;
                        }
                    case DownloadStatus.Failed:
                        {
                            Console.WriteLine("Download failed.");
                            break;
                        }
                }
            };
            request.Download(stream1);
            return FilePath;
        }

        // file save to server path
        private static void SaveStream(MemoryStream stream, string FilePath)
        {
            using (System.IO.FileStream file = new FileStream(FilePath, FileMode.Create, FileAccess.ReadWrite))
            {
                stream.WriteTo(file);
            }
        }

        //Delete file from the Google drive
        public static void DeleteFile(GoogleDriveFiles files)
        {
            DriveService service = GetService();
            try
            {
                // Initial validation.
                if (service == null)
                    throw new ArgumentNullException("service");

                if (files == null)
                    throw new ArgumentNullException(files.Id);

                // Make the request.
                service.Files.Delete(files.Id).Execute();
            }
            catch (Exception ex)
            {
                throw new Exception("Request Files.Delete failed.", ex);
            }
        }

        // create sharing folder
        public static string CreateFolderWithSharingPermission(string FolderName/*, string EmailAddress, string UserRole*/)
        {
            try
            {
                //Create Folder in Google Drive
                Google.Apis.Drive.v3.DriveService service = GetService();
                var fileMetadata = new Google.Apis.Drive.v3.Data.File()
                {
                    Name = FolderName,
                    MimeType = "application/vnd.google-apps.folder",
                    Description = "File_" + System.DateTime.Now.Hour + ":" + System.DateTime.Now.Minute + ":" + System.DateTime.Now.Second
                };

                Google.Apis.Drive.v3.FilesResource.CreateRequest request = service.Files.Create(fileMetadata);
                request.Fields = "id";
                string Fileid = request.Execute().Id;

                Google.Apis.Drive.v3.Data.Permission permission = new Google.Apis.Drive.v3.Data.Permission();

                permission.Type = "anyone"; // sharing permission
                //permission.EmailAddress = EmailAddress;
                permission.Role = "writer"; //all type of permission but not allow for delete file &folders

                permission = service.Permissions.Create(permission, Fileid).Execute();

                
                //if (UserRole == "writer" || UserRole == "reader")
                //{
                //    permission.Type = "anyone";
                //    permission.EmailAddress = EmailAddress;
                //    permission.Role = UserRole;

                //    permission = service.Permissions.Create(permission, Fileid).Execute();
                //}
                //else if (UserRole == "owner")
                //{
                //    permission.Type = "user";
                //    permission.EmailAddress = EmailAddress;
                //    permission.Role = UserRole;

                //    Google.Apis.Drive.v3.PermissionsResource.CreateRequest createRequestPermission = service.Permissions.Create(permission, Fileid);
                //    createRequestPermission.TransferOwnership = true;
                //    createRequestPermission.Execute();
                //}
                if (permission != null)
                {
                    return Fileid;
                }
                else return "error";
            }
            catch (Exception e)
            {
                return "An error occurred: " + e.Message;
            }
        }

        // upload file in folder
        public static async Task<string> FileUploadInFolder(string folderId, HttpPostedFile file)
        {
            try
            {
                if (file != null && file.ContentLength > 0)
                {
                    Google.Apis.Drive.v3.DriveService service = GetService();
                    var subPath = "~/GoogleDriveFiles";
                    bool exists = System.IO.Directory.Exists(HttpContext.Current.Server.MapPath(subPath));

                    if (!exists)
                        System.IO.Directory.CreateDirectory(HttpContext.Current.Server.MapPath(subPath));


                    string path = Path.Combine(HttpContext.Current.Server.MapPath(subPath),
                    Path.GetFileName(file.FileName));
                    file.SaveAs(path);

                    var FileMetaData = new Google.Apis.Drive.v3.Data.File()
                    {
                        Name = Path.GetFileName(file.FileName),
                        MimeType = MimeMapping.GetMimeMapping(path),
                        Parents = new List<string>
                    {
                        folderId
                    }
                    };
                    Google.Apis.Drive.v3.FilesResource.CreateMediaUpload request;
                    using (var stream = new System.IO.FileStream(path, System.IO.FileMode.Open))
                    {
                        request = service.Files.Create(FileMetaData, stream, FileMetaData.MimeType);
                        request.Fields = "id";
                        await request.UploadAsync();
                    }
                    var file1 = request.ResponseBody;
                    File.Delete(path); 
                    return file1.Id;
                }
                return null;
            }
            catch (Exception)
            {
                return null;
            }
            
        }



        // share file
        public static bool FileSharingPermission(string fileId/*, string EmailAddress, string UserRole*/)
        {
            try
            {
                var service = GetService();
                Google.Apis.Drive.v3.Data.Permission permission = new Google.Apis.Drive.v3.Data.Permission
                {
                    Type = "anyone", // sharing permission
                    //permission.EmailAddress = EmailAddress;
                    Role = "writer" //all type of permission but not allow for delete file &folders
                };
                permission = service.Permissions.Create(permission, fileId).Execute();

                if (permission != null)
                {
                    return true;
                }
                return false;
            }
            catch (Exception e)
            {
                return false;
            }
        }
    }
}
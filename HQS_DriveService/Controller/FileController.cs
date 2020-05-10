using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Net;
using System.Web.Http;
using HQS_DriveService.Services;
using System.Net.Http;
using System.Web.Hosting;
using System.Threading.Tasks;

namespace HQS_DriveService.Controller
{
    [RoutePrefix("api/driveService")]
    public class FileController : ApiController
    {

        /// <summary>
        /// upload file
        /// </summary>
        /// <returns></returns>
        [HttpPost]
        [Route("Upload")]
        public async Task<IHttpActionResult> UploadFileAsync()
        {
            try
            {
                var httpRequest = HttpContext.Current.Request;
                if (httpRequest.Files.Count > 0)
                {
                    var fileName = httpRequest.Files.Keys[0];
                    //foreach (string fileName in httpRequest.Files.Keys)
                    //{
                        var file = httpRequest.Files[fileName];
                        var result = await GoogleDriveFilesRepository.FileUploadAsync(file);
                    if (result != null)
                    {
                        var x = GoogleDriveFilesRepository.FileSharingPermission(result.ToString());
                        if (x) return Ok("https://drive.google.com/open?id=" + result);
                        return null;
                    }
                    // }

                    return Ok(result);
                }
                return InternalServerError();
            }
            catch (Exception ex)
            {
                return InternalServerError();
            }
            
        }

        /// <summary>
        /// Create folder - sharing permissions
        /// </summary>
        /// <param name="FolderName"></param>
        /// <returns></returns>
        [HttpGet]
        [Route("Folder")]
        public string CreateFolderWithPermission(string FolderName/*, string EmailAddress, string PermissionType, string UserRole*/)
        {
            string Result = GoogleDriveFilesRepository.CreateFolderWithSharingPermission(FolderName/*, EmailAddress, UserRole*/);
            return Result;
        }

        /// <summary>
        /// upload file in folder - sharing permission
        /// </summary>
        /// <returns></returns>
        [HttpPost]
        [Route("Upload/{folderId}")]
        public async Task<IHttpActionResult> UploadFileInFolder_SharingPermissonAsync(string folderId)
        {
            try
            {
                var httpRequest = HttpContext.Current.Request;
                if (httpRequest.Files.Count > 0)
                {
                    var fileName = httpRequest.Files.Keys[0];
                    //foreach (string fileName in httpRequest.Files.Keys)
                    //{
                    var file = httpRequest.Files[fileName];
                    var result = await GoogleDriveFilesRepository.FileUploadInFolder(folderId, file);
                    if(result != null)
                    {
                        var x = GoogleDriveFilesRepository.FileSharingPermission(result);
                        if (x) return Ok("https://drive.google.com/open?id=" + result);
                        return null;
                    }
                    // }

                    return Ok(result);
                }
                return InternalServerError();
            }
            catch (Exception ex)
            {
                return InternalServerError();
            }

        }
        
    }
}
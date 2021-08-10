using ImageResizeWebApp.Helpers;
using ImageResizeWebApp.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Internal;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace ImageResizeWebApp.Controllers
{
    [Route("api/[controller]")]
    public class ImagesController : Controller
    {
        // make sure that appsettings.json is filled with the necessary details of the azure storage
        private readonly AzureStorageConfig storageConfig = null;

        public ImagesController(IOptions<AzureStorageConfig> config)
        {
            storageConfig = config.Value;
        }

        // POST /api/images/upload
        [HttpPost("[action]")]
        public async Task<IActionResult> Upload(IFormCollection data)
        {
            bool isUploaded = false;


            try
            {
                // Create a local file in the ./data/ directory for uploading and downloading
                string localPath = "./";
                string fileName = data.Files[0].FileName + Guid.NewGuid().ToString() + ".txt";
                string localFilePath = Path.Combine(localPath, fileName);
                Microsoft.Extensions.Primitives.StringValues value = "";
                

                // Write text to the file
                if (data.TryGetValue("data", out value))
                await System.IO.File.WriteAllTextAsync(localFilePath, value);

                using (Stream stream = System.IO.File.OpenRead(localFilePath))
                {
                    isUploaded = await StorageHelper.UploadFileToStorage(stream, fileName, storageConfig);
                }

                using (Stream stream = data.Files[0].OpenReadStream())
                {
                    isUploaded = await StorageHelper.UploadFileToStorage(stream, data.Files[0].FileName, storageConfig);
                }

                if (isUploaded)
                {
                    if (storageConfig.ThumbnailContainer != string.Empty)
                        return new AcceptedAtActionResult("GetThumbNails", "Images", null, null);
                    else
                        return new AcceptedResult();
                }
                else
                    return BadRequest("Look like the image couldnt upload to the storage");
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        // GET /api/images/thumbnails
        [HttpGet("thumbnails")]
        public async Task<IActionResult> GetThumbNails()
        {
            try
            {
                if (storageConfig.AccountKey == string.Empty || storageConfig.AccountName == string.Empty)
                    return BadRequest("Sorry, can't retrieve your Azure storage details from appsettings.js, make sure that you add Azure storage details there.");

                if (storageConfig.ImageContainer == string.Empty)
                    return BadRequest("Please provide a name for your image container in Azure blob storage.");

                List<string> thumbnailUrls = await StorageHelper.GetThumbNailUrls(storageConfig);
                return new ObjectResult(thumbnailUrls);            
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
    }
}
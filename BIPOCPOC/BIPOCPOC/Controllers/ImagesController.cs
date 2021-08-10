using BIPOCPOC.Helpers;
using BIPOCPOC.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Internal;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace BIPOCPOC.Controllers
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
                
                Microsoft.Extensions.Primitives.StringValues name = "";
                Microsoft.Extensions.Primitives.StringValues email = "";
                data.TryGetValue("name", out name);
                data.TryGetValue("email", out email);

                string value = name + Environment.NewLine + email;

                string localPath = "./";
                string fileName = name + "-" + data.Files[0].FileName;
                string localFilePath = Path.Combine(localPath, fileName);

                // Write text to the file
                await System.IO.File.WriteAllTextAsync(localFilePath, value);

                using (Stream stream = System.IO.File.OpenRead(localFilePath))
                {
                    isUploaded = await StorageHelper.UploadFileToStorage(stream, Path.GetFileNameWithoutExtension(fileName) + ".txt", storageConfig);
                }

                using (Stream stream = data.Files[0].OpenReadStream())
                {
                    isUploaded = await StorageHelper.UploadFileToStorage(stream, fileName , storageConfig);
                }

                System.IO.File.Delete(localFilePath);

                if (isUploaded)
                {
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
    }
}
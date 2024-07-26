using System;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using MetadataExtractor;
using MetadataExtractor.Formats.Exif;
using MetadataExtractor.Formats.Jpeg;

namespace YourNamespace.Controllers
{
    [Route("api/ImageMetadata")]
    public class ImageMetadataController : Controller
    {
        // POST: /ImageMetadata/GetMetadata
        [HttpPost("GetMetadata")]
        public async Task<IActionResult> GetMetadata(IFormFile imageFile)
        {
            try
            {
                // Check if imageFile is provided
                if (imageFile == null || imageFile.Length == 0)
                {
                    return BadRequest("Image file is required.");
                }

                using (var memoryStream = new MemoryStream())
                {
                    await imageFile.CopyToAsync(memoryStream);
                    byte[] imageData = memoryStream.ToArray();

                    try
                    {
                        var directories = ImageMetadataReader.ReadMetadata(new MemoryStream(imageData));
                        var allMetadata = new Dictionary<string, Dictionary<string, string>>();

                        foreach (var directory in directories)
                        {
                            var directoryMetadata = new Dictionary<string, string>();
                            foreach (var tag in directory.Tags)
                            {
                                directoryMetadata[tag.Name] = tag.Description;
                            }
                            allMetadata[directory.Name] = directoryMetadata;
                        }

                        return Json(allMetadata);
                    }
                    catch (ImageProcessingException ex)
                    {
                        return BadRequest($"Error processing image: {ex.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                return BadRequest($"Unexpected error: {ex.Message}");
            }
        }

     
    }
}

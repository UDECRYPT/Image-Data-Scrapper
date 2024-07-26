using DocumentFormat.OpenXml.Packaging;
using MetadataExtractor;
using Microsoft.AspNetCore.Mvc;
using OfficeOpenXml;
using PdfSharp.Pdf.IO;

namespace data.Controllers
{
    public class FilesMetaDataExtractor : Controller
    {

        public IActionResult Index()
        {
            return View();
        }
        public async Task<IActionResult> GetMetadata(IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                return BadRequest("File is required.");
            }

            object metadata;
            var imageExtensions = new List<string>() { ".jpg", ".jpeg", ".png", ".svg", ".gif"};
            try
            {
                using (var memoryStream = new MemoryStream())
                {
                    await file.CopyToAsync(memoryStream);
                    byte[] fileData = memoryStream.ToArray();

                    var fileExtension = Path.GetExtension(file.FileName).ToLowerInvariant();

                    if (fileExtension == ".pdf")
                    {
                        metadata = GetPdfMetadata(fileData, file.FileName);
                    }
                    else if (fileExtension == ".docx")
                    {
                        metadata = GetDocsMetadata(fileData, file.FileName);
                    }
                    else if(imageExtensions.Contains(fileExtension.ToLower()))
                    {
                        metadata = GetImageMetaData(fileData, file.FileName);
                    }
                    else
                    {
                        return BadRequest("Unsupported file type.");
                    }

                    return Json(metadata);
                }
            }
            catch (Exception ex)
            {
                return BadRequest($"Unexpected error: {ex.Message}");
            }
        }

        public Dictionary<string, string> GetPdfMetadata(byte[] fileData, string fileName)
        {
            var metadata = new Dictionary<string, string>();
            long fileSizeBytes = fileData.Length;

            try
            {
                using (var pdfDocument = PdfReader.Open(new MemoryStream(fileData), PdfDocumentOpenMode.InformationOnly))
                {
                    metadata["File Name"] = fileName;
                    metadata["Title"] = pdfDocument.Info.Title;
                    metadata["Author"] = pdfDocument.Info.Author;
                    metadata["Subject"] = pdfDocument.Info.Subject;
                    metadata["Keywords"] = pdfDocument.Info.Keywords;
                    metadata["Creation Date"] = pdfDocument.Info.CreationDate.ToString();
                    metadata["Modified Date"] = pdfDocument.Info.ModificationDate.ToString();
                    metadata["Creator"] = pdfDocument.Info.Creator;
                    metadata["Producer"] = pdfDocument.Info.Producer;
                    metadata["Page Count"] = pdfDocument.PageCount.ToString();
                    metadata["Is Encrypted"] = pdfDocument.SecuritySettings.IsEncrypted.ToString();
                    metadata["Version"] = pdfDocument.Version.ToString();
                    metadata["File Size Bytes"] = fileSizeBytes.ToString();
                    metadata["File Size KB"] = (fileSizeBytes / 1024.0).ToString();
                    metadata["File Size MB"] = (fileSizeBytes / (1024.0 * 1024.0)).ToString();
                    metadata["File Path"] = Path.GetFullPath(fileName);
                    metadata["File Extension"] = Path.GetExtension(fileName);
                    metadata["File Directory"] = Path.GetDirectoryName(fileName);
                    metadata["PDF Version"] = pdfDocument.Version.ToString();
                    metadata["Language"] = pdfDocument.Language;
                    metadata["Title Length"] = pdfDocument.Info.Title?.Length.ToString();
                    metadata["Author Length"] = pdfDocument.Info.Author?.Length.ToString();
                    metadata["Subject Length"] = pdfDocument.Info.Subject?.Length.ToString();
                    metadata["Keywords Length"] = pdfDocument.Info.Keywords?.Length.ToString();
                    metadata["Creator Length"] = pdfDocument.Info.Creator?.Length.ToString();
                    metadata["Producer Length"] = pdfDocument.Info.Producer?.Length.ToString();
                    metadata["Outlines"] = pdfDocument.Outlines.Count.ToString();
                    metadata["Page Layout"] = pdfDocument.PageLayout.ToString();
                    metadata["Page Mode"] = pdfDocument.PageMode.ToString();
                    metadata["Color Mode"] = pdfDocument.Options.ColorMode.ToString();
                    var pageWidths = new List<double>();
                    var pageHeights = new List<double>();
                    foreach (var page in pdfDocument.Pages)
                    {
                        pageWidths.Add(page.Width);
                        pageHeights.Add(page.Height);
                    }
                    metadata["Average Page Width"] = pageWidths.Average().ToString();
                    metadata["Average Page Height"] = pageHeights.Average().ToString();
                }
            }
            catch (Exception ex)
            {
                metadata["Error"] = $"Error reading PDF metadata: {ex.Message}";
            }

            return metadata;
        }
        private Dictionary<string, string> GetDocsMetadata(byte[] fileData, string fileName)
        {
            var metadata = new Dictionary<string, string>();
            long fileSizeBytes = fileData.Length;
            try
            {
                using (var memoryStream = new MemoryStream(fileData))
                using (var wordDocument = WordprocessingDocument.Open(memoryStream, false))
                {
                    var packageProps = wordDocument.PackageProperties;
                    metadata["File Name"] = fileName;
                    metadata["Title"] = packageProps.Title;
                    metadata["Subject"] = packageProps.Subject;
                    metadata["Creator"] = packageProps.Creator;
                    metadata["Keywords"] = packageProps.Keywords;
                    metadata["Description"] = packageProps.Description;
                    metadata["LastModifiedBy"] = packageProps.LastModifiedBy;
                    metadata["Revision"] = packageProps.Revision;
                    metadata["Created"] = packageProps.Created.ToString();
                    metadata["Modified"] = packageProps.Modified.ToString();
                    metadata["Category"] = packageProps.Category;
                    metadata["ContentStatus"] = packageProps.ContentStatus;
                    metadata["ContentType"] = packageProps.ContentType;
                    metadata["Identifier"] = packageProps.Identifier;
                    metadata["Language"] = packageProps.Language;
                    metadata["Version"] = packageProps.Version;
                    metadata["File Size Bytes"] = fileSizeBytes.ToString();
                    metadata["File Size KB"] = (fileSizeBytes / 1024.0).ToString();
                    metadata["File Size MB"] = (fileSizeBytes / (1024.0 * 1024.0)).ToString();
                    metadata["File Path"] = Path.GetFullPath(fileName);
                    metadata["File Extension"] = Path.GetExtension(fileName);
                    metadata["File Directory"] = Path.GetDirectoryName(fileName);
                    metadata["Title Length"] = packageProps.Title?.Length.ToString();
                    metadata["Subject Length"] = packageProps.Subject?.Length.ToString();
                    metadata["Keywords Length"] = packageProps.Keywords?.Length.ToString();
                    metadata["Creator Length"] = packageProps.Creator?.Length.ToString();
                    var extendedProps = wordDocument.ExtendedFilePropertiesPart.Properties;
                    metadata["Company"] = extendedProps.Company?.Text;
                    metadata["Page Count"] = extendedProps.Pages?.Text;
                    metadata["Words"] = extendedProps.Words?.Text;
                    metadata["Characters"] = extendedProps.Characters?.Text;


                }
            }
            catch (Exception ex)
            {
                metadata["Error"] = $"Error reading Word document metadata: {ex.Message}";
            }

            return metadata;
        }

        public Dictionary<string, string> GetImageMetaData(byte[] imageData, string fileName)
        {
            try
            {
                var allMetadata = new Dictionary<string,  string>();
                using (var memoryStream = new MemoryStream())
                {
                    try
                    {
                        var directories = ImageMetadataReader.ReadMetadata(new MemoryStream(imageData));
                       

                        foreach (var directory in directories)
                        {
                            var directoryMetadata = new Dictionary<string, string>();
                            foreach (var tag in directory.Tags)
                            {
                                allMetadata[tag.Name] = tag.Description;
                            }
                        }

                        return allMetadata;
                    }
                    catch (ImageProcessingException ex)
                    {
                        throw new Exception($"Error processing image: {ex.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Unexpected error: {ex.Message}");
            }
        }
    }
}

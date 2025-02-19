
namespace pptx2doc.Controllers;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading.Tasks;

[ApiController]
[Route("api/files")]
public class Pptx2Controller : ControllerBase
{
   
 private readonly IWebHostEnvironment _env;

    public Pptx2Controller(IWebHostEnvironment env)
    {
        _env = env;
    }

    [HttpPost("upload")]
    public async Task<IActionResult> UploadFile(IFormFile file)
    {
        if (file == null || file.Length == 0)
        {
            return BadRequest(new { error = "No file uploaded" });
        }

        try
        {
            //  Récupérer le répertoire de destination basé sur le nom du fichier
            string fileBaseName = Path.GetFileNameWithoutExtension(file.FileName);
            string uploadDir = Path.Combine(_env.ContentRootPath, "input", fileBaseName);

            //  Créer le dossier si nécessaire
            if (!Directory.Exists(uploadDir))
            {
                Directory.CreateDirectory(uploadDir);
            }

            // Définir le chemin du fichier
            string filePath = Path.Combine(uploadDir, file.FileName);

            //  Enregistrer le fichier
            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            return Ok(new
            {
                message = "File received",
                fileDetails = new
                {
                    originalName = file.FileName,
                    storedPath = filePath
                }
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message });
        }
    }
    
[HttpGet("images/{fileName}")]
public IActionResult GetImages(string fileName)
{
    string baseName = Path.GetFileNameWithoutExtension(fileName);
    string zipFilePath = Path.Combine(_env.ContentRootPath, "output", $"{baseName}.zip");

    if (!System.IO.File.Exists(zipFilePath))
    {
        return NotFound(new { error = "ZIP file not found" });
    }

    // Create a temporary directory to extract images
    string tempDir = Path.Combine(_env.ContentRootPath, "temp", baseName);
    if (!Directory.Exists(tempDir))
    {
        Directory.CreateDirectory(tempDir);
    }

    try
    {
        // Extract images from the ZIP file
        ZipFile.ExtractToDirectory(zipFilePath, tempDir);

        // Get image files (PNG or JPEG) from the extracted directory
        var imageFiles = Directory.GetFiles(tempDir)
                                   .Where(f => f.EndsWith(".png") || f.EndsWith(".jpeg"))
                                   .ToList();

        if (imageFiles.Count == 0)
        {
            return NotFound(new { error = "No images found in the ZIP file" });
        }

        // Return the image files as a JSON response
        var imageResponses = imageFiles.Select(img => new
        {
            fileName = Path.GetFileName(img),
            filePath = Url.Content($"~/temp/{baseName}/{Path.GetFileName(img)}") // URL for accessing the image
        }).ToList();

        return Ok(imageResponses);
    }
    catch (Exception ex)
    {
        return StatusCode(500, new { error = "Error extracting images", details = ex.Message });
    }
    finally
    {
        // Clean up the temporary directory
        if (Directory.Exists(tempDir))
        {
            Directory.Delete(tempDir, true);
        }
    }
}



   [HttpPost("convert")]
    public async Task<IActionResult> ConvertFile([FromBody] ConvertRequest request)
    {
        if (string.IsNullOrEmpty(request.FileName))
        {
            return BadRequest(new { error = "fileName is required" });
        }

        string baseName = Path.GetFileNameWithoutExtension(request.FileName);
       
        string inputFilePath =  Path.Combine(_env.ContentRootPath, $"input/{baseName}/{request.FileName}");
        string outputDir = Path.Combine(_env.ContentRootPath, $"output/{baseName}");

        try
        {
            Directory.CreateDirectory(outputDir);

            // Convertir en PDF avec LibreOffice
            string libreOfficeCommand = $"libreoffice --headless --convert-to pdf --outdir {outputDir} {inputFilePath}";
            await RunCommand(libreOfficeCommand);

            string outputFile = Path.Combine(outputDir, $"{baseName}.pdf");

            if (!System.IO.File.Exists(outputFile))
            {
                return StatusCode(500, new { error = "PDF conversion failed" });
            }

            // Convertir PDF en images
            string convertCommand = $"pdftoppm {outputFile} {outputDir}/{baseName} -{request.FileFormat}";
            await RunCommand(convertCommand);

            var images = Directory.GetFiles(outputDir)
                                  .Where(f => f.EndsWith(".png") || f.EndsWith(".jpeg"))
                                  .ToList();

            if (images.Count == 0)
            {
                return StatusCode(500, new { error = "Image conversion failed" });
            }

            //  Créer un ZIP des images
            string zipFilePath = Path.Combine(outputDir, $"{baseName}.zip");
            using (var zip = ZipFile.Open(zipFilePath, ZipArchiveMode.Create))
            {
                foreach (var image in images)
                {
                    zip.CreateEntryFromFile(image, Path.GetFileName(image));
                }
            }

            return PhysicalFile(zipFilePath, "application/zip", $"{baseName}.zip");
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = "Conversion failed", details = ex.Message });
        }
    }

    private async Task RunCommand(string command)
    {
        using (var process = new Process())
        {
            process.StartInfo = new ProcessStartInfo
            {
                FileName = "/bin/bash",
                Arguments = $"-c \"{command}\"",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            process.Start();
            await process.WaitForExitAsync();

            if (process.ExitCode != 0)
            {
                throw new Exception($"Command failed: {await process.StandardError.ReadToEndAsync()}");
            }
        }
    }

    public class ConvertRequest
    {
        public string FileName { get; set; }="";
        public string FileFormat { get; set; } = "png"; // Format par défaut 
}



}
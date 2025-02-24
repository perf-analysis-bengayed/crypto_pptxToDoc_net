
namespace pptx2doc.Controllers;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading.Tasks;
using System.Drawing; 
using System.Drawing.Imaging; 

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
    

[HttpPost("converttest")]
public async Task<IActionResult> ConvertFiletest(IFormFile file, [FromForm] string fileFormat = "png")
{
    if (file == null || file.Length == 0)
    {
        return BadRequest(new { error = "A file is required" });
    }

    // Valider le format d'image (seulement "png" ou "jpeg" autorisés)
    string format = fileFormat.ToLower();
    if (format != "png" && format != "jpeg")
    {
        return BadRequest(new { error = "Invalid image format. Only 'png' or 'jpeg' are allowed." });
    }

    string baseName = Path.GetFileNameWithoutExtension(file.FileName);
    string tempDir = Path.Combine(_env.ContentRootPath, "temp", baseName);
    string inputFilePath = Path.Combine(tempDir, file.FileName);
    string outputDir = Path.Combine(tempDir, "output");

    try
    {
        Directory.CreateDirectory(outputDir);

        // Enregistrer le fichier téléchargé
        using (var stream = new FileStream(inputFilePath, FileMode.Create))
        {
            await file.CopyToAsync(stream);
        }

        string outputFile = inputFilePath;

        // Si le fichier n'est pas un PDF, le convertir en PDF avec LibreOffice
        if (Path.GetExtension(file.FileName).ToLower() != ".pdf")
        {
            string pdfOutputPath = Path.Combine(outputDir, $"{baseName}.pdf");
            string libreOfficeCommand = $"libreoffice --headless --convert-to pdf --outdir {outputDir} {inputFilePath}";
            await RunCommand(libreOfficeCommand);

            outputFile = pdfOutputPath;

            if (!System.IO.File.Exists(outputFile))
            {
                return StatusCode(500, new { error = "PDF conversion failed" });
            }
        }

        // Convertir le PDF en images en utilisant pdftoppm
        string convertCommand = $"pdftoppm -{format} {outputFile} {Path.Combine(outputDir, baseName)}";
        await RunCommand(convertCommand);

        // Déterminer l'extension des fichiers images générés
        string imageExtension = (format == "jpeg") ? ".jpg" : $".{format}";
        var images = Directory.GetFiles(outputDir)
                              .Where(f => f.EndsWith(imageExtension, StringComparison.OrdinalIgnoreCase))
                              .ToList();

        if (images.Count == 0)
        {
            return StatusCode(500, new { error = "Image conversion failed" });
        }

        // Créer un ZIP des images
        string zipFilePath = Path.Combine(outputDir, $"{baseName}.zip");
        using (var zip = ZipFile.Open(zipFilePath, ZipArchiveMode.Create))
        {
            foreach (var image in images)
            {
                zip.CreateEntryFromFile(image, Path.GetFileName(image));
            }
        }

        // Lire le contenu du fichier ZIP
        byte[] zipBytes = await System.IO.File.ReadAllBytesAsync(zipFilePath);

        // Démarrer une tâche en arrière-plan pour supprimer les fichiers temporaires après 2 minutes
        _ = Task.Run(async () =>
        {
            await Task.Delay(TimeSpan.FromMinutes(2));
            if (Directory.Exists(tempDir))
            {
                Directory.Delete(tempDir, true);
            }
        });

        // Retourner le fichier ZIP
        return File(zipBytes, "application/zip", $"{baseName}.zip");
    }
    catch (Exception ex)
    {
        return StatusCode(500, new { error = "Conversion failed", details = ex.Message });
    }
}




[HttpPost("convert")]
public async Task<IActionResult> ConvertFile(IFormFile file, [FromForm] string fileFormat = "png")
{
    if (file == null || file.Length == 0)
    {
        return BadRequest(new { error = "A file is required" });
    }

    string baseName = Path.GetFileNameWithoutExtension(file.FileName);
    string tempDir = Path.Combine(_env.ContentRootPath, "temp", baseName);
    string inputFilePath = Path.Combine(tempDir, file.FileName);
    string outputDir = Path.Combine(tempDir, "output");

    try
    {
        Directory.CreateDirectory(outputDir);

        // Enregistrer le fichier téléchargé
        using (var stream = new FileStream(inputFilePath, FileMode.Create))
        {
            await file.CopyToAsync(stream);
        }

        string outputFile = inputFilePath;

        // Si le fichier n'est pas un PDF, le convertir en PDF
        if (Path.GetExtension(file.FileName).ToLower() != ".pdf")
        {
            string pdfOutputPath = Path.Combine(outputDir, $"{baseName}.pdf");
            string libreOfficeCommand = $"libreoffice --headless --convert-to pdf --outdir {outputDir} {inputFilePath}";
            await RunCommand(libreOfficeCommand);

            outputFile = pdfOutputPath;

            if (!System.IO.File.Exists(outputFile))
            {
                return StatusCode(500, new { error = "PDF conversion failed" });
            }
        }

        // Convertir le PDF en images
        string convertCommand = $"pdftoppm -{fileFormat} {outputFile} {Path.Combine(outputDir, baseName)}";
        await RunCommand(convertCommand);

        var images = Directory.GetFiles(outputDir)
                              .Where(f => f.EndsWith($".{fileFormat}"))
                              .ToList();

        if (images.Count == 0)
        {
            return StatusCode(500, new { error = "Image conversion failed" });
        }

        // Créer un ZIP des images
        string zipFilePath = Path.Combine(outputDir, $"{baseName}.zip");
        using (var zip = ZipFile.Open(zipFilePath, ZipArchiveMode.Create))
        {
            foreach (var image in images)
            {
                zip.CreateEntryFromFile(image, Path.GetFileName(image));
            }
        }

        // Lire le contenu du fichier ZIP
        byte[] zipBytes = await System.IO.File.ReadAllBytesAsync(zipFilePath);

         // Démarrer une tâche en arrière-plan pour supprimer les fichiers temporaires après 2 minutes
        _ = Task.Run(async () =>
        {
            await Task.Delay(TimeSpan.FromMinutes(2));
            if (Directory.Exists(tempDir))
            {
                Directory.Delete(tempDir, true);
            }
            if (System.IO.File.Exists(inputFilePath))
            {
                System.IO.File.Delete(inputFilePath);
            }
        });

        // Retourner le fichier ZIP
        return File(zipBytes, "application/zip", $"{baseName}.zip");
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
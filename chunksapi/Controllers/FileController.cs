using Microsoft.AspNetCore.Mvc;

namespace chunksapi.Controllers
{
  public class Response
  {
    public bool IsError { get; set; } = false;
    public string? ErrorMessage { get; set; }
  }
  [ApiController]
  [Route("file")]
  public class FileController : ControllerBase
  {
    private readonly ILogger<FileController> _logger;
    public int chunkSize;
    private string uploadFolder;
    public FileController(IConfiguration configuration, ILogger<FileController> logger)
    {
      _logger = logger;
      chunkSize = 40000;
      uploadFolder = "c:/tmp";
    }
    [HttpPost("UploadChunks")]
    public async Task<IActionResult> UploadChunks(string id, string fileName)
    {
      try
      {
        string tempPath = Path.Combine(uploadFolder, "temp");
        var chunkNumber = id;
        string newpath = Path.Combine(tempPath, fileName + chunkNumber);
        using (FileStream fs = System.IO.File.Create(newpath))
        {
          byte[] bytes = new byte[chunkSize];
          int bytesRead = 0;
          while ((bytesRead = await Request.Body.ReadAsync(bytes, 0, bytes.Length)) > 0)
          {
            fs.Write(bytes, 0, bytesRead);
          }
        }
      }
      catch (Exception ex)
      {
        return BadRequest(ex.Message);
      }
      return Ok();
    }
    [HttpPost("UploadDone")]
    public IActionResult UploadDone(string fileName)
    {
      try
      {
        string tempPath = Path.Combine(uploadFolder, "temp");
        string newPath = Path.Combine(tempPath, fileName);
        var filePaths = Directory.GetFiles(tempPath)
                                      .Where(p => p.Contains(fileName))
                                      .OrderBy(p => Int32.Parse(p.Replace(fileName, "|").Split('|')[1]))
                                      .ToList();
        Merge(newPath, filePaths);
        System.IO.File.Move(newPath, Path.Combine(uploadFolder, fileName));
      }
      catch (Exception ex)
      {
        return BadRequest(ex.Message);
      }
      return Ok();
    }
    private static void Merge(string newPath, List<string> filePaths)
    {
      FileStream? concatenateFile = null;
      try
      {
        FileStream? toConcatenateFile = null;
        concatenateFile = System.IO.File.Open(newPath, FileMode.Append);
        filePaths.ForEach(fp =>
          {
            toConcatenateFile = System.IO.File.Open(fp, FileMode.Open);
            byte[] toConcatenateContent = new byte[toConcatenateFile.Length];
            toConcatenateFile.Read(toConcatenateContent, 0, (int)toConcatenateFile.Length);
            concatenateFile.Write(toConcatenateContent, 0, (int)toConcatenateFile.Length);
            if (toConcatenateFile != null) toConcatenateFile.Close();
            System.IO.File.Delete(fp);
          });
      }
      catch (Exception ex)
      {
        Console.WriteLine(ex.Message + " : " + ex.StackTrace);
      }
      finally
      {
        if (concatenateFile != null) concatenateFile.Close();

      }
    }
  }
}
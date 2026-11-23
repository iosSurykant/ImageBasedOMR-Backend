using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.IdentityModel.Tokens.Jwt;
using System.IO;
using DocumentFormat.OpenXml.Drawing.Diagrams;
using DocumentFormat.OpenXml.Office2010.Excel;
using DocumentFormat.OpenXml.Wordprocessing;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Org.BouncyCastle.Ocsp;
using Sentry;
using SharpCompress.Common;
using SQCScanner.Modal;
using SQCScanner.Modal.CustomTemplate;
using Version1.Data;
using static System.Runtime.InteropServices.JavaScript.JSType;
using static SQCScanner.Modal.CustomTemplate.TempDesignClass;
using DocumentFormat.OpenXml;
using File = System.IO.File;
using System.Security.Claims;
using System.Linq;
using Syncfusion.EJ2.Linq;



namespace SQCScanner.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class CustomImg_TempController : ControllerBase
    {
        private readonly ILogger _logger;
        private readonly ApplicationDbContext _dbContext;
        private readonly IWebHostEnvironment _env;
        private readonly string _root = Environment.CurrentDirectory;

        public CustomImg_TempController(ILogger<CustomImg_TempController> logger, ApplicationDbContext dbContext, IWebHostEnvironment env)
        {   
            _env = env;
            _logger = logger;
            _dbContext = dbContext;
        }

        // Static Template
        [HttpPost("CreateImgTemplate")]
        public async Task<IActionResult> CreateTemplate([FromForm] StaticImgClass model)
        {
            try
            {
                _logger.LogInformation(
                    "CreateTemplate started. TemplateId: {TemplateId}, TemplateName: {TemplateName}",
                    model.TemplateId,
                    model.TemplateName);

                var tempExist = _dbContext.ImgTemplate.FirstOrDefault(x => x.Id == int.Parse(model.TemplateId));
                if (model.TemplateImg == null || model.TemplateImg.Length == 0)
                {
                    _logger.LogWarning(
                        "CreateTemplate failed because no image was provided. TemplateId: {TemplateId}",
                        model.TemplateId);

                    return BadRequest("Template image is required.");
                }

                if(tempExist == null)
                {
                    _logger.LogWarning("CreateTemplate failed because no image was provided. TemplateId: {TemplateId}", model.TemplateId);
                    return BadRequest("Template not found.");
                }
                Console.WriteLine(tempExist.Id);
              

                using (var memoryStream = new MemoryStream())
                {
                    await model.TemplateImg.CopyToAsync(memoryStream);

                    var imageBytes = memoryStream.ToArray();
                    var base64String = Convert.ToBase64String(imageBytes);

                    var templateEntity = new StaticImgClass
                    {
                        TemplateId = $"{model.TemplateId}",
                        TemplateName = model.TemplateName,
                        TemplateImgBase64 = base64String,
                        discription = model.discription,
                    };
                    _dbContext.StaticImgClass.Add(templateEntity);
                    await _dbContext.SaveChangesAsync();

                    _logger.LogInformation(
                        "Template created successfully. TemplateId: {TemplateId}, TemplateName: {TemplateName}",
                        templateEntity.TemplateId,
                        templateEntity.TemplateName);
                }

                return Ok("Template created successfully.");
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error occurred while creating template. TemplateId: {TemplateId}, TemplateName: {TemplateName}",
                    model?.TemplateId,
                    model?.TemplateName);

                return StatusCode(500, "An error occurred while creating the template.");
            }
        }

        [HttpPut("UpdateImgTemplate")]
        public async Task<IActionResult> UpdateImgTemplate([FromForm] StaticImgClass model)
        {
            try
            {
                _logger.LogInformation("UpdateImgTemplate started. TemplateId: {TemplateId}, TemplateName: {TemplateName}", model.TemplateId,  model.TemplateName);

                // Find existing template
                var templateEntity = await _dbContext.StaticImgClass.FirstOrDefaultAsync(t => t.Id == model.Id);
                if (templateEntity == null)
                {
                    _logger.LogWarning(
                        "UpdateImgTemplate failed. TemplateId: {TemplateId} not found.",
                        model.TemplateId);

                    return NotFound("Record not found.");
                }
                // 1. Update Template Name
                if (!string.IsNullOrWhiteSpace(model.TemplateName))
                {
                    templateEntity.TemplateName = model.TemplateName;
                }
                // 2. TemplateId change
                if (!string.IsNullOrWhiteSpace(model.TemplateId))
                {
                    templateEntity.TemplateId = $"{model.TemplateId}";
                }
                // 3. Discription chamge
                if (!string.IsNullOrWhiteSpace(model.discription))
                {
                    templateEntity.discription = model.discription;
                }

                // 4. Update Image if new image is provided
                if (model.TemplateImg != null && model.TemplateImg.Length > 0)
                {
                    using var memoryStream = new MemoryStream();
                    await model.TemplateImg.CopyToAsync(memoryStream);
                    var imageBytes = memoryStream.ToArray();
                    templateEntity.TemplateImgBase64 = Convert.ToBase64String(imageBytes);
                }

                await _dbContext.SaveChangesAsync();

                _logger.LogInformation(
                    "Template updated successfully. TemplateId: {TemplateId}",
                    model.TemplateId);

                return Ok(new
                {
                    success = true,
                    message = "Template updated successfully."
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error occurred while updating template. TemplateId: {TemplateId}, TemplateName: {TemplateName}",
                    model?.TemplateId,
                    model?.TemplateName);

                return StatusCode(
                    StatusCodes.Status500InternalServerError,
                    "An error occurred while updating the template.");
            }
        }

        [HttpGet("ViewImgTempale")]
        public IActionResult GetAllTemplates([FromQuery] int? tempId, [FromQuery] int pageNo = 0, [FromQuery] int record = 0)
        {
            try
            {
                object templates;
                if (tempId == null)
                {
                    templates = _dbContext.StaticImgClass.OrderByDescending(a=>a.Id).Skip((pageNo - 1) * record).Take(record).ToList();
                }
                else
                {
                    templates = _dbContext.StaticImgClass.FirstOrDefault(a=>a.Id==tempId);
                }

                return Ok(templates);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while retrieving templates.");
                return StatusCode(500, "An error occurred while retrieving templates.");
            }
        }

        [HttpDelete("DeleteImgTempale")]
        public async Task<IActionResult> DeleteImageTemplate([FromQuery] string templateIdVal)
        {
            int templateId = int.Parse(templateIdVal);
            try
            {
                if (templateId == null || templateId == 0)
                {
                   return BadRequest("TemplateId is required and must be a valid integer.");
                }

                var template = await _dbContext.StaticImgClass.FirstOrDefaultAsync(t => t.Id == templateId);

                if (template == null)
                {
                    _logger.LogWarning(
                        "DeleteImageTemplate failed. TemplateId: {TemplateId} not found.",
                        templateId);

                    return NotFound("Template not found.");
                }

                _dbContext.StaticImgClass.Remove(template);

                await _dbContext.SaveChangesAsync();

                _logger.LogInformation("Template deleted successfully. TemplateId: {TemplateId}", templateId);

                return Ok(new
                {
                    success = true,
                    message = "Template deleted successfully."
                });
            }
            catch (OperationCanceledException)
            {
                _logger.LogWarning(
                    "DeleteImageTemplate request was cancelled. TemplateId: {TemplateId}",
                    templateId);

                return StatusCode(499, "Request cancelled.");
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error occurred while deleting template. TemplateId: {TemplateId}",
                    templateId);

                return StatusCode(
                    StatusCodes.Status500InternalServerError,
                    "An error occurred while deleting the template.");
            }
        }

        //dynamic template design
        [HttpPost("createimgtempDes")]
        public async Task<IActionResult> createimgtempdesing([FromForm] TempDesignModel model)
        {
            using var transaction = await _dbContext.Database.BeginTransactionAsync();
            try
            {
                _logger.LogInformation("createtemplate started. templateid: {templateid}, templatename: {templatename}", model.TemplateName, model.Discription, model.TemplateJSON, model.TemplateImg);

                var gettoken = Request.Headers["authorization"].FirstOrDefault()?.Replace("Bearer ", "").Trim();
                if (string.IsNullOrEmpty(gettoken))
                {
                    return Unauthorized(new { message = "no token provided" });
                }
                var handler = new JwtSecurityTokenHandler();
                var tokendecription = handler.ReadJwtToken(gettoken);
                var empid = tokendecription.Claims.FirstOrDefault(c => c.Type == "nameid")?.Value;

                // Save Temp image into Directory.
                if (!string.IsNullOrEmpty(model.TemplateImgFile?.Name))
                {
                    string ImguploadsPath = Path.Combine(_env.WebRootPath, "ImageManager");
                    string? getImgFileName = Path.GetFileName(model.TemplateImgFile?.FileName);
                    string imgFilePath = Path.Combine(ImguploadsPath, getImgFileName);
                    using (var fs = new FileStream(imgFilePath, FileMode.Create))
                    {
                        await model.TemplateImgFile.CopyToAsync(fs);
                    }
                    _logger.LogInformation("Image Created Successfully", getImgFileName);
                }

                // Save json into Directory
                if (!string.IsNullOrEmpty(model.TemplateJsonFile?.FileName))
                {
                    string tempfolder = Path.Combine(_env.WebRootPath, "TempManager");
                    string jsonfilename = Path.GetFileName(model.TemplateJsonFile.FileName);
                    string jsonfilepath = Path.Combine(tempfolder, jsonfilename);
                    using (var tempset = new FileStream(jsonfilepath, FileMode.Create))
                    {
                        await model.TemplateJsonFile.CopyToAsync(tempset);
                    }
                    _logger.LogInformation("JSON Template has been create", jsonfilename);
                }

                // Save Logo into Directory
                string directoryPath = Path.Combine(_root, "wFileManager", empid, "TempalteDesign");
                if (!string.IsNullOrEmpty(model.TemplateLogoFile?.Name))
                {
                    if (!Directory.Exists(directoryPath))
                    {
                        Directory.CreateDirectory(directoryPath);
                    }
                    using (var tempImg = new FileStream(Path.Combine(directoryPath, model.TemplateLogoFile.FileName), FileMode.Create))
                    {
                        await model.TemplateLogoFile.CopyToAsync(tempImg);
                    }
                }

                // Save Barcode into Directory
                if (!string.IsNullOrEmpty(model.TemplateBarcodeFile?.Name))
                {
                    if (!Directory.Exists(directoryPath))
                    {
                        Directory.CreateDirectory(directoryPath);
                    }
                    using (var tempImg = new FileStream(Path.Combine(directoryPath, model.TemplateBarcodeFile.FileName), FileMode.Create))
                    {
                        await model.TemplateBarcodeFile.CopyToAsync(tempImg);
                    }
                }

                DateTime date = DateTime.Now;
                var imgTemp = _dbContext.Add(new ImgTemp
                {
                    FileName = model.TemplateName,
                    imgPath = Path.Combine("ImageManager", model.TemplateImgFile?.FileName),
                    JsonPath = Path.Combine("TempManager", model.TemplateJsonFile?.FileName),
                    CreateAt = date.ToString(),
                    discription = model.Discription,
                    EmpiId = empid
                });
                await _dbContext.SaveChangesAsync();
                int getLastInsert = imgTemp.Entity.Id;

                var templateena = new TempDesignClass
                {
                    TemplateLogo = model.TemplateLogoFile != null ? Path.Combine("wFileManager", empid, "TempalteDesign", model.TemplateLogoFile.FileName) : "",
                    TemplateBarcode = model.TemplateBarcodeFile != null ? Path.Combine("wFileManager", empid, "TempalteDesign", model.TemplateBarcodeFile.FileName): "",
                    TemplateId = getLastInsert,
                    EmpId = empid
                };
                _dbContext.Add(templateena);
                await _dbContext.SaveChangesAsync();
                await transaction.CommitAsync();
                _logger.LogInformation("Designed Template has been created successfully");
                return Ok(new
                {
                    success = true,
                    message = "Desgin Template Create successfully."
                });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "");
                return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while deleting the template.");
            }
        }

        [HttpGet]
        [Route("getimgtempDes")] 
        public async Task<IActionResult> createimgtempDes(int? FindTempId = 0, [FromQuery] int pageNo = 0, [FromQuery] int record = 0)
        {
            try
            {
                var getToken = Request.Headers["Authorization"].FirstOrDefault()?.Replace("Bearer ", "").Trim();
                var tokenHandler = new JwtSecurityTokenHandler();
                var jwtToken = tokenHandler.ReadJwtToken(getToken);
                var userId = jwtToken.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier || c.Type == "nameid")?.Value;
                var userName = jwtToken.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier || c.Type == "unique_name")?.Value;
                string UserEmail = jwtToken.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier || c.Type == "email")?.Value;
                string UserRole = jwtToken.Claims.FirstOrDefault(a => a.Type == ClaimTypes.NameIdentifier || a.Type == "role")?.Value;
                string refranceId = jwtToken.Claims.FirstOrDefault(a => a.Type == ClaimTypes.NameIdentifier || a.Type == "refranceId")?.Value;
                List<CustomImage> result;
                List<CustomImage> result2;

                if (UserRole == "superadmin")
                {
                    _logger.LogInformation("StartGet design list records, SuperAdmin");
                    result = await _dbContext.ImgTemplate.Join(
                    _dbContext.staticImgDesigns, 
                    tempId => tempId.Id, 
                    TempDesign => TempDesign.TemplateId, 
                    (tempId, TempDesign) => 
                    new CustomImage { 
                        TemplateId = tempId.Id, 
                        TemplateName = tempId.FileName, 
                        ImgPath = tempId.imgPath, 
                        jsonPath = tempId.JsonPath, 
                        Create = tempId.CreateAt, 
                        discriotion = tempId.discription,
                        EmpId = tempId.EmpiId,
                        TemplateDesignBar = TempDesign.TemplateLogo, 
                        TemplateDesignImg = TempDesign.TemplateBarcode,
                    }).ToListAsync();
                    result2 = result.Join(_dbContext.empModels, a => a.EmpId, b => b.EmpId, (a, b) => new CustomImage
                    {
                        TemplateId = a.TemplateId,
                        TemplateName = a.TemplateName,
                        ImgPath = a.ImgPath,
                        jsonPath = a.jsonPath,
                        Create = a.Create,
                        discriotion = a.discriotion,
                        TemplateDesignBar = a.TemplateDesignBar,
                        TemplateDesignImg = a.TemplateDesignImg,
                        EmpId = a.EmpId,
                        empEmail = b.EmpEmail,
                        empRole = b.role
                    }).OrderByDescending(a=>a.Create).Skip((pageNo-1)*1).Take(record).ToList();
                }
                else
                {
                    _logger.LogInformation("StartGet design list records, Operator, Modrator");
                    var empIds = await _dbContext.empModels.Where(e => e.RefranceId == refranceId).Select(e => e.EmpId).ToListAsync();

                    result = await _dbContext.ImgTemplate.Join(
                    _dbContext.staticImgDesigns,
                    tempId => tempId.Id,
                    TempDesign => TempDesign.TemplateId,
                    (tempId, TempDesign) =>
                    new CustomImage {
                        TemplateId = tempId.Id,
                        TemplateName = tempId.FileName,
                        ImgPath = tempId.imgPath,
                        jsonPath = tempId.JsonPath,
                        Create = tempId.CreateAt,
                        discriotion = tempId.discription,
                        EmpId = tempId.EmpiId,
                        TemplateDesignBar = TempDesign.TemplateLogo,
                        TemplateDesignImg = TempDesign.TemplateBarcode,
                    }).Where(aa=>empIds.Contains(aa.EmpId)).ToListAsync();

                    result2 = result.Join(_dbContext.empModels, a => a.EmpId, b => b.EmpId, (a, b) => new CustomImage {
                        TemplateId = a.TemplateId,
                        TemplateName = a.TemplateName,
                        ImgPath = a.ImgPath,
                        jsonPath = a.jsonPath,
                        Create = a.Create,
                        discriotion = a.discriotion,
                        TemplateDesignBar = a.TemplateDesignBar,
                        TemplateDesignImg = a.TemplateDesignImg,
                        EmpId = a.EmpId,
                        empEmail = b.EmpEmail,
                        empRole = b.role
                    }).OrderByDescending(a=>a.Create).Skip((pageNo - 1) * 1).Take(record).ToList();

                }
                dynamic dataRec;
                if (FindTempId == 0)
                {
                    dataRec = result2;
                }
                else
                {
                    dataRec = result.Where(id => id.TemplateId == FindTempId);
                }
                return Ok(dataRec);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "");
                return StatusCode(StatusCodes.Status500InternalServerError, "");
            }
        }
        public class CustomImage
        {
            public int TemplateId { get; set; } = 0;
            public string? TemplateName { get; set; } = "";
            public string? ImgPath { get; set; } = "";
            public string? jsonPath { get; set; } = "";
            public string? Create { get; set; } = "";
            public string? discriotion { get; set; } = "";
            public string? TemplateDesignBar { get; set; } = "";
            public string? TemplateDesignImg { get; set; } = "";
            public string? EmpId { get; set; } = "";
            public string? empEmail { get; set; } = "";
            public string? empRole { get; set; } = "";
        }

        [HttpDelete]
        [Route("DeleteimgtempDes")]
        public async Task<IActionResult> DeleteimgtempDes(int TempId = 0)
        {
            try
            {
                //_logger.LogInformation();
                var resp1 = await _dbContext.ImgTemplate.FirstOrDefaultAsync(a => a.Id == TempId);
                var resp2 = await _dbContext.staticImgDesigns.FirstOrDefaultAsync(a => a.TemplateId == TempId);
                if(resp1==null && resp2 == null)
                {
                    return BadRequest("Plese share currect template Id with api");
                }

                _logger.LogInformation("Record delete Tempalte List and ");
                _dbContext.ImgTemplate.Remove(resp1);
                _dbContext.staticImgDesigns.Remove(resp2);
                await _dbContext.SaveChangesAsync();

                // Tempalate JSON 
                if (!string.IsNullOrEmpty(resp1.JsonPath))
                {
                    var path = Path.Combine(_env.WebRootPath, resp1.JsonPath);
                    System.IO.File.Delete(path);
                }
                // Template Image
                if (!string.IsNullOrEmpty(resp1.imgPath))
                {
                    var path = Path.Combine(_env.WebRootPath, resp1.imgPath);
                    System.IO.File.Delete(path);
                }

                // Design Image
                if (!string.IsNullOrEmpty(resp2.TemplateLogo))
                {
                    var path = Path.Combine(_env.ContentRootPath, resp2.TemplateLogo);
                    System.IO.File.Delete(path);
                }

                // Desgin Barcode 
                if (!string.IsNullOrEmpty(resp2.TemplateBarcode))
                {
                    var path = Path.Combine(_env.ContentRootPath, resp2.TemplateBarcode);
                    System.IO.File.Delete(path);
                }
                return Ok("Template Deleted successfully");
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, ex);
            }
        }

        [HttpPut]
        [Route("UpdateImgtempDes")]
        public async Task<IActionResult> UpdateImgtempDes(TempDesignModel model)
        {
            try
            {
                var TemplateManager = _dbContext.ImgTemplate.Where(a => a.Id == model.Id);
                var Templatedesign = _dbContext.staticImgDesigns.Where(a => a.Id == model.Id);
                var gettoken = Request.Headers["authorization"].FirstOrDefault()?.Replace("Bearer ", "").Trim();
                if (string.IsNullOrEmpty(gettoken))
                {
                    return Unauthorized(new { message = "no token provided" });
                }
                var handler = new JwtSecurityTokenHandler();
                var tokendecription = handler.ReadJwtToken(gettoken);
                var empid = tokendecription.Claims.FirstOrDefault(c => c.Type == "nameid")?.Value;

                // Tempalate JSON 
                if (!string.IsNullOrEmpty(model.TemplateJsonFile?.FileName))
                {
                    var path = Path.Combine(_env.WebRootPath, model.TemplateJsonFile?.FileName);
                    System.IO.File.Delete(path);

                    string ImguploadsPath = Path.Combine(_env.WebRootPath, "TempManager");
                    using (var fs = new FileStream(Path.Combine(_env.WebRootPath, "TempManager", model.TemplateJsonFile?.FileName), FileMode.Create))
                    {
                        await model.TemplateJsonFile.CopyToAsync(fs);
                    }
                }

                // Template Image
                if (!string.IsNullOrEmpty(model.TemplateImgFile?.FileName))
                {
                    var path = Path.Combine(_env.WebRootPath, model.TemplateImgFile?.FileName);
                    System.IO.File.Delete(path);
                    using (var fs = new FileStream(Path.Combine(_env.WebRootPath, "ImageManager", model.TemplateImgFile?.FileName), FileMode.Create))
                    {
                        await model.TemplateImgFile.CopyToAsync(fs);
                    }
                }

                // Design Image
                string directoryPath = Path.Combine(_root, "wFileManager", empid, "TempalteDesign");
                if (!string.IsNullOrEmpty(model.TemplateLogoFile?.FileName))
                {
                    var path = Path.Combine(_env.ContentRootPath, model.TemplateLogoFile?.FileName);
                    System.IO.File.Delete(path);
                    using (var tempImg = new FileStream(Path.Combine(directoryPath, model.TemplateLogoFile.FileName), FileMode.Create))
                    {
                        await model.TemplateLogoFile.CopyToAsync(tempImg);
                    }
                }

                // Desgin Barcode 
                if (!string.IsNullOrEmpty(model.TemplateBarcodeFile?.FileName))
                {
                    var path = Path.Combine(_env.ContentRootPath, model.TemplateBarcodeFile?.FileName);
                    System.IO.File.Delete(path);
                    using (var tempImg = new FileStream(Path.Combine(directoryPath, model.TemplateBarcodeFile.FileName), FileMode.Create))
                    {
                        await model.TemplateBarcodeFile.CopyToAsync(tempImg);
                    }
                }

                // 


                return Ok("abhi n hogi isme work karna hai");
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, "");
            }
        }
        public class TempDesignModel
        {
            public int Id { get; set; }
            public string? TemplateName { get; set; } = "";
            public string? TemplateId { get; set; } = "";
            public string? Discription { get; set; } = "";
            public string? TemplateJSON { get; set; } = "";
            public string? TemplateImg { get; set; } = "";
            public string? TemplateLogo { get; set; } = "";
            public string? TemplateBarcode { get; set; } = "";

            [NotMapped]
            public IFormFile? TemplateJsonFile { get; set; }
            [NotMapped]
            public IFormFile? TemplateImgFile { get; set; }
            [NotMapped]
            public IFormFile? TemplateLogoFile { get; set; }
            [NotMapped]
            public IFormFile? TemplateBarcodeFile { get; set; }
        }

    }
}

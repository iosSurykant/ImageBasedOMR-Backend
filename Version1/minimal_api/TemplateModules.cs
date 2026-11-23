using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using Dapper;
using DocumentFormat.OpenXml.Office2010.Excel;
using DocumentFormat.OpenXml.Office2010.ExcelAc;
using DocumentFormat.OpenXml.Office2016.Drawing.ChartDrawing;
using DocumentFormat.OpenXml.Spreadsheet;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json.Linq;
using SQCScanner.Modal;
using SQCScanner.Modal.CustomTemplate;
using TesseractOCR.Renderers;
using Version1.Data;
using ZXing.Aztec.Internal;

namespace SQCScanner.minimal_api
{
    public static class TemplateModules
    {

        public static void MapTemplateMoudle(this IEndpointRouteBuilder app)
        {
            app.MapGet("Template/PreDesign", static async (HttpContext context, ApplicationDbContext _context) =>
            {
                try
                {
                    var token = context.Request.Headers["Authorization"].FirstOrDefault()?.Replace("Bearer ", "").Trim();
                    if (string.IsNullOrEmpty(token))
                    {
                        return Results.Unauthorized();
                    }
                    var tokenHandler = new JwtSecurityTokenHandler();
                    var jwtToken = tokenHandler.ReadJwtToken(token);
                    var userId = jwtToken.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier || c.Type == "nameid")?.Value;
                    var userRole = jwtToken.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier || c.Type == "role")?.Value;
                    if (userRole != "superadmin")
                        return Results.BadRequest("Authrized with only super admin");
                    List<ImgTemp> dataTemple = await _context.ImgTemplate.ToListAsync();
                    List<StaticImgClass> dataStatic = await _context.StaticImgClass.ToListAsync();

                    var PreDesignTemplate = dataTemple.GroupJoin(
                        dataStatic,
                        temp => temp.Id.ToString(),
                        image => image.TemplateId,
                        (temp, images) => new { temp, images })
                    .Where(x => !x.images.Any())
                    .Select(x => x.temp.Id);
                    Console.WriteLine(PreDesignTemplate);
                    return Results.Ok(PreDesignTemplate);
                }catch(Exception ex)
                {
                    return Results.Problem(ex.Message);
                }
            });

            app.MapGet ("Template/TemplateBaseTest_mob", static async (HttpContext context, [FromServices] ILogger<Program> _logger, [FromServices] IConfiguration configuration, int templateId) =>
            {
                try {
                    var token = context.Request.Headers["Authorization"].FirstOrDefault()?.Replace("Bearer ", "").Trim();
                    if (string.IsNullOrEmpty(token))
                    {
                        return Results.Unauthorized();
                    }
                    var tokenHandler = new JwtSecurityTokenHandler();
                    var jwtToken = tokenHandler.ReadJwtToken(token);
                    var userId = jwtToken.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier || c.Type == "nameid")?.Value;
                    var userRole = jwtToken.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier || c.Type == "role")?.Value;
                    using (var _conn = new SqlConnection(configuration.GetConnectionString("dbc")))
                    {
                        var data = await _conn.QueryAsync($"select * from TestCases where TemplateId = @templateId AND EmpId = @userId", new {

                            templateId,
                            userId

                        });
                        return Results.Ok(new { 
                            massage="data result fatched",
                            result = data
                        });
                    }
                }
                catch (Exception ex)
                {
                    return Results.BadRequest(ex.Message);
                }
            });

            app.MapGet("Template/CreateTest_Template", static async (HttpContext context, ApplicationDbContext _context, [FromServices] ILogger<Program> Ilogger, [FromServices] IConfiguration configuration, [FromQuery] bool CreatePreDesign_Temp) =>
            {
            try
            {
                var token = context.Request.Headers["Authorization"].FirstOrDefault()?.Replace("Bearer ", "").Trim();
                if (string.IsNullOrEmpty(token))
                {
                    return Results.Unauthorized();
                }
                var tokenHandler = new JwtSecurityTokenHandler();
                var jwtToken = tokenHandler.ReadJwtToken(token);
                var userId = jwtToken.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier || c.Type == "nameid")?.Value;
                var userRole = jwtToken.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier || c.Type == "role")?.Value;
                var reffranceId = jwtToken.Claims.FirstOrDefault(a => a.Type == ClaimTypes.NameIdentifier || a.Type == "refranceId")?.Value;
                var empList = _context.empModels.Where(x => x.RefranceId == reffranceId).Select(a=>a.EmpId).ToList();
                List<ImgTemp> dataComplete = new List<ImgTemp>();
                List<int> PreDesign = await _context.StaticImgClass.Select(a => int.Parse(a.TemplateId)).ToListAsync();
                var CustomDesign = await _context.staticImgDesigns.Select(a => a.TemplateId).ToListAsync();

                if (!CreatePreDesign_Temp)
                {
                    // Jab Test Create karte hai, Tab Template list selection aata hai -- Done
                    if (userRole == "moderator" || userRole == "admin" || userRole == "operator")
                    {
                        dataComplete = await _context.ImgTemplate.Where(a => (a.JsonPath != "" &&  empList.Contains(a.EmpiId))).ToListAsync();
                    }
                    else // Admin Done 
                    {
                        dataComplete = await _context.ImgTemplate.Where(a => a.JsonPath != "" || PreDesign.Contains(a.Id)).ToListAsync();
                    }
                    // here we are getting template PreDesign list.
                    // Step 1: First table data load
                    var imgTemplates = _context.ImgTemplate
                        .AsNoTracking()
                        .ToList();

                    // Step 2: Second table data load
                    var staticImgClasses = _context.StaticImgClass
                        .AsNoTracking()
                        .ToList();

                    // Step 3: Join in C# memory
                    var PreDesign241 = imgTemplates.Join(staticImgClasses
                            .Select(b => new {
                                Data = b,
                                TemplateId = int.TryParse(b.TemplateId, out int id) ? (int?)id : null
                            })
                            .Where(x => x.TemplateId.HasValue),
                        a => a.Id,
                        b => b.TemplateId.Value,
                        (a, b) => new ImgTemp
                        {
                            Id = a.Id,
                            CreateAt = a.CreateAt,
                            FileName = b.Data.TemplateName,
                            imgPath = a.imgPath,
                            JsonPath = a.JsonPath,
                            discription = a.discription,
                            EmpiId = a.EmpiId
                        }
                    )
                    .ToList();
                    dataComplete.AddRange(PreDesign241);
                }
                else
                {
                    // Create PreDesgin Tempalte Facing Error Full Result -- Done
                    if(userRole != "superadmin")
                    {
                        return Results.BadRequest($"param CreatePreDesign_Temp=true for super Admin only");
                    }
                    else
                    {
                        dataComplete = await _context.ImgTemplate.Where(a => a.JsonPath != "" ).ToListAsync();
                    }
                }
                return Results.Ok(dataComplete);
                }
                catch (Exception ex)
                {
                    return Results.Problem(ex.Message);
                }
            });

            app.MapGet("Template/CreatePreDesign_Template", static async (HttpContext context, ApplicationDbContext _context, [FromServices] ILogger<Program> Ilogger, [FromServices] IConfiguration configuration) => {

                try {

                    var token = context.Request.Headers["Authorization"].FirstOrDefault()?.Replace("Bearer ", "").Trim();
                    if (string.IsNullOrEmpty(token))
                    {
                        return Results.Unauthorized();
                    }
                    var tokenHandler = new JwtSecurityTokenHandler();
                    var jwtToken = tokenHandler.ReadJwtToken(token);
                    var userId = jwtToken.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier || c.Type == "nameid")?.Value;
                    var userRole = jwtToken.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier || c.Type == "role")?.Value;
                    List<int> PreDesign = await _context.StaticImgClass.Select(a => int.Parse(a.TemplateId)).ToListAsync();
                    List<ImgTemp> dataComplete = await _context.ImgTemplate.Where(a => a.JsonPath != "" && a.EmpiId == userId).ToListAsync();
                    List<ImgTemp> dataPreDesign = await _context.ImgTemplate.Where(a => PreDesign.Contains(a.Id)).ToListAsync();
                    dataComplete.AddRange(dataPreDesign);
                    return Results.Ok(dataComplete);

                }
                catch (Exception ex)
                {
                    return Results.Problem(ex.Message);
                }
            });

        }
    }
}

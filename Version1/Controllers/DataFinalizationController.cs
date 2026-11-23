using System.IdentityModel.Tokens.Jwt;
using Dapper;
using DocumentFormat.OpenXml.Office2016.Drawing.ChartDrawing;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Microsoft.IdentityModel.Tokens;
using Serilog.Parsing;
using SharpCompress.Compressors.ZStandard.Unsafe;
using SQCScanner.Modal.DataFinalaization;
using SQCScanner.Services;
using Syncfusion.EJ2.Linq;
using Version1.Data;
using static SQCScanner.Controllers.userAuth;

namespace SQCScanner.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class DataFinalizationController : ControllerBase
    {
        private readonly IConfiguration conn;
        private readonly ApplicationDbContext context;
        private readonly ILogger logger;
        private readonly Tokendcript tokenService;



        public DataFinalizationController(IConfiguration _conn, ApplicationDbContext _context, ILogger<DataFinalizationController> _logger)
        {
            conn = _conn;
            context = _context;
            logger = _logger;
            tokenService = new Tokendcript();
        }


        
        [HttpGet("DataFinalGetList")]
        public async Task<IActionResult> DataFinalGetList2(int? Sr, [FromQuery] int page = 1, [FromQuery] int range = 10)
        {
            try
            {
                var gettoken = Request.Headers["authorization"].FirstOrDefault()?.Replace("Bearer ", "").Trim();
                if (string.IsNullOrEmpty(gettoken))
                {
                    logger.LogCritical("DataFinalGetList with api user is not authrized");
                    return Unauthorized("user is not authrized");
                }
                var handler = new JwtSecurityTokenHandler();
                var tokendecription = handler.ReadJwtToken(gettoken);
                int empid = int.Parse(tokendecription.Claims.FirstOrDefault(c => c.Type == "nameid")?.Value);

                string empRole = tokendecription.Claims.FirstOrDefault(c => c.Type == "role")?.Value;
                string refranceId = tokendecription.Claims.FirstOrDefault(c => c.Type == "refranceId")?.Value;

                logger.LogInformation("Get Data Finalzation Start", Sr);
                List<DataFinalizedClass> dataRec = new List<DataFinalizedClass>();
                List<DataFinalizedNew> dataRec2 = new List<DataFinalizedNew>();

                if (empRole == "superadmin")
                {
                    dataRec = await context.DataFinalized.OrderByDescending(a => a.Sr).Skip((page - 1) * range).Take(range).ToListAsync();
                    if (dataRec.Count == 0)
                        return NotFound("Data not found");

                }
                else if (empRole == "admin" || empRole == "moderator")
                {
                    var empList = await context.empModels.Where(x => x.RefranceId == refranceId).ToListAsync();

                    foreach (var data in empList)
                    {
                        int empId = int.Parse(data.EmpId);
                        var dataRec1 = await context.DataFinalized.Where(a=>a.EmpId== empId).ToListAsync();
                        dataRec.AddRange(dataRec1);
                    }
                    dataRec = dataRec.OrderByDescending(a => a.Sr).Skip((page - 1) * range).Take(range).ToList();
                    if (dataRec.Count == 0)
                        return NotFound("Data not found");

                }
                else
                {
                    dataRec = await context.DataFinalized.OrderByDescending(a => a.Sr).Where(a=> a.EmpId==empid).Skip((page - 1) * range).Take(range).ToListAsync();
                    if (dataRec.Count == 0)
                        return NotFound("Data not found");

                }

                if (Sr == 0)
                {
                    string path = "";
                    foreach (var data in dataRec)
                    {
                        DataFinalizedNew newItem = new DataFinalizedNew();
                        newItem.Sr = data.Sr;
                        newItem.Time = data.Time;
                        newItem.EmpId = data.EmpId;
                        newItem.TestId = data.TestId;
                        newItem.Remark = data.Remark;
                        newItem.state = data.state;
                        string? Template = data.Remark.Split(",").FirstOrDefault(a => a.StartsWith("TemplateId-"))?.Split("-")[1];
                        var template = await context.ImgTemplate.FirstOrDefaultAsync(a => a.Id == int.Parse(Template));
                        path = template != null ? template.JsonPath : "JSON Not Found";
                        newItem.templateJSON = path;
                        dataRec2.Add(newItem);
                    }
                    return Ok(dataRec2);
                }
                else
                {
                    dataRec = await context.DataFinalized.Where(x => x.Sr == Sr).Skip((page - 1) * range).Take(range).ToListAsync();
                    if (dataRec.Count == 0)
                        return NotFound("Data not found");

                    string[] dataResult = dataRec.FirstOrDefault()?.Remark.Split(',').Select(x => x.Trim()).ToArray();
                    string tableName = dataResult[^1];
                    tableName = tableName.Substring("TableName-".Length);
                    using (var conna = new SqlConnection(conn.GetConnectionString("dbc")))
                    {
                        conna.Open();
                        var result = await conna.QueryAsync($"select * from [{tableName}]");
                        return Ok(new { records = result, TableName = tableName });
                    }
                }
            }
            catch (Exception ex)
            {
                logger.LogError("Facing Exception error", ex.Message);
                return StatusCode(500, ex.Message);
            }
        }


        public class DataFinalizedNew
        {
            public int Sr { get; set; }
            public DateTime Time { get; set; }
            public int EmpId { get; set; } = 0;
            public int TestId { get; set; } = 0;
            public string Remark { get; set; } = "";
            public string state { get; set; } = "";
            public string templateJSON { get; set; } = "";
        }

        [HttpPut("DataFinalRecUpdate")]
        public async Task<IActionResult> DataFinalRecUpdate(udpateData model)
        {
            try
            {
                using (var conna = new SqlConnection(conn.GetConnectionString("dbc")))
                {
                    conna.Open();

                    string where = model.data["Sr"];
                    string query = $"update [{model.tableName}] SET {string.Join(",", model.data.Where(a => a.Key != "Sr").Select(a => $" [{a.Key}] = '{a.Value}'"))} , QC = 1 where Sr = '{where}'";  //  QC='C'
                    var result = await conna.ExecuteAsync(query);
                    var checkStatus = await conna.QueryAsync($"Select * from [{model.tableName}]");
                    //1. bool isDone = checkStatus.Any(a => Convert.ToInt32(a.QC) == 0);
                    //2. Best
                    string sql = $"SELECT CASE WHEN EXISTS (SELECT 1 FROM [{model.tableName}] WHERE QC = 0) THEN 1 ELSE 0 END";
                    bool isDone = await conna.ExecuteScalarAsync<bool>(sql);
                    TimeZoneInfo indiaTimeZone = TimeZoneInfo.FindSystemTimeZoneById("India Standard Time");
                    DateTime time = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, indiaTimeZone);

                    var data = await context.DataFinalized.FirstOrDefaultAsync(x => x.Sr == model.Sr);
                    if (data != null)
                    {
                        data.state = isDone ? "Pending" : "Complete";
                        data.Time = time;
                        await context.SaveChangesAsync();
                    }

                    return Ok(new { message = $"Record No. {where} has been updated successfully" });
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }


        public class udpateData
        {
            public int Sr { get; set; }
            public string tableName { get; set; }
            public Dictionary<string, string> data { get; set; } = new();
        }

        
    }
}

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
        public async Task<IActionResult> DataFinalGetList(int? Sr)
        {
            try {
                var gettoken = Request.Headers["authorization"].FirstOrDefault()?.Replace("Bearer ", "").Trim();
                if (string.IsNullOrEmpty(gettoken))
                {
                    logger.LogCritical("DataFinalGetList with api user is not authrized");
                    return Unauthorized("user is not authrized");
                }
                var handler = new JwtSecurityTokenHandler();
                var tokendecription = handler.ReadJwtToken(gettoken);
                var empid = tokendecription.Claims.FirstOrDefault(c => c.Type == "nameid")?.Value;
                string empRole = tokendecription.Claims.FirstOrDefault(c => c.Type == "role")?.Value;


                logger.LogInformation("Get Data Finalzation Start", Sr);
                List<DataFinalizedClass> dataRec = new List<DataFinalizedClass>();
                if(empRole == "admin")
                {
                    if (Sr==0)
                    {
                        dataRec = await context.DataFinalized.ToListAsync();
                        if (dataRec.Count == 0)
                        {
                            return NotFound("Data not found");
                        }

                    }    // Where(x => x.EmpId == int.Parse(empid))
                    else
                    {
                        dataRec = await context.DataFinalized.Where(x => x.Sr == Sr).ToListAsync();
                        if (dataRec.Count == 0)
                        {
                            return NotFound("Data not found");
                        }
                        string[] dataResult = dataRec.FirstOrDefault()?.Remark.Split(',').Select(x => x.Trim()).ToArray();
                        string tableName = dataResult[^1];
                        tableName = tableName.Substring("TableName-".Length);
                        using (var conna = new SqlConnection(conn.GetConnectionString("dbc")))
                        {
                            conna.Open();
                            var result = await conna.QueryAsync($"select * from [{tableName}]");
                            return Ok(new { records = result, TableName= tableName});
                        }
                    }
                    // Extract Teble n Target 
                    logger.LogInformation("Got Data Finalzation list and filter as per Id based.");
                }
                else
                {
                    if (Sr == 0)
                    {
                        dataRec = await context.DataFinalized.Where(x => x.EmpId == int.Parse(empid)).ToListAsync();    // Where(x => x.EmpId == int.Parse(empid))
                        if (dataRec.Count == 0)
                        {
                            return NotFound("Data not found");
                        }
                    }
                    else
                    {
                        dataRec = await context.DataFinalized.Where(x => x.Sr == Sr && x.EmpId == int.Parse(empid)).ToListAsync();
                        if (dataRec.Count == 0)
                        {
                            return NotFound("Data not found");
                        }
                        string[] dataList = dataRec.FirstOrDefault()?.Remark.Split(",").Select(x => x.Trim()).ToArray();
                        string tableName = dataList[^1].Substring("TableName-".Length);
                        using (var conna = new SqlConnection(conn.GetConnectionString("dbc")))
                        {
                            var result = conna.QueryAsync($"select * from [{tableName}]");
                            return Ok(new { records = result, TableName = tableName });
                        }
                    }
                    // Extract Teble n Target 
                    logger.LogInformation("Got Data Finalzation list and filter as per Id based.");
                }
                if (dataRec.Count == 0)
                {
                    logger.LogWarning("Data Finalzation list is Empty");
                    return NoContent();
                }
                return Ok(dataRec);
            }
            catch (Exception ex) {
                logger.LogError("Facing Exception error", ex.Message);
                return StatusCode(500, ex.Message);
            }
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
                    string query = $"update [{model.tableName}] SET {string.Join(",",model.data.Where(a=>a.Key!="Sr").Select(a => $" [{a.Key}] = '{a.Value}'"))} , QC = 1 where Sr = '{where}'";  //  QC='C'
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
                        data.state = isDone ? "Panding" : "Complete";
                        data.Time = time;
                        await context.SaveChangesAsync();
                    }

                    return Ok(new { message = $"Record No. {where} has been updated successfully" });
                }
            }catch(Exception ex)
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

        [HttpPut("DataFinaledUpdate")]
        public async Task<IActionResult> DataFinalUpdate(DataFinalizedClass model)
        {
            try {
     
                return Ok("USE LESS API");
            }
            catch(Exception ex)
            {
                return StatusCode(400, ex.Message);
            }
        }

    }
}

using System.Data;
using System.IdentityModel.Tokens.Jwt;
using System.Runtime.CompilerServices;
using System.Security.Claims;
using Dapper;
using Microsoft.AspNetCore.Mvc;
using SQCScanner.Modal;
using Version1.Data;
namespace SQCScanner.minimal_api
{
    public static class UserAuth
    {
        public static void MapUserAuth(this IEndpointRouteBuilder app)
        {
            app.MapGet("UserList/GetAdminRec", (HttpContext context, ApplicationDbContext _context, [FromQuery] string refranceId2,  [FromServices] ILogger<Program> _logger) =>
            {
                try {
                    if (refranceId2 == null)
                    {
                        _logger.LogWarning("Enter References Id");
                        return Results.BadRequest("Enter References Id");
                    }
                    List<EmpModel> FilterData = _context.empModels.Where(x=>x.RefranceId == refranceId2 && (x.role == "operator" || x.role == "moderator")).ToList();
                    _logger.LogInformation("Response fatch succssfully");
                    return Results.Ok(FilterData);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex.Message);
                    return Results.Problem(ex.Message);
                }
            });



        }
    }
}

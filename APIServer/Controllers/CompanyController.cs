using System;
using System.Threading.Tasks;
using APIServer.AuthAttributes;
using APIServer.Services;
using Business.Handler;
using Business.Model;
using Business.ViewModel;
using Microsoft.AspNetCore.Mvc;

namespace APIServer.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class CompanyController : ControllerBase
    {
        [HttpPut]
        [TypeFilter(typeof(ServiceProviderFilterFactory), Arguments = new object[] { Roles.Admin, 0, true })]
        public async Task<ActionResult<Company>> UpdateCompany(
            [FromServices] CompanyHandler handler,
            [FromBody] UpdateCompanyVM request)
        {
            if (!Request.Headers.TryGetValue("X-Company-Id", out var companyIdHeader) ||
                !Guid.TryParse(companyIdHeader, out var companyId))
            {
                return BadRequest(new { Message = "X-Company-Id header is required" });
            }

            var result = await handler.UpdateCompanyAsync(request, companyId);
            if (result == null) return NotFound();

            return Ok(result);
        }

        [HttpGet]
        [TypeFilter(typeof(ServiceProviderFilterFactory), Arguments = new object[] { Roles.Crew, 0, true })]
        public async Task<ActionResult<Company>> GetCompany(
            [FromServices] CompanyHandler handler)
        {
            if (!Request.Headers.TryGetValue("X-Company-Id", out var companyIdHeader) ||
               !Guid.TryParse(companyIdHeader, out var companyId))
            {
                return BadRequest(new { Message = "X-Company-Id header is required" });
            }

            var result = await handler.GetCompanyByIdAsync(companyId);
            if (result == null) return NotFound();

            return Ok(result);
        }
    }
}


using APIServer.AuthAttributes;
using APIServer.Services;
using Business.Handlers;
using Business.Response;
using Business.ViewModel;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace APIServer.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class JobGoalController : ControllerBase
    {
        public JobGoalController() { }

        [TypeFilter(typeof(ServiceProviderFilterFactory), Arguments = new object[] { Roles.Leader, 0, true })]
        [HttpPost("list")]
        public async Task<ActionResult<List<JobGoalRM>>> GetJobGoalList(
            [FromBody] JobGoalListRequestVM request,
            [FromServices] JobGoalHandler handler)
        {
            if (!Request.Headers.TryGetValue("X-Company-Id", out var companyIdHeader) ||
                !Guid.TryParse(companyIdHeader, out var companyId))
            {
                return BadRequest(new { Message = "X-Company-Id header is required" });
            }

            var result = await handler.GetJobGoalListAsync(request, companyId);
            return Ok(result);
        }

        [TypeFilter(typeof(ServiceProviderFilterFactory), Arguments = new object[] { Roles.Leader, 0, true })]
        [HttpGet("{id}")]
        public async Task<ActionResult<JobGoalRM>> GetJobGoalById(
            Guid id,
            [FromServices] JobGoalHandler handler)
        {
            if (!Request.Headers.TryGetValue("X-Company-Id", out var companyIdHeader) ||
                !Guid.TryParse(companyIdHeader, out var companyId))
            {
                return BadRequest(new { Message = "X-Company-Id header is required" });
            }

            var result = await handler.GetJobGoalByIdAsync(id, companyId);
            if (result == null)
                return NotFound(new { Message = "Job goal not found" });

            return Ok(result);
        }

        [TypeFilter(typeof(ServiceProviderFilterFactory), Arguments = new object[] { Roles.Leader, 0, true })]
        [HttpPost("create")]
        public async Task<ActionResult<JobGoalRM>> CreateJobGoal(
            [FromBody] CreateJobGoalVM request,
            [FromServices] JobGoalHandler handler)
        {
            if (!Request.Headers.TryGetValue("X-Company-Id", out var companyIdHeader) ||
                !Guid.TryParse(companyIdHeader, out var companyId))
            {
                return BadRequest(new { Message = "X-Company-Id header is required" });
            }

            try
            {
                var result = await handler.CreateJobGoalAsync(request, companyId);
                return Ok(result);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }

        [TypeFilter(typeof(ServiceProviderFilterFactory), Arguments = new object[] { Roles.Leader, 0, true })]
        [HttpPut("update")]
        public async Task<ActionResult<JobGoalRM>> UpdateJobGoal(
            [FromBody] UpdateJobGoalVM request,
            [FromServices] JobGoalHandler handler)
        {
            if (!Request.Headers.TryGetValue("X-Company-Id", out var companyIdHeader) ||
                !Guid.TryParse(companyIdHeader, out var companyId))
            {
                return BadRequest(new { Message = "X-Company-Id header is required" });
            }

            try
            {
                var result = await handler.UpdateJobGoalAsync(request, companyId);
                if (result == null)
                    return NotFound(new { Message = "Job goal not found" });

                return Ok(result);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }

        [TypeFilter(typeof(ServiceProviderFilterFactory), Arguments = new object[] { Roles.Crew, 0, true })]
        [HttpPost("complete")]
        public async Task<ActionResult<JobGoalRM>> CompleteJobGoal(
            [FromBody] CompleteJobGoalVM request,
            [FromServices] JobGoalHandler handler)
        {
            if (!Request.Headers.TryGetValue("X-Company-Id", out var companyIdHeader) ||
                !Guid.TryParse(companyIdHeader, out var companyId))
            {
                return BadRequest(new { Message = "X-Company-Id header is required" });
            }

            if (!Request.Headers.TryGetValue("X-User-Id", out var userIdHeader) ||
                !Guid.TryParse(userIdHeader, out var userId))
            {
                return BadRequest(new { Message = "X-User-Id header is required" });
            }

            try
            {
                var result = await handler.CompleteJobGoalAsync(request, companyId, userId);
                if (result == null)
                    return NotFound(new { Message = "Job goal not found" });

                return Ok(result);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }

        [TypeFilter(typeof(ServiceProviderFilterFactory), Arguments = new object[] { Roles.Leader, 0, true })]
        [HttpPost("verify")]
        public async Task<ActionResult<JobGoalRM>> VerifyJobGoal(
            [FromBody] VerifyJobGoalVM request,
            [FromServices] JobGoalHandler handler)
        {
            if (!Request.Headers.TryGetValue("X-Company-Id", out var companyIdHeader) ||
                !Guid.TryParse(companyIdHeader, out var companyId))
            {
                return BadRequest(new { Message = "X-Company-Id header is required" });
            }

            if (!Request.Headers.TryGetValue("X-User-Id", out var userIdHeader) ||
                !Guid.TryParse(userIdHeader, out var userId))
            {
                return BadRequest(new { Message = "X-User-Id header is required" });
            }

            try
            {
                var result = await handler.VerifyJobGoalAsync(request, companyId, userId);
                if (result == null)
                    return NotFound(new { Message = "Job goal not found" });

                return Ok(result);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }

        [TypeFilter(typeof(ServiceProviderFilterFactory), Arguments = new object[] { Roles.Crew, 0, true })]
        [HttpPost("add-media")]
        public async Task<ActionResult<JobGoalMediaRM>> AddJobGoalMedia(
            [FromBody] AddJobGoalMediaVM request,
            [FromServices] JobGoalHandler handler)
        {
            if (!Request.Headers.TryGetValue("X-Company-Id", out var companyIdHeader) ||
                !Guid.TryParse(companyIdHeader, out var companyId))
            {
                return BadRequest(new { Message = "X-Company-Id header is required" });
            }

            try
            {
                var result = await handler.AddJobGoalMediaAsync(request, companyId);
                return Ok(result);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }

        [TypeFilter(typeof(ServiceProviderFilterFactory), Arguments = new object[] { Roles.Admin, 0, true })]
        [HttpDelete("{id}")]
        public async Task<ActionResult> DeleteJobGoal(
            Guid id,
            [FromServices] JobGoalHandler handler)
        {
            if (!Request.Headers.TryGetValue("X-Company-Id", out var companyIdHeader) ||
                !Guid.TryParse(companyIdHeader, out var companyId))
            {
                return BadRequest(new { Message = "X-Company-Id header is required" });
            }

            var result = await handler.DeleteJobGoalAsync(id, companyId);
            if (!result)
                return NotFound(new { Message = "Job goal not found" });

            return Ok(new { Message = "Job goal deleted successfully" });
        }

        /// <summary>
        /// Cast a vote on a team goal. Team members can vote to complete goals assigned to everyone.
        /// Goal is marked complete when all team members vote complete (Y/Y).
        /// </summary>
        [TypeFilter(typeof(ServiceProviderFilterFactory), Arguments = new object[] { Roles.Crew, 0, true })]
        [HttpPost("vote")]
        public async Task<ActionResult<JobGoalRM>> CastGoalVote(
            [FromBody] CastGoalVoteVM request,
            [FromServices] JobGoalHandler handler)
        {
            if (!Request.Headers.TryGetValue("X-Company-Id", out var companyIdHeader) ||
                !Guid.TryParse(companyIdHeader, out var companyId))
            {
                return BadRequest(new { Message = "X-Company-Id header is required" });
            }

            if (!Request.Headers.TryGetValue("X-User-Id", out var userIdHeader) ||
                !Guid.TryParse(userIdHeader, out var userId))
            {
                return BadRequest(new { Message = "X-User-Id header is required" });
            }

            try
            {
                var result = await handler.CastGoalVoteAsync(request, companyId, userId);
                if (result == null)
                    return NotFound(new { Message = "Job goal not found" });

                return Ok(result);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }
    }
}

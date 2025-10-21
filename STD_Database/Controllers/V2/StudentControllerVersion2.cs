using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using STD_Database.Repositories;

namespace STD_Database.Controllers.V2
{
    [ApiVersion("2.0")]
    [Route("api/v{version:apiVersion}/[controller]")]
    [ApiController]
    public class StudentControllerVersion2 : ControllerBase
    {
        private readonly IUnitOfWork _UOW;
        public StudentControllerVersion2(IUnitOfWork UOW)
        {
            _UOW = UOW;
        }
        [HttpGet("Display-Students-Paged")]
        [Authorize(Roles = "Teacher")]
        public async Task<IActionResult> GetPagesAsync(int Page, int PageSize, string? SortBy = "Name", string? Order = "asc", string? Degree = null)
        {
            var (Students, TotalCount) = await _UOW.StdRepo.GetPagedAsync(Page, PageSize, SortBy, Order, Degree);
            var Response = new
            {
                version = "v2",
                success = true,
                message = "Student Fetched SuccessFully v2",
                pagination = new
                {
                    currentPage = Page,
                    PageSize,
                    totalItems = TotalCount,
                    TotalPages = (int)Math.Ceiling(TotalCount / (double)PageSize)
                },
                data = Students
            };
            return Ok(Response);
        }
    }
}

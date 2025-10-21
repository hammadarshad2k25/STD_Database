using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using STD_Database.DTO;
using STD_Database.Repositories;
using Serilog;

namespace STD_Database.Controllers
{
    /// <summary>
    /// Handles all student operations using Entity Framework (EF Core).
    /// </summary>
    [ApiVersion("1.0")]
    [Route("api/v{version:apiVersion}/[controller]")]
    [ApiController]
    public class StudentControllerEF : ControllerBase
    {
        private readonly IUnitOfWork _UOW;

        public StudentControllerEF(IUnitOfWork uow)
        {
            _UOW = uow;
        }

        /// <summary>
        /// Adds a new student to the database.
        /// </summary>
        /// <param name="Request">Student details to be added.</param>
        /// <returns>Returns the added student.</returns>
        [HttpPost("Add-Student")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> AddStudent([FromBody] StudentModelDTO Request)
        {
            try
            {
                if(Request.name == "LogTest")
                    throw new Exception("This is a test exception for logging.");
                var student = await _UOW.StdRepo.AddAsync(Request);
                return Ok(student);
            }
            catch (Exception ex)
            {
                Log.Error($"Error in AddStudent EndPoint: {ex.Message}");
                return StatusCode(500, "An Error Occured! Check Logs For Details");
            }
        }

        /// <summary>
        /// Retrieves all students (optionally includes courses).
        /// </summary>
        /// <param name="IncludeCourse">Include course details if true.</param>
        [HttpGet("Display-Students")]
        [Authorize(Roles = "Teacher")]
        public async Task<IActionResult> GetStudents([FromQuery] bool IncludeCourse = false)
        {
            var students = await _UOW.StdRepo.GetAsync(IncludeCourse);
            return Ok(new
            {
                version = "v1",
                message = "Fetched from API v1",
                data = students
            });
        }

        /// <summary>
        /// Retrieves paginated student records.
        /// </summary>
        [HttpGet("Paged")]
        public async Task<IActionResult> GetPagesAsync(
            int Page,
            int PageSize,
            string? SortBy = "Name",
            string? Order = "asc",
            string? Degree = null)
        {
            var (students, totalCount) = await _UOW.StdRepo.GetPagedAsync(Page, PageSize, SortBy, Order, Degree);

            return Ok(new
            {
                version = "v1",
                success = true,
                message = "Students fetched successfully (v1)",
                pagination = new
                {
                    currentPage = Page,
                    PageSize,
                    totalItems = totalCount,
                    TotalPages = (int)Math.Ceiling(totalCount / (double)PageSize)
                },
                data = students
            });
        }

        /// <summary>
        /// Searches for a student by Roll Number.
        /// </summary>
        [HttpGet("Search-Student-By-RollNumber/{rollnumber}")]
        public async Task<IActionResult> SearchStudents([FromRoute] int rollnumber, [FromQuery] bool IncludeCourse = false)
        {
            var student = await _UOW.StdRepo.SearchAsync(rollnumber, IncludeCourse);
            if (student == null)
                return NotFound($"Student with Roll Number {rollnumber} not found!");

            return Ok(student);
        }

        /// <summary>
        /// Updates an existing student.
        /// </summary>
        [HttpPut("Update-Student/{rollnumber}")]
        [Authorize(Roles = "Teacher")]
        public async Task<IActionResult> UpdateStudent([FromRoute] int rollnumber, [FromBody] StudentModelDTO Request)
        {
            var student = await _UOW.StdRepo.UpdateAsync(rollnumber, Request);
            if (student == null)
                return NotFound($"Student with Roll Number {rollnumber} not found!");

            return Ok(student);
        }

        /// <summary>
        /// Deletes a student by Roll Number.
        /// </summary>
        [HttpDelete("Delete-Student/{rollnumber}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteStudent([FromRoute] int rollnumber)
        {
            var deleted = await _UOW.StdRepo.DeleteAsync(rollnumber);
            if (!deleted)
                return NotFound($"Student with Roll Number {rollnumber} not found!");

            return Ok($"Student with Roll Number {rollnumber} deleted successfully!");
        }
    }
}

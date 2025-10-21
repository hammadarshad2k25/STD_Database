using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using STD_Database.DTO;
using STD_Database.Repositories;

namespace STD_Database.Controllers
{
    /// <summary>
    /// Handles all student operations using Dapper.
    /// </summary>
    [ApiVersion("1.0")]
    [Route("api/v{version:apiVersion}/[controller]")]
    [ApiController]
    public class StudentControllerDapper : ControllerBase
    {
        private readonly IUnitOfWork _UOW;

        public StudentControllerDapper(IUnitOfWork uow)
        {
            _UOW = uow;
        }

        /// <summary>
        /// Adds a new student using Dapper.
        /// </summary>
        [HttpPost("Add-Student-Dapper")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> AddStudent([FromBody] StudentModelDTO Request)
        {
            if (Request == null)
                return BadRequest("Request Not Found!");

            var student = await _UOW.StdRepoDapper.AddDapperAsync(Request);
            return Ok(student);
        }

        /// <summary>
        /// Retrieves all students using Dapper.
        /// </summary>
        [HttpGet("Display-Students-Dapper")]
        [Authorize(Roles = "Teacher")]
        public async Task<IActionResult> GetStudents([FromQuery] bool IncludeCourse = false)
        {
            var students = await _UOW.StdRepoDapper.GetAllDapperAsync(IncludeCourse);
            return Ok(students);
        }

        /// <summary>
        /// Searches a student by roll number using Dapper.
        /// </summary>
        [HttpGet("Search-Student-By-RollNumber-Dapper/{rollnumber}")]
        public async Task<IActionResult> SearchStudents([FromRoute] int rollnumber, [FromQuery] bool IncludeCourse = false)
        {
            var student = await _UOW.StdRepoDapper.SearchDapperAsync(rollnumber, IncludeCourse);
            if (student == null)
                return NotFound($"Student with Roll Number {rollnumber} not found!");

            return Ok(student);
        }

        /// <summary>
        /// Updates student record using Dapper.
        /// </summary>
        [HttpPut("Update-Student-Dapper/{rollnumber}")]
        [Authorize(Roles = "Teacher")]
        public async Task<IActionResult> UpdateStudent([FromRoute] int rollnumber, [FromBody] StudentModelDTO Request)
        {
            var student = await _UOW.StdRepoDapper.UpdateStudentDapper(rollnumber, Request);
            if (student == null)
                return NotFound($"Student with Roll Number {rollnumber} not found!");

            return Ok(student);
        }

        /// <summary>
        /// Deletes a student using Dapper.
        /// </summary>
        [HttpDelete("Delete-Student-Dapper/{rollnumber}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteStudent([FromRoute] int rollnumber)
        {
            var deleted = await _UOW.StdRepoDapper.DeleteStudentDapper(rollnumber);
            if (!deleted)
                return NotFound($"Student with Roll Number {rollnumber} not found!");

            return Ok($"Student with Roll Number {rollnumber} deleted successfully!");
        }
    }
}

using Xunit;
using STD_Database.Model;
using STD_Database.Tests.Helper;
using Moq;
using STD_Database.Repositories;
using STD_Database.DTO;
using System.Threading.Tasks;

namespace STD_Database.Tests
{
    public class StudentTests
    {
        [Fact]
        public async Task AddTest()
        {
            var MockRepo = new Mock<IStudentRepository>();
            var Student = new StudentModelDTO
            {
                name = "Zoya",
                rollnumber = 444,
                degree = "BSCS",
                semester = 6,
                cgpa = 2.32
            };
            MockRepo.Setup(r=>r.AddAsync(It.IsAny<StudentModelDTO>())).ReturnsAsync((StudentModelDTO s) => s);
            var Result = await MockRepo.Object.AddAsync(Student);
            Assert.NotNull(Result);
            Assert.Equal(Student.rollnumber,Result.rollnumber);
            Assert.Equal(Student.name,Result.name);
            MockRepo.Verify(r => r.AddAsync(It.Is<StudentModelDTO>(s => s.rollnumber == 444)), Times.Once);
        }
        [Fact]
        public async Task DeleteTest()
        {
            var MockRepo = new Mock<IStudentRepository>();
            int RollNumber = 444;
            MockRepo.Setup(r => r.DeleteAsync(It.IsAny<int>())).ReturnsAsync(true);
            var Result = await MockRepo.Object.DeleteAsync(RollNumber);
            Assert.True(Result);
            MockRepo.Verify(r=>r.DeleteAsync(RollNumber), Times.Once);
        }
    }
}

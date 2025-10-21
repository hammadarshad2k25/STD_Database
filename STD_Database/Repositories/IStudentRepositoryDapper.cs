using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Caching.Memory;
using Serilog;
using STD_Database.DTO;
using System.Diagnostics;
using StackExchange.Redis;
using System.Text.Json;

namespace STD_Database.Repositories
{
    public interface IStudentRepositoryDapper
    {
        Task<StudentModelDTO> AddDapperAsync(StudentModelDTO student);
        Task<IEnumerable<StudentModelDTO>> GetAllDapperAsync(bool IncludeCourses = false);
        Task<StudentModelDTO?> SearchDapperAsync(int rollnumber,bool IncludeCourses = false);
        Task<StudentModelDTO?> UpdateStudentDapper(int rollnumber,StudentModelDTO student);
        Task<bool> DeleteStudentDapper(int rollnumber);
    }
    public class StudentRepositoryDapper:IStudentRepositoryDapper
    {
        private readonly string _ConnectionString;
        private readonly IMemoryCache _Cache;
        private readonly IDatabase _Redis;
        public StudentRepositoryDapper(IConfiguration Config,IMemoryCache Cache,IConnectionMultiplexer Redis)
        {
            _ConnectionString = Config.GetConnectionString("DefaultConnection");
            _Cache = Cache;
            _Redis = Redis.GetDatabase();
        }
        public async Task<StudentModelDTO> AddDapperAsync(StudentModelDTO Request)
        {
            using var Connection = new SqlConnection(_ConnectionString);
                await Connection.ExecuteAsync("InsertSTDs",new
                {
                    Request.name,
                    Request.rollnumber,
                    Request.degree,
                    Request.semester,
                    Request.cgpa
                },commandType: System.Data.CommandType.StoredProcedure);
            foreach(var crs in Request.courses)
            {
                var Courseid = await Connection.ExecuteScalarAsync<int?>("SELECT CourseID FROM Courses WHERE CourseName = @Coursename", new { Coursename = crs.coursename });
                if(Courseid == null)
                {
                    var InsertCourses = @"INSERT INTO Courses (CourseName) VALUES (@courseName);SELECT CAST(SCOPE_IDENTITY() as int);";
                    Courseid = await Connection.ExecuteScalarAsync<int>(InsertCourses, new
                    {
                        courseName = crs.coursename
                    });
                }
                var InsertToSC = @"INSERT INTO StudentCourses (CoursesCourseID,StudentsRollNumber) VALUES(@coursescourseid,@studentsrollnumber)";
                await Connection.ExecuteAsync(InsertToSC, new
                {
                    CoursesCourseID = Courseid,
                    studentsrollnumber = Request.rollnumber
                });
            }
            return Request;
        }
        public async Task<IEnumerable<StudentModelDTO>> GetAllDapperAsync(bool IncludeCourses = false)
        {
            var stopwatch = Stopwatch.StartNew();
            string CacheKey = IncludeCourses ? "Students_WithCourses" : "Students_WithOutCourses";
            if(_Cache.TryGetValue(CacheKey, out IEnumerable<StudentModelDTO> CachedStudents))
            {
                stopwatch.Stop();
                Log.Information("(Dapper) Returning Students From Cache ({Key})", CacheKey, stopwatch.ElapsedMilliseconds);
                return CachedStudents;
            }
            Log.Information("(Dapper) Cache Miss! Fetching Students From DataBase ({Key})", CacheKey);
            using var Connection = new SqlConnection(_ConnectionString);
            var ShowStds = (await Connection.QueryAsync<StudentModelDTO>("GEtAllStudents",commandType:System.Data.CommandType.StoredProcedure)).ToList();
            if(IncludeCourses)
            {
                foreach(var student in ShowStds)
                {
                    var GetCourses = @"SELECT c.CourseID,c.CourseName FROM Courses c INNER JOIN StudentCourses sc ON c.CourseID = sc.CoursesCourseID WHERE sc.StudentsRollNumber = @RollNumber";
                    var ShowCourse = await Connection.QueryAsync<CourseModelDTO>(GetCourses, new
                    {
                        RollNumber = student.rollnumber
                    });
                    student.courses = ShowCourse.ToList();
                }
            }
            var CacheOption = new MemoryCacheEntryOptions().SetSlidingExpiration(TimeSpan.FromMinutes(2)).SetAbsoluteExpiration(TimeSpan.FromMinutes(5));
            _Cache.Set(CacheKey,ShowStds,CacheOption);
            stopwatch.Stop();
            Log.Information("(Dapper) Students Fetched From DB And Cached ({Key}) In {Elapsed} ms", CacheKey, stopwatch.ElapsedMilliseconds);
            return ShowStds;
        }
        public async Task<StudentModelDTO?> SearchDapperAsync(int rollnumber, bool IncludeCourses = false)
        {
            string CacheKey = $"Students:{rollnumber}:IncludeCourses:{IncludeCourses}";
            var Cached = await _Redis.StringGetAsync(CacheKey);
            if(!Cached.IsNullOrEmpty)
            {
                Log.Information("(Dapper) Redis Cache HIT for {CacheKey}", CacheKey);
                return JsonSerializer.Deserialize<StudentModelDTO>(Cached!);
            }
            Log.Information("(Dapper) Redis Cache MISS for {CacheKey} → Fetching from DB", CacheKey);
            using var Connection = new SqlConnection(_ConnectionString);
            var ShowStds = await Connection.QueryFirstOrDefaultAsync<StudentModelDTO>("GEtStudentBtRollNumber", new
            {
                rollNumber = rollnumber,
            }, commandType: System.Data.CommandType.StoredProcedure);
            if(ShowStds == null)
            {
                Log.Warning("(Dapper) Student with RollNumber {RollNumber} not found in DB", rollnumber);
                return null;    
            }
            if(IncludeCourses)
            {
                var GetCourses = @"SELECT c.CourseID,c.CourseName FROM Courses c INNER JOIN StudentCourses sc ON c.CourseID = sc.CoursesCourseID WHERE sc.StudentsRollNumber = @RollNumber";
                var ShowCourses = await Connection.QueryAsync<CourseModelDTO>(GetCourses, new
                {
                    RollNumber = rollnumber,
                });
                ShowStds.courses = ShowCourses.ToList();
            }
            await _Redis.StringSetAsync(CacheKey, JsonSerializer.Serialize(ShowStds), TimeSpan.FromMinutes(5));
            Log.Information("(Dapper) Student {RollNumber} cached in Redis ({CacheKey})", rollnumber, CacheKey);
            return ShowStds;
        }
        public async Task<StudentModelDTO?> UpdateStudentDapper(int rollnumber, StudentModelDTO Request)
        {
            using var Connection = new SqlConnection(_ConnectionString);
            var STD = "SELECT COUNT(1) FROM Students WHERE RollNumber = @Rollnumber";
            var StdExists = await Connection.ExecuteScalarAsync<int?>(STD, new
            {
                Rollnumber = rollnumber,
            });
            if (StdExists == null)
            {
                return null;
            }
            var StdUpdate = @"UPDATE Students SET Degree = @degree, Semester = @semester, Cgpa = @cgpa WHERE RollNumber = @rollNumber";
            await Connection.ExecuteAsync(StdUpdate, new
            {
                Request.degree,
                Request.semester,
                Request.cgpa,
                rollNumber = rollnumber
            });
            if (Request.courses != null && Request.courses.Any())
            {
                var DltCourses = "DELETE FROM StudentCourses WHERE StudentsRollNumber = @RollNumber";
                await Connection.ExecuteAsync(DltCourses, new
                {
                    RollNumber = rollnumber
                });
                foreach (var Crs in Request.courses)
                {
                    var ExistCourses = "SELECT CourseID FROM Courses WHERE CourseName = @courseName";
                    var Courseid = await Connection.ExecuteScalarAsync<int?>(ExistCourses, new
                    {
                        courseName = Crs.coursename,
                    });
                    if (Courseid == null)
                    {
                        var AddCourses = @"INSERT INTO Courses (CourseName) VALUES(@Coursename); SELECT CAST(SCOPE_IDENTITY() AS INT)";
                        Courseid = await Connection.ExecuteScalarAsync<int>(AddCourses, new
                        {
                            Coursename = Crs.coursename
                        });
                    }
                    var JoinCourses = "INSERT INTO StudentCourses (CoursesCourseID,StudentsRollNumber) VALUES(@Courseid,@RollNumber)";
                    await Connection.ExecuteAsync(JoinCourses, new
                    {
                        courseID = Courseid,
                        RollNumber = rollnumber
                    });
                }
            }
            return await SearchDapperAsync(rollnumber, IncludeCourses: true);
        }
        public async Task<bool> DeleteStudentDapper(int rollnumber)
        {
            using var Connection = new SqlConnection(_ConnectionString);
            var ExistsStudent = await Connection.ExecuteScalarAsync<int>("SELECT COUNT(1) FROM Students WHERE RollNumber = @Rollnumber", new
            {
                Rollnumber = rollnumber,
            });
            if(ExistsStudent == null)
            {
                return false;
            }
            var StudentCourseIDs = (await Connection.QueryAsync<int>("SELECT CoursesCourseID FROM StudentCourses WHERE StudentsRollNumber = @Rollnumber", new
            {
                Rollnumber = rollnumber,
            })).ToList();
            await Connection.ExecuteAsync("DELETE FROM StudentCourses WHERE StudentsRollNumber = @Rollnumber", new
            {
                Rollnumber = rollnumber
            });
            await Connection.ExecuteAsync("DELETE FROM Students WHERE RollNumber = @Rollnumber", new
            {
                Rollnumber = rollnumber
            });
            foreach(var crs in StudentCourseIDs)
            {
                var StillLinked = await Connection.ExecuteScalarAsync<int>("SELECT COUNT(1) FROM StudentCourses WHERE CoursesCourseID = @courseID", new
                {
                    courseID = crs
                });
                if(StillLinked == 0)
                {
                    await Connection.ExecuteAsync("DELETE FROM Courses WHERE CourseID = @courseid", new
                    {
                        courseid = crs
                    });
                }
            }
            return true;
        }
    }
}

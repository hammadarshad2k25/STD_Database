using Microsoft.EntityFrameworkCore;
using STD_Database.Database;
using STD_Database.DTO;
using STD_Database.Model;
using Microsoft.Extensions.Caching.Memory;
using Serilog;
using System.Diagnostics;
using StackExchange.Redis;
using System.Text.Json;

namespace STD_Database.Repositories
{
    public interface IStudentRepository
    {
        Task<StudentModelDTO> AddAsync(StudentModelDTO student);
        Task<List<StudentModelDTO>> GetAsync(bool IncludeCourse = false);
        Task<StudentModelDTO?> SearchAsync(int rollnumber,bool IncludCourse = false);
        Task<StudentModelDTO?> UpdateAsync(int Rollnumber,StudentModelDTO student);
        Task<bool> DeleteAsync(int Rollnumber);
        Task<(List<StudentModelDTO> stds, int TotalCount)> GetPagedAsync(int Page, int PageSize, string? SortBy = "Name", string? Order = "asc", string? Degree = null);
    }
    public class StudentRepository:IStudentRepository
    {
        private readonly StudentDB _db;
        private readonly IMemoryCache _Cache;
        private readonly IDatabase _Redis;
        public StudentRepository(StudentDB db, IMemoryCache cache,IConnectionMultiplexer Redis)
        {
            _db = db;
            _Cache = cache;
            _Redis = Redis.GetDatabase();
        }
        public async Task<StudentModelDTO> AddAsync(StudentModelDTO Request)
        { 
            var Student = new StudentMD
            {
                Name = Request.name,
                RollNumber = Request.rollnumber,
                Degree = Request.degree,
                Semester = Request.semester,    
                Cgpa = Request.cgpa,
            };
            foreach(var crs in Request.courses)
            {
                var ExistingCourses = await _db.Courses.FirstOrDefaultAsync(c=>c.CourseName == crs.coursename);
                if (ExistingCourses == null)
                {
                    ExistingCourses = new CoueseMD { CourseName = crs.coursename };
                   _db.Courses.Add(ExistingCourses);
                }
                Student.Courses.Add(ExistingCourses);
            }
            _db.Students.Add(Student);
            await _db.SaveChangesAsync();
            return new StudentModelDTO
            {
                name = Student.Name,
                rollnumber = Student.RollNumber,
                degree = Student.Degree,
                semester = Student.Semester,
                cgpa = Student.Cgpa,
                courses = Student.Courses.Select(c => new CourseModelDTO { coursename = c.CourseName }).ToList()
            };
        }
        public async Task<List<StudentModelDTO>> GetAsync(bool IncludeCourse = false)
        {
            var stopwatch = Stopwatch.StartNew();
            string CacheKey = IncludeCourse ? "Students_WithCourses" : "Students_WithOutCourses";
            if(_Cache.TryGetValue(CacheKey, out List<StudentModelDTO> CachedStudents))
            {
                stopwatch.Stop();
                Log.Information("Returning Students From Cache ({Key})", CacheKey, stopwatch.ElapsedMilliseconds);
                return CachedStudents;
            }
            Log.Information("Cache Miss! Fetching Students From DataBase ({Key})",CacheKey);
            List<StudentModelDTO> GetStd;
            if (IncludeCourse)
            {
                GetStd = await _db.Students.Include(c => c.Courses).Select(s => new StudentModelDTO
                {
                    name = s.Name,
                    rollnumber = s.RollNumber,
                    degree = s.Degree,
                    semester = s.Semester,
                    cgpa = s.Cgpa,
                    courses = s.Courses.Select(c => new CourseModelDTO { coursename = c.CourseName }).ToList()
                }).ToListAsync();
            }
            else
            {
                GetStd = await _db.Students.Select(s => new StudentModelDTO
                {
                    name = s.Name,
                    rollnumber= s.RollNumber,
                    degree = s.Degree,
                    semester = s.Semester,
                    cgpa= s.Cgpa,
                }).ToListAsync();
            }
            var CacheOptions = new MemoryCacheEntryOptions().SetSlidingExpiration(TimeSpan.FromMinutes(2)).SetAbsoluteExpiration(TimeSpan.FromMinutes(5));
            _Cache.Set(CacheKey,GetStd,CacheOptions);
            stopwatch.Stop();
            Log.Information("(EF) Students Fetched From DB And Cached ({Key}) In {Elapsed} ms",CacheKey,stopwatch.ElapsedMilliseconds);
            return GetStd;
        }
        public async Task<(List<StudentModelDTO> stds, int TotalCount)> GetPagedAsync(int Page, int PageSize, string? SortBy = "Name", string? Order = "asc", string? Degree = null)
        {
            var Querry = _db.Students.AsQueryable();
            if(!string.IsNullOrEmpty(Degree))
            {
                Querry = Querry.Where(d=>d.Degree == Degree);
            }
            if(!string.IsNullOrEmpty(SortBy))
            {
                Querry = Order?.ToLower() == "desc" ? Querry.OrderByDescending(s => EF.Property<object>(s,SortBy)) : Querry.OrderBy(s => EF.Property<object>(s,SortBy));
            }
            var TotalCount = await Querry.CountAsync();
            var Students = await Querry.Skip((Page - 1) * PageSize).Take(PageSize).Select(s => new StudentModelDTO
            {
                name = s.Name,
                rollnumber = s.RollNumber,
                degree = s.Degree,
                semester = s.Semester,
                cgpa = s.Cgpa,
            }).ToListAsync();
            return(Students, TotalCount);
        }
        public async Task<StudentModelDTO?> SearchAsync(int rollnumber, bool IncludCourse = false)
        {
            string CacheKey = $"Students:{rollnumber}:IncludeCourses:{IncludCourse}";
            var Cached = await _Redis.StringGetAsync(CacheKey);
            if(!Cached.IsNullOrEmpty)
            {
                Log.Information("(EF) Redis Cache HIT for {CacheKey}", CacheKey);
                return JsonSerializer.Deserialize<StudentModelDTO>(Cached!);    
            }
            Log.Information("(EF) Redis Cache MISS for {CacheKey} → Fetching from DB", CacheKey);
            StudentModelDTO? students;
            if(IncludCourse)
            {
                students = await _db.Students.Include(c => c.Courses).Where(r => r.RollNumber == rollnumber).Select(s => new StudentModelDTO
                {
                    name=s.Name,
                    rollnumber = s.RollNumber,
                    degree = s.Degree,
                    semester = s.Semester,
                    cgpa=s.Cgpa,
                    courses=s.Courses.Select(c=>new CourseModelDTO { coursename=c.CourseName }).ToList()
                }).FirstOrDefaultAsync();
            }
            else
            {
                students = await _db.Students.Where(r => r.RollNumber == rollnumber).Select(s => new StudentModelDTO
                {
                    name= s.Name,
                    rollnumber = s.RollNumber,
                    degree = s.Degree,
                    semester= s.Semester,
                    cgpa=s.Cgpa,
                }).FirstOrDefaultAsync();
            }
            if(students!=null)
            {
                await _Redis.StringSetAsync(CacheKey,JsonSerializer.Serialize(students),TimeSpan.FromMinutes(5));
                Log.Information("(EF) Student {RollNumber} cached in Redis ({CacheKey})", rollnumber, CacheKey);
            }
            else
            {
                Log.Warning("(EF) Student with RollNumber {RollNumber} not found in DB", rollnumber);
            }
            return students;
        }
        public async Task<StudentModelDTO?> UpdateAsync(int Rollnumber, StudentModelDTO Request)
        {
            var Student = await _db.Students.Include(c=>c.Courses).FirstOrDefaultAsync(r=>r.RollNumber==Rollnumber);
            if (Student == null)
            {
                return null;
            }
            Student.Degree = Request.degree;
            Student.Semester = Request.semester;
            Student.Cgpa = Request.cgpa;
            if(Request.courses!=null && Request.courses.Any())
            {
                Student.Courses.Clear();
                foreach(var crs in Request.courses)
                {
                    var ExistingCourse = await _db.Courses.FirstOrDefaultAsync(c => c.CourseName == crs.coursename);
                    if(ExistingCourse == null)
                    {
                        ExistingCourse = new CoueseMD { CourseName = crs.coursename };
                        _db.Courses.Add(ExistingCourse);
                    }
                    Student.Courses.Add(ExistingCourse);
                }
            }
            await _db.SaveChangesAsync();
            return new StudentModelDTO
            {
                name= Student.Name,
                rollnumber=Student.RollNumber,
                degree=Student.Degree,
                semester=Student.Semester,
                cgpa=Student.Cgpa,
                courses=Student.Courses.Select(c=>new CourseModelDTO { coursename=c.CourseName}).ToList()
            };
        }
        public async Task<bool> DeleteAsync(int Rollnumber)
        {
            var Student = await _db.Students.Include(c=>c.Courses).FirstOrDefaultAsync(r=>r.RollNumber == Rollnumber);
            if(Student == null)
            {
                return false;
            }
            var LinkedCourses = Student.Courses.ToList();
            _db.Students.Remove(Student);
            await _db.SaveChangesAsync();
            foreach(var crs in LinkedCourses)
            {
                bool StillUsed = await _db.Students.AnyAsync(c => c.Courses.Any(s => s.CourseID == crs.CourseID));
                if(!StillUsed)
                {
                    _db.Courses.Remove(crs);
                }
            }
            await _db.SaveChangesAsync();
            return true;
        }
    }
}

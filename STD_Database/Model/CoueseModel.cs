using System.ComponentModel.DataAnnotations;

namespace STD_Database.Model
{
    public class CoueseMD
    {
        [Key]
        public int CourseID { get; set; }
        [Required]  
        public string CourseName { get; set; }
        public ICollection<StudentMD> Students { get; set; } = new List<StudentMD>();
    }
}

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace STD_Database.Model
{
    public class StudentMD
    {
        [Required]
        public string Name { get; set; }
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public int RollNumber {  get; set; }
        public string Degree { get; set; }
        public int Semester { get; set; }
        public double Cgpa { get; set; }
        public ICollection<CoueseMD> Courses { get; set; } = new List<CoueseMD>();
    }
}

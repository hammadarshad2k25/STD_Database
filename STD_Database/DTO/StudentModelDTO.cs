namespace STD_Database.DTO
{
    public class StudentModelDTO
    {
        public string name { get; set; }
        public int rollnumber { get; set; }
        public string degree { get; set; }
        public int semester { get; set; }
        public double cgpa { get; set; }
        public List<CourseModelDTO> courses { get; set; } = new();
    }
}

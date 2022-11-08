namespace TodoApi.Models
{
    public class Course
    {
        public string? Fullname { get; set; }
        public string? Shortname { get; set; }
        public int CategoryId { get; set; }
        public int CourseId { get; set; }
    }
}

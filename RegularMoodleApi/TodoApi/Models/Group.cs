namespace TodoApi.Models
{
    public class Group
    {
        public int Groupid { get; set; }
        public int Userid { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }

        public int Courseid { get; set; }
        public int Descriptionformat { get; set; }
        public int Enrolmentkey { get; set; }
    }

}

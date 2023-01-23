namespace TodoApi.Models
{
    public class Enroll
    {
        
        public int Roleid { get; set; }
        
        public int Userid { get; set; }

        public int Courseid { get; set; }

        public int Timestart { get; set; }
        public int Timeend { get; set; }
        public int Suspend { get; set; }
        public string Contextlevel{ get; set; }
        public int Instanceid { get; set; }
    }
}

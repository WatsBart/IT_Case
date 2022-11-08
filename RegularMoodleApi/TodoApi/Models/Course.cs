namespace TodoApi.Models
{
    public partial class Course
    {
        public long? Id { get; set; }
        public string? Shortname { get; set; }
        public long? Categoryid { get; set; }
        public long? Categorysortorder { get; set; }
        public string? Fullname { get; set; }
        public string? Displayname { get; set; }
        public string? Idnumber { get; set; }
        public string? Summary { get; set; }
        public long? Summaryformat { get; set; }
        public string? Format { get; set; }
        public long? Showgrades { get; set; }
        public long? Newsitems { get; set; }
        public long? Startdate { get; set; }
        public long? Enddate { get; set; }
        public long? Numsections { get; set; }
        public long? Maxbytes { get; set; }
        public long? Showreports { get; set; }
        public long? Visible { get; set; }
        public long? Groupmode { get; set; }
        public long? Groupmodeforce { get; set; }
        public long? Defaultgroupingid { get; set; }
        public long? Timecreated { get; set; }
        public long? Timemodified { get; set; }
        public long? Enablecompletion { get; set; }
        public long? Completionnotify { get; set; }
        public string? Lang { get; set; }
        public string? Forcetheme { get; set; }
        public Courseformatoption[]? Courseformatoptions { get; set; }
        public bool? Showactivitydates { get; set; }
        public bool? Showcompletionconditions { get; set; }
        public long? Hiddensections { get; set; }
    }

    public partial class Courseformatoption
    {
        public string? Name { get; set; }
        public long? Value { get; set; }
    }
}

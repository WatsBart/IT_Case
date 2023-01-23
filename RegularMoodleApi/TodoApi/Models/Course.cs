public class Courseformatoption
{
    public string name { get; set; }
    public object value { get; set; }
}

public class Course
{
    public int? id { get; set; }
    public string shortname { get; set; }
    public int? categoryid { get; set; }
    public int? categorysortorder { get; set; }
    public string fullname { get; set; }
    public string displayname { get; set; }
    public string idnumber { get; set; }
    public string summary { get; set; }
    public int? summaryformat { get; set; }
    public string format { get; set; }
    public int? showgrades { get; set; }
    public int? newsitems { get; set; }
    public int? startdate { get; set; }
    public int? enddate { get; set; }
    public int? numsections { get; set; }
    public int? maxbytes { get; set; }
    public int? showreports { get; set; }
    public int? visible { get; set; }
    public int? groupmode { get; set; }
    public int? groupmodeforce { get; set; }
    public int? defaultgroupingid { get; set; }
    public int? timecreated { get; set; }
    public int? timemodified { get; set; }
    public int? enablecompletion { get; set; }
    public int? completionnotify { get; set; }
    public string lang { get; set; }
    public string forcetheme { get; set; }
    public List<Courseformatoption> courseformatoptions { get; set; }
    public bool? showactivitydates { get; set; }
    public bool? showcompletionconditions { get; set; }
    public int? hiddensections { get; set; }
}

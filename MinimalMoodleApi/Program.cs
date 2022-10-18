using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

var port = Environment.GetEnvironmentVariable("PORT") ?? "3000";
var options = new JsonSerializerOptions(JsonSerializerDefaults.Web);
HttpClient client = new HttpClient();

app.MapGet("/getcourses", async (HttpRequest request, HttpResponse response) => {
    var wstoken = request.Query["wstoken"];
    var wsfunction = "core_course_get_courses";
    var moodlewsrestformat = request.Query["moodlewsrestformat"];
    var stringTask = client.GetStreamAsync($"http://localhost/moodle/webservice/rest/server.php?wstoken={wstoken}&wsfunction={wsfunction}&moodlewsrestformat={moodlewsrestformat}");
    try{
        var message = await JsonSerializer.DeserializeAsync<List<Course>>(await stringTask);
        if(message is not null){
            foreach(var repo in message){
                response.WriteAsync(repo.id+"\n");
                response.WriteAsync(repo.shortname+"\n");
                response.WriteAsync(repo.fullname+"\n");
            }
        }        
    }catch(Exception e){
        var message = await JsonSerializer.DeserializeAsync<Object>(await stringTask);
        if(message is not null){
            Console.WriteLine(message.ToString());
        }
    }
});

app.MapGet("/createcourse", async (HttpRequest request, HttpResponse response) => {
    var wstoken = request.Query["wstoken"];
    var wsfunction = "core_course_create_courses";
    var fullname = request.Query["fullname"];
    var shortname = request.Query["shortname"];
    var categoryId = Int32.Parse(request.Query["categoryid"]);
    Course newCourse = new Course();
    newCourse.fullname = fullname;
    newCourse.shortname = shortname;
    newCourse.categoryid = categoryId;
    string course = Course.courseToString(newCourse);
    client.GetAsync($"http://localhost/moodle/webservice/rest/server.php?wstoken={wstoken}&wsfunction={wsfunction}&moodlewsrestformat=json"+course);
    response.WriteAsync($"Je hebt {newCourse.fullname} toegevoegd.");
});

app.MapGet("/deletecourse", async (HttpRequest request, HttpResponse response) => {
    var wstoken = request.Query["wstoken"];
    var wsfunction = "core_course_delete_courses";
    var id = request.Query["id"];
    client.GetAsync($"http://localhost/moodle/webservice/rest/server.php?wstoken={wstoken}&wsfunction={wsfunction}&moodlewsrestformat=json&courseids[0]={id}");
});

app.Run();

public class Course{
                public int id {get;set;}
                public string shortname {get;set;} = "";
                public int categoryid {get;set;}
                public int categorysortorder {get;set;}
                public string fullname {get;set;} = "";
                public string displayname {get;set;} = "";
                public string idnumber {get;set;} = "";
                public string summary {get;set;} = "";
                public int summaryformat {get;set;}
                public string format {get;set;} = "";
                public int showgrade {get;set;}
                public int newsitems {get;set;}
                public int startdate {get;set;}
                public int enddate {get;set;}
                public int numsections {get;set;}
                public int defaultgroupingid {get;set;}
                public long timecreated {get;set;}
                public long timemodified {get;set;}
                public int enablecompletion {get;set;}
                public int completionnotify {get;set;}
                public string lang {get;set;} = "";
                public string forcetheme {get;set;} = "";
                //public IcourseFormatOptions courseformatoptions {get;set;}
                public bool showactivitydates {get;set;}
                //public string? showcompletionconditions {get;set;}
                public static string courseToString(Course[] courses){
                    string stringCourses = "";
                    for(int i = 0;i < courses.Length;i++){
                        stringCourses = stringCourses + $"&courses[{i}][fullname]={courses[i].fullname}";
                        stringCourses = stringCourses + $"&courses[{i}][shortname]={courses[i].shortname}";
                        stringCourses = stringCourses + $"&courses[{i}][categoryid]={courses[i].categoryid}";
                    }
                    return stringCourses;
                } 

                public static string courseToString(Course course){
                    string stringCourses = "";
                    stringCourses = stringCourses + $"&courses[0][fullname]={course.fullname}";
                    stringCourses = stringCourses + $"&courses[0][shortname]={course.shortname}";
                    stringCourses = stringCourses + $"&courses[0][categoryid]={course.categoryid}";
                    return stringCourses;
                } 
}

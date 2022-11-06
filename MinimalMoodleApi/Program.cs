using System.Text.Json;
using System.Text;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();
app.UseHttpsRedirection();

var port = Environment.GetEnvironmentVariable("PORT") ?? "3000";
var options = new JsonSerializerOptions(JsonSerializerDefaults.Web);
HttpClient client = new HttpClient();
var uri = "http://localhost/webservice/rest/server.php";

//course methods
app.MapGet("/getcourses", async (HttpRequest request, HttpResponse response) => {
    var wstoken = request.Query["wstoken"];
    var wsfunction = "core_course_get_courses";
    var moodlewsrestformat = "json";
    var stringTask = client.GetStreamAsync($"{uri}?wstoken={wstoken}&wsfunction={wsfunction}&moodlewsrestformat={moodlewsrestformat}");
    try{
        var message = await JsonSerializer.DeserializeAsync<List<Course>>(await stringTask);
        if(message is not null){
            foreach(var repo in message){
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
    var moodlewsrestformat = "json";
    Course newCourse = new Course();
    newCourse.fullname = fullname;
    newCourse.shortname = shortname;
    newCourse.categoryid = categoryId;
    //string course = Course.courseToString(newCourse);
    var data = Course.courseToData(newCourse);
    client.PostAsync($"{uri}?wstoken={wstoken}&wsfunction={wsfunction}&moodlewsrestformat={moodlewsrestformat}",new FormUrlEncodedContent(data));
    response.WriteAsync($"Je hebt {newCourse.fullname} toegevoegd.");
});

app.MapGet("/deletecourse", async (HttpRequest request, HttpResponse response) => {
    var wstoken = request.Query["wstoken"];
    var wsfunction = "core_course_delete_courses";
    var id = request.Query["id"];
    var moodlewsrestformat = "json";
    client.GetAsync($"{uri}?wstoken={wstoken}&wsfunction={wsfunction}&moodlewsrestformat={moodlewsrestformat}&courseids[0]={id}");
});

//user methods
app.MapGet("/createuser", async (HttpRequest request, HttpResponse response) => {
    var wstoken = request.Query["wstoken"];
    var wsfunction = "core_user_create_users";
    var username = request.Query["username"];
    var password = request.Query["password"];
    var firstname = request.Query["firstname"];
    var lastname = request.Query["lastname"];
    var email = request.Query["email"];
    var moodlewsrestformat = "json";
    User newUser = new User();
    newUser.username = username;
    newUser.password = password;
    newUser.firstname = firstname;
    newUser.lastname = lastname;
    newUser.email = email;
    var data = User.userToData(newUser);
    client.PostAsync($"{uri}?wstoken={wstoken}&wsfunction={wsfunction}&moodlewsrestformat={moodlewsrestformat}",new FormUrlEncodedContent(data));
    response.WriteAsync(data.ToString());
});

app.MapGet("/resetpassword", async (HttpRequest request, HttpResponse response) => {
    var wstoken = request.Query["wstoken"];
    var username = request.Query["username"];
    var email = request.Query["email"];
    var wsfunction = "core_auth_request_password_reset";
    var moodlewsrestformat = "json";
    var data = new[]
    {
        new KeyValuePair<string, string>("username",username),
        new KeyValuePair<string,string>("email",email)
    };
    var reply = await client.PostAsync($"{uri}?wstoken={wstoken}&wsfunction={wsfunction}&moodlewsrestformat={moodlewsrestformat}",new FormUrlEncodedContent(data));
    reply.EnsureSuccessStatusCode();
    string replyBody = await reply.Content.ReadAsStringAsync();
    Console.WriteLine(replyBody);
});

app.Run();

public class Course{
                public string shortname {get;set;} = "";
                public int categoryid {get;set;}
                public string fullname {get;set;} = "";
                public static KeyValuePair<string,string>[][] courseToString(Course[] courses){
                    var data = new KeyValuePair<string,string>[3][];
                    for(int i = 0;i < courses.Length;i++){
                        data[i] = courseToData(courses[i]);
                    }
                    return data;
                } 

                public static KeyValuePair<string,string>[] courseToData(Course course){
                    return new[]
                        {
                        new KeyValuePair<string, string>("courses[0][fullname]",course.fullname),
                        new KeyValuePair<string, string>("courses[0][shortname]",course.shortname),
                        new KeyValuePair<string, string>("courses[0][categoryid]",course.categoryid.ToString())
                    };
                } 
}

public class User
        {
            public string username { get; set; }
            public string password { get; set; }
            public string firstname { get; set; }
            public string lastname { get; set; }
            public string email { get; set; }

            public static KeyValuePair<string,string>[] userToData(User user){
                    return new[]
                    {
                        new KeyValuePair<string, string>("users[0][username]",user.username),
                        new KeyValuePair<string, string>("users[0][password]",user.password),
                        new KeyValuePair<string, string>("users[0][firstname]",user.firstname),
                        new KeyValuePair<string, string>("users[0][lastname]",user.lastname),
                        new KeyValuePair<string, string>("users[0][email]",user.email),
                    };
                } 
        }

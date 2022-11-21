using System.Text.Json;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var port = Environment.GetEnvironmentVariable("PORT") ?? "3000";
var options = new JsonSerializerOptions(JsonSerializerDefaults.Web);
HttpClient client = new HttpClient();



//adjust authentication settings
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(options=>{
    options.TokenValidationParameters = new TokenValidationParameters(){
        ValidateActor = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = builder.Configuration["Jwt:Issuer"],
        ValidAudience = builder.Configuration["Jwt:Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]))
    };
});
//add authorization to endpoints
builder.Services.AddAuthorization();
var app = builder.Build();
app.UseAuthentication();
app.UseAuthorization();
app.UseSwagger();
app.UseSwaggerUI();

app.MapPost("/createToken",
 [AllowAnonymous] (HttpRequest request,TokenUser userz) =>{
    var username = userz.UserName;
    var password = userz.Password;

    if (username == "test" && password == "test123")
    {
        var issuer = builder.Configuration["Jwt:Issuer"];
        var audience = builder.Configuration["Jwt:Audience"];
        var key = Encoding.ASCII.GetBytes
        (builder.Configuration["Jwt:Key"]);
        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(new[]
            {
                new Claim("Id", Guid.NewGuid().ToString()),
                new Claim(JwtRegisteredClaimNames.Sub, userz.UserName),
                new Claim(JwtRegisteredClaimNames.Email, userz.UserName),
                new Claim(JwtRegisteredClaimNames.Jti,
                Guid.NewGuid().ToString())
             }),
            Expires = DateTime.UtcNow.AddMinutes(5),
            Issuer = issuer,
            Audience = audience,
            SigningCredentials = new SigningCredentials
            (new SymmetricSecurityKey(key),
            SecurityAlgorithms.HmacSha512Signature)
        };
        var tokenHandler = new JwtSecurityTokenHandler();
        var token = tokenHandler.CreateToken(tokenDescriptor);
        var jwtToken = tokenHandler.WriteToken(token);
        var stringToken = tokenHandler.WriteToken(token);
        return Results.Ok(stringToken);
    }
    return Results.Unauthorized();
});

//token security testing function
app.MapGet("/securityTest",[Authorize] async (HttpRequest request, HttpResponse response) => {
    response.WriteAsync("hello world");
});



//course methods
app.MapGet("/getcourses", async (HttpRequest request, HttpResponse response) => {
    var wstoken = request.Query["wstoken"];
    var wsfunction = "core_course_get_courses";
    var moodlewsrestformat = "json";
    var stringTask = client.GetStreamAsync($"http://localhost/webservice/rest/server.php?wstoken={wstoken}&wsfunction={wsfunction}&moodlewsrestformat={moodlewsrestformat}");
    try{
        var message = await JsonSerializer.DeserializeAsync<List<Course>>(await stringTask);
        if(message is not null){
            foreach(var repo in message){
                response.WriteAsync($"{repo.id} {repo.fullname} {repo.shortname} \n");

            }
        }        
    }catch(Exception e){
        var message = await JsonSerializer.DeserializeAsync<Object>(await stringTask);
        if(message is not null){
            Console.WriteLine(message.ToString());
        }
    }
});

app.MapPost("/createcourse", async (HttpRequest request, HttpResponse response) => {
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
    string course = Course.courseToString(newCourse);
    client.GetAsync($"http://localhost/webservice/rest/server.php?wstoken={wstoken}&wsfunction={wsfunction}&moodlewsrestformat={moodlewsrestformat}"+course);
    response.WriteAsync($"Je hebt {newCourse.fullname} toegevoegd.");
});

app.MapGet("/deletecourse", async (HttpRequest request, HttpResponse response) => {
    var wstoken = request.Query["wstoken"];
    var wsfunction = "core_course_delete_courses";
    var id = request.Query["id"];
    var moodlewsrestformat = "json";
    client.GetAsync($"http://localhost/webservice/rest/server.php?wstoken={wstoken}&wsfunction={wsfunction}&moodlewsrestformat={moodlewsrestformat}&courseids[0]={id}");
});

//user methods
app.MapPost("/createuser", async (HttpRequest request, HttpResponse response) => {
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
    string user = User.userToString(newUser);
    HttpClientHandler clientHandler = new HttpClientHandler();
    clientHandler.ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) => { return true; };

    // Pass the handler to httpclient
    HttpClient client = new HttpClient(clientHandler);
    
    await client.GetAsync($"https://localhost/webservice/rest/server.php?wstoken={wstoken}&wsfunction={wsfunction}&moodlewsrestformat={moodlewsrestformat}"+user);
    response.WriteAsync($"https://localhost/webservice/rest/server.php?wstoken={wstoken}&wsfunction={wsfunction}&moodlewsrestformat={moodlewsrestformat}"+user);
});

app.MapGet("/deleteuser", async (HttpRequest request, HttpResponse response) => {
    var wstoken = request.Query["wstoken"];
    var wsfunction = "core_user_delete_users";
    var id = request.Query["id"];
    var moodlewsrestformat = "json";
    await client.GetAsync($"http://localhost/webservice/rest/server.php?wstoken={wstoken}&wsfunction={wsfunction}&moodlewsrestformat={moodlewsrestformat}&userids[0]={id}");
});

app.MapGet("/getuser", async (HttpRequest request, HttpResponse response) => {
    var wstoken = request.Query["wstoken"];
    var wsfunction = "core_user_get_users";
    var moodlewsrestformat = "json";
    var stringTask = client.GetStreamAsync($"http://localhost/webservice/rest/server.php?wstoken={wstoken}&wsfunction={wsfunction}&moodlewsrestformat={moodlewsrestformat}");
    try{
        var message = await JsonSerializer.DeserializeAsync<List<User>>(await stringTask);
        if(message is not null){
            foreach(var repo in message){
                response.WriteAsync(repo.id+"\n");
                response.WriteAsync(repo.username+"\n");
                response.WriteAsync(repo.password+"\n");
            }
        }        
    }catch(Exception e){
        var message = await JsonSerializer.DeserializeAsync<Object>(await stringTask);
        if(message is not null){
            Console.WriteLine(message.ToString());
        }
    }
});
app.UseAuthentication();
app.UseAuthorization();
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

public class User
        {
            public string id { get; set; }
            public string username { get; set; }
            public string password { get; set; }
            public string firstname { get; set; }
            public string lastname { get; set; }
            public string email { get; set; }

            public static string userToString(User user){
                    string stringUsers = "";
                    stringUsers = stringUsers + $"&users[0][username]={user.username}";
                    stringUsers = stringUsers + $"&users[0][password]={user.password}";
                    stringUsers = stringUsers + $"&users[0][firstname]={user.firstname}";
                    stringUsers = stringUsers + $"&users[0][lastname]={user.lastname}";
                    stringUsers = stringUsers + $"&users[0][email]={user.email}";
                    return stringUsers;
                } 
        }
record TokenUser
{
    public string UserName { get; set; }
    public string Password { get; set; }
}

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
var uri = "https://moodlev4.cvoantwerpen.org/webservice/rest/server.php";

var post = async(string wstoken, string wsfunction, string moodlewsrestformat, KeyValuePair<string,string>[] data) => {
    client.PostAsync($"{uri}?wstoken={wstoken}&wsfunction={wsfunction}&moodlewsrestformat={moodlewsrestformat}", new FormUrlEncodedContent(data));
};



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
app.UseHttpsRedirection();

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
app.MapGet("/getcourses", async (HttpRequest request, HttpResponse response) =>
{
    var wstoken = request.Query["wstoken"];
    var wsfunction = "core_course_get_courses";
    var moodlewsrestformat = "json";
    var stringTask = client.GetStreamAsync($"{uri}?wstoken={wstoken}&wsfunction={wsfunction}&moodlewsrestformat={moodlewsrestformat}");
    try
    {
        var message = await JsonSerializer.DeserializeAsync<List<Course>>(await stringTask);
        if(message is not null){
            foreach(var repo in message){
                response.WriteAsync($" {repo.fullname} {repo.shortname} \n");

            }
        }
    }
    catch (Exception e)
    {
        var message = await JsonSerializer.DeserializeAsync<Object>(await stringTask);
        if (message is not null)
        {
            Console.WriteLine(message.ToString());
        }
    }
});

app.MapGet("/createcourse", async (HttpRequest request, HttpResponse response) =>
{
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
    post(wstoken,wsfunction,moodlewsrestformat,data);
    //client.PostAsync($"{uri}?wstoken={wstoken}&wsfunction={wsfunction}&moodlewsrestformat={moodlewsrestformat}", new FormUrlEncodedContent(data));
    response.WriteAsync($"Je hebt {newCourse.fullname} toegevoegd.");
});

app.MapGet("/deletecourse", async (HttpRequest request, HttpResponse response) =>
{
    var wstoken = request.Query["wstoken"];
    var wsfunction = "core_course_delete_courses";
    var id = request.Query["id"];
    var moodlewsrestformat = "json";
    client.GetAsync($"{uri}?wstoken={wstoken}&wsfunction={wsfunction}&moodlewsrestformat={moodlewsrestformat}&courseids[0]={id}");
});

app.MapGet("/addusertocourse", async(HttpRequest request, HttpResponse response) => 
{
    var wstoken = request.Query["wstoken"];
    var wsfunction = "enrol_manual_enrol_users";
    var roleid = request.Query["roleid"];
    var courseid = request.Query["courseid"];
    var userid = request.Query["userid"];
    var moodlewsrestformat = "json";

    var data = new[]
    {
        new KeyValuePair<string,string>("enrolments[0][roleid]",roleid),
        new KeyValuePair<string,string>("enrolments[0][userid]",userid),
        new KeyValuePair<string,string>("enrolments[0][courseid]",courseid)
    };

    post(wstoken,wsfunction,moodlewsrestformat,data);
});

//user methods
app.MapGet("/createuser", async (HttpRequest request, HttpResponse response) =>
{
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
    string user = newUser.ToString();
    HttpClientHandler clientHandler = new HttpClientHandler();
    clientHandler.ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) => { return true; };

    // Pass the handler to httpclient
    HttpClient client = new HttpClient(clientHandler);
    
    await client.GetAsync($"https://localhost/webservice/rest/server.php?wstoken={wstoken}&wsfunction={wsfunction}&moodlewsrestformat={moodlewsrestformat}"+user);
    response.WriteAsync($"https://localhost/webservice/rest/server.php?wstoken={wstoken}&wsfunction={wsfunction}&moodlewsrestformat={moodlewsrestformat}"+user);
    var data = User.userToData(newUser);
    post(wstoken,wsfunction,moodlewsrestformat,data);
    client.PostAsync($"{uri}?wstoken={wstoken}&wsfunction={wsfunction}&moodlewsrestformat={moodlewsrestformat}", new FormUrlEncodedContent(data));
    response.WriteAsync(data.ToString());
});

app.MapGet("/changeusersuspendstatus", async(HttpRequest request, HttpResponse response) =>
{
    var wstoken = request.Query["wstoken"];
    var wsfunction = "core_user_get_users";
    var username = request.Query["username"];
    
    var client = new HttpClient();
    var stringTask = client.GetAsync($"{uri}?wstoken={wstoken}&wsfunction={wsfunction}&criteria[0][key]=username&criteria[0][value]={username}&moodlewsrestformat=json");
    var message = await stringTask;
    var jsonString = await message.Content.ReadAsStringAsync();
    MoodleUserlistObject userlist = JsonSerializer.Deserialize<MoodleUserlistObject>(jsonString);
    
    if (userlist.users[0].suspended == false)
    {
        var suspendTask = client.GetAsync($"http://localhost/webservice/rest/server.php?wstoken={wstoken}&wsfunction=core_user_update_users&users[0][id]={userlist.users[0].id}&users[0][suspended]=1&moodlewsrestformat=json");
        var suspendMessage = await suspendTask;
        var suspendJsonString = await suspendMessage.Content.ReadAsStringAsync();
        response.WriteAsync($"{username} suspended");
    }else
    {
        var suspendTask = client.GetAsync($"http://localhost/webservice/rest/server.php?wstoken={wstoken}&wsfunction=core_user_update_users&users[0][id]={userlist.users[0].id}&users[0][suspended]=0&moodlewsrestformat=json");
        var suspendMessage = await suspendTask;
        var suspendJsonString = await suspendMessage.Content.ReadAsStringAsync();
        response.WriteAsync($"{username} unsuspended");
    }
});

/*
DELETE NOG NIET, ik wil vragen aan mr verbesselt of het beter is dit op te splitsen in twee

app.MapGet("/unsuspenduser", async(HttpRequest request, HttpResponse response) =>
{
    var wstoken = request.Query["wstoken"];
    var wsfunction = "core_user_get_users";
    var username = request.Query["username"];
    
    var client = new HttpClient();
    var stringTask = client.GetAsync($"{uri}?wstoken={wstoken}&wsfunction={wsfunction}&criteria[0][key]=username&criteria[0][value]={username}&moodlewsrestformat=json");
    var message = await stringTask;
    var jsonString = await message.Content.ReadAsStringAsync();    
    MoodleUserlistObject userlist = JsonSerializer.Deserialize<MoodleUserlistObject>(jsonString);
    
    if (userlist.users[0].suspended == true)
    {
        var suspendTask = client.GetAsync($"http://localhost/webservice/rest/server.php?wstoken={wstoken}&wsfunction=core_user_update_users&users[0][id]={userlist.users[0].id}&users[0][suspended]=0&moodlewsrestformat=json");
        var suspendMessage = await suspendTask;
        var suspendJsonString = await suspendMessage.Content.ReadAsStringAsync();
        response.WriteAsync($"{username} unsuspended");
    }
});
*/

//Group methods
app.MapGet("/addusertogroup", async(HttpRequest request, HttpResponse response) => 
{
    var wstoken = request.Query["wstoken"];
    var wsfunction = "core_group_add_group_members";
    var groupid = request.Query["groupid"];
    var userid = request.Query["userid"];
    var moodlewsrestformat = "json";
});
/*app.MapGet("/deleteuser", async (HttpRequest request, HttpResponse response) => {
    var wstoken = request.Query["wstoken"];
    var wsfunction = "core_user_delete_users";
    var id = request.Query["id"];
    var moodlewsrestformat = "json";
    await client.GetAsync($"http://localhost/webservice/rest/server.php?wstoken={wstoken}&wsfunction={wsfunction}&moodlewsrestformat={moodlewsrestformat}&userids[0]={id}");
    var data = new[]
    {
        new KeyValuePair<string,string>("members[0][groupid]",groupid),
        new KeyValuePair<string,string>("members[0][userid]",userid)
    };

    post(wstoken,wsfunction,moodlewsrestformat,data);
});*/

app.MapGet("/removeuserfromgroup", async(HttpRequest request, HttpResponse response) => 
{
    var wstoken = request.Query["wstoken"];
    var wsfunction = "core_group_delete_group_members";
    var groupid = request.Query["groupid"];
    var userid = request.Query["userid"];
    var moodlewsrestformat = "json";

    var data = new[]
    {
        new KeyValuePair<string,string>("members[0][groupid]",groupid),
        new KeyValuePair<string,string>("members[0][userid]",userid)
    };

    post(wstoken,wsfunction,moodlewsrestformat,data);
});


//Password Reset

app.MapGet("/resetpassword", async (HttpRequest request, HttpResponse response) =>
{
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
    post(wstoken,wsfunction,moodlewsrestformat,data);
    /*
    var reply = await client.PostAsync($"{uri}?wstoken={wstoken}&wsfunction={wsfunction}&moodlewsrestformat={moodlewsrestformat}", new FormUrlEncodedContent(data));
    reply.EnsureSuccessStatusCode();
    string replyBody = await reply.Content.ReadAsStringAsync();
    Console.WriteLine(replyBody);
    */
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

public class Course
{
    public string shortname { get; set; } = "";
    public int categoryid { get; set; }
    public string fullname { get; set; } = "";
    public static KeyValuePair<string, string>[][] courseToString(Course[] courses)
    {
        var data = new KeyValuePair<string, string>[3][];
        for (int i = 0; i < courses.Length; i++)
        {
            data[i] = courseToData(courses[i]);
        }
        return data;
    }

    public static KeyValuePair<string, string>[] courseToData(Course course)
    {
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

    public static KeyValuePair<string, string>[] userToData(User user)
    {
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

public class UserElement
{
    public long? id { get; set; }
    public string? username { get; set; }
    public string? firstname { get; set; }
    public string? lastname { get; set; }
    public string? fullname { get; set; }
    public string? email { get; set; }
    public string? department { get; set; }
    public long? firstaccess { get; set; }
    public long? lastaccess { get; set; }
    public string? auth { get; set; }
    public bool? suspended { get; set; }
    public bool? confirmed { get; set; }
    public string? lang { get; set; }
    public string? theme { get; set; }
    public string? timezone { get; set; }
    public long? mailformat { get; set; }
    public string? description { get; set; }
    public long? descriptionformat { get; set; }
    public Uri? profileimageurlsmall { get; set; }
    public Uri? profileimageurl { get; set; }
}

public class MoodleUserlistObject
{
    public UserElement[] users { get; set; }
    public object[] warnings { get; set; }
}

record TokenUser
{
    public string UserName { get; set; }
    public string Password { get; set; }
}
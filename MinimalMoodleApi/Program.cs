using System.Text.Json;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
<<<<<<< HEAD
using System.Linq;
using Microsoft.OpenApi.Models;

=======
using Microsoft.AspNetCore.Mvc;
>>>>>>> 50467b149339d78c5de1ea1ba363d4de12d99c0d

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options=>
{
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme 
    {
        Scheme = "Bearer",
        BearerFormat ="JWT",
        In = ParameterLocation.Header,
        Name = "Authorization",
        Description = "Bearer Authentication with JWT Token",
        Type = SecuritySchemeType.Http
    });
    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference{
                    Id = "Bearer",
                    Type = ReferenceType.SecurityScheme
                }
            },
            new List<string>()
        }
            
    });
});

var port = Environment.GetEnvironmentVariable("PORT") ?? "3000";
var options = new JsonSerializerOptions(JsonSerializerDefaults.Web);

HttpClientHandler clientHandler = new HttpClientHandler();
clientHandler.ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) => { return true; };
// Pass the handler to httpclient
HttpClient client = new HttpClient(clientHandler);

var uri = "https://localhost/webservice/rest/server.php";

var post = async(string wstoken, string wsfunction, string moodlewsrestformat, KeyValuePair<string,string>[] data) => {
    client.PostAsync($"{uri}?wstoken={wstoken}&wsfunction={wsfunction}&moodlewsrestformat={moodlewsrestformat}", new FormUrlEncodedContent(data));
};

//adjust authentication settings
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(options=>{
    options.TokenValidationParameters = new TokenValidationParameters(){
        ValidateActor = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ClockSkew = TimeSpan.Zero,
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

app.MapPost("/createToken",(TokenUser userz) =>{

    
    if (!string.IsNullOrEmpty(userz.Username)&&!string.IsNullOrEmpty(userz.Password))
    {
        var loggedInUser =  UserRepository.Users.FirstOrDefault(o=> o.Username.Equals(userz.Username, StringComparison.OrdinalIgnoreCase)&& o.Password.Equals(userz.Password));;
        if (loggedInUser is null) return Results.NotFound("user not found");
        
        var claims = new[]{
            new Claim(ClaimTypes.NameIdentifier,loggedInUser.Username),
            new Claim(ClaimTypes.Role, loggedInUser.Role)
        };
        
         var token = new JwtSecurityToken(
            issuer: builder.Configuration["Jwt:Issuer"],
            audience: builder.Configuration["Jwt:Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(1),
            signingCredentials: new SigningCredentials
            (new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"])),
            SecurityAlgorithms.HmacSha256)
        );
         var tokenstring = new JwtSecurityTokenHandler().WriteToken(token);
         return Results.Ok(tokenstring);
    }
    return Results.Unauthorized();
});

//token security testing function
app.MapGet("/securityTest",[Authorize] async (HttpRequest request, HttpResponse response) => {
    response.WriteAsync("hello world");
});

//course methods
app.MapGet("/getcourses",[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme,Roles = "Administrator, Service")] async (HttpRequest request, HttpResponse response,string token) =>
{
    var wstoken = token;
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

<<<<<<< HEAD
app.MapGet("/createcourse",[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme,Roles = "Administrator, Service")] async (HttpRequest request, HttpResponse response) =>
=======
app.MapPost("/createcourse", async ([FromBody] dataCourseObject dataObject) =>
>>>>>>> 50467b149339d78c5de1ea1ba363d4de12d99c0d
{
    var wstoken = dataObject.wstoken;
    var wsfunction = "core_course_create_courses";
    var fullname = dataObject.fullname;
    var shortname = dataObject.shortname;
    var categoryId = dataObject.categoryid;
    var moodlewsrestformat = "json";

    Course newCourse = new Course();
    newCourse.fullname = fullname;
    newCourse.shortname = shortname;
    newCourse.categoryid = categoryId;

    var data = Course.courseToData(newCourse);
    post(wstoken,wsfunction,moodlewsrestformat,data);

});

app.MapGet("/deletecourse",[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme,Roles = "Administrator, Service")] async (HttpRequest request, HttpResponse response) =>
{
    var wstoken = request.Query["wstoken"];
    var wsfunction = "core_course_delete_courses";
    var id = request.Query["id"];
    var moodlewsrestformat = "json";
    client.GetAsync($"{uri}?wstoken={wstoken}&wsfunction={wsfunction}&moodlewsrestformat={moodlewsrestformat}&courseids[0]={id}");
});

<<<<<<< HEAD
app.MapGet("/addusertocourse",[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme,Roles = "Administrator, Service")] async(HttpRequest request, HttpResponse response) => 
=======
app.MapPost("/addusertocourse", async([FromBody] dataEnrolmentObject dataObject) => 
>>>>>>> 50467b149339d78c5de1ea1ba363d4de12d99c0d
{
    var wstoken = dataObject.wstoken;
    var wsfunction = "enrol_manual_enrol_users";
    var roleid = dataObject.roleid;
    var courseid = dataObject.courseid;
    var userid = dataObject.userid;
    var moodlewsrestformat = "json";

    var data = new[]
    {
        new KeyValuePair<string,string>("enrolments[0][roleid]",roleid.ToString()),
        new KeyValuePair<string,string>("enrolments[0][userid]",userid.ToString()),
        new KeyValuePair<string,string>("enrolments[0][courseid]",courseid.ToString())
    };

    post(wstoken,wsfunction,moodlewsrestformat,data);
});

app.MapPost("/removestudentfromcourse", async([FromBody] dataRoleObject dataObject) => 
{
    var wstoken = dataObject.wstoken;
    var wsfunction = "core_role_unassign_roles";
    var roleid = "5";
    var instanceid = dataObject.instanceid;
    var userid = dataObject.userid;
    var contextlevel = "course";
    var moodlewsrestformat = "json";

    var data = new[]
    {
        new KeyValuePair<string,string>("unassignments[0][roleid]",roleid.ToString()),
        new KeyValuePair<string,string>("unassignments[0][userid]",userid.ToString()),
        new KeyValuePair<string,string>("unassignments[0][contextlevel]",contextlevel),
        new KeyValuePair<string,string>("unassignments[0][instanceid]",instanceid.ToString())
    };

    post(wstoken,wsfunction,moodlewsrestformat,data);
});

//user methods
<<<<<<< HEAD
app.MapGet("/createuser",[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme,Roles = "Administrator, Service")] async (HttpRequest request, HttpResponse response) =>
=======
app.MapPost("/createuser", async ([FromBody] dataUserObject dataObject) =>
>>>>>>> 50467b149339d78c5de1ea1ba363d4de12d99c0d
{
    var wstoken = dataObject.wstoken;
    var wsfunction = "core_user_create_users";
    var username = dataObject.username;
    var password = dataObject.password;
    var firstname = dataObject.firstname;
    var lastname = dataObject.lastname;
    var email = dataObject.email;
    var moodlewsrestformat = "json";
    User newUser = new User();
    newUser.username = username;
    newUser.password = password;
    newUser.firstname = firstname;
    newUser.lastname = lastname;
    newUser.email = email;
    var data = User.userToData(newUser);
    post(wstoken,wsfunction,moodlewsrestformat,data);
<<<<<<< HEAD
    //response.WriteAsync(data.ToString());
});

app.MapGet("/changeusersuspendstatus",[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme,Roles = "Administrator, Service")] async(HttpRequest request, HttpResponse response) =>
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
        var suspendTask = client.GetAsync($"{uri}?wstoken={wstoken}&wsfunction=core_user_update_users&users[0][id]={userlist.users[0].id}&users[0][suspended]=1&moodlewsrestformat=json");
        var suspendMessage = await suspendTask;
        var suspendJsonString = await suspendMessage.Content.ReadAsStringAsync();
        response.WriteAsync($"{username} suspended");
    }else
    {
        var suspendTask = client.GetAsync($"{uri}?wstoken={wstoken}&wsfunction=core_user_update_users&users[0][id]={userlist.users[0].id}&users[0][suspended]=0&moodlewsrestformat=json");
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
=======
>>>>>>> 50467b149339d78c5de1ea1ba363d4de12d99c0d
});

//Group methods
<<<<<<< HEAD
app.MapGet("/addusertogroup",[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme,Roles = "Administrator, Service")] async(HttpRequest request, HttpResponse response) => {
    var wstoken = request.Query["wstoken"];
=======
app.MapPost("/addusertogroup", async([FromBody] dataGroupObject dataObject) => {
    var wstoken = dataObject.wstoken;
>>>>>>> 50467b149339d78c5de1ea1ba363d4de12d99c0d
    var wsfunction = "core_group_add_group_members";
    var groupid = dataObject.groupid;
    var userid = dataObject.userid;
    var moodlewsrestformat = "json";

    var data = new[]
    {
        new KeyValuePair<string,string>("members[0][groupid]",groupid.ToString()),
        new KeyValuePair<string,string>("members[0][userid]",userid.ToString())
    };
    post(wstoken,wsfunction,moodlewsrestformat,data);
});

<<<<<<< HEAD
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

app.MapGet("/removeuserfromgroup",[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme,Roles = "Administrator, Service")] async(HttpRequest request, HttpResponse response) => 
=======
app.MapPost("/removeuserfromgroup", async([FromBody] dataGroupObject dataObject) => 
>>>>>>> 50467b149339d78c5de1ea1ba363d4de12d99c0d
{
    
    var wstoken = dataObject.wstoken;
    var wsfunction = "core_group_delete_group_members";
    var groupid = dataObject.groupid;
    var userid = dataObject.userid;
    var moodlewsrestformat = "json";
    var data = new[]
    {
        new KeyValuePair<string,string>("members[0][groupid]",groupid.ToString()),
        new KeyValuePair<string,string>("members[0][userid]",userid.ToString())
    };

    post(wstoken,wsfunction,moodlewsrestformat,data);
});


//Password Reset

<<<<<<< HEAD
app.MapGet("/resetpassword",[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme,Roles = "Administrator")] async (HttpRequest request, HttpResponse response) =>
=======
app.MapPost("/resetpassword", async ([FromBody] dataPasswordResetObject dataObject) =>
>>>>>>> 50467b149339d78c5de1ea1ba363d4de12d99c0d
{
    var wstoken = dataObject.wstoken;
    var username = dataObject.username;
    var email = dataObject.email;
    var wsfunction = "core_auth_request_password_reset";
    var moodlewsrestformat = "json";
    var data = new[]
    {
        new KeyValuePair<string, string>("username",username),
        new KeyValuePair<string,string>("email",email)
    };
    post(wstoken,wsfunction,moodlewsrestformat,data);
});

app.MapGet("/getuser",[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme,Roles = "Administrator, Service")] async (HttpRequest request, HttpResponse response) => {
    var wstoken = request.Query["wstoken"];
    var wsfunction = "core_user_get_users";
    var moodlewsrestformat = "json";
    var stringTask = client.GetStreamAsync($"{uri}?wstoken={wstoken}&wsfunction={wsfunction}&moodlewsrestformat={moodlewsrestformat}");
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

public class TokenUser
{
    public string Username { get; set; }
    public string Password { get; set; }
}
<<<<<<< HEAD
public class UserInfo{
    public string Username { get; set; }
    public string Password { get; set; }
    public string Role { get; set; }
}

public class UserRepository{
    public static List<UserInfo> Users = new(){
        new() {Username = "Admin", Password = "123",Role ="Administrator"},
        new(){Username = "fake", Password = "account",Role = "fake"},
        new(){Username = "Service", Password = "123",Role = "Service"}
    };
=======

public class dataUserObject
{
    public string wstoken {get;set;}
    public string username { get; set; }
    public string password { get; set; }
    public string firstname { get; set; }
    public string lastname { get; set; }
    public string email { get; set; }
}

public class dataCourseObject
{
    public string wstoken { get; set; }
    public string fullname { get; set; }
    public string shortname { get; set; }
    public int categoryid { get; set; }
}

public class dataEnrolmentObject
{
    public string wstoken { get; set; }
    public byte roleid { get; set; }
    public long courseid { get; set; }
    public long userid { get; set; }
}

public class dataRoleObject
{
    public string wstoken { get; set; }
    public byte roleid { get; set; }
    public long userid { get; set; }
    public long instanceid { get; set; }
}

public class dataGroupObject
{
    public string wstoken { get; set; }
    public int groupid { get; set; }
    public long userid { get; set; }
}

public class dataPasswordResetObject
{
    public string wstoken { get; set; }
    public string username { get; set; } = "";
    public string email { get; set; } = "";
>>>>>>> 50467b149339d78c5de1ea1ba363d4de12d99c0d
}
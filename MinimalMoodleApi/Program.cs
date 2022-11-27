using System.Text.Json;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Mvc;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var port = Environment.GetEnvironmentVariable("PORT") ?? "3000";
var options = new JsonSerializerOptions(JsonSerializerDefaults.Web);

HttpClientHandler clientHandler = new HttpClientHandler();
clientHandler.ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) => { return true; };
// Pass the handler to httpclient
HttpClient client = new HttpClient(clientHandler);

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

app.MapPost("/createcourse", async ([FromBody] dataCourseObject dataObject) =>
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

app.MapGet("/deletecourse", async (HttpRequest request, HttpResponse response) =>
{
    var wstoken = request.Query["wstoken"];
    var wsfunction = "core_course_delete_courses";
    var id = request.Query["id"];
    var moodlewsrestformat = "json";
    client.GetAsync($"{uri}?wstoken={wstoken}&wsfunction={wsfunction}&moodlewsrestformat={moodlewsrestformat}&courseids[0]={id}");
});

app.MapPost("/addusertocourse", async([FromBody] dataEnrolmentObject dataObject) => 
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

app.MapPost("/removeuserfromcourse", async([FromBody] dataEnrolmentObject dataObject) => 
{
    var wstoken = dataObject.wstoken;
    var wsfunction = "enrol_manual_unenrol_users";
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

//user methods
app.MapPost("/createuser", async ([FromBody] dataUserObject dataObject) =>
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
});

//Group methods
app.MapPost("/addusertogroup", async([FromBody] dataGroupObject dataObject) => {
    var wstoken = dataObject.wstoken;
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

app.MapPost("/removeuserfromgroup", async([FromBody] dataGroupObject dataObject) => 
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

app.MapPost("/resetpassword", async ([FromBody] dataPasswordResetObject dataObject) =>
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

app.MapGet("/getuser", async (HttpRequest request, HttpResponse response) => {
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

record TokenUser
{
    public string UserName { get; set; }
    public string Password { get; set; }
}

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
}
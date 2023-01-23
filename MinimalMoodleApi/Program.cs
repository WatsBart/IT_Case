using System.Text.Json;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Linq;
using Microsoft.OpenApi.Models;
using Microsoft.AspNetCore.Mvc;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Scheme = "Bearer",
        BearerFormat = "JWT",
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

var uri = "https://moodlev4.cvoantwerpen.org/webservice/rest/server.php";
//var uri = "http://localhost/webservice/rest/server.php";

var post = async (string wstoken, string wsfunction, string moodlewsrestformat, KeyValuePair<string, string>[] data) =>
{
    client.PostAsync($"{uri}?wstoken={wstoken}&wsfunction={wsfunction}&moodlewsrestformat={moodlewsrestformat}", new FormUrlEncodedContent(data));
};

//adjust authentication settings
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters()
    {
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

app.MapPost("/createToken", (TokenUser userz) =>
{
    if (!string.IsNullOrEmpty(userz.Username) && !string.IsNullOrEmpty(userz.Password))
    {
        var loggedInUser = UserRepository.Users.FirstOrDefault(o => o.Username.Equals(userz.Username, StringComparison.OrdinalIgnoreCase) && o.Password.Equals(userz.Password)); ;
        if (loggedInUser is null) return Results.NotFound("user not found");

        var claims = new[]{
            new Claim(ClaimTypes.NameIdentifier,loggedInUser.Username),
            new Claim(ClaimTypes.Role, loggedInUser.Role)
        };

        var token = new JwtSecurityToken(
           issuer: builder.Configuration["Jwt:Issuer"],
           audience: builder.Configuration["Jwt:Audience"],
           claims: claims,
           expires: DateTime.UtcNow.AddMinutes(30),
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
app.MapGet("/securityTest", [Authorize] async (HttpRequest request, HttpResponse response) =>
{
    response.WriteAsync("hello world");
});

//course methods
app.MapGet("/getcourses", [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = "Administrator, Service")] async (HttpRequest request, HttpResponse response, string token) =>
{
    var wstoken = token;
    var wsfunction = "core_course_get_courses";
    var moodlewsrestformat = "json";
    var stringTask = client.GetStreamAsync($"{uri}?wstoken={wstoken}&wsfunction={wsfunction}&moodlewsrestformat={moodlewsrestformat}");
    try
    {
        var message = await JsonSerializer.DeserializeAsync<List<Course>>(await stringTask);
        if (message is not null)
        {
            return message;
        }
        return null;
    }
    catch (Exception e)
    {
        var message = await JsonSerializer.DeserializeAsync<Object>(await stringTask);
        if (message is not null)
        {
            response.WriteAsync(message.ToString());
        }
    }
    return null;
});


app.MapPost("/createcourse", [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = "Administrator, Service")] async ([FromBody] dataCourseObject dataObject) =>
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
    post(wstoken, wsfunction, moodlewsrestformat, data);

});

app.MapGet("/deletecourse", [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = "Administrator, Service")] async ([FromBody] dataIdObject dataIdObject) =>
{
    var wstoken = dataIdObject.wstoken;
    var wsfunction = "core_course_delete_courses";
    var id = dataIdObject.id;
    var moodlewsrestformat = "json";
    client.GetAsync($"{uri}?wstoken={wstoken}&wsfunction={wsfunction}&moodlewsrestformat={moodlewsrestformat}&courseids[0]={id}");
});


app.MapPost("/addusertocourse", [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = "Administrator, Service")] async ([FromBody] dataEnrolmentObject dataObject) =>
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

    post(wstoken, wsfunction, moodlewsrestformat, data);
});

app.MapPost("/removestudentfromcourse", async ([FromBody] dataRoleObject dataObject) =>
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

    post(wstoken, wsfunction, moodlewsrestformat, data);
});

//user methods
app.MapPost("/createuser", [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = "Administrator, Service")] async ([FromBody] dataUserObject dataObject) =>
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
    post(wstoken, wsfunction, moodlewsrestformat, data);
});

//Group methods
app.MapPost("/addusertogroup", [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = "Administrator, Service")] async ([FromBody] dataGroupObject dataObject) =>
{
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
    post(wstoken, wsfunction, moodlewsrestformat, data);
});

app.MapPost("/removeuserfromgroup", [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = "Administrator, Service")] async ([FromBody] dataGroupObject dataObject) =>
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

    post(wstoken, wsfunction, moodlewsrestformat, data);
});


//Password Reset

app.MapPost("/resetpassword", [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = "Administrator")] async ([FromBody] dataPasswordResetObject dataObject) =>
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
    post(wstoken, wsfunction, moodlewsrestformat, data);
});

app.MapGet("/getuser", [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = "Administrator, Service")] async (HttpRequest request, HttpResponse response) =>
{
    var wstoken = request.Query["wstoken"];
    var wsfunction = "core_user_get_users";
    var moodlewsrestformat = "json";
    var stringTask = client.GetStreamAsync($"{uri}?wstoken={wstoken}&wsfunction={wsfunction}&moodlewsrestformat={moodlewsrestformat}");
    try
    {
        var message = await JsonSerializer.DeserializeAsync<List<User>>(await stringTask);
        if (message is not null)
        {
            foreach (var repo in message)
            {
                response.WriteAsync(repo.username + "\n");
                response.WriteAsync(repo.password + "\n");
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


/*
    Een simpele form met submit knop die een id en een username vraagt.
    De form submit naar /postform
*/
app.MapGet("/secretariaatsForm", /*[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = "Administrator, Service")]*/ async (HttpRequest request, HttpResponse response) =>
{
    response.WriteAsync("<body><form method='post' action='/postform'><label for='id'>Student's id</label><br/><input type='text' name='id' value='' /><br/><label for='username'>Student's username</label><br/><input type='text' name='username' /><br/><input type='submit' /></form></body>");
});

/*
    Deze functie verwacht een form met id en username.
    Deze functie zal het wachtwoord van de gebruiker met het ingegeven id of username resetten.
*/
app.MapPost("/postform", async (HttpRequest request, HttpResponse response) =>
{
    //De parameters uit de aangekregen form
    string id = request.Form["id"];
    string username = request.Form["username"];
    //De token voor het afhandelen van de uiteindelijke post functie. Dit zou ook via de form kunnen.
    var wstoken = "4aedb8e394c3ac61c042c0753e4d5c57";
    //De uiteindelijke moodle functie die wordt aangeroepen
    var wsfunction = "core_user_update_users";
    var moodlewsrestformat = "json";

    //Controle op de inhoud van de velden
    if(id==""){
        if(username==""){
            //Beide velden zijn leeg
            response.WriteAsync($"<body><p>Beide velden zijn leeg. Probeer het opnieuw.</p><form method='get' action='/secretariaatsForm'><input type='submit' value='return'/></form></body>");            
        }else{
            //Enkel id is leeg. Zoek in de database naar een gebruiker met de ingegeven username
            var stringTask = await client.GetAsync($"{uri}?wstoken={wstoken}&wsfunction=core_user_get_users&moodlewsrestformat=json&criteria[0][key]=username&criteria[0][value]={username}");
            var jsonContent = await stringTask.Content.ReadAsStringAsync();
            var message = JsonSerializer.Deserialize<UserListObject>(jsonContent);
            //Als de lijst van users met deze username 0 lang is bestaat deze user niet.
            if(message.users.Count == 0) response.WriteAsync($"Geen student met username {username} gevonden.");
            else{
                //Update het wachtwoord van de user adhv de ingegeven username
                var data = new[]
                {
                    new KeyValuePair<string,string>("users[0][username]",username),
                    new KeyValuePair<string,string>("users[0][password]","Moodle1."),
                    new KeyValuePair<string,string>("users[0][preferences][0][type]","auth_forcepasswordchange"),
                    new KeyValuePair<string,string>("users[0][preferences][0][value]","1")
                };
                post(wstoken, wsfunction, moodlewsrestformat, data);
            }
        }
    }else if(username==""){
        //Enkel username is leeg. Zoek in de database naar een gebruiker met ingegeven id
        var stringTask = await client.GetAsync($"{uri}?wstoken={wstoken}&wsfunction=core_user_get_users&moodlewsrestformat=json&criteria[0][key]=id&criteria[0][value]={id}");
        var jsonContent = await stringTask.Content.ReadAsStringAsync();
        var message = JsonSerializer.Deserialize<UserListObject>(jsonContent);    
        //Als de lijst van users met ingegeven id 0 lang is bestaat de user niet
        if(message.users.Count == 0) response.WriteAsync($"Geen student met id {id} gevonden");            
        else {
            //Update het wachtwoord adhv het ingegeven id
            var data = new[]
            {
                new KeyValuePair<string,string>("users[0][id]",id),
                new KeyValuePair<string,string>("users[0][password]","Moodle1."),
                new KeyValuePair<string,string>("users[0][preferences][0][type]","auth_forcepasswordchange"),
                new KeyValuePair<string,string>("users[0][preferences][0][value]","1")
            };
            post(wstoken, wsfunction, moodlewsrestformat, data);
        }
    }else{
        //Beide velden zijn ingevuld. Zoek in de database naar een gebruiker met het ingegeven id.
        var stringTask = await client.GetAsync($"{uri}?wstoken={wstoken}&wsfunction=core_user_get_users&moodlewsrestformat=json&criteria[0][key]=id&criteria[0][value]={id}");
        var jsonContent = await stringTask.Content.ReadAsStringAsync();
        var message = JsonSerializer.Deserialize<UserListObject>(jsonContent);
        //Als de lijst van users met ingegeven id 0 lang is bestaat de user niet
        if(message.users.Count == 0) response.WriteAsync($"Geen student met id {id} gevonden.");
        //Controleer of de ingegeven username overeenkomt met de username van de gebruiker met het ingegeven id
        else if (username == message.users[0].username)
        {
            //Zo ja, update het wachtwoord van de gebruiker
            var data = new[]
            {
                new KeyValuePair<string,string>("users[0][id]",id),
                new KeyValuePair<string,string>("users[0][password]","Moodle1."),
                new KeyValuePair<string,string>("users[0][preferences][0][type]","auth_forcepasswordchange"),
                new KeyValuePair<string,string>("users[0][preferences][0][value]","1")
            };
            post(wstoken, wsfunction, moodlewsrestformat, data);
        }
        else
        {
            //Zo nee, probeer opnieuw
            response.WriteAsync($"<body><p>Bedoelde je {username} of {message.users[0].username}?</p><form method='get' action='/secretariaatsForm'><input type='submit' value='return'/></form></body>");
        }
    }
});

/*
    Een functie met dezelfde functionaliteit als /postform maar werkt in swagger.
    Deze functie verwacht een string id en string username.
    Deze functie zal het wachtwoord van de gebruiker met het ingegeven id of username resetten.
*/
app.MapPost("/postformSwagger", async (string? studentId, string? studentUsername) =>
{
    //Haal de id en username uit de parameters
    string id = studentId;
    string username = studentUsername;
    //Hardcoded token voor de moodle functie aan te roepen
    var wstoken = "4aedb8e394c3ac61c042c0753e4d5c57";
    var wsfunction = "core_user_update_users";
    var moodlewsrestformat = "json";
    //Een lege input in swagger geeft null ipv een lege string
    //Verder idem aan /postform
    if(id == "" || id is null){
        if(username == "" || username is null){
            return "Beide velden zijn leeg. Probeer het opnieuw.";
        }else{
            var stringTask = await client.GetAsync($"{uri}?wstoken={wstoken}&wsfunction=core_user_get_users&moodlewsrestformat=json&criteria[0][key]=username&criteria[0][value]={username}");
            var jsonContent = await stringTask.Content.ReadAsStringAsync();
            var message = JsonSerializer.Deserialize<UserListObject>(jsonContent);    
            if(message.users.Count == 0) return $"Geen student met username {username} gevonden.";
            var data = new[]
            {
                new KeyValuePair<string,string>("users[0][username]",username),
                new KeyValuePair<string,string>("users[0][password]","Moodle1."),
                new KeyValuePair<string,string>("users[0][preferences][0][type]","auth_forcepasswordchange"),
                new KeyValuePair<string,string>("users[0][preferences][0][value]","1")
            };
            post(wstoken, wsfunction, moodlewsrestformat, data);
            return "Wachtwoord correct reset.";
        }
    }else if(username == "" || username is null){
        var stringTask = await client.GetAsync($"{uri}?wstoken={wstoken}&wsfunction=core_user_get_users&moodlewsrestformat=json&criteria[0][key]=id&criteria[0][value]={id}");
        var jsonContent = await stringTask.Content.ReadAsStringAsync();
        var message = JsonSerializer.Deserialize<UserListObject>(jsonContent);
        if(message.users.Count == 0) return $"Geen student met id {id} gevonden.";
        var data = new[]
        {
            new KeyValuePair<string,string>("users[0][id]",id),
            new KeyValuePair<string,string>("users[0][password]","Moodle1."),
            new KeyValuePair<string,string>("users[0][preferences][0][type]","auth_forcepasswordchange"),
            new KeyValuePair<string,string>("users[0][preferences][0][value]","1")
        };
        post(wstoken, wsfunction, moodlewsrestformat, data);
        return "Wachtwoord correct reset.";
    }else{
        var stringTask = await client.GetAsync($"{uri}?wstoken={wstoken}&wsfunction=core_user_get_users&moodlewsrestformat=json&criteria[0][key]=id&criteria[0][value]={id}");
        var jsonContent = await stringTask.Content.ReadAsStringAsync();
        var message = JsonSerializer.Deserialize<UserListObject>(jsonContent);
        if(message.users.Count == 0) return $"Geen student met id {id} gevonden.";
        if (username == message.users[0].username)
        {
            var data = new[]
            {
                new KeyValuePair<string,string>("users[0][id]",id),
                new KeyValuePair<string,string>("users[0][password]","Moodle1."),
                new KeyValuePair<string,string>("users[0][preferences][0][type]","auth_forcepasswordchange"),
                new KeyValuePair<string,string>("users[0][preferences][0][value]","1")
            };
            post(wstoken, wsfunction, moodlewsrestformat, data);
            return "Wachtwoord correct reset.";
        }
        else
        {
            return $"Bedoelde je {username} of {message.users[0].username}?";
        }
    }
});

/*
    Een simpele form met 4 inputvelden. Id, username, fullname, email
*/
app.MapGet("/studentForm", /*[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = "Administrator, Service")]*/ async (HttpRequest request, HttpResponse response) =>
{
    response.WriteAsync("<body><form method='post' action='/postStudentForm'><label for='id'>Student's id</label><br/><input type='text' name='id' value='' /><br/><label for='username'>Student's username</label><br/><input type='text' name='username' /><br/><label for='fullname'>Student's full name</label><br/><input type='text' name='fullname' value='' /><br /><label for='email'>Student's e-mail</label><br/><input type='text' name='email' value='' /><br /><input type='submit' /></form></body>");
});

/*
    Deze functie verwacht een form met id, username, fullname, email
    Deze functie zal het wachtwoord van de gebruiker met ingegeven id of username resetten als de ingegeven fullname of email overeenkomt.
    Fullname en email dienen als extra beveiliging.
*/
app.MapPost("/postStudentForm", async (HttpRequest request, HttpResponse response) =>
{
    //Haal de gegevens uit de form
    string id = request.Form["id"];
    string username = request.Form["username"];
    string fullname = request.Form["fullname"];
    string email = request.Form["email"];
    //Hardcoded token om de moodle functie uit te voeren
    var wstoken = "4aedb8e394c3ac61c042c0753e4d5c57";
    var wsfunction = "core_user_update_users";
    var moodlewsrestformat = "json";
    //Controle op de inhoud van de velden
    if(id==""){
        if(username==""){
            //Zowel id als username is leeg
            response.WriteAsync($"<body><p>Id en username zijn leeg. Probeer het opnieuw.</p><form method='get' action='/studentenForm'><input type='submit' value='return'/></form></body>");            
        }else{
            //Enkel id is leeg. Zoek in de database naar een gebruiker met ingegeven username
            var stringTask = await client.GetAsync($"{uri}?wstoken={wstoken}&wsfunction=core_user_get_users&moodlewsrestformat=json&criteria[0][key]=username&criteria[0][value]={username}");
            var jsonContent = await stringTask.Content.ReadAsStringAsync();
            var message = JsonSerializer.Deserialize<UserListObject>(jsonContent);
            //Als de lijst van studenten met ingegeven username 0 lang is bestaat de gebruiker niet
            if(message.users.Count == 0) response.WriteAsync($"<body><p>Geen student met username {username} gevonden. Probeer het opnieuw.</p><form method='get' action='/studentenForm'><input type='submit' value='return'/></form></body>");
            else {
                string fullnameOfStudentWithUsername = message.users[0].fullname;
                string emailOfStudentWithUsername = message.users[0].email;
                //Fullname of email moet ingegeven worden ter controle
                if(fullname == "" && email == "") response.WriteAsync($"<body><p>Gelieve uw volledige naam of email in te geven om te verifiëren dat u het bent. Probeer het opnieuw.</p><form method='get' action='/studentenForm'><input type='submit' value='return'/></form></body>");
                //Als zowel fullname als email niet overeenkomen met de gevonden gebruiker mag het wachtwoord niet reset worden.
                else if(fullnameOfStudentWithUsername != fullname && emailOfStudentWithUsername != email) response.WriteAsync($"<body><p>Je naam en/of email komt niet overeen met je ingevoerde username. Probeer het opnieuw.</p><form method='get' action='/studentenForm'><input type='submit' value='return'/></form></body>");
                else {
                    //Fullname of email komen overeen met de gebruiker met ingegeven username. Het wachtwoord wordt reset.
                    var data = new[]
                    {
                        new KeyValuePair<string,string>("users[0][username]",username),
                        new KeyValuePair<string,string>("users[0][password]","Moodle1."),
                        new KeyValuePair<string,string>("users[0][preferences][0][type]","auth_forcepasswordchange"),
                        new KeyValuePair<string,string>("users[0][preferences][0][value]","1")
                    };
                    post(wstoken, wsfunction, moodlewsrestformat, data);
                }
            }
        }
    }else if(username==""){
        //Enkel username is leeg. Zoek een gebruiker met het ingegeven id.
        var stringTask = await client.GetAsync($"{uri}?wstoken={wstoken}&wsfunction=core_user_get_users&moodlewsrestformat=json&criteria[0][key]=id&criteria[0][value]={id}");
        var jsonContent = await stringTask.Content.ReadAsStringAsync();
        var message = JsonSerializer.Deserialize<UserListObject>(jsonContent);    
        //Als de lijst van gebruikers met ingegeven id 0 lang is bestaat de gebruiker niet.
        if(message.users.Count == 0) response.WriteAsync($"Geen student met id {id} gevonden");
        else{
            string fullnameOfStudentWithId = message.users[0].fullname;
            string emailOfStudentWithId = message.users[0].email;
            //Fullname of email moet ingegeven worden ter controle
            if(fullname == "" && email == "") response.WriteAsync($"<body><p>Gelieve uw volledige naam of email in te geven om te verifiëren dat u het bent. Probeer het opnieuw.</p><form method='get' action='/studentenForm'><input type='submit' value='return'/></form></body>");
            //Als zowel fullname als email niet overeenkomen met de gevonden gebruiker mag het wachtwoord niet reset worden.
            else if(fullnameOfStudentWithId != fullname && emailOfStudentWithId != email) response.WriteAsync($"<body><p>Je naam en/of email komt niet overeen met je ingevoerde id. Probeer het opnieuw.</p><form method='get' action='/studentenForm'><input type='submit' value='return'/></form></body>");
            else{
                //Fullname of email komen overeen met de gebruiker met ingegeven id. Het wachtwoord wordt reset.
                var data = new[]
                {
                    new KeyValuePair<string,string>("users[0][id]",id),
                    new KeyValuePair<string,string>("users[0][password]","Moodle1."),
                    new KeyValuePair<string,string>("users[0][preferences][0][type]","auth_forcepasswordchange"),
                    new KeyValuePair<string,string>("users[0][preferences][0][value]","1")
                };
                post(wstoken, wsfunction, moodlewsrestformat, data);
            }
        }
    }else{
        //Beide velden zijn ingevuld. Zoek in de database naar een gebruiker met het ingegeven id.
        var stringTask = await client.GetAsync($"{uri}?wstoken={wstoken}&wsfunction=core_user_get_users&moodlewsrestformat=json&criteria[0][key]=id&criteria[0][value]={id}");
        var jsonContent = await stringTask.Content.ReadAsStringAsync();
        var message = JsonSerializer.Deserialize<UserListObject>(jsonContent);
        //Als de lijst van gebruikers met ingegeven id 0 lang is bestaat de gebruiker niet.
        if(message.users.Count == 0) response.WriteAsync($"Geen student met id {id} gevonden.");
        //Controleer of de ingegeven username overeenkomt met de username van de gebruiker met het ingegeven id.
        if (username == message.users[0].username)
        {
            string fullnameOfStudentWithId = message.users[0].fullname;
            string emailOfStudentWithId = message.users[0].email;
            //Fullname of email moeten ingevoerd worden ter controle.
            if(fullname == "" && email == "") response.WriteAsync($"<body><p>Gelieve uw volledige naam of email in te geven om te verifiëren dat u het bent. Probeer het opnieuw.</p><form method='get' action='/studentenForm'><input type='submit' value='return'/></form></body>");
            //Als zowel fullname als email niet overeenkomen mag het wachtwoord niet reset worden.
            else if(fullnameOfStudentWithId != fullname && emailOfStudentWithId != email) response.WriteAsync($"<body><p>Je naam en/of email komt niet overeen met je ingevoerde id. Probeer het opnieuw.</p><form method='get' action='/studentenForm'><input type='submit' value='return'/></form></body>");
            else{
                //Fullname of email is correct. Het wachtwoord wordt reset.
                var data = new[]
                {
                    new KeyValuePair<string,string>("users[0][id]",id),
                    new KeyValuePair<string,string>("users[0][password]","Moodle1."),
                    new KeyValuePair<string,string>("users[0][preferences][0][type]","auth_forcepasswordchange"),
                    new KeyValuePair<string,string>("users[0][preferences][0][value]","1")
                };
                post(wstoken, wsfunction, moodlewsrestformat, data);
            }
        }
        else
        {
            //De ingevoerde username komt niet overeen met de username van de gebruiker met ingevoerde id.
            response.WriteAsync($"<body><p>Bedoelde je {username} of {message.users[0].username}?</p><form method='get' action='/studentenForm'><input type='submit' value='return'/></form></body>");
        }
    }
});

/*
    Deze functie heeft dezelfde functionaliteit als /postStudentForm, maar werkt in Swagger.
    Deze functie verwacht een string StudentId, string StudentUsername, string StudentFullName, string StudentEmail
*/
app.MapPost("/postStudentFormSwagger", async (string? StudentId, string? StudentUsername, string? StudentFullName, string? StudentEmail) =>
{
    string id = StudentId;
    string username = StudentUsername;
    string fullname = StudentFullName;
    string email = StudentEmail;
    var wstoken = "4aedb8e394c3ac61c042c0753e4d5c57";
    var wsfunction = "core_user_update_users";
    var moodlewsrestformat = "json";
    if(id is null){
        if(username is null){
            return($"Id en username zijn leeg. Probeer het opnieuw.");            
        }else{
            var stringTask = await client.GetAsync($"{uri}?wstoken={wstoken}&wsfunction=core_user_get_users&moodlewsrestformat=json&criteria[0][key]=username&criteria[0][value]={username}");
            var jsonContent = await stringTask.Content.ReadAsStringAsync();
            var message = JsonSerializer.Deserialize<UserListObject>(jsonContent);
            if(message.users.Count == 0) return($"Geen student met username {username} gevonden. Probeer het opnieuw.");
            else {
                string fullnameOfStudentWithUsername = message.users[0].fullname;
                string emailOfStudentWithUsername = message.users[0].email;
                if(fullname is null && email is null) return($"Gelieve uw volledige naam of email in te geven om te verifiëren dat u het bent. Probeer het opnieuw.");
                else if(fullnameOfStudentWithUsername != fullname && emailOfStudentWithUsername != email) return($"Je naam en/of email komt niet overeen met je ingevoerde username. Probeer het opnieuw.");
                else {
                    var data = new[]
                    {
                        new KeyValuePair<string,string>("users[0][username]",username),
                        new KeyValuePair<string,string>("users[0][password]","Moodle1."),
                        new KeyValuePair<string,string>("users[0][preferences][0][type]","auth_forcepasswordchange"),
                        new KeyValuePair<string,string>("users[0][preferences][0][value]","1")
                    };
                    post(wstoken, wsfunction, moodlewsrestformat, data);
                    return "Wachtwoord correct reset.";
                }
            }
        }
    }else if(username is null){
        var stringTask = await client.GetAsync($"{uri}?wstoken={wstoken}&wsfunction=core_user_get_users&moodlewsrestformat=json&criteria[0][key]=id&criteria[0][value]={id}");
        var jsonContent = await stringTask.Content.ReadAsStringAsync();
        var message = JsonSerializer.Deserialize<UserListObject>(jsonContent);    
        if(message.users.Count == 0) return($"Geen student met id {id} gevonden");
        else{
            string fullnameOfStudentWithId = message.users[0].fullname;
            string emailOfStudentWithId = message.users[0].email;
            if(fullname is null && email is null) return($"Gelieve uw volledige naam of email in te geven om te verifiëren dat u het bent. Probeer het opnieuw.");
            else if(fullnameOfStudentWithId != fullname && emailOfStudentWithId != email) return($"Je naam en/of email komt niet overeen met je ingevoerde id. Probeer het opnieuw.");
            else{
                var data = new[]
                {
                    new KeyValuePair<string,string>("users[0][id]",id),
                    new KeyValuePair<string,string>("users[0][password]","Moodle1."),
                    new KeyValuePair<string,string>("users[0][preferences][0][type]","auth_forcepasswordchange"),
                    new KeyValuePair<string,string>("users[0][preferences][0][value]","1")
                };
                post(wstoken, wsfunction, moodlewsrestformat, data);
                return "Wachtwoord correct reset.";
            }
        }
    }else{
        var stringTask = await client.GetAsync($"{uri}?wstoken={wstoken}&wsfunction=core_user_get_users&moodlewsrestformat=json&criteria[0][key]=id&criteria[0][value]={id}");
        var jsonContent = await stringTask.Content.ReadAsStringAsync();
        var message = JsonSerializer.Deserialize<UserListObject>(jsonContent);
        if(message.users.Count == 0) return($"Geen student met id {id} gevonden.");
        if (username == message.users[0].username)
        {
            string fullnameOfStudentWithId = message.users[0].fullname;
            string emailOfStudentWithId = message.users[0].email;
            if(fullname is null && email is null) return($"Gelieve uw volledige naam of email in te geven om te verifiëren dat u het bent. Probeer het opnieuw.");
            else if(fullnameOfStudentWithId != fullname && emailOfStudentWithId != email) return($"Je naam en/of email komt niet overeen met je ingevoerde id. Probeer het opnieuw.");
            else{
                var data = new[]
                {
                    new KeyValuePair<string,string>("users[0][id]",id),
                    new KeyValuePair<string,string>("users[0][password]","Moodle1."),
                    new KeyValuePair<string,string>("users[0][preferences][0][type]","auth_forcepasswordchange"),
                    new KeyValuePair<string,string>("users[0][preferences][0][value]","1")
                };
                post(wstoken, wsfunction, moodlewsrestformat, data);
                return "Wachtwoord correct reset.";
            }
        }
        else
        {
            return($"Bedoelde je {username} of {message.users[0].username}?");
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

public class TokenUser
{
    public string Username { get; set; }
    public string Password { get; set; }
}
public class UserInfo
{
    public string Username { get; set; }
    public string Password { get; set; }
    public string Role { get; set; }
}

public class UserRepository
{
    public static List<UserInfo> Users = new(){
        new() {Username = "Admin", Password = "123",Role ="Administrator"},
        new(){Username = "fake", Password = "account",Role = "fake"},
        new(){Username = "Service", Password = "123",Role = "Service"}
    };
}

public class dataUserObject
{
    public string wstoken { get; set; }
    public string username { get; set; }
    public string password { get; set; }
    public string firstname { get; set; }
    public string lastname { get; set; }
    public string email { get; set; }
}

public class dataIdObject{
    public string wstoken { get; set; }
    public string id { get; set; }
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
}

public class UserListObject
{
    public List<UserObject> users { get; set; }
    public List<object> warnings { get; set; }
}

public class UserObject
{
    public int id { get; set; }
    public string username { get; set; }
    public string firstname { get; set; }
    public string lastname { get; set; }
    public string fullname { get; set; }
    public string email { get; set; }
    public string department { get; set; }
    public int firstaccess { get; set; }
    public int lastaccess { get; set; }
    public string auth { get; set; }
    public bool suspended { get; set; }
    public bool confirmed { get; set; }
    public string lang { get; set; }
    public string theme { get; set; }
    public string timezone { get; set; }
    public int mailformat { get; set; }
    public string description { get; set; }
    public int descriptionformat { get; set; }
    public string profileimageurlsmall { get; set; }
    public string profileimageurl { get; set; }
}

public class SwaggerForm {
    public string id { get; set; }
    public string username { get; set; }
}
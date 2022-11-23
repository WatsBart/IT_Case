using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System.Net;
using TodoApi.Models;
using static System.Net.WebRequestMethods;

namespace TodoApi.Controllers
{
    [ApiController]
    public class UserController
    {

        public string token = "1d5ecc3c89bff085d3fb31ba1db0c03a";
        public string uri = "http://localhost/webservice/rest/server.php?";


        [Route(@"api/createuser/{firstName}/{lastName}/{email}/{username}/{password}")]
        [HttpGet]
        public string CreateUsers(string firstName,string lastName,string email, string password, string username)
        {
            var newUser = new Cursist();
            newUser.Voornaam = firstName;
            newUser.Achternaam = lastName;
            newUser.Email = $@"{email}";
            newUser.Password = password; 
            newUser.Username = username; 
            
            string variables = $"&users[0][username]={newUser.Username}&users[0][password]={newUser.Password}&users[0][firstname]={newUser.Voornaam}&users[0][lastname]={newUser.Achternaam}&users[0][email]={newUser.Email}";
            var client = new HttpClient();
            Console.WriteLine($"{uri}wstoken={token}&wsfunction=core_user_create_users{variables}&moodlewsrestformat=json");
            var log = client.GetAsync($"{uri}wstoken={token}&wsfunction=core_user_create_users{variables}&moodlewsrestformat=json");
            log.Wait();
            return $"Function complete: {firstName} {lastName} {username} {password} {email}\n {uri}wstoken={token}&wsfunction=core_user_create_users{variables}&moodlewsrestformat=json";
        }
        [Route(@"api/suspenduser/{username}")]
        [HttpGet]
        public string ChangeUserSuspendStatus(string username)
        {
            var client = new HttpClient();
            var response = client.GetAsync($"{uri}wstoken={token}&wsfunction=core_user_get_users&criteria[0][key]=username&criteria[0][value]={username}&moodlewsrestformat=json");
            var result = response.Result.Content.ReadAsStringAsync();
            Console.WriteLine(result.Result);
            result.Wait();
            
            User user = JsonConvert.DeserializeObject<User>(result.Result);
            //user.Users[0].Id

            if (user.Users[0].Suspended == false)
            {
                response = client.GetAsync($"{uri}wstoken={token}&wsfunction=core_user_update_users&users[0][id]={user.Users[0].Id}&users[0][suspended]=1&moodlewsrestformat=json");
                result = response.Result.Content.ReadAsStringAsync();
                result.Wait();
                return $"{username} suspended";
            }
            else
            {
                response = client.GetAsync($"{uri}wstoken={token}&wsfunction=core_user_update_users&users[0][id]={user.Users[0].Id}&users[0][suspended]=0&moodlewsrestformat=json");
                result = response.Result.Content.ReadAsStringAsync();
                result.Wait();
                return $"{username} unsuspended";
            }
            
        }
        [Route(@"api/v1/EnrollUserInCourse/{username}/{shortname}/{role}")]
        [HttpGet]
        public string EnrollUserInCourse(string username,string shortname,string role)
        {
            
            //Ophalen van Course
            HttpClient clientCourses = new HttpClient();
            var responseTaskCourses = clientCourses.GetAsync($"{uri}wstoken={token}&wsfunction=core_course_get_courses_by_field&field=shortname&values[0]={shortname}&moodlewsrestformat=json");
            var resultCourses = responseTaskCourses.Result;
            var logCourses = resultCourses.Content.ReadAsStringAsync();
            logCourses.Wait();
            Course course = JsonConvert.DeserializeObject<Course>(logCourses.Result);

            //Ophalen van User
            HttpClient clientUsers = new HttpClient();
            var responseTaskUsers = clientUsers.GetAsync($"{uri}wstoken={token}&wsfunction=core_user_get_users_by_field&field=username&values[0]={username}&moodlewsrestformat=json");
            var resultUsers = responseTaskUsers.Result;
            var logUsers = resultUsers.Content.ReadAsStringAsync();
            logUsers.Wait();
            UserElement[] users = JsonConvert.DeserializeObject<UserElement[]>(logUsers.Result);

            int userid = (int)users[0].Id;
            int courseid = (int)course.Id;
            int roleid = -1;
            switch (role)
            {
                case "Cursist":
                    roleid = 5;
                    break;
                case "Leeraar":
                    roleid = 3;
                    break;
                default:
                    break;
            }
            string function = "&wsfunction=enrol_manual_enrol_users";
            string param = $"wstoken={token}&wsfunction=enrol_manual_enrol_users&enrolments[0][roleid]={roleid}&enrolments[0][userid]={userid}&enrolments[0][courseid]={courseid}&moodlewsrestformat=json";
            if (roleid == -1) return "Foute role meegegeven.";
            //var data = new[]
           // {
            //    new KeyValuePair<string,string>("enrolments[0][roleid]",$"{roleid}"),
             //   new KeyValuePair<string,string>("enrolments[0][userid]",$"{userid}"),
             //   new KeyValuePair<string,string>("enrolments[0][courseid]",$"{courseid}")
            //};
            HttpClient client = new HttpClient();
            var responseTask = client.GetAsync($"{uri}{param}");
            var result = responseTask.Result;
            var log = result.Content.ReadAsStringAsync();
            return $"{result.StatusCode}\n{log.Result}";
        }
    }
}

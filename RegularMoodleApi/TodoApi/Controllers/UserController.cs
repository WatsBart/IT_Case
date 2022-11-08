using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System.Net;
using TodoApi.Models;
namespace TodoApi.Controllers
{
    [ApiController]
    public class UserController
    {

        public string token = "1d5ecc3c89bff085d3fb31ba1db0c03a";
        
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
            Console.WriteLine($"http://localhost/webservice/rest/server.php?wstoken={token}&wsfunction=core_user_create_users{variables}&moodlewsrestformat=json");
            var log = client.GetAsync($"http://localhost/webservice/rest/server.php?wstoken={token}&wsfunction=core_user_create_users{variables}&moodlewsrestformat=json");
            log.Wait();
            return $"Function complete: {firstName} {lastName} {username} {password} {email}\n http://localhost/webservice/rest/server.php?wstoken={token}&wsfunction=core_user_create_users{variables}&moodlewsrestformat=json";
        }
        [Route(@"api/suspenduser/{username}")]
        [HttpGet]
        public string ChangeUserSuspendStatus(string username)
        {
            var client = new HttpClient();
            var response = client.GetAsync($"http://localhost/webservice/rest/server.php?wstoken={token}&wsfunction=core_user_get_users&criteria[0][key]=username&criteria[0][value]={username}&moodlewsrestformat=json");
            var result = response.Result.Content.ReadAsStringAsync();
            Console.WriteLine(result.Result);
            result.Wait();
            
            User user = JsonConvert.DeserializeObject<User>(result.Result);
            //user.Users[0].Id

            if (user.Users[0].Suspended == false)
            {
                response = client.GetAsync($"http://localhost/webservice/rest/server.php?wstoken={token}&wsfunction=core_user_update_users&users[0][id]={user.Users[0].Id}&users[0][suspended]=1&moodlewsrestformat=json");
                result = response.Result.Content.ReadAsStringAsync();
                result.Wait();
                return $"{username} suspended";
            }
            else
            {
                response = client.GetAsync($"http://localhost/webservice/rest/server.php?wstoken={token}&wsfunction=core_user_update_users&users[0][id]={user.Users[0].Id}&users[0][suspended]=0&moodlewsrestformat=json");
                result = response.Result.Content.ReadAsStringAsync();
                result.Wait();
                return $"{username} unsuspended";
            }

            


        }
    }
}

using Microsoft.AspNetCore.Mvc;
using TodoApi.Models;
namespace TodoApi.Controllers
{
    [ApiController]
    public class UserController
    {
        public string token = "31d43f030eb949c3dbb1d3bfc4c9d91e";
        
        [Route("api/v1/createuser/{firstName}/{lastName}/{email}/{password}")]
        [HttpGet]
        public string CreateUser(string firstName,string lastName,string email)
        {
            string token = "31d43f030eb949c3dbb1d3bfc4c9d91e";
            var newUser = new Cursist();
            newUser.Voornaam = firstName;
            newUser.Achternaam = lastName;
            newUser.Email = email;

            //newUser.Password = password;

            string variables = $"&users[0][firstname]={newUser.Voornaam}&users[0][lastname]={newUser.Achternaam}&users[0][email]={newUser.Email}&users[0]";
            var client = new HttpClient();
            client.GetAsync($"http://localhost/webservice/rest/server.php?wstoken={token}&wsfunction=core_user_create_users{variables}&moodlewsrestformat=json");
            return $"{newUser.Password}";
        }
    }
}

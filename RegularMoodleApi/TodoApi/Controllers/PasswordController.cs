using Microsoft.AspNetCore.Mvc;
using TodoApi.Models;

namespace TodoApi.Controllers
{
    [ApiController]
    public class PasswordController: PasswordReset
    {
        private string token = "4aedb8e394c3ac61c042c0753e4d5c57";

        [Route("api/passwordreset/{email}")]
        [HttpGet]
        public string ResetPassword(string email,string username)
        {

            //  core_auth_request_password_reset

            var passwordreset = new PasswordReset
            {
                Email = @$"{email}",
                Username = username
            };

            string ems = $"&email={passwordreset.Email}";

            HttpClient client = new HttpClient();

            var a = "";

            try
            {
                var result =client.GetAsync(@$"https://moodlev4.cvoantwerpen.org/webservice/rest/server.php?wstoken={token}&wsfunction=core_auth_request_password{ems}&moodlewsrestformat=json");
                var log = result.Result.Content;
                a = log.ReadAsStringAsync().Result;
            }
            catch(Exception e)
            {
                return e.Message;
            }

            return @$"Het wachtwoord van {email} is gereset\n {a} ";
        }
    }
}

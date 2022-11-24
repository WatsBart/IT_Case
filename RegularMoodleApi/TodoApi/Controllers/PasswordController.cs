using Microsoft.AspNetCore.Mvc;
using TodoApi.Models;

namespace TodoApi.Controllers
{
    [ApiController]
    public class PasswordController: PasswordReset
    {
        private string token = "1d5ecc3c89bff085d3fb31ba1db0c03a";

        [Route("api/passwordreset/{email}")]
        [HttpGet]
        public string ResetPassword(string email)
        {

            //  core_auth_request_password_reset

            var passwordreset = new PasswordReset
            {
                Email = email
            };

            string ems = $"$email={passwordreset.Email}";

            HttpClient client = new HttpClient();

            try
            {
                client.GetAsync($"http://localhost/webservice/rest/server.php?wstoken={token}&wsfunction=core_auth_request_password{ems}&moodlewsrestformat=json");
            }
            catch(Exception e)
            {
                return e.Message;
            }

            return $"Het wachtwoord van {email} is gereset";
        }
    }
}

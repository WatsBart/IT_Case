using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System;
using System.Net;
using TodoApi.Models;
namespace TodoApi.Controllers
{
    [ApiController]
    public class UserController
    {

        public string token = "4aedb8e394c3ac61c042c0753e4d5c57";

        [Route(@"api/createuser")]
        [HttpPost]
        public string CreateUsers(string firstName, string lastName, string email, string password, string username)
        {
            var newUser = new Cursist()
            {
                Voornaam = firstName,
                Achternaam = lastName,
                Email = $@"{email}",
                Password = password,
                Username = username
            };
            var json = JsonConvert.SerializeObject(newUser);
            var data = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
            using (var client = new HttpClient())
            {
                var response = client.PostAsync($"https://moodlev4.cvoantwerpen.org/webservice/rest/server.php?wstoken={token}&wsfunction=core_user_create_users&users[0][username]={newUser.Username}&users[0][password]={newUser.Password}&users[0][firstname]={newUser.Voornaam}&users[0][lastname]={newUser.Achternaam}&users[0][email]={newUser.Email}&moodlewsrestformat=json", data);
                var result = response.Result.Content.ReadAsStringAsync();
                result.Wait();
                return $"Function complete: {firstName} {lastName} {username} {password} {email}\n{result.Result}";
            }



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

        // Add user to course and Remove user from course methods
        [Route(@"api/Enrolluser")]
        [HttpPost]
        public string Enrolluser(int roleid, int userid, int courseid)
        {
            var enroles = new Enroll()
            {
                Roleid = roleid,
                Userid = userid,
                Courseid = courseid
            };
            string enr = $"&enrolments[0][roleid]={enroles.Roleid}";
            string enu = $"&enrolments[0][userid]={enroles.Userid}";
            string enc = $"&enrolments[0][courseid]={enroles.Courseid}";


            HttpClient client = new HttpClient();
            try
            {
                client.GetAsync($"https://moodlev4.cvoantwerpen.org/webservice/rest/server.php?wstoken={token}&wsfunction=enrol_manual_enrol_users{enr + enu + enc}&moodlewsrestformat=json");
            }
            catch (Exception ex)
            {
                return ex.Message;
            }
            return "User enrolled";
        }
        [Route(@"api/removerol")]
        [HttpPost]
        public string Unassignrole(int roleid, int userid,int instanceid,string contextlevel)
        {
            var enroles = new Enroll()
            {
                Roleid=roleid,
                Userid=userid,
                Instanceid = instanceid,
                Contextlevel = contextlevel
            };
            string rid = $"&unassignments[0][roleid]={enroles.Roleid}";
            string uid = $"&unassignments[0][userid]={enroles.Userid}";
            string inid = $"&unassignments[0][instanceid]={enroles.Instanceid}";
            string cid = $"&unassignments[0][contextlevel]={enroles.Contextlevel}";



            HttpClient client = new HttpClient();
            try
            {
                client.GetAsync($"https://moodlev4.cvoantwerpen.org/webservice/rest/server.php?wstoken={token}&wsfunction=core_role_unassign_roles{rid +uid+ inid+cid}&moodlewsrestformat=json");
            }
            catch (Exception ex)
            {
                return ex.Message;
            }
            return "the user rol has been unassigned ";
        }
        // Group methods


       
    }
}

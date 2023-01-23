using Microsoft.AspNetCore.Mvc;
using System.Net;
using Newtonsoft.Json;
using TodoApi.Models;
using Microsoft.AspNetCore.Authorization;

namespace TodoApi.Controllers
{
    

    [ApiController]
    public class CourseController :Controller
    {
        public string token = "4aedb8e394c3ac61c042c0753e4d5c57";
        //  API main page

        [Route("api/")]
        [HttpGet]
        public string MainPage()
        {
            return "Dit is de hoofd route van de api, je moet achter deze link nog de nodige info meegeven.";
        }

        //  CURSUS TOEVOEGEN

        [Route("api/addcourse")]
        [HttpPost]
        //[Authorize(Roles = "admin")]
        public string CreateCourse(string fullname, string shortname, long categoryid, string courseid)
        {
            var course = new Course()
            {
                fullname = fullname,
                shortname = shortname,
                categoryid = (int)categoryid,
                idnumber = courseid,
            };
            
            var json = JsonConvert.SerializeObject(course);
            var data = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
            using(HttpClient client = new HttpClient()) 
            {
                var response = client.PostAsync($"https://moodlev4.cvoantwerpen.org/webservice/rest/server.php?wstoken={token}&wsfunction=core_course_create_courses&courses[0][fullname]={course.fullname}&courses[0][shortname]={course.shortname}&courses[0][categoryid]={course.categoryid}&courses[0][idnumber]={course.idnumber}&moodlewsrestformat=json",data);
                var result = response.Result.Content.ReadAsStringAsync();
                result.Wait();
                return $"Je hebt de volgende cursus toegevoegd: {course.fullname} ({course.shortname})\n{result.Result}";
            };
        }

        //  CURSUS VERWIJDEREN (ID via course_get verkrijgen)

        [Route("api/deletecourse/{shortname}")]
        [HttpGet]
        public string DeleteCourse(string shortname)
        {
            HttpClient client = new HttpClient();
            var responseTask = client.GetAsync($"https://moodlev4.cvoantwerpen.org/webservice/rest/server.php?wstoken={token}&wsfunction=core_course_get_courses&moodlewsrestformat=json");
            var result = responseTask.Result;
            var log = result.Content.ReadAsStringAsync();
            log.Wait();
            Course[] course = JsonConvert.DeserializeObject<Course[]>(log.Result);
            long id = -1;
            for (int i = 0; i < course.Length; i++)
            {
                if (course[i].shortname == shortname)
                {
                    id = (long)course[i].id;
                    string param = $"&courseids[0]={id}";
                    responseTask = client.GetAsync($"https://moodlev4.cvoantwerpen.org/webservice/rest/server.php?wstoken={token}&wsfunction=core_course_delete_courses{param}&moodlewsrestformat=json");
                    responseTask.Wait();
                    return $"Je hebt de cursus {course[i].fullname} verwijderd. \nStatuscode: {responseTask.Result.StatusCode}";
                }
            }
            return $"Cursus niet gevonden.";
            
        }

        [Route("api/getcourses")]
        [HttpGet]
        public string GetCourses()
        {
            HttpClient client = new HttpClient();
            var responseTask = client.GetAsync($"https://moodlev4.cvoantwerpen.org/webservice/rest/server.php?wstoken={token}&wsfunction=core_course_get_courses&moodlewsrestformat=json");
            var result = responseTask.Result;
            var log = result.Content.ReadAsStringAsync();
            log.Wait();
            Course[] course = JsonConvert.DeserializeObject<Course[]>($@"{log.Result}");
            string returnvalues = "";
            for (int i = 1; i < course.Length; i++)
            {
                returnvalues += $"Fullname: {course[i].fullname}\nShortname:{course[i].shortname}\nId: {course[i].id}\n\n";
            }

            return returnvalues;


        }
    }
}

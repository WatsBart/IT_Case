using Microsoft.AspNetCore.Mvc;
using System.Net;
using Newtonsoft.Json;
using TodoApi.Models;

namespace TodoApi.Controllers
{
    [ApiController]
    public class CourseController :Controller
    {
        public string token = "1d5ecc3c89bff085d3fb31ba1db0c03a";
        //  API main page

        [Route("api/")]
        [HttpGet]
        public string MainPage()
        {
            return "Dit is de hoofd route van de api, je moet achter deze link nog de nodige info meegeven.";
        }

        //  CURSUS TOEVOEGEN

        [Route("api/addcourse/{fullname}/{shortname}/{categoryid}/{courseid}")]
        [HttpGet]
        public string CreateCourse(string fullname, string shortname, long categoryid, string courseid)
        {
            var course = new Course();

            course.Fullname = fullname;
            course.Shortname = shortname;
            course.Categoryid = categoryid;
            course.Idnumber = courseid;

            string fns = $"&courses[0][fullname]={course.Fullname}";
            string sns = $"&courses[0][shortname]={course.Shortname}";
            string cis = $"&courses[0][categoryid]={course.Categoryid}";
            string cis2 = $"&courses[0][idnumber]={course.Idnumber}";
            //string desc = $"&courses[0][summary]={description}";
            //string sumformat = $"&courses[0][summaryformat]={1}";
            

            HttpClient client = new HttpClient();
            try
            {
                client.GetAsync($"http://localhost/webservice/rest/server.php?wstoken={token}&wsfunction=core_course_create_courses{sns + fns + cis+cis2}&moodlewsrestformat=json");
            }
            catch(Exception ex)
            {
                return ex.Message;
            }
            return $"Je hebt de volgende cursus toegevoegd: {course.Fullname} ({course.Shortname})";
        }

        //  CURSUS VERWIJDEREN (ID via course_get verkrijgen)

        [Route("api/deletecourse/{id}")]
        [HttpGet]
        public string DeleteCourse(int id)
        {
            string ids = $"&courseids[0]={id}";

            HttpClient client = new HttpClient();
            client.GetAsync($"http://localhost/webservice/rest/server.php?wstoken={token}&wsfunction=core_course_delete_courses{ids}&moodlewsrestformat=json");

            return $"Je hebt de cursus met id-nummer: {id} verwijderd.";
        }

        [Route("api/getcourses")]
        [HttpGet]
        public string GetCourses()
        {
            HttpClient client = new HttpClient();
            var responseTask = client.GetAsync($"http://localhost/webservice/rest/server.php?wstoken={token}&wsfunction=core_course_get_courses&moodlewsrestformat=json");
            var result = responseTask.Result;
            var log = result.Content.ReadAsStringAsync();
            log.Wait();
            Course[] course = JsonConvert.DeserializeObject<Course[]>(log.Result);
            string returnvalues = "";
            for (int i = 1; i < course.Length; i++)
            {
                returnvalues += $"Fullname: {course[i].Fullname}\nShortname:{course[i].Shortname}\nId: {course[i].Id}\n\n";
            }

            return returnvalues;


        }
    }
}

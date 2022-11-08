using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System.Net;
using System.Text;
using TodoApi.Models;

namespace TodoApi.Controllers
{
    [ApiController]
    public class CourseController
    {
        private string token = "31d43f030eb949c3dbb1d3bfc4c9d91e";
        //  API main page

        [Route("api/v1")]
        [HttpGet]
        public String MainPage()
        {
            return "Dit is de hoofd route van de api, je moet achter deze link nog de nodige info meegeven.";
        }

        //  CURSUS TOEVOEGEN

        [Route("api/v1/addcourse/{fullname}/{shortname}/{categoryid}/{courseid}")]
        [HttpGet]
        public String CreateCourse(string fullname, string shortname, int categoryid, int courseid = -1)
        {
            var course = new Course()
            {
                Fullname = fullname,
                Shortname = shortname,
                CategoryId = categoryid,
                CourseId = courseid
            };

            string fns = $"&courses[0][fullname]={course.Fullname}";
            string sns = $"&courses[0][shortname]={course.Shortname}";
            string cis = $"&courses[0][categoryid]={course.CategoryId}";
            string cis2 = $"&courses[0][idnumber]={course.CourseId}";

            //string desc = $"&courses[0][summary]={description}";
            //string sumformat = $"&courses[0][summaryformat]={1}";
            

            HttpClient client = new HttpClient();
            try
            {
                client.GetAsync($"http://localhost/webservice/rest/server.php?wstoken={token}&wsfunction=core_course_create_courses{sns+fns+cis+cis2}&moodlewsrestformat=json");
            }
            catch(Exception ex)
            {
                return ex.Message;
            }
            return $"Je hebt de volgende cursus toegevoegd: {course.Fullname} ({course.Shortname})";
        }

        //  CURSUS TOEVOEGEN MET POST

        [Route("api/v1/addcoursepost/{fullname}/{shortname}/{categoryid}/{courseid}")]
        [HttpPost]
        public String CreateCourseWithPost(string fullname, string shortname, int categoryid, int courseid = -1)
        {
            var course = new Course()
            {
                Fullname = fullname,
                Shortname = shortname,
                CategoryId = categoryid,
                CourseId = courseid
            };


            using (var client = new HttpClient())
            {
                var endpoint = new Uri("http://localhost/webservice/rest/server.php?wstoken={token}&wsfunction=core_course_create_courses&moodlewsrestformat=json");
                var courseJson = JsonConvert.SerializeObject(course);
                var payload = new StringContent(courseJson, Encoding.UTF8, "application/json");

                var result = client.PostAsync(endpoint, payload).Result.Content.ReadAsStringAsync().Result;

                return result;
            }
        }


        //  CURSUS VERWIJDEREN (ID via course_get verkrijgen)

        [Route("api/v1/deletecourse/{id}")]
        [HttpGet]
        public String DeleteCourse(int id)
        {
            string ids = $"&courseids[0]={id}";

            HttpClient client = new HttpClient();
            client.GetAsync($"http://localhost/webservice/rest/server.php?wstoken={token}&wsfunction=core_course_delete_courses{ids}&moodlewsrestformat=json");

            return $"Je hebt de cursus met id-nummer: {id} verwijderd.";
        }

        [Route("api/v1/getcourses")]
        [HttpGet]
        public String GetCourses()
        {
            HttpClient client = new HttpClient();
            var responseTask = client.GetAsync($"http://localhost/webservice/rest/server.php?wstoken={token}&wsfunction=core_course_get_courses&moodlewsrestformat=json");
            var result = responseTask.Result;
            var log = result.Content.ReadAsStringAsync();
            log.Wait();
            return log.Result;


        }
    }
}

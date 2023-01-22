using Microsoft.AspNetCore.Mvc;
using TodoApi.Models;

namespace TodoApi.Controllers
{

    [ApiController]
    public class GroupController
    {
        public string token = "4aedb8e394c3ac61c042c0753e4d5c57";


        // Create groups
        [Route(@"api/Creategroup")]
        [HttpPost]
        public string CreateGroup(int courseid,string name,string description,int descriptionformat)
        {
            var group = new Group()
            {
                
                Courseid = courseid,
                Name = name,
                Description = description,
                Descriptionformat = descriptionformat
            };
            string enr = $"&groups[0][courseid={group.Courseid}";
            string enu = $"&groups[0][name]={group.Name}";
            string enc = $"&groups[0][description]={group.Description}";
            string dformat = $"&groups[0][descriptionformat]={group.Descriptionformat}";


            HttpClient client = new HttpClient();
            try
            {
                client.GetAsync($"https://moodlev4.cvoantwerpen.org/webservice/rest/server.php?wstoken={token}&wsfunction=core_group_create_groups{enr + enu + enc+dformat}&moodlewsrestformat=json");
            }
            catch (Exception ex)
            {
                return ex.Message;
            }
            return "Group has been created";
        }

        [Route(@"api/addusertogroup")]
        [HttpPost]

        public string Addtogroup(int groupid, int userid)
        {
            var group = new Group()
            {
                Groupid = groupid,
                Userid = userid
            };
            string gid = $"&members[0][groupid]={group.Groupid}";
            string uid = $"&members[0][userid]={group.Userid}";


            HttpClient client = new HttpClient();
            try
            {
                client.GetAsync($"https://moodlev4.cvoantwerpen.org/webservice/rest/server.php?wstoken={token}&wsfunction=core_group_add_group_members{gid + uid}&moodlewsrestformat=json");
            }
            catch (Exception ex)
            {
                return ex.Message;
            }
            return "user added to the group";

        }
        [Route(@"api/Removeuserfromgroup")]
        [HttpPost]

        public string Removefromthegroup(int groupid, int userid)
        {
            var group = new Group()
            {
                Groupid = groupid,
                Userid = userid
            };
            string gid = $"&members[0][groupid]={group.Userid}";
            string uid = $"&members[0][userid]={group.Userid}";


            HttpClient client = new HttpClient();
            try
            {
                client.GetAsync($"https://moodlev4.cvoantwerpen.org/webservice/rest/server.php?wstoken={token}&wsfunction=core_group_delete_group_members{gid + uid}&moodlewsrestformat=json");
            }
            catch (Exception ex)
            {
                return ex.Message;
            }
            return "User has been removed from the group";
        }
    }
}

namespace TodoApi.Models
{
    public class ApiUserConstants
    {
        public static List<ApiUser> ApiUsers = new List<ApiUser>()
        {
            new ApiUser(){ UserName = "admin",EmailAddress="jeroen.folla@hotmail.com", Password="password", GiveName="admin", Role="admin"},
            new ApiUser(){ UserName = "service",EmailAddress="jeroen.folla@hotmail.com", Password="password", GiveName="admin", Role="service"},
            new ApiUser(){ UserName = "app",EmailAddress="jeroen.folla@hotmail.com", Password="password", GiveName="admin", Role="app"},
        };
    }
}

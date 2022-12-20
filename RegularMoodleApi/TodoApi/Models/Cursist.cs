namespace TodoApi.Models;
using System;
    using System.Collections.Generic;

    using System.Globalization;
    
    using Newtonsoft.Json;
    using Newtonsoft.Json.Converters;

public class Cursist
{
    public string Voornaam { get; set; }
    public string Achternaam { get; set; }  
    public string Email { get; set; }
    public string Password { get; set; }
    public string Username { get; set; }
    
}
public partial class User
{
    public UserElement[] Users { get; set; }
    public object[] Warnings { get; set; }
}

public partial class UserElement
{
    public long? Id { get; set; }
    public string Username { get; set; }
    public string Firstname { get; set; }
    public string Lastname { get; set; }
    public string Fullname { get; set; }
    public string Email { get; set; }
    public string Department { get; set; }
    public long? Firstaccess { get; set; }
    public long? Lastaccess { get; set; }
    public string Auth { get; set; }
    public bool? Suspended { get; set; }
    public bool? Confirmed { get; set; }
    public string Lang { get; set; }
    public string Theme { get; set; }
    public long? Timezone { get; set; }
    public long? Mailformat { get; set; }
    public string Description { get; set; }
    public long? Descriptionformat { get; set; }
    public Uri Profileimageurlsmall { get; set; }
    public Uri Profileimageurl { get; set; }
}


namespace VacancySave.Models;

public class Response
{
    public int StatusCode { get; set; }
    public string Body { get; set; }

    public Response(int statusCode, string body)
    {
        StatusCode = statusCode;
        Body = body;
    }

}

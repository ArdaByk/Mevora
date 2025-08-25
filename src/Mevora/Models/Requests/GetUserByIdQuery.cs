namespace Mevora.Models.Requests;

public class GetUserByIdQuery : IRequest<string> 
{ 
    public string Name { get; set; } 
}

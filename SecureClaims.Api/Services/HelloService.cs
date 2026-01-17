namespace SecureClaims.Api.Services;
public interface IHelloService
{
    public string GetMessage();
}
public class HelloService: IHelloService
{
    public string GetMessage()
    {
        return "Hello from Secure Claims API";
    }
}
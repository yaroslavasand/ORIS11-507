namespace MyHttpServer.Framework;

[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class HttpController : Attribute
{
    public string Name { get; }

    public HttpController(string name)
    {
        Name = name;
    }
}

[AttributeUsage(AttributeTargets.Method, Inherited = false)]
public sealed class HttpGET : Attribute
{
    public string Route { get; }

    public HttpGET(string route)
    {
        Route = route;
    }
}

[AttributeUsage(AttributeTargets.Method, Inherited = false)]
public sealed class HttpPOST : Attribute
{
    public string Route { get; }

    public HttpPOST(string route)
    {
        Route = route;
    }
}
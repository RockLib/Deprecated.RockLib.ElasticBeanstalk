AWS now supports retrieving the values of Environment Properties for .NET Core applications. [Environment Variables with .NET Core and Elastic Beanstalk](https://aws.amazon.com/blogs/developer/environment-variables-with-net-core-and-elastic-beanstalk/)

# [DEPRECATED] RockLib.ElasticBeanstalk [![Build status](https://ci.appveyor.com/api/projects/status/8cx54p9g01xmow6a?svg=true)](https://ci.appveyor.com/project/RockLib/rocklib-elasticbeanstalk)

```powershell
PM> Install-Package RockLib.ElasticBeanstalk
```

Elastic Beanstalk does not provide a mechanism for .NET Core applications to retrieve the values of its Environment Properties. The `RockLib.ElasticBeanstalk` package implements a workaround that maps Environment Properties to the current process's environment variables.

Call the `EnvironmentProperties.MapToEnvironmentVariables` method at the very beginning of your application. In ASP.NET Core applications, it is important to do this *before* creating the `IWebHost`, since the default configuration reads environment variables.

```c#
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using RockLib.ElasticBeanstalk

class Program
{
    static void Main(string[] args)
    {
        EnvironmentProperties.MapToEnvironmentVariables();
        
        BuildWebHost(args).Run();
    }

    public static IWebHost BuildWebHost(string[] args) =>
        WebHost.CreateDefaultBuilder(args)
            .UseStartup<Startup>()
            .Build();
}
```

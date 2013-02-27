# EmbeddedResourceVirtualPathProvider #

Load your views and assets from references assemblies. To get started, install into your ASP.NET web application via nuget:

> Install-Package EmbeddedResourceVirtualPathProvider

This will add some code into App_Start registering the provider.

Move views and assets into other assemblies, maintaining folder structure. e.g.

`/MyAspNetApp/Views/Thing/Thing.cshtml -> /ThingComponent/Views/Thing/Thing.cshtml`

And set the the files BuildAction as EmbbeddedResource. Make sure your assembly is referenced, and you're done!

By default, all assemblies in the appdomain are scanned. You can restrict this in `App_Start\RegisterVirtualPathProvider.cs` file. You can also map assemblies to their location on disk, so they ca nbe refereshed when you edit the files during development.

## Dynamic Content Routing ##

You can set up rules determining the order to check assemblies for resources, letting you (for example) have different view assemblies for different hostnames.

Please check out my other projects! 

Cheers, Harry


MIT Licenced



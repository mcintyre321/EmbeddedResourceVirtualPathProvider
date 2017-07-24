# EmbeddedResourceVirtualPathProvider #

A custom VirtualPathProvider for IIS - load views and assets from Embedded Resources in referenced assemblies . To get started, install into your ASP.NET web application via nuget:

> Install-Package EmbeddedResourceVirtualPathProvider

This will add some code into `App_Start` registering the provider.

Move views and assets into other assemblies, maintaining folder structure. e.g.

`/MyAspNetApp/Views/Thing/Thing.cshtml -> /ThingComponent/Views/Thing/Thing.cshtml`

And set the the files BuildAction as EmbbeddedResource. Make sure your assembly is referenced, and you're done!

By default, all assemblies in the appdomain are scanned. You can restrict this in `App_Start\EmbeddedResourceVirtualPathProviderStart.cs` file. You can also map assemblies to their location on disk, so they ca nbe refereshed when you edit the files during development.

There is some help at https://github.com/mcintyre321/EmbeddedResourceVirtualPathProvider/wiki/Help


## Install Actions
On installing the package we'll get:

* `App_Start/EmbeddedResourceVirtualPathProviderStart.cs` file created - it contains provider registration via WebActivator
* section `system.webServer`/`handlers` in web.config will be updated with:

```
<add verb="GET" path="*.js" name="Static for js" type="System.Web.StaticFileHandler" />
<add verb="GET" path="*.css" name="Static for css" type="System.Web.StaticFileHandler" />
<add verb="GET" path="*.png" name="Static for png" type="System.Web.StaticFileHandler" />
<add verb="GET" path="*.jpg" name="Static for jpg" type="System.Web.StaticFileHandler" />
```

* WebActivatorEx package will be installed

If you don't want all these side effects then use package `EmbeddedResourceVirtualPathProvider.Core` which contains only an assembly.


## Dynamic Content Routing ##

You can set up rules determining the order to check assemblies for resources, letting you (for example) have different view assemblies for different hostnames.

Please check out my other projects! 

Cheers, Harry

@mcintyre321

MIT Licenced




[![Bitdeli Badge](https://d2weczhvl823v0.cloudfront.net/mcintyre321/embeddedresourcevirtualpathprovider/trend.png)](https://bitdeli.com/free "Bitdeli Badge")


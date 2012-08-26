using System;
using System.Web.Http;

namespace Code2012SurveyBot.WebApp
{
    public class WebApiApplication : System.Web.HttpApplication
    {
        protected void Application_Start()
        {
            GlobalConfiguration.Configuration.Routes.MapHttpRoute(
                name: "DefaultApi",
                routeTemplate: "{action}",
                defaults: new { controller = "Bot", id = RouteParameter.Optional }
            );
        }
    }
}
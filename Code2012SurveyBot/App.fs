namespace Code2012SurveyBot
open System
open System.Web.Http

type Defaults = {controller:string; id:RouteParameter}

type App() =
    inherit System.Web.HttpApplication()

    member this.Application_Start() =
        GlobalConfiguration.Configuration
            .Routes
            .MapHttpRoute("DefaultApi", "{action}", {controller="Bot"; id=RouteParameter.Optional})
            |> ignore
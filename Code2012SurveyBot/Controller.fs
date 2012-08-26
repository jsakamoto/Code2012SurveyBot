namespace Code2012SurveyBot
open System.Web.Http
open System.Configuration

type BotController() = 
    inherit ApiController()
    
    [<HttpGet>]
    member this.Ping() = 
        Bot.Tweet()
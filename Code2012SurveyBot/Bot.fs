module Code2012SurveyBot.Bot
open System.Configuration
open System.Linq
open System.Net
open System.Text
open Newtonsoft.Json
open Twitterizer

    type Reply = {Id:int; Free_Comment:string}

    let replies = 
        let webClient = new WebClient()
        webClient.Encoding <- Encoding.UTF8
        "http://code-survey.herokuapp.com/surveys.json"
            |> webClient.DownloadString
            |> fun x -> JsonConvert.DeserializeObject<Reply[]>(x)
            |> Array.filter (fun x -> System.String.IsNullOrEmpty(x.Free_Comment) = false)
            |> Array.sortBy (fun x -> x.Id) 

    let Tweet () = 
        //replies

        //let rep = replies.Last()

        let key = ConfigurationManager.AppSettings.["twitterKey"]
        let tokens = JsonConvert.DeserializeObject<OAuthTokens>(key)
        TwitterStatus.Update(tokens, "AppHarbor 上からもツイートできるだろうか。") |> ignore


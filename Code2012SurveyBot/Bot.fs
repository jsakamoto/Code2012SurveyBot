module Code2012SurveyBot.Bot
open System.Configuration
open System.Linq
open System.Net
open System.Text
open Newtonsoft.Json
open Twitterizer
open Raven.Client.Document

    type Reply = {Id:int; Free_Comment:string}

    let replies = 
        let webClient = new WebClient()
        webClient.Encoding <- Encoding.UTF8
        "http://code-survey.herokuapp.com/surveys.json"
            |> webClient.DownloadString
            |> fun x -> JsonConvert.DeserializeObject<Reply[]>(x)
            |> Array.filter (fun x -> System.String.IsNullOrEmpty(x.Free_Comment) = false)
            |> Array.sortBy (fun x -> x.Id) 

    type RavenConnSetting = {Url:string; ApiKey:string}

    let Tweet () = 
        
//        let connSetting = JsonConvert.DeserializeObject<RavenConnSetting>(ConfigurationManager.AppSettings.["RAVENHQ_CONNECTION_STRING"])
//        use docStore = new DocumentStore()
//        docStore.Url <- connSetting.Url
//        docStore.ApiKey <- connSetting.ApiKey
//        use session = docStore.Initialize().OpenSession()
//        
//        let lastTweets = session.Query<Reply>().ToArray()
//        let nextTweetTo = 
//            if lastTweets.Any() then
//                let nexts = replies.Where(fun x -> x.Id > lastTweets.First().Id)
//                if nexts.Any() then nexts.First() else replies.First()
//            else
//                replies.First()
//
//        nextTweetTo


        let key = ConfigurationManager.AppSettings.["twitterKey"]
        let tokens = JsonConvert.DeserializeObject<OAuthTokens>(key)
        TwitterStatus.Update(tokens, "AppHarbor 上からもツイートできるだろうか。") |> ignore


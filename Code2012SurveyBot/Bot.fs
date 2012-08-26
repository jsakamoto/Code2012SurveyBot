module Code2012SurveyBot.Bot
open System.Configuration
open System.Linq
open System.Net
open System.Text
open Newtonsoft.Json
open Twitterizer
open Raven.Client.Document

    type Reply = {Id:int; Free_Comment:string}

    let replies () = 
        let webClient = new WebClient()
        webClient.Encoding <- Encoding.UTF8
        "http://code-survey.herokuapp.com/surveys.json"
            |> webClient.DownloadString
            |> fun x -> JsonConvert.DeserializeObject<Reply[]>(x)
            |> Array.filter (fun x -> System.String.IsNullOrEmpty(x.Free_Comment) = false)
            |> Array.sortBy (fun x -> x.Id) 

    type LastTweet = {RepId:int}

    let Tweet () = 
        let reps = replies()
        let connSetting = ConfigurationManager.AppSettings.["RAVENHQ_CONNECTION_STRING"].Split(';').ToDictionary((fun (x:string) -> x.Split('=').First()), (fun (x:string) -> x.Split('=').Last()))
        use docStore = new DocumentStore()
        docStore.Url <- connSetting.["Url"]
        docStore.ApiKey <- connSetting.["ApiKey"]
        use session = docStore.Initialize().OpenSession()
        
        let lastTweets = session.Query<LastTweet>().ToArray()
        let nextTweetTo = 
            if lastTweets.Any() then
                let lastTweet = lastTweets.First()
                session.Delete(lastTweet)
                let nexts = reps.Where(fun x -> x.Id > lastTweet.RepId).ToArray()
                if nexts.Any() then nexts.First() else reps.First()
            else
                reps.First()
        session.Store({RepId = nextTweetTo.Id})
        session.SaveChanges()

        let hashtag = ConfigurationManager.AppSettings.["hashtag"]
        let msg  = nextTweetTo.Free_Comment
        let msg' = msg.Substring(0, min (msg.Length) (140 - 1 - hashtag.Length)) + " " + hashtag

        let key = ConfigurationManager.AppSettings.["twitterKey"]
        let tokens = JsonConvert.DeserializeObject<OAuthTokens>(key)
        TwitterStatus.Update(tokens, msg') |> ignore




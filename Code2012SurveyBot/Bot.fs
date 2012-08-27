module Code2012SurveyBot.Bot
open System.Configuration
open System.Linq
open System.Net
open System.Text
open System.Text.RegularExpressions
open Newtonsoft.Json
open Twitterizer
open Raven.Client.Document

    type Reply = {Id:int; Free_Comment:string}

    /// Retrieve free commnets from "Code 2012 Survey" site.
    let replies () = 
        let webClient = new WebClient()
        webClient.Encoding <- Encoding.UTF8
        "http://code-survey.herokuapp.com/surveys.json"
            |> webClient.DownloadString
            |> fun x -> JsonConvert.DeserializeObject<Reply[]>(x)
            |> Array.filter (fun x -> System.String.IsNullOrEmpty(x.Free_Comment) = false)
            |> Array.sortBy (fun x -> x.Id) 

    type LastTweet = {RepId:int}

    /// Tweet free commnets of "Code 2012 Survey" sequencialy.
    let Tweet () = 

        // Connect to RavenDB and retrieve the reply id of last tweet.
        let connSetting = ConfigurationManager.AppSettings.["RAVENHQ_CONNECTION_STRING"].Split(';').ToDictionary((fun (x:string) -> x.Split('=').First()), (fun (x:string) -> x.Split('=').Last()))
        use docStore = new DocumentStore()
        docStore.Url <- connSetting.["Url"]
        docStore.ApiKey <- connSetting.["ApiKey"]
        use session = docStore.Initialize().OpenSession()
        let lastTweets = session.Query<LastTweet>().ToArray()

        // Chose the message to next tweet and store to RavenDB.
        let reps = replies()
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

        // Pre prpcess to tweet.(Appned hash tag, quote, cut off to 140 characters, etc.)
        let maxLenOfTweet = 140
        let hashtag = ConfigurationManager.AppSettings.["hashtag"]
        let lead = "..."
        let maxLenOfMsg = maxLenOfTweet - (hashtag + " \"\"").Length
        let msg  = Regex.Replace(nextTweetTo.Free_Comment, @"[ \t\r\n]", "")
        let msg' = if msg.Length > maxLenOfMsg 
                    then msg.Substring(0, maxLenOfMsg - lead.Length) + lead
                    else msg
        let fullmsg = "\"" + msg' + "\" " + hashtag

        // Tweet!
        let key = ConfigurationManager.AppSettings.["twitterKey"]
        let tokens = JsonConvert.DeserializeObject<OAuthTokens>(key)
        TwitterStatus.Update(tokens, fullmsg) |> ignore

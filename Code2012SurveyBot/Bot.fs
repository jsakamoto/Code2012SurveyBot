module Code2012SurveyBot.Bot
open System
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
            |> Array.filter (fun x -> String.IsNullOrEmpty(x.Free_Comment) = false)
            |> Array.sortBy (fun x -> x.Id) 

    type LastTweet = {RepId:int}

    let (|??) opt def =
        match opt with
        |Some(a) -> a
        |None -> def

    /// Tweet free commnets of "Code 2012 Survey" sequencialy.
    let Tweet () = 

        // Connect to RavenDB and retrieve the reply id of last tweet.
        let connSetting = ConfigurationManager.AppSettings.["RAVENHQ_CONNECTION_STRING"].Split(';').ToDictionary((fun (x:string) -> x.Split('=').First()), (fun (x:string) -> x.Split('=').Last()))
        use docStore = new DocumentStore()
        docStore.Url <- connSetting.["Url"]
        docStore.ApiKey <- connSetting.["ApiKey"]
        use session = docStore.Initialize().OpenSession()

        // Chose the message to next tweet and store to RavenDB.
        let lastTweet = session.Query<LastTweet>() |> Seq.tryFind (fun _-> true) |?? {RepId = -1}
        let reps = replies()
        let nextTweetTo = reps |> Seq.tryFind (fun x -> x.Id > lastTweet.RepId) |?? reps.First()

        if lastTweet.RepId <> -1 then session.Delete(lastTweet)
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

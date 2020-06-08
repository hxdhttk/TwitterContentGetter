#r "System.Net.Http.dll"

open System
open System.IO
open System.Net
open System.Net.Http
open System.Net.NetworkInformation

type TwitterConfig = {
    ThumbPath: string
    OrigPath: string
    OrigUrlTemplate: Printf.StringFormat<string -> string>
    Proxy: string
}

let parseConfig () = 
    let configPath = __SOURCE_DIRECTORY__ + @"\twitterConfig.txt"
    let lines = File.ReadAllLines(configPath)
    let parsedDict = 
        lines
        |> Seq.map (fun line -> 
            let lineArr = line.Split([| "=>" |], StringSplitOptions.RemoveEmptyEntries)
            lineArr.[0], lineArr.[1])
        |> dict
    {
        ThumbPath = parsedDict.["thumbPath"]
        OrigPath = parsedDict.["origPath"]
        OrigUrlTemplate = (Printf.StringFormat<string -> string>)parsedDict.["origUrlTemplate"]
        Proxy = parsedDict.["proxy"]
    }

let isNetworkAvailable () =
    NetworkInterface.GetIsNetworkAvailable()

let run() =
    if not <| isNetworkAvailable() then ()
    else
        let config = parseConfig()
        let thumbPath = config.ThumbPath
        let origPath = config.OrigPath
        let origUrlTemplate = config.OrigUrlTemplate
        let proxy = config.Proxy
        
        let origImgRequestParams = 
            thumbPath
            |> Directory.EnumerateFiles
            |> Seq.map (fun filePath ->
                let file = FileInfo filePath
                let fileName = file.Name.Substring(0, file.Name.Length - file.Extension.Length)
                filePath, fileName, sprintf origUrlTemplate fileName)
        
        use handler = new HttpClientHandler()
        handler.Proxy <- WebProxy(proxy)
        use httpClient = new HttpClient(handler)
        for origImgRequestParam in origImgRequestParams do
            let thumbFilePath, thumbFilename, origFileUrl = origImgRequestParam
            let origImagePath = sprintf @"%s\%s.jpg" origPath thumbFilename
            if File.Exists(origImagePath) then
                File.Delete(thumbFilePath)
            else
                try
                    let response = httpClient.GetByteArrayAsync(origFileUrl).Result
                    use imageFile = new FileStream(origImagePath, FileMode.OpenOrCreate)
                    imageFile.Write(response, 0, response.Length)
                    printfn "%s" origImagePath
                    File.Delete(thumbFilePath)
                with
                | _ -> ()

run()
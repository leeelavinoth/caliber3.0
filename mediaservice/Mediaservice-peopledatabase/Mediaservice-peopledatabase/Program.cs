using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web;

namespace Mediaservice_peopledatabase
{
    class Program
    {
        static void Main(string[] args)
        {
            // The code provided will print ‘Hello World’ to the console.
            // Press Ctrl+F5 (or go to Debug > Start Without Debugging) to run your app.
            Console.WriteLine("Sample video indexer..");
            //Console.ReadKey();

            // Go to http://aka.ms/dotnet-get-started-console to continue learning how to build a console app! 


            var processvideo = process();

            //var processvideo = Sample();

            Console.ReadKey();



        }








        public static int process()
        {
            int ret = 0;
            try
            {

                var apiUrl = "https://api.videoindexer.ai";
                var accountId = "97a838d9-1531-459e-a48e-80b2a9d2ddcd";
                var location = "trial";
                var apiKey = "491e71b1e3e849ee8cf1633ec6093261";

                System.Net.ServicePointManager.SecurityProtocol =
                    System.Net.ServicePointManager.SecurityProtocol | System.Net.SecurityProtocolType.Tls12;

                // create the http client
                var handler = new HttpClientHandler();
                handler.AllowAutoRedirect = false;
                var client = new HttpClient(handler);
                client.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", apiKey);

                // obtain account access token
                var accountAccessTokenRequestResult = client
                    .GetAsync($"{apiUrl}/auth/{location}/Accounts/{accountId}/AccessToken?allowEdit=true").Result;
                var accountAccessToken =
                    accountAccessTokenRequestResult.Content.ReadAsStringAsync().Result.Replace("\"", "");

                client.DefaultRequestHeaders.Remove("Ocp-Apim-Subscription-Key");

                // upload a video
                var content = new MultipartFormDataContent();
                Console.WriteLine("Uploading...");
                // get the video from URL
                var videoUrl = "https://peopledatabasestorage.blob.core.windows.net/video/onecportal.mp4"; // replace with the video URL

                // as an alternative to specifying video URL, you can upload a file.
                // remove the videoUrl parameter from the query string below and add the following lines:
                //FileStream video =File.OpenRead(Globals.VIDEOFILE_PATH);
                //byte[] buffer = new byte[video.Length];
                //video.Read(buffer, 0, buffer.Length);
                //content.Add(new ByteArrayContent(buffer));

                var uploadRequestResult = client
                    .PostAsync(
                        $"{apiUrl}/{location}/Accounts/{accountId}/Videos?accessToken={accountAccessToken}&name=some_name&description=some_description&privacy=private&partition=some_partition&videoUrl={videoUrl}",
                        content).Result;
                var uploadResult = uploadRequestResult.Content.ReadAsStringAsync().Result;

                // get the video id from the upload result
                var videoId = JsonConvert.DeserializeObject<dynamic>(uploadResult)["id"];
                Console.WriteLine("Uploaded");
                Console.WriteLine("Video ID: " + videoId);

                // obtain video access token
                client.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", apiKey);
                var videoTokenRequestResult =
                    client.GetAsync(
                            $"{apiUrl}/auth/{location}/Accounts/{accountId}/Videos/{videoId}/AccessToken?allowEdit=true")
                        .Result;
                var videoAccessToken = videoTokenRequestResult.Content.ReadAsStringAsync().Result.Replace("\"", "");

                client.DefaultRequestHeaders.Remove("Ocp-Apim-Subscription-Key");

                // wait for the video index to finish
                while (true)
                {
                    Thread.Sleep(10000);

                    var videoGetIndexRequestResult = client
                        .GetAsync(
                            $"{apiUrl}/{location}/Accounts/{accountId}/Videos/{videoId}/Index?accessToken={videoAccessToken}&language=English")
                        .Result;
                    var videoGetIndexResult = videoGetIndexRequestResult.Content.ReadAsStringAsync().Result;

                    var processingState = JsonConvert.DeserializeObject<dynamic>(videoGetIndexResult)["state"];

                    Console.WriteLine("");
                    Console.WriteLine("State:");
                    Console.WriteLine(processingState);

                    // job is finished
                    if (processingState != "Uploaded" && processingState != "Processing")
                    {
                        Console.WriteLine("");
                        Console.WriteLine("Full JSON:");
                        Console.WriteLine(videoGetIndexResult);
                        break;
                    }
                }

                // search for the video
                var searchRequestResult =
                    client.GetAsync(
                            $"{apiUrl}/{location}/Accounts/{accountId}/Videos/Search?accessToken={accountAccessToken}&id={videoId}")
                        .Result;
                var searchResult = searchRequestResult.Content.ReadAsStringAsync().Result;
                Console.WriteLine("");
                Console.WriteLine("Search:");
                Console.WriteLine(searchResult);

                // get insights widget url
                var insightsWidgetRequestResult = client
                    .GetAsync(
                        $"{apiUrl}/{location}/Accounts/{accountId}/Videos/{videoId}/InsightsWidget?accessToken={videoAccessToken}&widgetType=Keywords&allowEdit=true")
                    .Result;
                var insightsWidgetLink = insightsWidgetRequestResult.Headers.Location;
                Console.WriteLine("Insights Widget url:");
                Console.WriteLine(insightsWidgetLink);

                // get player widget url
                var playerWidgetRequestResult = client
                    .GetAsync(
                        $"{apiUrl}/{location}/Accounts/{accountId}/Videos/{videoId}/PlayerWidget?accessToken={videoAccessToken}")
                    .Result;
                var playerWidgetLink = playerWidgetRequestResult.Headers.Location;
                Console.WriteLine("");
                Console.WriteLine("Player Widget url:");
                Console.WriteLine(playerWidgetLink);

            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                ret = 1;
            }

            return ret;
        }














        //

        static async Task Sample()
        {
            var apiUrl = "https://api.videoindexer.ai";
            var apiKey = "491e71b1e3e849ee8cf1633ec6093261"; // replace with API key taken from https://aka.ms/viapi

            System.Net.ServicePointManager.SecurityProtocol =
                System.Net.ServicePointManager.SecurityProtocol | System.Net.SecurityProtocolType.Tls12;

            // create the http client
            var handler = new HttpClientHandler();
            handler.AllowAutoRedirect = false;
            var client = new HttpClient(handler);
            client.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", apiKey);

            // obtain account information and access token
            string queryParams = CreateQueryString(
                new Dictionary<string, string>()
                {
            {"generateAccessTokens", "true"},
            {"allowEdit", "true"},
                });
            HttpResponseMessage result = await client.GetAsync($"{apiUrl}/auth/trial/Accounts?{queryParams}");
            var json = await result.Content.ReadAsStringAsync();
            var accounts = JsonConvert.DeserializeObject<AccountContractSlim[]>(json);

            // take the relevant account, here we simply take the first, 
            // you can also get the account via accounts.First(account => account.Id == <GUID>);
            var accountInfo = accounts.First();

            // we will use the access token from here on, no need for the apim key
            client.DefaultRequestHeaders.Remove("Ocp-Apim-Subscription-Key");

            // upload a video
            var content = new MultipartFormDataContent();
            Console.WriteLine("Uploading...");
            // get the video from URL
            var videoUrl = "https://peopledatabasestorage.blob.core.windows.net/video/onecportal.mp4"; // replace with the video URL

            // as an alternative to specifying video URL, you can upload a file.
            // remove the videoUrl parameter from the query params below and add the following lines:
            //FileStream video =File.OpenRead(Globals.VIDEOFILE_PATH);
            //byte[] buffer =new byte[video.Length];
            //video.Read(buffer, 0, buffer.Length);
            //content.Add(new ByteArrayContent(buffer));

            queryParams = CreateQueryString(
                new Dictionary<string, string>()
                {
            {"accessToken", accountInfo.AccessToken},
            {"name", "video_name"},
            {"description", "video_description"},
            {"privacy", "private"},
            {"partition", "partition"},
            {"videoUrl", videoUrl},
                });
            var uploadRequestResult = await client.PostAsync($"{apiUrl}/{accountInfo.Location}/Accounts/{accountInfo.Id}/Videos?{queryParams}", content);
            var uploadResult = await uploadRequestResult.Content.ReadAsStringAsync();

            // get the video ID from the upload result
            string videoId = JsonConvert.DeserializeObject<dynamic>(uploadResult)["id"];
            Console.WriteLine("Uploaded");
            Console.WriteLine("Video ID:");
            Console.WriteLine(videoId);

            // wait for the video index to finish
            while (true)
            {
                await Task.Delay(10000);

                queryParams = CreateQueryString(
                    new Dictionary<string, string>()
                    {
                {"accessToken", accountInfo.AccessToken},
                {"language", "English"},
                    });

                var videoGetIndexRequestResult = await client.GetAsync($"{apiUrl}/{accountInfo.Location}/Accounts/{accountInfo.Id}/Videos/{videoId}/Index?{queryParams}");
                var videoGetIndexResult = await videoGetIndexRequestResult.Content.ReadAsStringAsync();

                string processingState = JsonConvert.DeserializeObject<dynamic>(videoGetIndexResult)["state"];

                Console.WriteLine("");
                Console.WriteLine("State:");
                Console.WriteLine(processingState);

                // job is finished
                if (processingState != "Uploaded" && processingState != "Processing")
                {
                    Console.WriteLine("");
                    Console.WriteLine("Full JSON:");
                    Console.WriteLine(videoGetIndexResult);
                    break;
                }
            }

            // search for the video
            queryParams = CreateQueryString(
                new Dictionary<string, string>()
                {
            {"accessToken", accountInfo.AccessToken},
            {"id", videoId},
                });

            var searchRequestResult = await client.GetAsync($"{apiUrl}/{accountInfo.Location}/Accounts/{accountInfo.Id}/Videos/Search?{queryParams}");
            var searchResult = await searchRequestResult.Content.ReadAsStringAsync();
            Console.WriteLine("");
            Console.WriteLine("Search:");
            Console.WriteLine(searchResult);

            // Generate video access token (used for get widget calls)
            client.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", apiKey);
            var videoTokenRequestResult = await client.GetAsync($"{apiUrl}/auth/{accountInfo.Location}/Accounts/{accountInfo.Id}/Videos/{videoId}/AccessToken?allowEdit=true");
            var videoAccessToken = (await videoTokenRequestResult.Content.ReadAsStringAsync()).Replace("\"", "");
            client.DefaultRequestHeaders.Remove("Ocp-Apim-Subscription-Key");

            // get insights widget url
            queryParams = CreateQueryString(
                new Dictionary<string, string>()
                {
            {"accessToken", videoAccessToken},
            {"widgetType", "Keywords"},
            {"allowEdit", "true"},
                });
            var insightsWidgetRequestResult = await client.GetAsync($"{apiUrl}/{accountInfo.Location}/Accounts/{accountInfo.Id}/Videos/{videoId}/InsightsWidget?{queryParams}");
            var insightsWidgetLink = insightsWidgetRequestResult.Headers.Location;
            Console.WriteLine("Insights Widget url:");
            Console.WriteLine(insightsWidgetLink);

            // get player widget url
            queryParams = CreateQueryString(
                new Dictionary<string, string>()
                {
            {"accessToken", videoAccessToken},
                });
            var playerWidgetRequestResult = await client.GetAsync($"{apiUrl}/{accountInfo.Location}/Accounts/{accountInfo.Id}/Videos/{videoId}/PlayerWidget?{queryParams}");
            var playerWidgetLink = playerWidgetRequestResult.Headers.Location;
            Console.WriteLine("");
            Console.WriteLine("Player Widget url:");
            Console.WriteLine(playerWidgetLink);
            Console.WriteLine("\nPress Enter to exit...");
            String line = Console.ReadLine();
            if (line == "enter")
            {
                System.Environment.Exit(0);
            }

        }

        static string CreateQueryString(IDictionary<string, string> parameters)
        {
            var queryParameters = HttpUtility.ParseQueryString(string.Empty);
            foreach (var parameter in parameters)
            {
                queryParameters[parameter.Key] = parameter.Value;
            }

            return queryParameters.ToString();
        }

        public class AccountContractSlim
        {
            public Guid Id { get; set; }
            public string Name { get; set; }
            public string Location { get; set; }
            public string AccountType { get; set; }
            public string Url { get; set; }
            public string AccessToken { get; set; }
        }
        //

    }
}

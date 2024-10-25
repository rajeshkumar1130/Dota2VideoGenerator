
using SteamKit2.GC.Dota.Internal;
using SteamKit2;
using MetaDota.DotaReplay;
using System.Net;
using System.Text;
using System.Reflection;
using System;
using System.Runtime.InteropServices;
using System.Diagnostics;
using static System.Net.Mime.MediaTypeNames;
using MetaDota.Common;
using Interceptor;
using MetaDota.Common.Native;
using System.Drawing;
using MetaDota.config;
using static SteamKit2.GC.Dota.Internal.CDOTAMatchMetadata;
using SteamKit2.CDN;
using System.Reflection.Emit;
using static SteamKit2.GC.Dota.Internal.CMsgSDOAssert;


namespace ConsoleApp2
{
    class Program
    {
        public static Queue<string> requestQueue = new Queue<string>();
        public static MDConfig config;
        public static bool JobStart = false;

        static void test()
        {

        }
        public async static Task Main(string[] args)
        {

            JobStart = false;

            MDFile.Init();
            config = new MDConfig();

            MDTools.SleepCtr(true);

            //movie maker
            MDMovieMaker.Instance.Init();
            //launch mode
            if (args.Length > 0 && args[0] == "WebMode")
            {
                MDWebServer.Instance.Start();
            }
            else
            {
                //MDSever.Instance.Start();
                MDWebServer.Instance.Start();
            }


            //demo downloader
            MDReplayDownloader.Instance.Init();

            MDDotaClientRequestor.Instance.Init();

            MDDemoAnalystor.Instance.Init();

            //dota client
            DotaClient.Instance.Init(config.dotaPath);
            //DotaClient.Instance.Reconnect();

            if (!DotaClient.Instance.IsLogonDota)
            {
                Console.WriteLine("dota2 launch fail");
                //return;
            }

            Console.WriteLine("dota2 launch success! start check replay");


            CheckDownloadTask().Wait();

            Console.WriteLine("Dota Client DisConnected, Please ReConnect");
            Console.ReadLine();
        }


        static async Task CheckColor()
        {
            MDMovieMaker.Instance.Init();

            //Process[] processes = Process.GetProcessesByName("dota2");
            //NativeMethods.SwitchToThisWindow(processes[0].MainWindowHandle, true);
            //MDMovieMaker.Instance._input.SendText(@"\");
            Color color = Color.White;
            while (true)
            {
                await Task.Delay(1000);
                POINT cPOINT = MDTools.GetCursorPosition();
                color = MDTools.GetPixelColor(cPOINT.X, cPOINT.Y);
                Console.WriteLine($"x:{cPOINT.X} y:{cPOINT.Y} color : {color.ToArgb()}");
            }
        }

        static async Task CheckDownloadTask()
        {
            requestQueue = new Queue<string>();
            string[] requestArry = File.ReadAllLines(ClientParams.MATCH_REQUEST_FILE);
            Task.Run(() => Download(requestArry));
            for (int i = 0; i < requestArry.Length; i++)
            {
                requestQueue.Enqueue(requestArry[i]);
            }
            JobStart = true;
            string requestStr;
            while (true)
            {
                await Task.Delay(ClientParams.DOWNLOAD_CHECK_INTERVAL);
                if (requestQueue.Count > 0)
                {
                    File.WriteAllLines(ClientParams.MATCH_REQUEST_FILE, requestQueue.ToArray());
                    requestStr = requestQueue.Dequeue();
                    if (!string.IsNullOrEmpty(requestStr))
                    {
                        MDReplayGenerator.Generate(requestStr);
                        Console.WriteLine($"result : {MDReplayGenerator.GetResult(requestStr)}");
                    }

                }
            }
        }
        static void Download(string[] requestArry)
        {
            //Thread.Sleep(60000);
            try
            {
                for (int i = 1; i < requestArry.Length; i++)
                {
                    string[] splitArray = requestArry[i].Split('_');

                    MDReplayDownloader mDReplayDownloader = new MDReplayDownloader();
                   
                    mDReplayDownloader.Download(ulong.Parse(splitArray[0]), splitArray[1]);

                    //account_id = uint.Parse(splitArray[1]);
                    //mDReplayGenerator.heroName = splitArray[1];
                    //using HttpClient client = new HttpClient();

                    //string url = $"http://localhost:8000/getHighlights1?match_id={match_id}&hero_name={heroName}";

                    //try
                    //{
                    //    client.Timeout = TimeSpan.FromMinutes(30);
                    //    HttpResponseMessage response = client.GetAsync(url).Result;
                    //    response.EnsureSuccessStatusCode(); // Throws an exception for HTTP error responses
                    //}
                    //catch (HttpRequestException e)
                    //{
                    //    Console.WriteLine($"Request error: {e.Message}");
                    //    i--;
                    //}
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Request error: {ex.Message}");

            }

        }

    }
}


using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using SteamKit2.GC.Dota.Internal;
using static SteamKit2.Internal.CMsgDownloadRateStatistics;
using SteamKit2.CDN;
using System.Reflection.Emit;
using ConsoleApp2;

namespace MetaDota.DotaReplay
{
    class MDReplayDownloader : MDFactory<MDReplayDownloader>
    {
        public void GetPlayerSlot(MDReplayGenerator generator)
        {
            using HttpClient client = new HttpClient();
            string slot = "";
            string url = $"http://localhost:8000/getPlayerSlot?match_id={generator.match_id}&hero_name={generator.heroName}";

            try
            {
                client.Timeout = TimeSpan.FromMinutes(30);
                HttpResponseMessage response = client.GetAsync(url).Result;
                response.EnsureSuccessStatusCode(); // Throws an exception for HTTP error responses
                string responseBody = response.Content.ReadAsStringAsync().Result;
                slot = responseBody;
            }
            catch (HttpRequestException e)
            {
                Console.WriteLine($"Request error: {e.Message}");
            }

        }

        public override async Task Work(MDReplayGenerator generator)
        {
            string momentsPath = Path.Combine(ClientParams.DEMO_DIR, $"{generator.match_id}.json");
            string savePath = Path.Combine(ClientParams.DEMO_DIR, $"{generator.match_id}.dem");

            CMsgDOTAMatch match = generator.match;
            //Console.WriteLine("replay available: " + match.replay_state);
            //if (match == null)
            //{
            //    generator.eReplayGenerateResult = MDReplayGenerator.EReplayGenerateResult.NoMatch;
            //}
            //else if (false)
            //{
            //    generator.eReplayGenerateResult = MDReplayGenerator.EReplayGenerateResult.DemoUnavailable;
            //}
            //else 
            if (!File.Exists(momentsPath))
            {
                //if (!File.Exists(savePath))
                if (false)
                {
                    var cluster = match.cluster;
                    var match_id = match.match_id;
                    var replay_salt = match.replay_salt;
                    var _download_url = string.Format(ClientParams.DEMO_URL_STRING, cluster, match_id, replay_salt);
                    Console.WriteLine("demo url:" + _download_url);
                    var zip = string.Format(savePath + ".bz2", match_id);
                    if (!File.Exists(zip))
                    {
                        Console.WriteLine(zip + " downloading...");
                        //先下载到临时文件
                        var tmp = zip + ".tmp";
                        using (var web = new WebClient())
                        {
                            try
                            {
                                web.DownloadFileTaskAsync(_download_url, tmp).Wait();
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine("download err:" + ex.ToString());
                            }
                        }

                        File.Move(tmp, zip, true);
                        Console.WriteLine("demo download success");
                    }
                    //start unzip demo
                    using (Process zipProcess = new Process())
                    {
                        zipProcess.StartInfo.FileName = "7z.exe";
                        zipProcess.StartInfo.RedirectStandardInput = true;
                        zipProcess.StartInfo.UseShellExecute = false;
                        zipProcess.StartInfo.Arguments = $"x {zip} -o{ClientParams.DEMO_DIR} -aoa";
                        zipProcess.Start();
                        zipProcess.WaitForExit();
                    }
                    File.Delete(zip);
                    Console.WriteLine("download complete");

                    if (!File.Exists(savePath))
                    {
                        generator.eReplayGenerateResult = MDReplayGenerator.EReplayGenerateResult.DemoDownloadFail;
                    }
                }

                //if (match.players.Any(x => x.account_id == generator.account_id))
                //{
                //    heroName = DotaClient.Instance.GetHeroNameByID(match.players.FirstOrDefault(x => x.account_id == generator.account_id).hero_id);
                //}

                Download(generator.match_id, generator.heroName);

                if (!File.Exists(momentsPath))
                {
                    generator.eReplayGenerateResult = MDReplayGenerator.EReplayGenerateResult.DemoDownloadFail;
                }
            }

            //string savePath = Path.Combine(ClientParams.DEMO_DIR, $"{generator.match_id}.dem");
            //CMsgDOTAMatch match = generator.match;
            //Console.WriteLine("replay available: " + match.replay_state);
            //if (match == null)
            //{
            //    generator.eReplayGenerateResult = MDReplayGenerator.EReplayGenerateResult.NoMatch;
            //}
            //else if (false)
            //{
            //    generator.eReplayGenerateResult = MDReplayGenerator.EReplayGenerateResult.DemoUnavailable;
            //}
            //else if (!File.Exists(savePath))
            //{
            //    var cluster = match.cluster;
            //    var match_id = match.match_id;
            //    var replay_salt = match.replay_salt;
            //    var _download_url = string.Format(ClientParams.DEMO_URL_STRING, cluster, match_id, replay_salt);
            //    Console.WriteLine("demo url:" + _download_url);
            //    var zip = string.Format(savePath + ".bz2", match_id);
            //    if (!File.Exists(zip))
            //    {
            //        Console.WriteLine(zip + " downloading...");
            //        //先下载到临时文件
            //        var tmp = zip + ".tmp";
            //        using (var web = new WebClient())
            //        {
            //            try
            //            {
            //                 web.DownloadFileTaskAsync(_download_url, tmp).Wait();
            //            }
            //            catch (Exception ex)
            //            {
            //                Console.WriteLine("download err:" + ex.ToString());
            //            }
            //        }

            //        File.Move(tmp, zip, true);
            //        Console.WriteLine("demo download success");
            //    }
            //    //start unzip demo
            //    using (Process zipProcess = new Process())
            //    {
            //        zipProcess.StartInfo.FileName = "7z.exe";
            //        zipProcess.StartInfo.RedirectStandardInput = true;
            //        zipProcess.StartInfo.UseShellExecute = false;
            //        zipProcess.StartInfo.Arguments = $"x {zip} -o{ClientParams.DEMO_DIR} -aoa";
            //        zipProcess.Start();
            //        zipProcess.WaitForExit();
            //    }
            //    File.Delete(zip);
            //    Console.WriteLine("download complete");

            //    if (!File.Exists(savePath))
            //    {
            //        generator.eReplayGenerateResult = MDReplayGenerator.EReplayGenerateResult.DemoDownloadFail;
            //    }
            //}



            generator.block = false;
        }

        public void Download(ulong matchId, string hero_name)
        {
            string momentsPath = Path.Combine(ClientParams.DEMO_DIR, $"{matchId}.json");

            var level = Program.configuration["AppSEttings:Level"];

            using HttpClient client = new HttpClient();
            //string url = $"http://localhost:8000/getHighlights1/{match.match_id}";
            string url = $"http://localhost:8000/getHighlights1?match_id={matchId}&hero_name={hero_name}";

            if (level == "1")
            {
                url = $"http://localhost:8000/getSinglePlayerHighlights?match_id={matchId}&hero_name={hero_name}";
            }

            try
            {
                client.Timeout = TimeSpan.FromMinutes(30);
                HttpResponseMessage response = client.GetAsync(url).Result;
                response.EnsureSuccessStatusCode(); // Throws an exception for HTTP error responses
                string responseBody = response.Content.ReadAsStringAsync().Result;

                File.WriteAllText(momentsPath, responseBody);
            }
            catch (HttpRequestException e)
            {
                Console.WriteLine($"Request error: {e.Message}");
            }
        }
    }
}

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MetaDota.Common.Native;
using System.Runtime.InteropServices;
using Interceptor;
using System.Drawing;
using static Dota2.GC.Dota.Internal.CMsgGCToClientSocialMatchDetailsResponse;
using static SteamKit2.GC.Dota.Internal.CMsgSteamLearn_InferenceBackend_Response;
using static SteamKit2.Internal.CContentBuilder_CommitAppBuild_Request;
using SteamKit2;
using static SteamKit2.GC.Dota.Internal.CDOTAMatchMetadata;
using System.Numerics;
using MetaDota.config;
using ConsoleApp2;
using Newtonsoft.Json;
using System.Drawing.Imaging;
using Newtonsoft.Json.Linq;
using Tesseract;
using ImageFormat = System.Drawing.Imaging.ImageFormat;

namespace MetaDota.DotaReplay
{
    internal class MDMovieMaker : MDFactory<MDMovieMaker>
    {

        public Input _input;
        private string _cfgFilePath = "";
        private string _keyFilePath = "";

        private Dictionary<string, Interceptor.Keys> s2k;
        private int offset = 150;
        int add = 15;
        int noOfClips = 23;

        public override async Task Init()
        {
            base.Init();
            //check drivers
            bool driverInstalled = false;
            DirectoryInfo driverDi = new DirectoryInfo(@"C:\Windows\System32\drivers");
            FileInfo[] fis = driverDi.GetFiles("*.sys");
            foreach (FileInfo fi in fis)
            {
                if (fi.Name == "keyboard.sys")
                {
                    driverInstalled = true;
                    break;
                }
            }
            if (!driverInstalled)
            {

                Console.Write("please install driver interception with install-interception.exe  ");
                Console.ReadLine();
                Environment.Exit(0);
                return;
            }

            try
            {
                _input = new Input();
                _input.KeyboardFilterMode = KeyboardFilterMode.All;
                _input.Load();
                Console.Write("To Start DirectX Input, please enter any key:");
                Console.ReadLine();
                Console.Write("MDMovieMaker Init Success");
                s2k = new Dictionary<string, Interceptor.Keys>() {
                {"kp_0",Interceptor.Keys.Numpad0 },
                {"kp_1",Interceptor.Keys.Numpad1 },
                {"kp_2",Interceptor.Keys.Numpad2 },
                {"kp_3",Interceptor.Keys.Numpad3 },
                {"kp_4",Interceptor.Keys.Numpad4 },
                {"kp_5",Interceptor.Keys.Numpad5 },
                {"kp_6",Interceptor.Keys.Numpad6 },
                {"kp_7",Interceptor.Keys.Numpad7 },
                {"kp_8",Interceptor.Keys.Numpad8 },
                {"kp_9",Interceptor.Keys.Numpad9 },
                {"b",Interceptor.Keys.B },
                {"c",Interceptor.Keys.C },
                {"d",Interceptor.Keys.D },
                {"f",Interceptor.Keys.F },
                {"g",Interceptor.Keys.G },
                {"h",Interceptor.Keys.H },
                {"j",Interceptor.Keys.J },
                {"k",Interceptor.Keys.K },
                {"l",Interceptor.Keys.L },
                {"m",Interceptor.Keys.M },
                {"n",Interceptor.Keys.N },
                {"p",Interceptor.Keys.P },
                {"v",Interceptor.Keys.V },
                //{"x",Interceptor.Keys.X },
                //{"z",Interceptor.Keys.Z },
                {"kp_multiply",Interceptor.Keys.NumpadAsterisk },
                {"kp_minus",Interceptor.Keys.NumpadMinus },
                {"kp_plus",Interceptor.Keys.NumpadPlus },
                };
            }
            catch (Exception e)
            {
                Console.WriteLine("MDMovieMaker Init Fail:" + e.Message);
            }

        }

        public async Task Test()
        {
            await Task.Delay(3000);
            _input.SendText("d");
        }

        public override async Task Work(MDReplayGenerator generator)
        {
            _cfgFilePath = Path.Combine(ClientParams.REPLAY_CFG_DIR, "replayCfg.txt");
            _keyFilePath = Path.Combine(ClientParams.REPLAY_CFG_DIR, "keyCfg.txt");
            //string keys = "bcdfghjklmnpvxz";
            if (CancelRecording(generator))
            {
                RECT rECT = new RECT();
                while (!NativeMethods.GetWindowRect(Process.GetProcessesByName("dota2")[0].MainWindowHandle, ref rECT))
                {
                    await Task.Delay(1000);
                }
                int centerX = rECT.Left + (rECT.Right - rECT.Left) / 2;
                int centerY = rECT.Top + (rECT.Bottom - rECT.Top) / 2;
                Color pixelColor = MDTools.GetPixelColor(centerX, centerY);
                int sameCount = 10;
                while (sameCount > 0)
                {
                    Color curColor = MDTools.GetPixelColor(centerX, centerY);
                    Console.WriteLine($"{curColor.ToArgb()} {centerX} {centerY}");
                    if (curColor.ToArgb() != pixelColor.ToArgb())
                    {
                        pixelColor = curColor;
                        sameCount--;
                    }
                    await Task.Delay(500);
                }

                string momentsPath = Path.Combine(ClientParams.DEMO_DIR, $"{generator.match_id}.json");
                string json = File.ReadAllText(momentsPath);
                var data = JsonConvert.DeserializeObject<Data>(json) ?? new Data();
                string  slot = "1", war_fog = "";
               // _prepareAnalystParams(generator, out hero_name, out slot, out war_fog);
                if(generator.heroName != "123")
                {
                    offset = 300;
                }
                int prev = 0;

                int count = (data.data.Count % noOfClips != 0 ? data.data.Count / noOfClips : data.data.Count / noOfClips - 1);

                for (int i = 0; i <= count; i++)
                {
                    List<string> cfg = new List<string>();
                    //if(i == 0) cfg.Add("hud_toggle_visibility");
                    int ticks = (int)data.data[noOfClips * i].Start- offset;

                    cfg.Add($"demo_gototick {ticks}");
                    if (i == 0 && generator.heroName != "123")
                    {
                        cfg.Add($"dota_spectator_hero_index {slot}");
                        //cfg.Add($"dota_spectator_fog_of_war {war_fog}");
                        cfg.Add($"dota_spectator_mode 3");
                    }
                    else if (i == 0)
                    {
                        //cfg.Add($"dota_spectator_mode 0");
                    }

                    //cfg.Add($"startmovie ../../../../../movie/{generator.match_id}-{i} mp4");

                    for (int j = noOfClips * i; j < Math.Min(data.data.Count, noOfClips * (i + 1)); j++)
                    {
                        ticks = (int)data.data[j].Start- offset;
                        ticks = Math.Max(ticks, prev+3*30);
                        prev = (int)data.data[j].End + add*30;
                        cfg.Add($"bind {s2k.ElementAt(j % noOfClips).Key} \"demo_gototick {ticks}\"");
                    }
                    cfg.Add($"bind x \"endmovie\"");
                    cfg.Add($"bind z \"quit\"");
                    cfg.Add($"demo_resume");
                    cfg.Add($"hideConsole");

                    File.WriteAllLines($"{DotaClient.dotaCfgPath}/replayCfg.txt", cfg);

                    _input.SendKey(Interceptor.Keys.BackslashPipe, KeyState.Down);
                    _input.SendKey(Interceptor.Keys.BackslashPipe, KeyState.Up);
                    await Task.Delay(1500);
                    _input.KeyPressDelay = Program.config.GetKeyInputDelay();
                    _input.SendText("exec replayCfg.txt");
                    _input.SendKey(Interceptor.Keys.Enter, KeyState.Down);
                    _input.SendKey(Interceptor.Keys.Enter, KeyState.Up);


                    if (i == 0) Twitch();

                    YouTube();

                    for (int j = noOfClips * i; j < Math.Min(data.data.Count, noOfClips * (i + 1)); j++)
                    {
                        //string key = keys[j % noOfClips].ToString();
                        _input.SendKey(s2k.ElementAt(j % noOfClips).Value);

                        int start = 0;
                        while (start == 0)
                        {
                            start = GetTime(GetText());
                            Thread.Sleep(100);
                        }

                        //if (j > data.data.Count * 3 / 4) add = 20;
                        var wait = (int)(data.data[j].End - data.data[j].Start)/30+add;

                        await Task.Delay(wait * 1000);

                        if (i == 0 && j%noOfClips == 0)
                        {
                            _input.SendText("w");
                        }
                        else if (i == 1 && j % noOfClips == 0)
                        {
                            _input.SendText("y");
                        }

                        Stopwatch stopwatch = new Stopwatch();

                        // Start the stopwatch
                        stopwatch.Start();

                        while (j>2 && GetTime(GetText())< start+wait)
                        {
                            Thread.Sleep(100);
                            if (stopwatch.ElapsedMilliseconds > 30 * 1000) break;
                        }

                        stopwatch.Stop();

                        //Console.WriteLine($"start: {start} end: {end} wait:{wait} diff{end-start}");
                    }

                    if (i == count)
                    {
                        
                        Thread.Sleep(60 * 1000);
                    }

                    YouTube();
                    _input.SendText("x");
                }

                Twitch();
                _input.SendText("z");

                Console.WriteLine("Enter any key to continue");
                Console.ReadKey();

                //check is in demo


                //using (Process zipProcess = new Process())
                //{
                //    zipProcess.StartInfo.FileName = "ffmpeg.exe";
                //    zipProcess.StartInfo.UseShellExecute = false;
                //    zipProcess.StartInfo.RedirectStandardInput = true;
                //    zipProcess.StartInfo.Arguments = $"-y -r 30 -i \"{Path.GetFullPath(DotaClient.dotaMoviePath)}\\%08d.jpg\" -i \"{Path.GetFullPath(DotaClient.dotaMoviePath)}\\.wav\" -c:v libx264 -c:a aac -strict experimental -b:a 192k -shortest \"{Path.GetFullPath(DotaClient.dotaMoviePath)}\\{generator.match_id}_{generator.account_id}.mp4\"";
                //    zipProcess.Start();
                //    zipProcess.WaitForExit();
                //}
                //File.Delete(Path.Combine(DotaClient.dotaCfgPath, "autoexec.cfg"));
            }
            generator.block = false;
        }

        void Twitch()
        {
            SendAlt0();
        }

        void YouTube()
        {
            //SendAlt0();
        }

        void SendAlt0()
        {
            _input.SendKey(Interceptor.Keys.RightAlt, KeyState.Down);
            _input.SendKey(Interceptor.Keys.Zero, KeyState.Down);
            _input.SendKey(Interceptor.Keys.Zero, KeyState.Up);
            _input.SendKey(Interceptor.Keys.RightAlt, KeyState.Up);
        }

        bool _prepareAnalystParams(MDReplayGenerator generator, out string hero_name, out string slot, out string war_fog)
        {
            hero_name = "";
            slot = "";
            war_fog = "";
            foreach (var player in generator.match.players)
            {
                if (player.account_id == generator.account_id)
                {
                    hero_name = DotaClient.Instance.GetHeroNameByID(player.hero_id);
                    slot = (player.team_slot + (player.player_slot > 100 ? 5 : 0)).ToString();
                    war_fog = (player.player_slot > 100 ? 3 : 2).ToString();
                    return true;
                }
            }
            return false;
        }

        public bool CancelRecording(MDReplayGenerator generator)
        {
            if (!File.Exists(generator.demoFilePath))
            {
                generator.eReplayGenerateResult = MDReplayGenerator.EReplayGenerateResult.DemoDownloadFail;
                return false;
            }
            //else if (!File.Exists(_cfgFilePath) || (!File.Exists(_keyFilePath)))
            //{
            //    generator.eReplayGenerateResult = MDReplayGenerator.EReplayGenerateResult.AnalystFail;
            //    return false;
            //}

            //move file and delete movie file
            File.Copy(generator.demoFilePath, $"{DotaClient.dotaReplayPath}/{generator.match_id}.dem", true);
            //var replayCfg = File.ReadAllLines(_cfgFilePath);
            //replayCfg[4] = $"startmovie ../../../../../movie/{generator.match_id} mp4";
            //File.WriteAllLines(_cfgFilePath, replayCfg);
            //File.Copy(_cfgFilePath, $"{DotaClient.dotaCfgPath}/replayCfg.txt", true);
            if (!Directory.Exists(DotaClient.dotaMoviePath))
            {
                Directory.CreateDirectory(DotaClient.dotaMoviePath);
            }
            //Delete movie clip file
            Console.WriteLine("delete movie file ing ...");
            foreach (String file in Directory.GetFiles(DotaClient.dotaMoviePath))
            {
                //File.Delete(file);
            }
            Console.WriteLine("delete movie file ing over");



            string playDemoCmd = $"playdemo replays/{generator.match_id}\nhideConsole";
            File.WriteAllText(Path.Combine(DotaClient.dotaCfgPath, "autoexec.cfg"), playDemoCmd);

            Process[] processes = Process.GetProcessesByName("dota2");
            if (processes.Length == 0)
            {
                Process process = new Process();
                process.StartInfo.FileName = DotaClient.dotaLauncherPath;
                process.StartInfo.Arguments = "-console -novid";
                process.Start();
            }
            else
            {
                NativeMethods.SwitchToThisWindow(processes[0].MainWindowHandle, true);
                Task.Run(async () =>
                {
                    await Task.Delay(3000);
                    _input.SendKey(Interceptor.Keys.BackslashPipe, KeyState.Down);
                    _input.SendKey(Interceptor.Keys.BackslashPipe, KeyState.Up);
                    await Task.Delay(1500);
                    _input.KeyPressDelay = Program.config.GetKeyInputDelay();
                    _input.SendText($"exec autoexec.cfg");
                    _input.SendKey(Interceptor.Keys.Enter, KeyState.Down);
                    _input.SendKey(Interceptor.Keys.Enter, KeyState.Up);
                    await Task.Delay(3000);
                }).Wait();
            }

            return true;
        }

        string GetText()
        {
            Rectangle captureArea = new Rectangle(935, 23, 50, 25); // Change these values to specify your crop area
            Bitmap screenCapture = new Bitmap(captureArea.Width, captureArea.Height);
            Thread.Sleep(1000);

            using (Graphics g = Graphics.FromImage(screenCapture))
            {
                // Capture the specified area from the screen
                g.CopyFromScreen(captureArea.Left, captureArea.Top, 0, 0, captureArea.Size, CopyPixelOperation.SourceCopy);
            }

            Size newSize = new Size(800, 400); // Example desired size
            string imagePath = "";
            using (Bitmap scaledImage = new Bitmap(newSize.Width, newSize.Height))
            {
                using (Graphics g = Graphics.FromImage(scaledImage))
                {
                    // Set the quality of the scaled image
                    g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                    g.DrawImage(screenCapture, 0, 0, newSize.Width, newSize.Height);
                }

                // Save the scaled image to a file
                string framePath = $"{AppDomain.CurrentDomain.BaseDirectory}frame";
                if (!Directory.Exists(framePath)) Directory.CreateDirectory(framePath);
                imagePath = $@"{framePath}\cropped_image{DateTime.Now.Ticks}.jpg"; // Set your file path here

                scaledImage.Save(imagePath, ImageFormat.Jpeg);
            }


            Stopwatch stopwatch = Stopwatch.StartNew();

            //string text = EasyOCR(imagePath);

            string text = Tessaract(imagePath);
            Console.WriteLine($"text = {text}");
            stopwatch.Stop();
            Console.WriteLine($"Time taken for detection: {stopwatch.ElapsedMilliseconds} ms");
            File.Delete(imagePath);

            return text;

        }

        string Tessaract(string imagePath)
        {
            string text = "";
            try
            {
                // Define the path to the tessdata folder and the image file
                string tessDataPath = @"C:\Code\tessdata";  // Path to tessdata folder

                // Initialize the Tesseract engine with English language
                using (var engine = new TesseractEngine(tessDataPath, "eng", EngineMode.Default))
                {
                    // Load the image into Pix (the format Tesseract expects)
                    using (var img = Pix.LoadFromFile(imagePath))
                    {
                        // Perform OCR on the image
                        using (var page = engine.Process(img))
                        {
                            // Extract the recognized text
                            text = page.GetText();
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("Error: " + e.Message);
            }

            return text;
        }

        int GetTime(string text)
        {
            try
            {
                var time = new List<string>();
                if (text.Contains('.'))
                {
                    time = text.Split('.').ToList();
                }
                else if (text.Contains(':'))
                {
                    time = text.Split(':').ToList();
                }
                else if (text.Contains(' '))
                {
                    time = text.Split(' ').ToList();
                }

                return Convert.ToInt32(time[0]) * 60 + Convert.ToInt32(time[1]);
            }
            catch (Exception ex)
            {
                Console.WriteLine("error GetTime");
            }

            return 0;
        }

        string DetectText(string framePath)
        {
            string apiUrl = "http://localhost:5000/ocr"; // URL of your Flask API

            ///var textA = Utility.GetTextUsingTrOCR(framePath);
            //var textA = Utility.GetTextUsingEasyOCR(framePath);

            //using (var client = new HttpClient())
            //{
            //    using (var form = new MultipartFormDataContent())
            //    {
            //        byte[] imageData = File.ReadAllBytes(framePath);
            //        var byteArrayContent = new ByteArrayContent(imageData);
            //        form.Add(byteArrayContent, "file", Path.GetFileName(framePath));

            //        var response = client.PostAsync(apiUrl, form).Result;
            //        response.EnsureSuccessStatusCode();

            //        var jsonResponse = response.Content.ReadAsStringAsync().Result;
            //        JArray data = JArray.Parse(jsonResponse);
            //        if (data.Count > 0)
            //        {
            //            string text = data.First()["text"].ToString();
            //            return text;
            //        }
            //    }
            //}

            using (HttpClient client = new HttpClient())
            {
                var payload = new { path = framePath };
                string jsonPayload = JsonConvert.SerializeObject(payload);
                var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

                HttpResponseMessage response = client.PostAsync(apiUrl, content).Result;
                string responseString = response.Content.ReadAsStringAsync().Result;
                JArray data = JArray.Parse(responseString);
                if (data.Count > 0)
                {
                    string text = data.First()["text"].ToString();
                    return text;
                }
            }

            return "";
        }
    }
}

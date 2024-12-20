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
using System.Reflection.Emit;

namespace MetaDota.DotaReplay
{
    internal class MDMovieMaker : MDFactory<MDMovieMaker>
    {

        public Input _input;
        private string _cfgFilePath = "";
        private string _keyFilePath = "";

        private Dictionary<string, Interceptor.Keys> s2k;
        private int offset = 180;
        int add = 10;
        int noOfClips = 31;
        public bool started = false;
        //int noOfClips = 10;

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
                {"a",Interceptor.Keys.A },
                {"b",Interceptor.Keys.B },
                {"c",Interceptor.Keys.C },
                {"d",Interceptor.Keys.D },
                {"e",Interceptor.Keys.E },
                {"f",Interceptor.Keys.F },
                {"g",Interceptor.Keys.G },
                {"h",Interceptor.Keys.H },
                {"i",Interceptor.Keys.I },
                {"j",Interceptor.Keys.J },
                {"k",Interceptor.Keys.K },
                {"l",Interceptor.Keys.L },
                {"m",Interceptor.Keys.M },
                {"n",Interceptor.Keys.N },
                {"o",Interceptor.Keys.O },
                {"p",Interceptor.Keys.P },
                {"q",Interceptor.Keys.Q },
                {"r",Interceptor.Keys.R },
                {"t",Interceptor.Keys.T },
                {"u",Interceptor.Keys.U },
                {"v",Interceptor.Keys.V },
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

            var mode = Program.configuration["AppSettings:Mode"];
            if(mode == "2")
            {
                await PlayerChase(generator);
            }
            else
            {
                await PlayerPerspectiveDirectedCamera(generator);
            }
        }

        private async Task PlayerPerspectiveDirectedCamera(MDReplayGenerator generator)
        {
           
            //string keys = "bcdfghjklmnpvxz";
            if (CancelRecording(generator))
            {
                YouTube1();

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
                string slot = "1", war_fog = "";
                Random rnd = new Random();
                slot = rnd.Next(0, 1).ToString();
                // _prepareAnalystParams(generator, out hero_name, out slot, out war_fog);
                if (generator.heroName != "123")
                {
                    //offset = 300;
                }
                var level = Program.configuration["AppSEttings:Level"];
                if (level == "1")
                {
                    add = 12;
                }

                int prev = 0;

                int count = (data.data.Count % noOfClips != 0 ? data.data.Count / noOfClips : data.data.Count / noOfClips - 1);

                for (int i = 0; i <= count; i++)
                {
                    List<string> cfg = new List<string>();
                    //if(i == 0) cfg.Add("hud_toggle_visibility");
                    int ticks = (int)data.data[noOfClips * i].Start - offset;

                    cfg.Add($"demo_gototick {ticks}");
                    if (i == 0 && (generator.heroName != "123" || generator.heroName == "1234"))
                    {
                        if (generator.heroName != "1234")
                        {
                            slot = data.slot.ToString();
                        }
                        cfg.Add($"dota_spectator_hero_index {slot}");
                        //cfg.Add($"dota_spectator_fog_of_war {war_fog}");
                        //cfg.Add($"dota_spectator_mode 0");
                        cfg.Add($"dota_spectator_mode 3");
                    }
                    else if (generator.heroName == "123")
                    {
                        //cfg.Add($"dota_spectator_mode 0");
                    }

                    //cfg.Add($"startmovie ../../../../../movie/{generator.match_id}-{i} mp4");

                    for (int j = noOfClips * i; j < Math.Min(data.data.Count, noOfClips * (i + 1)); j++)
                    {
                        ticks = (int)data.data[j].Start - offset;
                        ticks = Math.Max(ticks, prev + 3 * 30);
                        prev = (int)data.data[j].End + add * 30;
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
                        Thread.Sleep(1000);
                        //Console.WriteLine("start");
                        SendAlt7();
                        //int start = 0;
                        //while (start == 0)
                        //{
                        //    start = GetTime(GetText());
                        //    Thread.Sleep(100);
                        //}

                        //if (j > data.data.Count * 3 / 4) add = 20;
                        var wait = (int)(data.data[j].End - data.data[j].Start) / 30 + add;

                        await Task.Delay(wait * 1000);

                        if (i == 0 && j % noOfClips == 0)
                        {
                            _input.SendText("w");
                        }
                        else if (j == 12)
                        {
                            _input.SendText("y");
                        }

                        //Stopwatch stopwatch = new Stopwatch();

                        //// Start the stopwatch
                        //stopwatch.Start();

                        //while (j>2 && GetTime(GetText())< start+wait)
                        //{
                        //    Thread.Sleep(100);
                        //    if (stopwatch.ElapsedMilliseconds > 30 * 1000) break;
                        //}

                        //stopwatch.Stop();
                        //Console.WriteLine("End");
                        SendAlt7();
                        //Console.WriteLine($"start: {start} end: {end} wait:{wait} diff{end-start}");
                    }

                    if (i == count)
                    {
                        Console.WriteLine("last");
                        Thread.Sleep(1000);
                        SendAlt7();
                        Console.WriteLine("Enter any key to continue");
                        Console.ReadLine();
                        //Thread.Sleep(15 * 1000);
                        SendAlt7();
                    }

                    YouTube();
                    _input.SendText("x");
                }
                YouTube1();

                Twitch();
                //_input.SendText("z");


                Console.WriteLine("Enter any key to continue");
                Console.ReadLine();

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

        private async Task PlayerChase(MDReplayGenerator generator)
        {
            offset = 4*30;
            add = 0;
            
            //string keys = "bcdfghjklmnpvxz";
            if (CancelRecording(generator))
            {
                YouTube1();

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

                var settings = new JsonSerializerSettings
                {
                    Error = (sender, args) =>
                    {
                        // Handle error here
                        Console.WriteLine($"Error: {args.ErrorContext.Error.Message}");
                        args.ErrorContext.Handled = true; // Bypass error
                    }
                };

                var data = JsonConvert.DeserializeObject<Data>(json) ?? new Data();
                string slot = "1", war_fog = "";
                Random rnd = new Random();
                slot = rnd.Next(0, 1).ToString();
                // _prepareAnalystParams(generator, out hero_name, out slot, out war_fog);
                if (generator.heroName != "123")
                {
                    //offset = 300;
                }

                int count = (data.data.Count % noOfClips != 0 ? data.data.Count / noOfClips : data.data.Count / noOfClips - 1);
                int prev = 0;
                float prev1 = 0;
                for (int i = 0; i <= count; i++)
                {
                    List<string> cfg = new List<string>();
                    //if(i == 0) cfg.Add("hud_toggle_visibility");
                    int ticks = (int)data.data[noOfClips * i].Start - offset;

                    if (i > 0) ticks = Math.Max(ticks, prev);

                    if (true || i == 0 || (int)data.data[noOfClips * i].Start> (int)data.data[noOfClips * i - 1].End)
                    {
                        cfg.Add($"demo_gototick {ticks}");
                    }

                    //if (i == 0 && (generator.heroName != "123" || generator.heroName == "1234"))
                    if (true)
                    {
                        if (generator.heroName != "1234")
                        {
                            if (data.data[noOfClips * i].Attackers is double)
                            {
                                slot = ((int)Convert.ToDouble(data.data[noOfClips * i].Slot) + 1).ToString();
                            }
                            else
                            {
                                slot = ((JArray)data.data[noOfClips * i].Attackers).First().ToString();
                            }
                        }
                        cfg.Add($"dota_spectator_hero_index {slot}");
                        cfg.Add($"dota_spectator_fog_of_war None");
                        //cfg.Add($"dota_spectator_fog_of_war {war_fog}");
                        //cfg.Add($"dota_spectator_mode 0");
                        cfg.Add($"dota_spectator_mode 2");
                        //cfg.Add($"dota_camera_distance 2000");
                    }
                    else if (generator.heroName == "123")
                    {
                        //cfg.Add($"dota_spectator_mode 0");
                    }

                    //cfg.Add($"startmovie ../../../../../movie/{generator.match_id}-{i} mp4");
                    for (int j = noOfClips * i; j < Math.Min(data.data.Count, noOfClips * (i + 1)); j++)
                    {
                        data.data[j].Start -= offset;
                        if (j > 0) data.data[j].Start = Math.Max(data.data[j].Start, prev);
                        data.data[j].End += add * 30;
                        prev = Math.Max((int)data.data[j].End, prev);
                        ticks = (int)data.data[j].Start;
                        cfg.Add($"bind {s2k.ElementAt(j % noOfClips).Key} \"demo_gototick {ticks}\"");
                    }
                    //cfg.Add($"bind x dota_camera_distance 1400");
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

                    await Task.Delay(1000);

                    SendAlt7();
                    await Task.Delay(1000);
                    YouTube();

                    for (int j = noOfClips * i; j < Math.Min(data.data.Count, noOfClips * (i + 1)); j++)
                    {
                        //string key = keys[j % noOfClips].ToString();
                        if (data.data[j].Start > prev1+60)
                        {
                            //Console.WriteLine(s2k.ElementAt(j % noOfClips).Value);
                            _input.SendKey(s2k.ElementAt(j % noOfClips).Value);
                        }
                        prev1 = Math.Max(prev1, data.data[j].End);
                        //if (j==0 || data.data[j].Start> data.data[j-1].End)
                        //{
                        //    _input.SendKey(s2k.ElementAt(j % noOfClips).Value);
                        //}
                        //else
                        //{
                        //    data.data[j-1].End = data.data[j - 1].Start;
                        //}
                        if (data.data[j].Attackers is double)
                        {
                            slot = ((int)Convert.ToDouble(data.data[j].Slot) + 1).ToString();
                        }
                        else
                        {
                            var attackers = ((JArray)data.data[j].Attackers);
                            slot = ((int)Convert.ToDouble(attackers.First()) + 1).ToString();
                            if(j< data.data.Count-1 && attackers.Count>1)
                            {
                                slot = ((int)Convert.ToDouble(attackers.First(x=> (int)Convert.ToDouble(x) != (int)Convert.ToDouble(data.data[j+1].Slot))) + 1).ToString();
                            }
                        }
                        _input.SendText(slot);

                        Thread.Sleep(1000);

                        //Console.WriteLine("start");
                        //SendAlt7();
                        //int start = 0;
                        //while (start == 0)
                        //{
                        //    start = GetTime(GetText());
                        //    Thread.Sleep(100);
                        //}

                        //if (j > data.data.Count * 3 / 4) add = 20;
                        var wait = (int)(data.data[j].End - data.data[j].Start) / 30;
                        Console.WriteLine($"{wait}");

                        if (wait > 0)
                            await Task.Delay(wait * 1000);

                        if (i == 0 && j % noOfClips == 0)
                        {
                            _input.SendText("w");
                            //_input.SendText("x");
                        }
                        else if (j == 12)
                        {
                            _input.SendText("y");
                        }

                        //Stopwatch stopwatch = new Stopwatch();

                        //// Start the stopwatch
                        //stopwatch.Start();

                        //while (j>2 && GetTime(GetText())< start+wait)
                        //{
                        //    Thread.Sleep(100);
                        //    if (stopwatch.ElapsedMilliseconds > 30 * 1000) break;
                        //}

                        //stopwatch.Stop();
                        //Console.WriteLine("End");
                        //Thread.Sleep(1000);
                        //SendAlt7();
                        //Console.WriteLine($"start: {start} end: {end} wait:{wait} diff{end-start}");
                    }

                    if (i == count)
                    {
                        Console.WriteLine("last");
                        //Thread.Sleep(1000);
                        //SendAlt7();
                        Console.WriteLine("Enter any key to continue");
                        Console.ReadLine();
                        //Thread.Sleep(30 * 1000);
                        //SendAlt7();
                    }
                    Thread.Sleep(3000);

                    SendAlt7();
                    //_input.SendText("x");
                }
                YouTube1();

                Twitch();
                //_input.SendText("z");


                Console.WriteLine("Enter any key to continue");
                Console.ReadLine();

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
            //SendAlt0();
        }

        public void YouTube()
        {
            var stream = Program.configuration["AppSettings:Stream"];

            if (!started && stream == "1")
            {
                SendAlt0();
                started = true;
            }
        }
        void YouTube1()
        {
            //SendAlt0();
        }

        /// <summary>
        /// obs hotkey to start/stop streaming
        /// </summary>
        void SendAlt0()
        {
            _input.SendKey(Interceptor.Keys.F4);
            //_input.SendKey(Interceptor.Keys.RightAlt, KeyState.Down);
            //_input.SendKey(Interceptor.Keys.Zero, KeyState.Down);
            //_input.SendKey(Interceptor.Keys.Zero, KeyState.Up);
            //_input.SendKey(Interceptor.Keys.RightAlt, KeyState.Up);
        }

        /// <summary>
        /// obs hotkey to start/stop recording
        /// </summary>
        void SendAlt7()
        {
            _input.SendKey(Interceptor.Keys.F1);
            //_input.SendKey(Interceptor.Keys.RightAlt, KeyState.Down);
            //_input.SendKey(Interceptor.Keys.Seven, KeyState.Down);
            //_input.SendKey(Interceptor.Keys.Seven, KeyState.Up);
            //_input.SendKey(Interceptor.Keys.RightAlt, KeyState.Up);
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

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

namespace MetaDota.DotaReplay
{
    internal class MDMovieMaker : MDFactory<MDMovieMaker>
    {

        public Input _input;
        private string _cfgFilePath = "";
        private string _keyFilePath = "";

        private Dictionary<string, Interceptor.Keys> s2k;

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
                {"x",Interceptor.Keys.X },
                {"z",Interceptor.Keys.Z },
                {"kp_3",Interceptor.Keys.Numpad3 },
                {"kp_4",Interceptor.Keys.Numpad4 },
                {"kp_5",Interceptor.Keys.Numpad5 },
                {"kp_6",Interceptor.Keys.Numpad6 },
                {"kp_7",Interceptor.Keys.Numpad7 },
                {"kp_8",Interceptor.Keys.Numpad8 },
                {"kp_9",Interceptor.Keys.Numpad9 },
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
            string keys = "bcdfghjklmnpvxz";
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
                string hero_name, slot, war_fog;
                _prepareAnalystParams(generator, out hero_name, out slot, out war_fog);

                for (int i = 0; i < data.data.Count / 10 + 1; i++)
                {
                    List<string> cfg = new List<string>();
                    int ticks = (int)data.data[10 * i].Start;
                    //cfg.Add($"demo_gototick {ticks}");
                    cfg.Add($"dota_spectator_hero_index {slot}");
                    cfg.Add($"dota_spectator_fog_of_war {war_fog}");
                    cfg.Add($"dota_spectator_mode 3");
                    cfg.Add($"startmovie ../../../../../movie/{generator.match_id}-{i} mp4");

                    for (int j = 10 * i; j < Math.Min(data.data.Count, 10 * (i + 1)); j++)
                    {
                        ticks = (int)data.data[j].Start;
                        cfg.Add($"bind {keys[j % 10]} \"demo_gototick {ticks}\"");
                    }
                    cfg.Add($"bind x \"endmovie\"");
                    cfg.Add($"bind c \"quit\"");
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

                    for (int j = 10 * i; j < Math.Min(data.data.Count, 10 * (i + 1)); j++)
                    {
                        await Task.Delay(2000);
                        _input.SendText(keys[j % 10].ToString());
                        Console.WriteLine(keys[j % 10].ToString());
                        var wait = (int)(data.data[j].End - data.data[j].Start)/30+10;
                        Console.WriteLine(wait);

                        await Task.Delay(wait * 1000);
                    }

                    _input.SendText("x");
                    await Task.Delay(5000);
                }
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
                File.Delete(Path.Combine(DotaClient.dotaCfgPath, "autoexec.cfg"));
            }
            generator.block = false;
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
                process.StartInfo.Arguments = "-console";
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




    }
}

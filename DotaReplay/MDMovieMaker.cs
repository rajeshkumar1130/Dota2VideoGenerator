﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MetaDota.Common.Native;
using System.Runtime.InteropServices;
using WindowsInput;
using WindowsInput.Native;
using MetaDota.InputSimulation;
using Interceptor;
using System.Drawing;

namespace MetaDota.DotaReplay
{
    internal class MDMovieMaker : MDFactory<MDMovieMaker>
    {

        public Input _input;
        private string _cfgFilePath = "";
        private string _keyFilePath = "";


        public override async Task Init()
        {
            base.Init();
            try
            {
                _input = new Input();
                _input.KeyboardFilterMode = KeyboardFilterMode.All;
                _input.Load();
                Console.Write("To Start DirectX Input, please enter any key:");
                Console.ReadLine();
                Console.Write("MDMovieMaker Init Success");
            }
            catch (Exception e)
            {
                Console.WriteLine("MDMovieMaker Init Fail:" + e.Message);
            }

        }

        public override async Task Work(MDReplayGenerator generator)
        {
            _cfgFilePath = Path.Combine(ClientParams.REPLAY_CFG_DIR, "replayCfg.txt");
            _keyFilePath = Path.Combine(ClientParams.REPLAY_CFG_DIR, "keyCfg.txt");
            if (CancelRecording(generator))
            {
                if (!Directory.Exists(DotaClient.dotaMoviePath))
                {
                    Directory.CreateDirectory(DotaClient.dotaMoviePath);
                }
                //Delete movie clip file
                foreach (String file in Directory.GetFiles(DotaClient.dotaMoviePath))
                {
                    File.Delete(file);
                }

                //check is in demo
                Color pixelColor = MDTools.GetPixelColor(131, 77);
                while (pixelColor.ToArgb() != -1840390)
                {
                    await Task.Delay(1000);
                    pixelColor = MDTools.GetPixelColor(131, 77);
                }
                _input.SendText("exec replayCfg.txt");
                string[] keyLines = File.ReadAllLines(_keyFilePath);
                string clipFile = "";
                for (int i = 0; i < keyLines.Length; i++)
                {
                    string[] fileKey = keyLines[i].Split('$');
                    if (fileKey.Length == 2)
                    {
                        clipFile = Path.Combine(DotaClient.dotaMoviePath, fileKey[0]);
                        while (!File.Exists(clipFile))
                        {
                            Task.Delay(500);
                        }
                        _input.SendText(fileKey[1]);
                    }
                    
                }
               

            }

            generator.block = false;
        }

        public bool CancelRecording(MDReplayGenerator generator)
        {
            if (!File.Exists(generator.demoFilePath))
            {
                generator.eReplayGenerateResult = MDReplayGenerator.EReplayGenerateResult.DemoDownloadFail;
                return false;
            }
            else if (!File.Exists(_cfgFilePath) || (!File.Exists(_keyFilePath)))
            {
                generator.eReplayGenerateResult = MDReplayGenerator.EReplayGenerateResult.AnalystFail;
                return false;
            }

            File.Copy(generator.demoFilePath, $"{DotaClient.dotaReplayPath}/{generator.match_id}.dem");
            File.Copy(_cfgFilePath, $"{DotaClient.dotaCfgPath}/replayCfg.txt");

            string playDemoCmd = $"playdemo replays/{generator.match_id}\nshowConsole";

            Process[] processes = Process.GetProcessesByName("dota2");
            if (processes.Length == 0)
            {
                File.WriteAllText(Path.Combine(DotaClient.dotaCfgPath, "autoexec.cfg"), playDemoCmd);
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
                    _input.SendKey(Keys.BackslashPipe, KeyState.Down);
                    _input.SendText($"playdemo replays/{generator.match_id}");
                    _input.SendKey(Keys.Enter, KeyState.Down);
                }).Wait();
            }

            return true;
        }




    }
}

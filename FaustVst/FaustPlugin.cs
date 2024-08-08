using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.Loader;
using System.Threading;
using AudioPlugSharp;
using FaustDSP;
using UILayout;

namespace FaustVst
{
    public class FaustPlugin : AudioPluginBase
    {
        public string PluginFilePath;

        AudioIOPort monoInput;
        AudioIOPort monoOutput;

        public IFaustDSP FaustDSP { get; private set; } = null;
        double[][] buf = new double[1][];

        MonoGameHost GameHost;
        DSPCompiler compiler = new DSPCompiler();

        public FaustPlugin()
		{
			Company = "Nostatic Software";
			Website = "github.com/mikeoliphant";
			Contact = "contact@nostatic.org";
			PluginName = "FaustPlugin";
			PluginCategory = "Fx";
			PluginVersion = "1.0.0";

			PluginID = 0xA7D6AED74104A2C5;

			HasUserInterface = true;
			EditorWidth = 400;
			EditorHeight = 200;
		}

        public override void Initialize()
        {
            base.Initialize();

            //Logger.Log("Plugin has " + plugin.GetNumInputs() + " inputs and " + plugin.GetNumOutputs() + " outputs");

            InputPorts = new AudioIOPort[] { monoInput = new AudioIOPort("Mono Input", EAudioChannelConfiguration.Mono) };
            OutputPorts = new AudioIOPort[] { monoOutput = new AudioIOPort("Mono Output", EAudioChannelConfiguration.Mono) };
        }

        public void LoadPlugin(string path)
        {
            PluginFilePath = path;

            Logger.Log("Compiling plugin");

            Assembly faustAssembly = typeof(IFaustDSP).Assembly;

            Logger.Log("FaustDSP location: " + faustAssembly.Location);

            try
            {
                AssemblyLoadContext loadContext = AssemblyLoadContext.GetLoadContext(faustAssembly);

                Logger.Log("LoadContext: " + loadContext.ToString());

                FaustDSP = compiler.CompileDSP(path, loadContext);
            }
            catch (Exception ex)
            {
                Logger.Log("Compilation failed with: " + ex.ToString());
            }

            if (FaustDSP != null)
            {
                FaustDSP.InstanceResetUserInterface();
                FaustDSP.Init((int)Host.SampleRate);
            }
            else
            {
                Logger.Log("*** Plugin is null");
            }
        }

        IntPtr parentWindow;

        public override void ShowEditor(IntPtr parentWindow)
        {
            Logger.Log("Show Editor");

            this.parentWindow = parentWindow;

            if (parentWindow == IntPtr.Zero)
            {
                RunGame();
            }
            else
            {
                Thread thread = new Thread(new ThreadStart(RunGame));

                thread.SetApartmentState(ApartmentState.STA);

                thread.Start();
            }
        }

        void RunGame()
        {
            Logger.Log("Start game");

            try
            {
                int screenWidth = (int)EditorWidth;
                int screenHeight = (int)EditorHeight;

                FaustLayout layout = new FaustLayout(this);

                layout.Scale = 0.35f;

                using (GameHost = new MonoGameHost(parentWindow, screenWidth, screenHeight, fullscreen: false))
                {
                    GameHost.IsMouseVisible = true;

                    GameHost.StartGame(layout);
                }

                layout = null;
            }
            catch (Exception ex)
            {
                Logger.Log("Run game failed with: " + ex.ToString());
            }
        }

        public override void ResizeEditor(uint newWidth, uint newHeight)
        {
            base.ResizeEditor(newWidth, newHeight);

            if (GameHost != null)
            {
                GameHost.RequestResize((int)newWidth, (int)newHeight);
            }
        }


        public override void HideEditor()
        {
            base.HideEditor();

            GameHost.Exit();
        }

        public override void InitializeProcessing()
        {
            base.InitializeProcessing();

            if (FaustDSP != null)
            {
                FaustDSP.Init((int)Host.SampleRate);
            }
        }

        public override void SetMaxAudioBufferSize(uint maxSamples, EAudioBitsPerSample bitsPerSample)
        {
            base.SetMaxAudioBufferSize(maxSamples, bitsPerSample);

            buf[0] = new double[maxSamples];
        }

        public override void Process()
        {
            base.Process();

            Host.ProcessAllEvents();

            if (FaustDSP == null)
            {
                monoInput.PassThroughTo(monoOutput);
            }
            else
            {
                monoInput.GetAudioBuffer(0).CopyTo(buf[0]);

                FaustDSP.Compute((int)Host.CurrentAudioBufferSize, buf, buf);

                Span<double> outSpan = buf[0];

                outSpan.CopyTo(monoOutput.GetAudioBuffer(0));
            }
        }
    }
}

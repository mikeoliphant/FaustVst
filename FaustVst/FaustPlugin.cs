﻿using System;
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
        AudioIOPort monoInput;
        AudioIOPort monoOutput;

        IFaustDSP faustPlugin = null;
        public List<FaustUIElement> FaustParameters = new List<FaustUIElement>();
        double[][] buf = new double[1][];

        MonoGameHost GameHost;
        DSPCompiler compiler = new DSPCompiler();

        public FaustPlugin()
		{
			Company = "Nostatic Software";
			Website = "www.nostaticsoftware.com";
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
            if (faustPlugin == null)
            {
            }

            FaustParameters.Clear();

            Logger.Log("Compiling plugin");

            Assembly faustAssembly = typeof(IFaustDSP).Assembly;

            Logger.Log("FaustDSP location: " + faustAssembly.Location);

            try
            {
                AssemblyLoadContext loadContext = AssemblyLoadContext.GetLoadContext(faustAssembly);

                Logger.Log("LoadContext: " + loadContext.ToString());

                faustPlugin = compiler.CompileDSP(path, loadContext);
            }
            catch (Exception ex)
            {
                Logger.Log("Compilation failed with: " + ex.ToString());
            }

            if (faustPlugin != null)
            {
                AddParameters(faustPlugin.UIDefinition.RootElement);
            }
            else
            {
                Logger.Log("*** Plugin is null");
            }
        }

        void AddParameters(FaustUIElement element)
        {
            if (element is FaustBoxElement)
            {
                foreach (FaustUIElement child in (element as FaustBoxElement).Children)
                {
                    AddParameters(child);
                }
            }
            else
            {
                if (element is FaustUIWriteableFloatElement)
                {
                    FaustParameters.Add(element);
                }
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
                new Thread(new ThreadStart(RunGame)).Start();
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

            if (faustPlugin != null)
            {
                faustPlugin.Init((int)Host.SampleRate);
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

            if (faustPlugin == null)
            {
                monoInput.PassThroughTo(monoOutput);
            }
            else
            {
                monoInput.GetAudioBuffer(0).CopyTo(buf[0]);

                faustPlugin.Compute((int)Host.CurrentAudioBufferSize, buf, buf);

                Span<double> outSpan = buf[0];

                outSpan.CopyTo(monoOutput.GetAudioBuffer(0));
            }
        }
    }
}
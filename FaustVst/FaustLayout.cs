using Microsoft.Xna.Framework;
using System;
using System.IO;
using System.Windows.Forms;
using UILayout;

namespace FaustVst
{
    public class FaustLayout : MonoGameLayout
    {
        FaustPlugin plugin;
        Dock mainDock;
        HorizontalStack paramStack;
        TextBlock pluginFileText;
        string pluginFilePath;


        public FaustLayout(FaustPlugin plugin)
        {
            this.plugin = plugin;
        }

        public override void SetHost(Game host)
        {
            base.SetHost(host);

            Host.InactiveSleepTime = TimeSpan.Zero;

            Host.Window.Title = "FaustVst";

            LoadImageManifest("ImageManifest.xml");

            GraphicsContext.SingleWhitePixelImage = GetImage("SingleWhitePixel");
            GraphicsContext.SamplerState = new Microsoft.Xna.Framework.Graphics.SamplerState()
            {
                AddressU = Microsoft.Xna.Framework.Graphics.TextureAddressMode.Clamp,
                AddressV = Microsoft.Xna.Framework.Graphics.TextureAddressMode.Clamp,
                Filter = Microsoft.Xna.Framework.Graphics.TextureFilter.Anisotropic,
                MipMapLevelOfDetailBias = -0.8f
            };

            DefaultFont = GetFont("MainFont");
            DefaultFont.SpriteFont.Spacing = 1;

            GetFont("SmallFont").SpriteFont.Spacing = 0;

            DefaultForegroundColor = UIColor.Black;

            DefaultOutlineNinePatch = GetImage("PopupBackground");

            DefaultPressedNinePatch = GetImage("ButtonPressed");
            DefaultUnpressedNinePatch = GetImage("ButtonUnpressed");

            DefaultDragImage = GetImage("ButtonPressed");

            RootUIElement = mainDock = new Dock()
            {
                BackgroundColor = UIColor.White,
                Padding = new LayoutPadding(20)
            };

            HorizontalStack pluginLoadStack = new HorizontalStack()
            {
                HorizontalAlignment = EHorizontalAlignment.Right,
                DesiredHeight = 80
            };

            mainDock.Children.Add(pluginLoadStack);

            pluginFileText = new TextBlock("No Plugin Loaded")
            {
                VerticalAlignment = EVerticalAlignment.Center
            };

            pluginLoadStack.Children.Add(pluginFileText);

            pluginLoadStack.Children.Add(new ImageButton("FileOpen")
            {
                VerticalAlignment = EVerticalAlignment.Stretch,
                ClickAction = LoadPlugin
            });

            pluginLoadStack.Children.Add(new ImageButton("Reload")
            {
                VerticalAlignment = EVerticalAlignment.Stretch,
                ClickAction = ReloadPlugin
            });

            paramStack = new HorizontalStack()
            {
                HorizontalAlignment = EHorizontalAlignment.Center,
                VerticalAlignment = EVerticalAlignment.Center
            };

            mainDock.Children.Add(paramStack);

            mainDock.UpdateContentLayout();
        }

        void UpdateParameters()
        {
            pluginFileText.Text = Path.GetFileName(pluginFilePath);

            paramStack.Children.Clear();

            foreach (FaustUIElement element in plugin.FaustParameters)
            {
                if (element is FaustUIWriteableFloatElement)
                {
                    FaustUIWriteableFloatElement floatElement = element as FaustUIWriteableFloatElement;

                    UIColor foregroundColor = UIColor.Black;

                    VerticalStack controlVStack = new VerticalStack()
                    {
                        HorizontalAlignment = EHorizontalAlignment.Stretch,
                        VerticalAlignment = EVerticalAlignment.Stretch
                    };

                    controlVStack.Children.Add(new TextBlock(floatElement.Label)
                    {
                        HorizontalAlignment = EHorizontalAlignment.Center,
                        TextColor = foregroundColor,
                        TextFont = Layout.Current.GetFont("SmallFont")
                    });

                    string valueFormat = "{0:0.0}";

                    Dock controlDock = new Dock() { HorizontalAlignment = EHorizontalAlignment.Stretch, VerticalAlignment = EVerticalAlignment.Center };
                    controlVStack.Children.Add(controlDock);

                    float strWidthMax;
                    float strHeightMax;
                    Layout.Current.GetFont("SmallFont").MeasureString(String.Format(valueFormat, floatElement.MaxValue), out strWidthMax, out strHeightMax);
                    float strWidthMin;
                    float strHeightMin;
                    Layout.Current.GetFont("SmallFont").MeasureString(String.Format(valueFormat, floatElement.MinValue), out strWidthMin, out strHeightMin);

                    ParameterValueDisplay valueDisplay = new ParameterValueDisplay()
                    {
                        HorizontalAlignment = EHorizontalAlignment.Absolute,
                        VerticalAlignment = EVerticalAlignment.Absolute,
                        Margin = new LayoutPadding(-Math.Max(strWidthMin, strWidthMax), -Math.Max(strHeightMin, strHeightMax)),
                        ValueFormat = valueFormat
                    };

                    controlVStack.DesiredWidth = 200;

                    ParameterDial dial = new ParameterDial()
                    {
                        MinValue = floatElement.MinValue,
                        MaxValue = floatElement.MaxValue,
                        DefaultValue = floatElement.DefaultValue
                    };

                    controlDock.Children.Add(dial);

                    dial.SetDialColor(UIColor.Black);

                    dial.SetPointerColor(((foregroundColor.R + foregroundColor.G + foregroundColor.B) / 3) > 128 ? UIColor.Black : UIColor.White);

                    dial.SetValue(floatElement.VariableAccessor.GetValue());

                    dial.ValueChangedAction = delegate (double val)
                    {
                        floatElement.VariableAccessor.SetValue(val);

                        valueDisplay.SetValue(val);
                    };

                    controlDock.Children.Add(valueDisplay);

                    paramStack.Children.Add(controlVStack);
                }
            }

            mainDock.UpdateContentLayout();
        }

        void ReloadPlugin()
        {
            if (!string.IsNullOrEmpty(pluginFilePath))
            {
                plugin.LoadPlugin(pluginFilePath);

                UpdateParameters();
            }
        }

        void LoadPlugin()
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();

            openFileDialog.Filter = "Faust Files|*.dsp";

            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                if (!string.IsNullOrEmpty(openFileDialog.FileName))
                {
                    pluginFilePath = openFileDialog.FileName;

                    plugin.LoadPlugin(pluginFilePath);

                    UpdateParameters();
                }
            }
        }
    }
}

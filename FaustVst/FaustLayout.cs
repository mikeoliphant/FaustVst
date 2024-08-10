using FaustDSP;
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
        UIColor foregroundColor = UIColor.Black;

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
                BackgroundColor = new UIColor(230, 230, 230),
                Padding = new LayoutPadding(20)
            };

            HorizontalStack pluginLoadStack = new HorizontalStack()
            {
                HorizontalAlignment = EHorizontalAlignment.Right,
                DesiredHeight = 80
            };

            mainDock.Children.Add(pluginLoadStack);

            pluginFileText = new TextBlock()
            {
                Margin = new LayoutPadding(10),
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

            UpdateParameters();
        }

        void UpdateParameters()
        {
            pluginFileText.Text = String.IsNullOrEmpty(plugin.PluginFilePath) ? "No Plugin Loaded" : Path.GetFileName(plugin.PluginFilePath);

            paramStack.Children.Clear();

            if (plugin.FaustDSP != null)
                AddParameters(plugin.FaustDSP.UIDefinition.RootElement, paramStack);

            mainDock.UpdateContentLayout();
        }

        void AddParameters(FaustUIElement element, ListUIElement container)
        {
            if (element is FaustBoxElement)
            {
                NinePatchWrapper outline = new NinePatchWrapper(Layout.Current.GetImage("PluginBackground"))
                {
                    Margin = new LayoutPadding(20)
                };
                container.Children.Add(outline);

                VerticalStack verticalStack = new VerticalStack();
                outline.Child = verticalStack;

                verticalStack.Children.Add(new TextBlock(element.Label));

                ListUIElement stack = element.ElementType == EFaustUIElementType.HorizontalBox ? new HorizontalStack() : new VerticalStack();
                verticalStack.Children.Add(stack);

                foreach (FaustUIElement child in (element as FaustBoxElement).Children)
                {
                    AddParameters(child, stack);
                }
            }
            else
            {
                if (element is FaustUIVariableElement)
                {
                    if ((element.ElementType == EFaustUIElementType.Button) || (element.ElementType == EFaustUIElementType.CheckBox))
                    {
                        FaustUIVariableElement variableElement = element as FaustUIVariableElement;

                        TextButton button = new TextButton(element.Label)
                        {
                            HorizontalAlignment = EHorizontalAlignment.Center,
                            VerticalAlignment = EVerticalAlignment.Center
                        };

                        button.IsToggleButton = (element.ElementType == EFaustUIElementType.CheckBox);

                        button.SetPressed(variableElement.VariableAccessor.GetValue() == 1.0f);

                        button.PressAction = delegate
                        {
                            variableElement.VariableAccessor.SetValue(button.IsPressed ? 1.0 : 0.0);
                        };

                        container.Children.Add(button);
                    }
                    else if ((element.ElementType == EFaustUIElementType.HorizontalBargraph) || (element.ElementType == EFaustUIElementType.VerticalBargraph))
                    {
                        FaustUIFloatElement floatElement = element as FaustUIFloatElement;

                        VerticalStack controlVStack = new VerticalStack()
                        {
                            HorizontalAlignment = EHorizontalAlignment.Stretch,
                            VerticalAlignment = EVerticalAlignment.Stretch
                        };

                        controlVStack.Children.Add(new TextBlock(element.Label)
                        {
                            Margin = new LayoutPadding(5, 0),
                            HorizontalAlignment = EHorizontalAlignment.Center,
                            TextColor = foregroundColor,
                            TextFont = Layout.Current.GetFont("SmallFont")
                        });

                        LevelBar levelBar = null;

                        if (element.ElementType == EFaustUIElementType.HorizontalBargraph)
                        {
                            levelBar = new HorizontalLevelBar()
                            {
                                DesiredHeight = 40,
                                DesiredWidth = 300,
                                HorizontalAlignment = EHorizontalAlignment.Center,
                                VerticalAlignment = EVerticalAlignment.Center,
                                Margin = new LayoutPadding(20),
                            };
                        }
                        else
                        {
                            levelBar = new VerticalLevelBar()
                            {
                                DesiredHeight = 300,
                                DesiredWidth = 40,
                                HorizontalAlignment = EHorizontalAlignment.Center,
                                VerticalAlignment = EVerticalAlignment.Center,
                                Margin = new LayoutPadding(20),
                            };
                        }

                        levelBar.GetValue = delegate
                        {
                            return floatElement.GetNormalizedValue(floatElement.VariableAccessor.GetValue());
                        };

                        controlVStack.Children.Add(levelBar);

                        container.Children.Add(controlVStack);
                    }
                }
                
                if (element is FaustUIWriteableFloatElement)
                {
                    FaustUIWriteableFloatElement floatElement = element as FaustUIWriteableFloatElement;

                    VerticalStack controlVStack = new VerticalStack()
                    {
                        HorizontalAlignment = EHorizontalAlignment.Stretch,
                        VerticalAlignment = EVerticalAlignment.Stretch
                    };

                    controlVStack.Children.Add(new TextBlock(floatElement.Label)
                    {
                        Margin = new LayoutPadding(5, 0),
                        HorizontalAlignment = EHorizontalAlignment.Center,
                        TextColor = foregroundColor,
                        TextFont = Layout.Current.GetFont("SmallFont")
                    });

                    string valueFormat = "{0:0.0}";

                    string unit = element.GetMetaData("unit");

                    if (!string.IsNullOrEmpty(unit))
                    {
                        valueFormat += unit;
                    }

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

                    if (element.GetMetaData("style") == "knob")
                    {
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
                    }
                    else if ((element.ElementType == EFaustUIElementType.HorizontalSlider) || (element.ElementType == EFaustUIElementType.VerticalSlider))
                    {
                        Slider slider = null;

                        if (element.ElementType == EFaustUIElementType.HorizontalSlider)
                        {
                            slider = new HorizontalSlider("VerticalSlider")
                            {
                                HorizontalAlignment = EHorizontalAlignment.Center,
                                DesiredWidth = 300
                            };
                        }
                        else
                        {
                            slider = new VerticalSlider("VerticalSlider")
                            {
                                HorizontalAlignment = EHorizontalAlignment.Center,
                                DesiredHeight = 300
                            };
                        };

                        slider.InvertLevel = true;
                        slider.ChangeAction = delegate (float value)
                        {
                            value = (float)floatElement.GetDenormalizedValue(value);

                            floatElement.VariableAccessor.SetValue(value);

                            valueDisplay.SetValue(value);
                        };

                        slider.SetLevel((float)floatElement.GetNormalizedValue(floatElement.VariableAccessor.GetValue()));

                        controlDock.Children.Add(slider);
                    }

                    controlDock.Children.Add(valueDisplay);

                    container.Children.Add(controlVStack);
                }
            }
        }

        void ReloadPlugin()
        {
            if (!string.IsNullOrEmpty(plugin.PluginFilePath))
            {
                LoadPlugin(plugin.PluginFilePath);
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
                    LoadPlugin(openFileDialog.FileName);
                }
            }
        }

        void LoadPlugin(string pluginFilePath)
        {
            try
            {
                plugin.LoadPlugin(pluginFilePath);
            }
            catch (Exception ex)
            {
                if (ex is DspCompiler.FaustCompileException)
                {
                    MessageBox.Show(ex.Message);
                }
                else
                {
                    MessageBox.Show("An error occurred: " + ex.ToString());
                }
            }

            UpdateParameters();
        }
    }
}

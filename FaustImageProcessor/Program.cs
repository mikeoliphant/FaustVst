using System;
using System.Drawing;
using System.IO;
using System.Reflection;

namespace FaustImageProcessor
{
    class FaustImageProcessor : ImageSheetProcessor.ImageSheetProcessor
    {
        public void RenderImages(string destPath)
        {
            BeginRenderImages(destPath);
            Render();

            EndRenderImages();
        }

        public void Render()
        {
            MaxImageSheetSize = 2048;

            BeginSpriteSheetGroup("UISheet");

            AddFont("MainFont", "Calibri", FontStyle.Bold, 36);
            AddFont("SmallFont", "Calibri", FontStyle.Bold, 30);

            PushDirectory("UserInterface");

            Add("SingleWhitePixel");

            AddWithShadow("HoverTextOutline");

            Add("DialBackground");
            Add("DialPointer");

            Add("PopupBackground");
            Add("PluginBackground");
            Add("ButtonPressed");
            Add("ButtonUnpressed");

            Add("LevelDisplay");
            Add("LevelDisplayHorizontal");

            Add("VerticalSlider");

            AddSvg("FileOpen", 50);
            AddSvg("Reload", 50);

            PopDirectory();

            EndSpriteSheetGroup();
        }
    }


    class Program
    {
        static void Main(string[] args)
        {
            var processor = new FaustImageProcessor();

            string path = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), @"..\..\..\..");

            processor.ForceRegen = false;

            processor.SrcPath = Path.Combine(path, "SrcTextures");

            processor.RenderImages(Path.Combine(path, @"FaustVst\Content\Textures"));
        }

    }
}

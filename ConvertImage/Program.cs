using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace ConvertImage
{
    class Program
    {
        static void Main(string[] args)
        {
            if (args.Length != 4) Environment.Exit(-1);
            var input = args[0];
            var output = args[1];
            var fmt = args[2];
            var mode = args[3];
            if (string.IsNullOrEmpty(input) || string.IsNullOrEmpty(output)
                || string.IsNullOrEmpty(fmt))
                Environment.Exit(-1);

            var outFmt = ConvertFmt(fmt);
            ConvertImage(input, output, outFmt, mode.ToLower() == "grayscale");
        }

        static IDictionary<string, ImageFormat> supported =
            typeof(ImageFormat).GetProperties(BindingFlags.Public | BindingFlags.Static)
                .ToDictionary(p => p.Name.ToLower(), p => (ImageFormat)p.GetValue(null));
        static ImageFormat ConvertFmt(string fmt)
        {
            fmt = fmt.ToLower();
            switch (fmt)
            {
                case "jpg": fmt = "jpeg"; break;
                case "tif": fmt = "tiff"; break;
                case "ico": fmt = "icon"; break;
            }
            if (supported.ContainsKey(fmt.ToLower()))
                return supported[fmt];
            throw new NotSupportedException("Cannot convert to image format " + fmt);
        }

        static ColorMatrix colorToGrayMatrix = new ColorMatrix(
            new float[][]
            {
                                    new float[] {.299f, .299f, .299f, 0, 0},
                                    new float[] {.587f, .587f, .587f, 0, 0},
                                    new float[] {.114f, .114f, .114f, 0, 0},
                                    new float[] {0, 0, 0, 1, 0},
                                    new float[] {0, 0, 0, 0, 1}
            });
        private static void ConvertImage(string input, string output, ImageFormat outFmt, bool isGrayscale)
        {
            using (var img = Image.FromFile(input))
            {
                using (var b = new Bitmap(img.Width, img.Height))
                {
                    b.SetResolution(img.HorizontalResolution, img.VerticalResolution);

                    using (var g = Graphics.FromImage(b))
                    {
                        g.Clear(Color.White);

                        if (isGrayscale)
                        {
                            var attributes = new ImageAttributes();
                            attributes.SetColorMatrix(colorToGrayMatrix);
                            g.DrawImage(img, new Rectangle(0, 0, img.Width, img.Height),
                                0, 0, img.Width, img.Height,
                                GraphicsUnit.Pixel, attributes);
                        }
                        else
                            g.DrawImageUnscaled(img, 0, 0);
                    }

                    b.Save(output, outFmt);
                }
            }
        }
    }
}

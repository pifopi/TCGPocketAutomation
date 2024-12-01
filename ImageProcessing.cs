//#define SAVE_IMAGE

using AdvancedSharpAdbClient.Models;
using OpenCvSharp;

namespace TCGPocketAutomation
{
    public class ImageProcessing
    {
        private static readonly Mat titleScreenTemplate = Cv2.ImRead("titleScreen.png");
        private static readonly Mat wonderPickTemplate = Cv2.ImRead("wonderPick.png");
        private static readonly Mat bonusWonderPickTemplate = Cv2.ImRead("bonusWonderPick.png");
        private static readonly Mat OKTemplate = Cv2.ImRead("OK.png");
        private static readonly Mat cardTemplate = Cv2.ImRead("card.png");

        private static Mat AsImage(Framebuffer framebuffer)
        {
            if (framebuffer.Header.Red.Length != 8 ||
                framebuffer.Header.Green.Length != 8 ||
                framebuffer.Header.Blue.Length != 8 ||
                framebuffer.Header.Alpha.Length != 8 ||
                framebuffer.Header.Red.Offset != 0 ||
                framebuffer.Header.Green.Offset != 8 ||
                framebuffer.Header.Blue.Offset != 16 ||
                framebuffer.Header.Alpha.Offset != 24)
            {
                throw new Exception("The screenshot color are not encoded in the expected way");
            }

            int height = (int)framebuffer.Header.Height;
            int width = (int)framebuffer.Header.Width;
            Mat mat = new Mat(height, width, MatType.CV_8UC3);

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    int sourceIndex = (y * width + x) * 4;

                    byte red = framebuffer.Data[sourceIndex + 0];
                    byte green = framebuffer.Data[sourceIndex + 1];
                    byte blue = framebuffer.Data[sourceIndex + 2];

                    mat.Set(y, x, new Vec3b(blue, green, red));
                }
            }
#if SAVE_IMAGE
            mat.SaveImage("AsImage.png");
#endif
            return mat;
        }

        private static (bool, double, System.Drawing.Point) Search(Mat screen, Mat template)
        {
            double ratio = screen.Width / 540;
            screen = screen.Resize(new Size { Width = 540, Height = (int)(screen.Height / ratio)}); ;

            Mat result = screen.MatchTemplate(template, TemplateMatchModes.CCoeffNormed);
            Cv2.MinMaxLoc(result, out double minVal, out double maxVal, out Point minLoc, out Point maxLoc);

#if SAVE_IMAGE
            Rect matchRect = new Rect(maxLoc, new Size(template.Width, template.Height));
            Cv2.Rectangle(screen, matchRect, Scalar.Red, 2);
            screen.SaveImage("Search.png");
#endif
            return (maxVal > 0.8, maxVal, new System.Drawing.Point((int)((maxLoc.X + template.Width / 2) * ratio), (int)((maxLoc.Y + template.Height / 2) * ratio)));
        }

        public static (bool, double, System.Drawing.Point) SearchTitleScreen(Framebuffer framebuffer)
        {
            return Search(AsImage(framebuffer), titleScreenTemplate);
        }

        public static (bool, double, System.Drawing.Point) SearchWonderPick(Framebuffer framebuffer)
        {
            return Search(AsImage(framebuffer), wonderPickTemplate);
        }

        public static (bool, double, System.Drawing.Point) SearchBonusWonderPick(Framebuffer framebuffer)
        {
            return Search(AsImage(framebuffer), bonusWonderPickTemplate);
        }

        public static (bool, double, System.Drawing.Point) SearchOK(Framebuffer framebuffer)
        {
            return Search(AsImage(framebuffer), OKTemplate);
        }

        public static (bool, double, System.Drawing.Point) SearchCard(Framebuffer framebuffer)
        {
            return Search(AsImage(framebuffer), cardTemplate);
        }
    }
}

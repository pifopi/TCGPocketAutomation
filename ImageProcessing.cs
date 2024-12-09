namespace TCGPocketAutomation
{
    public static class ImageProcessing
    {
        private static OpenCvSharp.Mat titleScreenTemplate = OpenCvSharp.Cv2.ImRead("data/titleScreen.png");
        private static OpenCvSharp.Mat whiteScreenTemplate = OpenCvSharp.Cv2.ImRead("data/whiteScreen.png");
        private static OpenCvSharp.Mat wonderPickButtonTemplate = OpenCvSharp.Cv2.ImRead("data/wonderPickButton.png");
        private static OpenCvSharp.Mat wonderPickMenuTemplate = OpenCvSharp.Cv2.ImRead("data/wonderPickMenu.png");
        private static OpenCvSharp.Mat bonusWonderPickTemplate = OpenCvSharp.Cv2.ImRead("data/bonusWonderPick.png");
        private static OpenCvSharp.Mat OKTemplate = OpenCvSharp.Cv2.ImRead("data/OK.png");
        private static OpenCvSharp.Mat cardTemplate = OpenCvSharp.Cv2.ImRead("data/card.png");
        private static OpenCvSharp.Mat registerNewCardTemplate = OpenCvSharp.Cv2.ImRead("data/registerNewCard.png");

        private static (double, System.Drawing.Point)? Search(OpenCvSharp.Mat screen, OpenCvSharp.Mat template)
        {
            double ratio = screen.Width / 540;
            screen = screen.Resize(new OpenCvSharp.Size { Width = 540, Height = (int)(screen.Height / ratio) }); ;

            OpenCvSharp.Mat result = screen.MatchTemplate(template, OpenCvSharp.TemplateMatchModes.CCoeffNormed);
            OpenCvSharp.Cv2.MinMaxLoc(result, out double minVal, out double maxVal, out OpenCvSharp.Point minLoc, out OpenCvSharp.Point maxLoc);

            if (maxVal < 0.8)
            {
                return null;
            }
            return (maxVal, new System.Drawing.Point((int)((maxLoc.X + template.Width / 2) * ratio), (int)((maxLoc.Y + template.Height / 2) * ratio)));
        }

        public static (double, System.Drawing.Point)? SearchTitleScreen(OpenCvSharp.Mat image)
        {
            return Search(image, titleScreenTemplate);
        }

        public static (double, System.Drawing.Point)? SearchWhiteScreen(OpenCvSharp.Mat image)
        {
            return Search(image, whiteScreenTemplate);
        }

        public static (double, System.Drawing.Point)? SearchWonderPickButton(OpenCvSharp.Mat image)
        {
            return Search(image, wonderPickButtonTemplate);
        }

        public static (double, System.Drawing.Point)? SearchWonderPickMenu(OpenCvSharp.Mat image)
        {
            return Search(image, wonderPickMenuTemplate);
        }

        public static (double, System.Drawing.Point)? SearchBonusWonderPick(OpenCvSharp.Mat image)
        {
            return Search(image, bonusWonderPickTemplate);
        }

        public static (double, System.Drawing.Point)? SearchOK(OpenCvSharp.Mat image)
        {
            return Search(image, OKTemplate);
        }

        public static (double, System.Drawing.Point)? SearchCard(OpenCvSharp.Mat image)
        {
            return Search(image, cardTemplate);
        }

        public static (double, System.Drawing.Point)? SearchRegisterNewCard(OpenCvSharp.Mat image)
        {
            return Search(image, registerNewCardTemplate);
        }
    }
}

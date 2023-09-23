using BiometricPhotoChecker;
using BiometricPhotoChecker.Service;
using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;
using System.Drawing;
using System.Drawing.Imaging;

public class BiometricPhotoValidationService : IBiometricPhotoValidationService
{
    private static string faceFileName = Path.Combine(Directory.GetParent(System.Environment.CurrentDirectory).Parent.Parent.FullName, "haarcascades", "haarcascade_frontalface_default.xml");
    private static string smileFileName = Path.Combine(Directory.GetParent(System.Environment.CurrentDirectory).Parent.Parent.FullName, "haarcascades", "haarcascade_smile.xml");
    private static string eyeFileName = Path.Combine(Directory.GetParent(System.Environment.CurrentDirectory).Parent.Parent.FullName, "haarcascades", "haarcascade_eye.xml");
    private static string mouthFileName = Path.Combine(Directory.GetParent(System.Environment.CurrentDirectory).Parent.Parent.FullName, "haarcascades", "haarcascade_mcs_mouth.xml");
    private static string noseFileName = Path.Combine(Directory.GetParent(System.Environment.CurrentDirectory).Parent.Parent.FullName, "haarcascades", "haarcascade_mcs_nose.xml");
    private const int MIN_WIDTH = 35; // Minimum genişlik (mm)
    private const int MIN_HEIGHT = 45; // Minimum yükseklik (mm)

    public CheckInfo ValidateBiometricPhoto(byte[] imageBytes)
    {
        CheckInfo checkInfo = new CheckInfo();
        checkInfo.IsImageValid = IsImageValid(imageBytes);
        checkInfo.IsSmileValid = IsSmileValid(imageBytes);
        checkInfo.IsMouthValid = IsMouthValid(imageBytes);
        checkInfo.IsNoseValid = IsNoseValid(imageBytes);
        checkInfo.IsEyesValid = IsEyesValid(imageBytes);
        checkInfo.IsHeadAndShouldersVisible = IsHeadAndShouldersVisible(imageBytes);
        checkInfo.IsBackgroundValid = IsBackgroundValid(imageBytes);
        checkInfo.IsProperLighting = IsProperLighting(imageBytes);
        checkInfo.IsFacePositionValid = IsFacePositionValid(imageBytes);
        checkInfo.IsEyePositionValid = IsEyePositionValid(imageBytes);
        return checkInfo;
    }
    private bool IsFacePositionValid(byte[] imageBytes)
    {
        using (Mat image = LoadImage(imageBytes))
        {
            using (CascadeClassifier faceCascade = LoadCascadeClassifier(faceFileName))
            {
                CvInvoke.CvtColor(image, image, ColorConversion.Bgr2Gray);
                Rectangle[] faces = faceCascade.DetectMultiScale(image, 1.1, 3, Size.Empty, Size.Empty);

                if (faces.Length == 0)
                {
                    return false; // Yüz tespit edilemedi
                }

                Rectangle face = faces[0]; // İlk tespit edilen yüzü kullan
                int imageWidth = image.Width;
                int imageHeight = image.Height;

                // Yüzün öne bakması gereklidir
                double faceCenterX = face.X + face.Width / 2;
                double faceCenterY = face.Y + face.Height / 2;
                double faceCenterThresholdX = 0.1 * imageWidth; // Yatayda %10 sapma izin verilebilir

                if (Math.Abs(faceCenterX - imageWidth / 2) > faceCenterThresholdX)
                {
                    return false; // Yüz yatayda çok fazla sapmış
                }

                double faceCenterThresholdY = 0.1 * imageHeight; // Dikeyde %10 sapma izin verilebilir

                if (Math.Abs(faceCenterY - imageHeight / 2) > faceCenterThresholdY)
                {
                    return false; // Yüz dikeyde çok fazla sapmış
                }

                return true;
            }
        }
    }

    private bool IsEyePositionValid(byte[] imageBytes)
    {
        using (Mat image = LoadImage(imageBytes))
        {
            using (CascadeClassifier faceCascade = LoadCascadeClassifier(faceFileName))
            using (CascadeClassifier eyeCascade = LoadCascadeClassifier(eyeFileName))
            {
                CvInvoke.CvtColor(image, image, ColorConversion.Bgr2Gray);
                Rectangle[] faces = faceCascade.DetectMultiScale(image, 1.1, 3, Size.Empty, Size.Empty);

                if (faces.Length == 0)
                {
                    return false; // Yüz tespit edilemedi
                }

                Rectangle face = faces[0]; // İlk tespit edilen yüzü kullan

                Rectangle[] eyes = eyeCascade.DetectMultiScale(image, 1.1, 3, Size.Empty, Size.Empty);

                if (eyes.Length < 2)
                {
                    return false; // İki göz tespit edilemedi
                }

                Rectangle leftEye = eyes[0]; // İlk gözü kullan
                Rectangle rightEye = eyes[1]; // İkinci gözü kullan
                int imageWidth = image.Width;
                int imageHeight = image.Height;

                // Gözler aynı yükseklikte olmalıdır
                double leftEyeCenterY = leftEye.Y + leftEye.Height / 2;
                double rightEyeCenterY = rightEye.Y + rightEye.Height / 2;
                double eyeCenterThresholdY = 0.05 * imageHeight; // Dikeyde %5 sapma izin verilebilir

                if (Math.Abs(leftEyeCenterY - rightEyeCenterY) > eyeCenterThresholdY)
                {
                    return false; // Gözler dikeyde çok fazla sapmış
                }

                // Gözler arasındaki yatay mesafe belirli bir aralıkta olmalıdır
                double eyeDistanceThreshold = 0.2 * imageWidth; // Yatayda %20 sapma izin verilebilir
                double leftEyeCenterX = leftEye.X + leftEye.Width / 2;
                double rightEyeCenterX = rightEye.X + rightEye.Width / 2;
                double distanceBetweenEyes = Math.Abs(leftEyeCenterX - rightEyeCenterX);

                if (distanceBetweenEyes < eyeDistanceThreshold)
                {
                    return false; // Gözler arası mesafe çok dar
                }

                // Gözlerin merkezleri yüzün merkezine yakın olmalıdır
                double eyeCenterToFaceCenterThreshold = 0.2 * imageWidth; // Yatayda %20 sapma izin verilebilir
                double faceCenterX = imageWidth / 2;

                if (Math.Abs(leftEyeCenterX - faceCenterX) > eyeCenterToFaceCenterThreshold ||
                    Math.Abs(rightEyeCenterX - faceCenterX) > eyeCenterToFaceCenterThreshold)
                {
                    return false; // Gözler yüz merkezine çok uzak
                }

                return true;
            }
        }
    } 

    private bool IsImageValid(byte[] imageBytes)
    {
        using (MemoryStream ms = new MemoryStream(imageBytes))
        {
            System.Drawing.Image image = System.Drawing.Image.FromStream(ms);
            int width = image.Width; // Görüntünün genişliği (mm)
            int height = image.Height; // Görüntünün yüksekliği (mm)

            return (width >= MIN_WIDTH && height >= MIN_HEIGHT);
        }
    }
    private bool IsSmileValid(byte[] imageBytes)
    {
        using (Mat image = LoadImage(imageBytes))
        {
            using (CascadeClassifier faceCascade = LoadCascadeClassifier(faceFileName))
            using (CascadeClassifier smileCascade = LoadCascadeClassifier(smileFileName))
            {
                CvInvoke.CvtColor(image, image, ColorConversion.Bgr2Gray);

                Rectangle[] faces = faceCascade.DetectMultiScale(image, 1.1, 3, Size.Empty, Size.Empty);

                foreach (Rectangle face in faces)
                {
                    Mat faceRegion = new Mat(image, face);
                    CvInvoke.EqualizeHist(faceRegion, faceRegion); // Histogram eşitleme uygula

                    Rectangle[] smiles = smileCascade.DetectMultiScale(faceRegion, 1.8, 20, Size.Empty, Size.Empty);

                    foreach (Rectangle smile in smiles)
                    {
                        // Gülümseme tespiti bulunduğunda false döndürün
                        return false;
                    }
                }
            }
        }

        // Gülümseme tespiti bulunamadığında veya yüz tespiti yapılamadığında true döndürün
        return true;
    }


    private bool IsEyesValid(byte[] imageBytes)
    {
        using (Mat image = LoadImage(imageBytes))
        {
            using (CascadeClassifier eyeCascade = LoadCascadeClassifier(eyeFileName))
            {
                CvInvoke.CvtColor(image, image, ColorConversion.Bgr2Gray);

                Rectangle[] eyes = eyeCascade.DetectMultiScale(image, 1.1, 3, Size.Empty, Size.Empty);

                return eyes.Length >= 2;
            }
        }
    }
    private bool IsMouthValid(byte[] imageBytes)
    {
        using (Mat image = LoadImage(imageBytes))
        {
            using (CascadeClassifier mouthCascade = LoadCascadeClassifier(mouthFileName))
            {
                CvInvoke.CvtColor(image, image, ColorConversion.Bgr2Gray);

                Rectangle[] mouths = mouthCascade.DetectMultiScale(image, 1.1, 3, Size.Empty, Size.Empty);

                return mouths.Length >= 1;
            }
        }
    }

    private bool IsNoseValid(byte[] imageBytes)
    {
        using (Mat image = LoadImage(imageBytes))
        {
            using (CascadeClassifier noseCascade = LoadCascadeClassifier(noseFileName))
            {
                CvInvoke.CvtColor(image, image, ColorConversion.Bgr2Gray);

                Rectangle[] noses = noseCascade.DetectMultiScale(image, 1.1, 3, Size.Empty, Size.Empty);

                return noses.Length >= 1;
            }
        }
    }
    private bool IsHeadAndShouldersVisible(byte[] imageBytes)
    {
        using (Mat image = LoadImage(imageBytes))
        {
            using (CascadeClassifier faceCascade = LoadCascadeClassifier(faceFileName))
            {
                CvInvoke.CvtColor(image, image, ColorConversion.Bgr2Gray);

                Rectangle[] faces = faceCascade.DetectMultiScale(image, 1.1, 3, Size.Empty, Size.Empty);

                foreach (Rectangle face in faces)
                {
                    Mat faceRegion = new Mat(image, face); 

                    bool headAndShouldersVisible = CheckHeadAndShoulders(faceRegion);

                    if (!headAndShouldersVisible)
                    {
                        return false;
                    }
                }
            }
        }

        return true;
    }

    private bool CheckHeadAndShoulders(Mat faceRegion)
    {
        // Yüz bölgesinin tamamının beyaz piksellerini sayın
        int totalPixels = faceRegion.Width * faceRegion.Height;
        int whitePixels = CvInvoke.CountNonZero(faceRegion);
        double visibleArea = (double)whitePixels / totalPixels;

        // Baş ve omuzlar en az %90 görünürse true döndürün (eşik değeri değiştirilebilir)
        return (visibleArea >= 0.90);
    }


    private bool IsBackgroundValid(byte[] imageBytes)
    {
        using (Mat image = LoadImage(imageBytes))
        {
            Bitmap imageBM = ConvertToBitmap(image);
            int bgRectWidth = image.Width / 10;
            int bgRectHeight = image.Height / 10;

            Rectangle topLeftRect = new Rectangle(5, 5, bgRectWidth, bgRectHeight);
            Color averageColorTopLeft = CalculateAverageColor(imageBM, topLeftRect);

            Rectangle topRightRect = new Rectangle(image.Width - bgRectWidth - 5, 5, bgRectWidth, bgRectHeight);
            Color averageColorTopRight = CalculateAverageColor(imageBM, topRightRect);

            Color RGB = Color.FromArgb((int)(averageColorTopLeft.R + averageColorTopRight.R) / 2, (int)(averageColorTopLeft.G + averageColorTopRight.G) / 2, (int)(averageColorTopLeft.B + averageColorTopRight.B) / 2);

            return (RGB.R >= 200 && RGB.G >= 200 && RGB.B >= 200);
        }
    }

    private Color CalculateAverageColor(Bitmap imageBM, Rectangle rect)
    {
        double R = 0;
        double G = 0;
        double B = 0;

        for (int x = rect.X; x < rect.X + rect.Width; x++)
        {
            for (int y = rect.Y; y < rect.Y + rect.Height; y++)
            {
                Color pixelColor = imageBM.GetPixel(x, y);
                R = R + pixelColor.R;
                G = G + pixelColor.G;
                B = B + pixelColor.B;
            }
        }

        R = R / (rect.Height * rect.Width);
        G = G / (rect.Height * rect.Width);
        B = B / (rect.Height * rect.Width);

        return Color.FromArgb((int)R, (int)G, (int)B);
    }

    private bool IsProperLighting(byte[] imageBytes)
    {
        using (MemoryStream ms = new MemoryStream(imageBytes))
        {
            Bitmap image = new Bitmap(ms);

            double totalBrightness = 0;

            for (int x = 0; x < image.Width; x++)
            {
                for (int y = 0; y < image.Height; y++)
                {
                    Color pixelColor = image.GetPixel(x, y);
                    totalBrightness += (0.299 * pixelColor.R + 0.587 * pixelColor.G + 0.114 * pixelColor.B);
                }
            }

            double averageBrightness = totalBrightness / (image.Width * image.Height);

            return (averageBrightness >= 80 && averageBrightness <= 240);
        }
    }

    private Mat LoadImage(byte[] imageBytes)
    {
        Mat image = new Mat();
        CvInvoke.Imdecode(imageBytes, ImreadModes.Color, image);
        return image;
    }

    private CascadeClassifier LoadCascadeClassifier(string fileName)
    {
        return new CascadeClassifier(fileName);
    }

    private Bitmap ConvertToBitmap(Mat image)
    {
        using (MemoryStream ms = new MemoryStream())
        {
            image.ToImage<Bgr, byte>().ToBitmap().Save(ms, ImageFormat.Jpeg);
            return new Bitmap(ms);
        }
    }
}

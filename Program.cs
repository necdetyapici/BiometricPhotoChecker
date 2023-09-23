using BiometricPhotoChecker;

string photoPath = Path.Combine(Directory.GetParent(System.Environment.CurrentDirectory).Parent.Parent.FullName, "img", "biyo.png");
byte[] imageArray = System.IO.File.ReadAllBytes(photoPath);

BiometricPhotoValidationService service = new BiometricPhotoValidationService();
CheckInfo result = service.ValidateBiometricPhoto(imageArray);
Console.WriteLine(nameof(result.IsImageValid) + " - " + result.IsImageValid);
Console.WriteLine(nameof(result.IsSmileValid) + " - " + result.IsSmileValid);
Console.WriteLine(nameof(result.IsMouthValid) + " - " + result.IsMouthValid);
Console.WriteLine(nameof(result.IsNoseValid) + " - " + result.IsNoseValid);
Console.WriteLine(nameof(result.IsEyesValid) + " - " + result.IsEyesValid);
Console.WriteLine(nameof(result.IsHeadAndShouldersVisible) + " - " + result.IsHeadAndShouldersVisible);
Console.WriteLine(nameof(result.IsBackgroundValid) + " - " + result.IsBackgroundValid);
Console.WriteLine(nameof(result.IsProperLighting) + " - " + result.IsProperLighting);
Console.WriteLine(nameof(result.IsFacePositionValid) + " - " + result.IsFacePositionValid);
Console.WriteLine(nameof(result.IsEyePositionValid) + " - " + result.IsEyePositionValid);
Console.ReadKey();
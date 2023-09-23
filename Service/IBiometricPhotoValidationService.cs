namespace BiometricPhotoChecker.Service
{
    public interface IBiometricPhotoValidationService
    {
        CheckInfo ValidateBiometricPhoto(byte[] imageBytes); 
    }
}

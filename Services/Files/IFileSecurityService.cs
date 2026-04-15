namespace eProtokoll.Services.Files
{
    public interface IFileSecurityService
    {
        byte[] Encrypt(byte[] data);
        byte[] Decrypt(byte[] encryptedData);
    }
}
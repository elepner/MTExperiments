namespace Messaging.Contracts
{
    public interface CaesarCipher
    {
        string Message { get; set; }
        int Offset { get; set; }
    }
    public class CaesarCipherImpl : CaesarCipher
    {
        public string Message { get; set; }
        public int Offset { get; set; }
    }
}
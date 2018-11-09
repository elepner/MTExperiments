using System;

namespace Messaging.Contracts
{
    public class Class1
    {
    }

    public interface ChangeCaseCommand
    {
        string Message { get; set; }
    }

    public class ChangeCaseCommandImpl : ChangeCaseCommand
    {
        public string Message { get; set; }
    }

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

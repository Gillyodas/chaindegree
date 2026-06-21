using System;
using System.Collections.Generic;
using System.Text;

namespace ChainDegree.Core.Domain.Degrees.ValueObjects
{
    public class CryptoSnapshot
    {
        public string PlainDataJson { get; private set; } = null!;
        public string Salt { get; private set; } = null!;
        public string DataHashLocal { get; private set; } = null!;

        public bool VerifyLocal(string calculatedHash)
        {
            throw new NotImplementedException();
        }
    }
}

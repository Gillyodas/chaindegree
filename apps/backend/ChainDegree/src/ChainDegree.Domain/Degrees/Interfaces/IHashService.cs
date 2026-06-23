using System;
using System.Collections.Generic;
using System.Text;
using ChainDegree.SharedKernel.Result;

namespace ChainDegree.Core.Domain.Degrees.Interfaces
{
    public interface IHashService
    {
        Result<string> GenerateSalt();
        Result<string> HashData(string plainText, string salt);
    }
}

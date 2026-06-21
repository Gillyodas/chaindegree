namespace ChainDegree.SharedKernel.Common.Error
{
    public interface IErrorCatalog
    {
        string? GetCodeByMessage(string message);
    }
}

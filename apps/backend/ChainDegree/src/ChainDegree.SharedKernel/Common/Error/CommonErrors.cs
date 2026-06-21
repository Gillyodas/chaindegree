namespace ChainDegree.SharedKernel.Common.Error
{
    public static class CommonErrors
    {
        // L?i 500: L?i c?u hình server
        public static readonly Error SystemConfigurationError = Error.Failure(
            "System.ConfigurationError", "System configuration is invalid. Please contact administrator.");

        // L?i 403: Sai Master Key
        public static readonly Error InvalidMasterKey = Error.Forbidden(
            "Auth.InvalidMasterKey", "The provided Master Key is incorrect.");
    }
}

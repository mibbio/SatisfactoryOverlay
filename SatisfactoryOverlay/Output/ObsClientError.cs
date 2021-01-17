namespace SatisfactoryOverlay.Output
{
    public sealed class ObsClientError
    {
        public ObsClientErrorType ErrorType { get; private set; }

        public string Message { get; private set; }

        public ObsClientError(ObsClientErrorType errorType) : this(errorType, string.Empty) { }

        public ObsClientError(ObsClientErrorType errorType, string message)
        {
            ErrorType = errorType;
            Message = message;
        }
    }
}
using System;

namespace CodePulse.Client.Errors
{
    public interface IErrorHandler
    {
        event EventHandler<Tuple<string, Exception>> ErrorOccurred;

        void HandleError(string errorMessage, Exception exception);
    }
}

using System;

namespace CodePulse.Client.Errors
{
    public class ErrorHandler : IErrorHandler
    {
        public event EventHandler<Tuple<string, Exception>> ErrorOccurred;

        public void HandleError(string errorMessage)
        {
            HandleError(errorMessage, null);
        }

        public void HandleError(string errorMessage, Exception exception)
        {
            try
            {
                OnErrorOccurred(new Tuple<string, Exception>(errorMessage, exception));
            }
            catch (Exception e)
            {
                Console.Error.WriteLine(e);
            }
        }

        protected virtual void OnErrorOccurred(Tuple<string, Exception> e)
        {
            ErrorOccurred?.Invoke(this, e);
        }
    }
}

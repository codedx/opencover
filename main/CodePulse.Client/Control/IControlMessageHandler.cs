namespace CodePulse.Client.Control
{
    public interface IControlMessageHandler
    {
        void OnStart();

        void OnStop();

        void OnPause();

        void OnUnpause();

        void OnSuspend();

        void OnUnsuspend();

        void OnError(string error);
    }
}

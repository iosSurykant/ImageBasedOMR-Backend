namespace SQCScanner.Services
{
    public class OmrProcessingControlService
    {
        // globle state me kaam karta hai, Synchronization mechanism implement karna
        private readonly ManualResetEventSlim _pauseEvent = new ManualResetEventSlim(true); 
        public bool IsProcessingPaused => !_pauseEvent.IsSet;


        // user of power reset
        public void PauseProcessing()
        {
            _pauseEvent.Reset(); 
        }

        public void ResumeProcessing()
        {
            _pauseEvent.Set();
        }

        public void WaitIfPaused()
        {
            _pauseEvent.Wait(); 
        }
    }
}

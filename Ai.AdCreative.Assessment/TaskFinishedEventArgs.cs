namespace Ai.AdCreative.Assessment
{
    public class TaskFinishedEventArgs
    {
        private readonly Downloader _instance;
        private readonly HttpResponseMessage _httpResponseMessage;
        private readonly Action _proceedCallback;
        private readonly Action _reloadCallback;

        public Downloader Instance => _instance;
        public HttpResponseMessage HttpResponseMessage => _httpResponseMessage;
        internal TaskFinishedEventArgs(Downloader instance, HttpResponseMessage httpResponseMessage , Action proceedCallback, Action reloadCallback)
        {
            _instance= instance;
            _httpResponseMessage = httpResponseMessage;
            _proceedCallback = proceedCallback;
            _reloadCallback = reloadCallback;
        }
        public void ProceedRedirection()
        {
            _proceedCallback?.Invoke();
        }
        public void ReloadUrl()
        {
            _reloadCallback?.Invoke();
        }
    }
}

using UnityEngine;

namespace Ragendom
{
    public abstract class LoadingTask
    {
        public enum CompleteStatus
        {
            Completed,
            Skipped,
            Failed
        }

        public delegate void LoadingTaskCallback(LoadingTask task, CompleteStatus status);
        public event LoadingTaskCallback OnTaskCompleted;

        public string TaskName { get; private set; }
        public bool IsCompleted { get; private set; }

        public LoadingTask(string taskName)
        {
            TaskName = taskName;
        }

        public void Activate()
        {
            IsCompleted = false;
            OnTaskActivated();
        }

        protected abstract void OnTaskActivated();

        protected void CompleteTask(CompleteStatus status)
        {
            if (IsCompleted)
                return;

            IsCompleted = true;

            if (Monetization.VerboseLogging)
                Debug.Log($"[Monetization]: Loading task '{TaskName}' completed with status: {status}");

            OnTaskCompleted?.Invoke(this, status);
        }
    }
}

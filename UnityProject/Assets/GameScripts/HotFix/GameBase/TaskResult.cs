using System;
using System.Threading.Tasks;
using GameFramework;

namespace GameBase {

    public class TaskResult<T> : IReference {

        public object UserData { get; private set; }

        public Task<T> Task => source.Task;

        private TaskCompletionSource<T> source;

        public static TaskResult<T> Create(object userData) {
            TaskResult<T> taskResult = ReferencePool.Acquire<TaskResult<T>>();
            taskResult.UserData = userData;
            taskResult.source = new TaskCompletionSource<T>();
            return taskResult;
        }

        public static TaskResult<T> Create(object userData, TaskCompletionSource<T> source) {
            TaskResult<T> taskResult = ReferencePool.Acquire<TaskResult<T>>();
            taskResult.UserData = userData;
            taskResult.source = source;
            return taskResult;
        }

        public void Clear() {
            UserData = null;
            source = null;
        }

        public void SetResult(T result) {
            if (source != null) {
                source.SetResult(result);
            }
        }

        public bool TrySetResult(T result) {
            if (source != null) {
                return source.TrySetResult(result);
            }
            return false;
        }

        public void SetException(Exception exception) {
            if (source != null) {
                source.SetException(exception);
            }
        }
    }

}
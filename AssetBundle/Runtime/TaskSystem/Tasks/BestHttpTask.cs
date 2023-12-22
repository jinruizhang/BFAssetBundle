/****************************************************
文件：BestHttpTask
作者：haitao.li
日期：2023/02/08 19:36:38
功能：包装之后的BestHttpRequest
*****************************************************/

using System;
using BFNetwork;

namespace ResourceTools
{
    public class BestHttpTask : BaseTask
    {
        private string uri;
        private Action<bool,string, object> onFinished;
        private BFNetworkEvent.RequestType requestType;
        private string fileName = String.Empty;

        internal override Delegate FinishedCallback
        {
            get
            {
                return onFinished;
            }

            set
            {
                onFinished = (Action<bool,string, object>)value;
            }
        }
        
        public BestHttpTask(TaskExcutor owner, string name,string uri,  Action<bool,string, object> onFinished) : base(owner, name)
        {
            this.uri = uri;
            this.onFinished = onFinished;
            this.requestType = BFNetworkEvent.RequestType.REQUEST_TEXT;
        }
        
        public BestHttpTask(TaskExcutor owner, string name,string uri,string fileName, Action<bool,string, object> onFinished) : base(owner, name)
        {
            this.uri = uri;
            this.onFinished = onFinished;
            this.requestType = BFNetworkEvent.RequestType.REQUEST_FILE;
            this.fileName = fileName;
        }

        public override void Execute()
        {
            BFHttpManager.GetInstance().SendMessage(requestType, uri, fileName, String.Empty, null, OnResponseFinish);
            TaskState = TaskStatus.Executing;
        }
        

        private void  OnResponseFinish(int code, object data, string message)
        {

            TaskState = TaskStatus.Finished;

            if (code != 200)
            {
                onFinished?.Invoke(false, message, null);
                return;
            }
            onFinished?.Invoke(true, message, data);

        }

        public override void Update()
        {
            
        }
        
    }
}
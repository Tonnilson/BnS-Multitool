using BnS_Multitool.Messages;
using CommunityToolkit.Mvvm.Messaging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BnS_Multitool.Services
{
    public class MessageService
    {
        private readonly Queue<MessagePrompt> _queue;
        private bool _queueRunning;
        public TaskCompletionSource<bool> MessageClosed;

        public MessageService()
        {
            _queue = new Queue<MessagePrompt>();
            _queueRunning = false;
            MessageClosed = new TaskCompletionSource<bool>();
        }

        public void Enqueue(MessagePrompt prompt)
        {
            _queue.Enqueue(prompt);
            if (!_queueRunning)
                Task.Run(() => Task.FromResult(ProcessQueue()));
        }

        private async Task ProcessQueue()
        {
            _queueRunning = true;
            while(true)
            {
                if (!_queue.Any()) break;
                MessagePrompt item;
                if(_queue.TryDequeue(out item))
                {
                    WeakReferenceMessenger.Default.Send(new ShowMessagePrompt(item));
                    await MessageClosed.Task;
                    MessageClosed = new TaskCompletionSource<bool>();
                    await Task.Delay(25);
                }
            }

            _queueRunning = false;
            MessageClosed = new TaskCompletionSource<bool>();
        }

        public class MessagePrompt
        {
            public string Message { get; set; } = string.Empty;
            public bool IsError { get; set; } = false;
            public bool UseBold { get; set; } = false;
        }
    }
}

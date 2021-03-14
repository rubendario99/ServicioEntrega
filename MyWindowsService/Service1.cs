using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MyWindowsService
{
    public partial class ChatServer : ServiceBase
    {
        ServerChat serverChat;
        Thread thread;

        public ChatServer()
        {
            InitializeComponent();
        }
        protected override void OnStart(string[] args)
        {
            serverChat = new ServerChat();
            thread = new Thread(serverChat.serverStart);
            thread.IsBackground = true;
            thread.Start();
        }
        protected override void OnStop()
        {
            serverChat.escribeEvento("Stopping service");
            serverChat.hasEnded = true;
        }
        protected override void OnPause()
        {
            serverChat.escribeEvento("OnPause service");
            serverChat.hasEnded = true;
        }
    }
}


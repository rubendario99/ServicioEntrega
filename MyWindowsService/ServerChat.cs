using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Configuration;
using System.Globalization;

namespace MyWindowsService
{
    class ServerChat
    {
        public static readonly object l = new object();
        public bool hasEnded;

        static List<StreamWriter> clientSWList = new List<StreamWriter>();
        static ArrayList usernameList = new ArrayList();
        public EventWaitHandle waitHandle = new AutoResetEvent(false);
        public Thread tClient;

        public ServerChat()
        {
            hasEnded = false;
        }

        public void serverStart()
        {

            escribeEvento("Starting server");

            IPEndPoint ie;
            bool isPortFree;
            int port;

            if (ConfigurationManager.AppSettings["Port"] != null)
            {
                port = Convert.ToInt32(ConfigurationManager.AppSettings["Port"]);
            }
            else
            {
                port = 22222;
            }

            using (Socket s = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
            {
                do
                {
                    isPortFree = true;
                    ie = new IPEndPoint(IPAddress.Any, port);

                    try
                    {
                        s.Bind(ie);
                    }
                    catch (SocketException se)
                    {
                        escribeEvento("Busy ports");
                        isPortFree = false;
                        port++;
                    }

                } while (!isPortFree);

                s.Listen(10);
                Console.WriteLine("Server waiting at port {0}", ie.Port);
                escribeEvento(String.Format("Server waiting at port {0}", ie.Port));

                while (!hasEnded)
                {
                    Socket sClient = s.Accept();
                    if (hasEnded)
                    {
                        sClient.Shutdown(SocketShutdown.Both);
                        tClient.Abort();
                        waitHandle.WaitOne();
                    }
                    tClient = new Thread(clientThread);
                    tClient.IsBackground = true;
                    tClient.Start(sClient);
                }
            }
        }

        public void clientThread(object socket)
        {
            escribeEvento("Client thread starting");

            bool connected = true;
            string msg;

            Socket sClient = (Socket)socket;
            IPEndPoint ieClient = (IPEndPoint)sClient.RemoteEndPoint;

            escribeEvento(String.Format("Connected with client: {0} in port: {1}", ieClient.Address, ieClient.Port));

            using (NetworkStream ns = new NetworkStream(sClient))
            using (StreamReader sr = new StreamReader(ns))
            using (StreamWriter sw = new StreamWriter(ns))
            {
                string welcome = "Welcome to the chat, write your username";
                sw.WriteLine(welcome);
                sw.Flush();

                string username = sr.ReadLine();
                if (username != null)
                {
                    sw.WriteLine("Hi " + username + ", welcome to the chat");
                    sw.Flush();

                    for (int i = 0; i < clientSWList.Count; i++)
                    {
                        clientSWList[i].WriteLine(String.Format("{0} is connected", username));
                        clientSWList[i].Flush();
                    }

                    clientSWList.Add(sw);
                    usernameList.Add(username + "@" + ieClient.Address);

                    while (connected)
                    {
                        try
                        {
                            msg = sr.ReadLine();

                            lock (l)
                            {
                                Console.WriteLine(msg != null ? msg : "Client disconnected");

                                if (msg == null || msg.ToLower().Equals("exit"))
                                {
                                    usernameList.Remove(username + "@" + ieClient.Address);
                                    clientSWList.Remove(sw);
                                    connected = false;
                                }

                                for (int i = clientSWList.Count - 1; i >= 0; i--)
                                {
                                    if (msg == null || msg.Equals("exit"))
                                    {
                                        clientSWList[i].WriteLine(String.Format("{0}@{1} disconected", ieClient.Address, username));
                                        clientSWList[i].Flush();
                                    }
                                    else
                                    {
                                        if (clientSWList[i] != sw && !msg.ToLower().Equals("list"))
                                        {
                                            try
                                            {
                                                clientSWList[i].WriteLine(String.Format("{0}@{1}:--{2}", username, ieClient.Address, msg));
                                                clientSWList[i].Flush();
                                            }
                                            catch (Exception e)
                                            {
                                                Console.WriteLine("Exception: " + e.Message);
                                            }
                                        }

                                        if (msg.ToLower().Equals("list"))
                                        {
                                            sw.WriteLine(usernameList[i]);
                                            sw.Flush();
                                        }
                                    }
                                }
                            }
                        }
                        catch (IOException e)
                        {
                            break;
                        }
                    }
                }
                escribeEvento("connection closed");
                sClient.Close();
            }
        }
        public void escribeEvento(string msg)
        {
            string name = "Servidor de chat";
            string logDestino = "Application";

            if (!EventLog.SourceExists(name))
            {
                EventLog.CreateEventSource(name, logDestino);
            }
            EventLog.WriteEntry(name, msg);
        }
    }
}

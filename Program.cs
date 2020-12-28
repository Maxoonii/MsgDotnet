using System;
using System.IO;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;


namespace Message
{
    class Program
    {
        private static event RequestHandler Request;

        private static Msg[] msgs = new Msg[5];

        private static int count = 0;

        private static int c = 1;

        static void Main(string[] args)
        {
            HttpListener http = new HttpListener();
            http.Prefixes.Add("http://127.0.0.1:3000/");
            Request += Handler;
            CancellationToken cancellationToken = new CancellationToken();
            Task t = new Task(() =>
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    HttpListenerContext context = http.GetContext();
                    if (Request != null)
                        Task.Run(() => Request(context.Request, context.Response));
                    else
                        context.Response.Abort();
                }
            });
            http.Start();
            t.Start();
            Console.WriteLine("Hello World!");
            string enter;
            while(http.IsListening)
            {
                enter = Console.ReadLine();
                switch(enter)
                {
                    case "close":
                    case "stop":
                    case "exit":
                        http.Stop();
                        break;
                }
            }
        }

        private static void Handler(HttpListenerRequest req, HttpListenerResponse res)
        {
            switch (req.RawUrl)
            {
                case "/":
                    if(req.HttpMethod != "GET")
                    {
                        SendBadReq(res);
                        break;
                    }
                    SendPage(res, "index.html");
                    break;
                case "/message":
                    if (req.HttpMethod != "GET")
                    {
                        SendBadReq(res);
                        break;
                    }
                    res.StatusCode = 200;
                    res.ContentType = "application/json";
                    Msg[] temp_msg = new Msg[count];
                    Array.Copy(msgs, temp_msg, count);
                    res.OutputStream.Write(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(temp_msg)));
                    res.Close();
                    break;
                case "/add-msg":
                    if (req.HttpMethod != "POST")
                    {
                        SendBadReq(res);
                        break;
                    }
                    byte[] buffer = new byte[1024];
                    for(int i = 0; ; i++)
                    {
                        int t = req.InputStream.ReadByte();
                        if (t == -1)
                        {
                            Array.Resize(ref buffer, i);
                            break;
                        }
                        buffer[i] = (byte)t;
                    }
                    Msg temp = JsonSerializer.Deserialize<Msg>(Encoding.UTF8.GetString(buffer));
                    temp.nomer = c++;
                    if (count == 5)
                    {
                        for (int i = msgs.Length - 1; i > 0; i--)
                            msgs[i] = msgs[i - 1];
                        msgs[0] = temp;
                    }
                    else
                        msgs[count++] = temp;
                    res.StatusCode = 202;
                    res.Close();
                    break;
                default:
                    SendNotFound(res);
                    break;
            }
        }

        private static void SendBadReq(HttpListenerResponse res)
        {
            res.StatusCode = 400;
            res.Close();
        }

        private static void SendNotFound(HttpListenerResponse res)
        {
            res.StatusCode = 404;
            res.Close();
        }

        private static void SendPage(HttpListenerResponse res, string path)
        {
            res.StatusCode = 200;
            res.ContentType = "text/html";
            res.AddHeader("Charset", "UTF-8");
            BinaryReader reader = new BinaryReader(new FileStream(path, FileMode.Open, FileAccess.Read));
            res.OutputStream.Write(reader.ReadBytes((int)reader.BaseStream.Length), 0, (int)reader.BaseStream.Length);
            res.Close();
        }
        class Msg
        {
            public int nomer{get;set;}
            public string text{get;set;}

            
        }
        delegate void RequestHandler(HttpListenerRequest req, HttpListenerResponse res);
    }
}

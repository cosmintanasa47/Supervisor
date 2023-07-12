using System.Text;
using System.Security.Cryptography;
using System.Diagnostics;
using System.IO.Pipes;
using System.Net.Mail;
using static System.Net.Mime.MediaTypeNames;

namespace Work_serv
{
    public class do_it : BackgroundService
    {
        private readonly ILogger<do_it> logger;
        public do_it(ILogger<do_it> _logger)
        {
            this.logger = _logger;
        }

        public string filePath;

        public void Maintenance()
        {
            string folderName = "Days";
            string folderPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, folderName);
            if (!Directory.Exists(folderPath))
            {
                Directory.CreateDirectory(folderPath);
            }

            string fileName = DateTime.Now.ToString("ddMMyyyy") + ".txt";
            filePath = Path.Combine(folderPath, fileName);

            string[] files = Directory.GetFiles(folderPath);
            foreach (string file in files)
            {
                DateTime fileDate = File.GetCreationTime(file);
                TimeSpan difference = DateTime.Now - fileDate;

                int dayLimit = 30;

                if (difference.TotalDays > dayLimit)
                {
                    File.Delete(file);
                }
            }
        }

        public void Get_Timer()
        {
           var time = new System.Timers.Timer(3 * 1000);
           time.Elapsed += (sender, e) => Time_pass();
           time.Start();
            void Time_pass()
            {
                 using (Aes new_aes = Aes.Create())
                 {
                 byte[] en = null;
                 new_aes.KeySize = 256;
                 string text = null;
                 foreach (ProcessActivity _process in processActivities)
                 {
                    text = text + _process.MainWindowTitle.ToString() + "\n"
                                // + _process.Type + "\n"
                                + _process.StartTime.ToString("dd/MM/yyyy HH:mm:ss") + "\n"
                                + _process.ActiveDuration.ToString(@"hh\:mm\:ss") + "\n";
                 }
                    File.WriteAllText(filePath, text);
                    // en = Encrypt(text, new_aes.Key, new_aes.IV);
                    // File.WriteAllText(filePath, en.ToString());
                    //en = Encrypt(text, new_aes.Key, new_aes.IV);
                    // Console.Write($"{Encoding.UTF8.GetString(en)}");
                    // string dec = Decrypt(en,new_aes.Key,new_aes.IV);
                    // Console.Write($"{dec}");
                }
            }
        }
        

        public class ProcessActivity
        {
            public string MainWindowTitle { get; set; }
          // public string Type { get; set; }
            public DateTime StartTime { get; set; }
            public TimeSpan ActiveDuration { get; set; }
        }

        List<ProcessActivity> processActivities = new List<ProcessActivity>();

        List<string> titles = new List<string>();
        string target, pass,email; 
        TimeSpan start, stop;

        public async Task Pipe_S()
        {
            NamedPipeServerStream pipeServer = new NamedPipeServerStream("mypipe");
            await pipeServer.WaitForConnectionAsync();
            var reader = new StreamReader(pipeServer);
            StreamWriter writer = new StreamWriter(pipeServer);

            while (test == false)
            {
                        string message = await reader.ReadLineAsync();
                        if (message == "StartSendingData")
                        {
                            foreach (ProcessActivity p in processActivities)
                            {
                                await writer.WriteLineAsync(p.MainWindowTitle);
                                await writer.FlushAsync();
                            }
                        }
                        else if (message == "Title_IN")
                        {
                            titles.Clear();
                            while(message != "Title_OUT")
                            {
                                if(!titles.Contains(message))
                                titles.Add(message);
                            }
                                await reader.ReadLineAsync();
                                target = await reader.ReadLineAsync();
                                pass = await reader.ReadLineAsync();
                                email = await reader.ReadLineAsync();
                                start = TimeSpan.Parse(await reader.ReadLineAsync());
                                stop = TimeSpan.Parse(await reader.ReadLineAsync());
                                writer.WriteLine("DataReceived"); writer.Flush();
                                Supervise();
                        }
                        else if(message == "Start_P_List")
                        {
                            while(message != "Stop_P_List")
                            {
                                kill.Add(message);
                            }
                            if(message == "Stop_P_List")
                            {
                                   await writer.WriteLineAsync("List_Received");
                                   await writer.FlushAsync();
                            }
                        }
            }
            if(test == true)
            {
                await writer.WriteLineAsync("Supervising");
                await writer.FlushAsync();
                if(reader.ReadLine() == "Stop_supervise")
                {
                    Send_Email_with_File();
                    test = false;
                }
            }

            writer.Dispose();
            reader.Dispose();
            pipeServer.Dispose();
        }

        List<string> kill = new List<string>();
        List<ProcessActivity> Bad = new List<ProcessActivity>();

        public bool test = false;

        public void Supervise()
        {
            var until_start = new System.Timers.Timer(start-DateTime.Now.TimeOfDay);
            until_start.Elapsed += (sender, e) => Time_pass();
            until_start.Start();
            void Time_pass()
            {
                var until_stop = new System.Timers.Timer(stop - start);
                until_stop.Elapsed += (sender, e) => Time_pass1();
                until_stop.Start();
                void Time_pass1()
                {
                    if (test == true)
                    {
                        Send_Email_with_File();
                        test = false;
                    }
                    until_stop.Stop();
                }
                until_start.Stop();
                test = true;
            }
        }

        public void MonitorProcessActivity()
        { 
            Process[] processes = Process.GetProcesses();
            foreach (Process process in processes)
            {
                    if ((process.MainWindowTitle.Length > 0) && (!process.MainWindowTitle.Equals("Settings")) && (!process.MainWindowTitle.Equals("Microsoft Text Input Application")))
                    {
                        if (!kill.Contains(process.ProcessName))
                        {
                            DateTime startTime = process.StartTime;
                            TimeSpan activeDuration;
                            if (!process.HasExited)
                            {
                                activeDuration = DateTime.Now - startTime;
                            }
                            else
                            {
                                DateTime exitTime = process.ExitTime;
                                activeDuration = exitTime - startTime;
                            }
                            ProcessActivity activity = new ProcessActivity
                            {
                                MainWindowTitle = process.MainWindowTitle,
                                // Type = result.PredictedLabel,
                                StartTime = startTime,
                                ActiveDuration = activeDuration,
                            };
                            bool found = false;
                            if (test == false)
                            {
                                foreach (var _item in processActivities.Where(x => x.MainWindowTitle == activity.MainWindowTitle))
                                    found = true;
                                if (found == false)
                                {
                                    //  var sampleData = new ML_Model.ModelInput()
                                    //  {
                                    //     Col1 = @process.MainWindowTitle,
                                    //  };
                                    // var result = ML_Model.Predict(sampleData);
                                    // activity.Type = result.PredictedLabel;
                                    processActivities.Add(activity);
                                }
                                else
                                {
                                    TimeSpan Time = TimeSpan.FromMilliseconds(interval);
                                    foreach (var item in processActivities.Where(x => x.MainWindowTitle == activity.MainWindowTitle))
                                    {
                                        item.ActiveDuration = item.ActiveDuration + Time;
                                    }
                                }
                            }
                            else
                            {
                                foreach (var _item in Bad.Where(x => x.MainWindowTitle == activity.MainWindowTitle))
                                    found = true;
                                if ((found == false) && (!titles.Contains(activity.MainWindowTitle)))
                                {
                                    //  var sampleData = new ML_Model.ModelInput()
                                    //  {
                                    //     Col1 = @process.MainWindowTitle,
                                    //  };
                                    // var result = ML_Model.Predict(sampleData);
                                    // activity.Type = result.PredictedLabel;
                                    Bad.Add(activity);
                                }
                                else
                                {
                                    TimeSpan Time = TimeSpan.FromMilliseconds(interval);
                                    foreach (var item in Bad.Where(x => x.MainWindowTitle == activity.MainWindowTitle))
                                    {
                                        item.ActiveDuration = item.ActiveDuration + Time;
                                    }
                                }

                                foreach (var _item in processActivities.Where(x => x.MainWindowTitle == activity.MainWindowTitle))
                                    found = true;
                                if (found == false)
                                {
                                    //  var sampleData = new ML_Model.ModelInput()
                                    //  {
                                    //     Col1 = @process.MainWindowTitle,
                                    //  };
                                    // var result = ML_Model.Predict(sampleData);
                                    // activity.Type = result.PredictedLabel;
                                    processActivities.Add(activity);
                                }
                                else
                                {
                                    TimeSpan Time = TimeSpan.FromMilliseconds(interval);
                                    foreach (var item in processActivities.Where(x => x.MainWindowTitle == activity.MainWindowTitle))
                                    {
                                        item.ActiveDuration = item.ActiveDuration + Time;
                                    }
                                }
                            }
                        }
                        else process.Kill();
                    }
            }
        }

        static int interval = 30;
        int pipe = 0;
        int maintenance = 0;

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            if (maintenance == 0) { Maintenance(); maintenance++; }
            if (pipe == 0) { Pipe_S(); pipe++; }
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    MonitorProcessActivity();
                }
                catch(Exception ex) { logger.LogError(ex,ex.Message); }
                await Task.Delay(interval,stoppingToken);
            }
        }
        public override Task StartAsync(CancellationToken cancellationToken)
        {
            Get_Timer();
            logger.LogInformation("Started");
            return base.StartAsync(cancellationToken);
        }
        public override Task StopAsync(CancellationToken cancellationToken)
        {
            logger.LogInformation("Stopped");
            return base.StopAsync(cancellationToken);
        }

        public byte[] Encrypt(string simple_text, byte[] Key, byte[] IV)
        {
            byte[] encrypted;
            using (var aes = Aes.Create())
            {
                ICryptoTransform encryptor = aes.CreateEncryptor(Key, IV);
                using (MemoryStream ms = new MemoryStream())
                {
                    using (CryptoStream cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write))
                    {
                        using (StreamWriter sw = new StreamWriter(cs))
                            sw.Write(simple_text);
                        encrypted = ms.ToArray();
                    }
                }
            }
            return encrypted;
        }
        
        public string Decrypt(byte[] encrypted_text, byte[] Key , byte[] IV)
        {
            string simple_text = null;
            using (Aes aes = Aes.Create())
            {
                ICryptoTransform decryptor = aes.CreateDecryptor(Key, IV);
                using (MemoryStream ms = new MemoryStream(encrypted_text))
                {
                    using (CryptoStream cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read))
                    {
                        using (StreamReader reader = new StreamReader(cs))
                            simple_text = reader.ReadToEnd();
                    }
                }
            }
            return simple_text;
        }

        public void Send_Email_with_File()
        {
            string folderName = "Supervise";
            string folderPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, folderName);
            if (!Directory.Exists(folderPath))
            {
                Directory.CreateDirectory(folderPath);
            }

            string fileName = target + " - " + start + ".txt";
            string filePath = Path.Combine(folderPath, fileName);

            string text = null;
            foreach (ProcessActivity _process in Bad)
            {
                text = text + _process.MainWindowTitle.ToString() + "\n"
                         // + _process.Type + "\n"
                            + _process.StartTime.ToString("dd/MM/yyyy HH:mm:ss") + "\n"
                            + _process.ActiveDuration.ToString(@"hh\:mm\:ss") + "\n";
            }
            File.WriteAllText(filePath, text);

            MailMessage mail = new MailMessage("supervisoremail0@gmail.com", email, target + " - " + start , "All the unpermitted processes are in the text file.");
            mail.Attachments.Add(new Attachment(filePath));
            SmtpClient support_email = new SmtpClient("smtp.gmail.com");
            support_email.Port = 587;
            support_email.UseDefaultCredentials = false;
            support_email.Credentials = new System.Net.NetworkCredential("supervisoremail0", "fdkoztcrfnqjepui");
            support_email.EnableSsl = true;
            support_email.DeliveryMethod = SmtpDeliveryMethod.Network;
            support_email.Send(mail);
        }
    }
}


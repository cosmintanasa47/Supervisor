namespace Supervisor;

using Microsoft.Maui.Controls;
using CommunityToolkit.Maui.Alerts;
using CommunityToolkit.Maui.Storage;
using System.Diagnostics;
using CommunityToolkit.Maui.Core;
//using Microsoft.Maui.Essentials;
using System.Collections.ObjectModel;
using System.ServiceProcess;
using System.Collections.ObjectModel;
using System.IO.Pipes;
using Work_serv;

public partial class ProductivityPage : ContentPage
{
    public ProductivityPage()
    {
        InitializeComponent();

        Check_Service();
    }

    public void Check_Service()
    {
        using (ServiceController serviceController = new ServiceController("Supervisor_Service"))
        {
            if (serviceController.Status != ServiceControllerStatus.Running)
            {
                Service_State.Text = "Service is Stopped";
            }
            else { Service_State.Text = "Service is Running"; }
        }
    }

    private void Restrict_Clicked(object sender, EventArgs e)
    {
        var cancellationTokenSource = new CancellationTokenSource();
        PickFolder(cancellationTokenSource.Token);
    }

    int id = 0;

    async Task PickFolder(CancellationToken cancellationToken)
    {
        var result = await FolderPicker.Default.PickAsync(cancellationToken);
        string path = null;
        if (result.IsSuccessful)
        {
            id++;
            await Toast.Make($"The folder was picked!", ToastDuration.Long).Show(cancellationToken);
            path = result.Folder.Path;
        }
        else
        {
            await Toast.Make($"The folder was not picked with error: {result.Exception.Message}").Show(cancellationToken);
        }
        var exeFiles = Directory.GetFiles(path, "*.exe");
        BindingContext = this;
        foreach (var filePath in exeFiles)
        {
            var processName = Path.GetFileNameWithoutExtension(filePath).ToString();
            var processPath = Path.Combine(path, processName).ToString();
            Device.InvokeOnMainThreadAsync(() =>
            {
                AppList.Add(new AppItem { Id = id, Name = processName, Path = processPath });
            });
        }
    }


    public ObservableCollection<AppItem> AppList { get; set; } = new ObservableCollection<AppItem>();


    public class AppItem
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Path { get; set; }
    }

    private void Delete_Clicked(object sender, EventArgs e)
    {
        var delete = sender as MenuItem;
        var process = delete.CommandParameter as AppItem;
        var _prc = AppList.FirstOrDefault(x => x.Id == process.Id);
        if (_prc != null)
        {
            Device.InvokeOnMainThreadAsync(() => { AppList.Remove(process); });

        }
    }


        private async void Service_Clicked(object sender, EventArgs e)
        {
            try
            {
                using (ServiceController serviceController = new ServiceController("Supervisor_Service"))
                {
                    if (serviceController.Status != ServiceControllerStatus.Running)
                    {
                        serviceController.Start();
                        serviceController.WaitForStatus(ServiceControllerStatus.Running);
                    await DisplayAlert("Alert", "Service is running", "OK");
                    }
                    else
                    {
                    await DisplayAlert("Alert", "Service is already running", "OK");
                     }
                }
            }
            catch (Exception ex)
            {
               await DisplayAlert("Alert", "Service is not starting - "+ex, "OK");
              }
            Check_Service();
        }

    List<Stat> Stats = new List<Stat>();
    TimeSpan total,work,code,study,enter,social,game;
    public class Stat
    {
        public string name { get; set; }
        public TimeSpan time { get; set; }
    }

    public void Add_Stat(string name,string time)
    {
        TimeSpan timeSpan = TimeSpan.Parse(time);
        var _stat = Stats.FirstOrDefault(x => x.name == name);
        if (_stat == null) Stats.Add(new Stat { name = name, time = timeSpan });
        else
        {
            _stat.time = _stat.time + timeSpan;
        }
    }

    public void Clear_timespan()
    {
        total = TimeSpan.Zero;
        work = TimeSpan.Zero;
        code = TimeSpan.Zero;
        study = TimeSpan.Zero;
        enter = TimeSpan.Zero;
        social = TimeSpan.Zero;
        game = TimeSpan.Zero;
    }

    private void Statistic_Clicked(object sender, EventArgs e)
    {
        Stats.Clear();
        Clear_timespan();
        Process[] processes = Process.GetProcessesByName("Supervisor_Service");
        string[] filepath = null;
        string folderpath = null;
        if (processes.Length > 0)
        {
            Process targetProcess = processes[0];
            var processPath = targetProcess.MainModule?.FileName;
            folderpath = Path.Combine(processPath, "Supervise");
            filepath = Directory.GetFiles(folderpath);
        }
        else DisplayAlert("Error","Folder was not found!","OK");
        
        if (mode == 1)
        {
            if (filepath.Contains($"{ DateTime.Now.ToString("ddMMyyyy")}.txt"))
            {
                using (StreamReader reader = new StreamReader(Path.Combine(folderpath, $"{DateTime.Now.ToString("ddMMyyyy")}.txt")))
                {
                    while(reader.ReadLine() != null)
                    {
                        string name, time;
                        name = reader.ReadLine();
                        reader.ReadLine();
                        time = reader.ReadLine();
                        Add_Stat(name,time);
                    }
                }
                if(Stats.Count > 0)
                {
                    foreach(Stat stat in Stats)
                    {
                        var sampleData = new ML_Model.ModelInput()
                        {
                            Col1 = @stat.name,
                        };
                        var result = ML_Model.Predict(sampleData);
                        if (result.ToString() == "Working") work = work + stat.time;
                        else if (result.ToString() == "Coding and Programming") code = code + stat.time;
                        else if (result.ToString() == "Studying/Learning") study = study + stat.time;
                        else if (result.ToString() == "Entertainment") enter = enter + stat.time;
                        else if (result.ToString() == "Social Media") social = social + stat.time;
                        else if (result.ToString() == "Gaming") game = game + stat.time;
                    }
                    total = work + code + study + enter + social + game;
                }
            }
            else DisplayAlert("Error","Text file for today wasn't found!","OK");
        }
        else if(mode == 2)
        {
            List<string> recentFiles = new List<string>();

            DateTime currentDate = DateTime.Now;
            DateTime startDate = currentDate.AddDays(-6);
            while (startDate <= currentDate)
            {
                string fileName = startDate.ToString("ddMMyyyy") + ".txt";
                string filePath = Path.Combine(folderpath, fileName);

                if (File.Exists(filePath))
                {
                    recentFiles.Add(filePath);
                }

                startDate = startDate.AddDays(1);
            }
            foreach (string _filepath in recentFiles)
                using (StreamReader reader = new StreamReader(_filepath))
                {
                    while (reader.ReadLine() != null)
                    {
                        string name, time;
                        name = reader.ReadLine();
                        reader.ReadLine();
                        time = reader.ReadLine();
                        Add_Stat(name, time);
                    }
                }
                    if (Stats.Count > 0)
                    {
                        foreach (Stat stat in Stats)
                        {
                            var sampleData = new ML_Model.ModelInput()
                            {
                                Col1 = @stat.name,
                            };
                            var result = ML_Model.Predict(sampleData);
                            if (result.ToString() == "Working") work = work + stat.time;
                            else if (result.ToString() == "Coding and Programming") code = code + stat.time;
                            else if (result.ToString() == "Studying/Learning") study = study + stat.time;
                            else if (result.ToString() == "Entertainment") enter = enter + stat.time;
                            else if (result.ToString() == "Social Media") social = social + stat.time;
                            else if (result.ToString() == "Gaming") game = game + stat.time;
                        }
                        total = work + code + study + enter + social + game;
                    }
            
        }
        else if(mode == 3)
        {
            foreach (string _filepath in filepath)
                using (StreamReader reader = new StreamReader(_filepath))
                {
                    while (reader.ReadLine() != null)
                    {
                        string name, time;
                        name = reader.ReadLine();
                        reader.ReadLine();
                        time = reader.ReadLine();
                        Add_Stat(name, time);
                    }
                }
            if (Stats.Count > 0)
            {
                foreach (Stat stat in Stats)
                {
                    var sampleData = new ML_Model.ModelInput()
                    {
                        Col1 = @stat.name,
                    };
                    var result = ML_Model.Predict(sampleData);
                    if (result.ToString() == "Working") work = work + stat.time;
                    else if (result.ToString() == "Coding and Programming") code = code + stat.time;
                    else if (result.ToString() == "Studying/Learning") study = study + stat.time;
                    else if (result.ToString() == "Entertainment") enter = enter + stat.time;
                    else if (result.ToString() == "Social Media") social = social + stat.time;
                    else if (result.ToString() == "Gaming") game = game + stat.time;
                }
                total = work + code + study + enter + social + game;
            }
        }
        else
        {
            DisplayAlert("Error", "Select a time period first", "OK");
        }
    }

    public void Percentage()
    {
        // ADD HERE category/total * 100
        //st
        double p_w = (work.TotalMinutes/total.TotalMinutes)*100;
        double p_c = (code.TotalMinutes/total.TotalMinutes)*100;
        double p_s = (study.TotalMinutes/total.TotalMinutes)*100;
        double p_e = (enter.TotalMinutes/total.TotalMinutes)*100;
        double p_sm = (social.TotalMinutes/total.TotalMinutes)*100;
        double p_g = (game.TotalMinutes/total.TotalMinutes)*100;
        // Write the results
        st.Text = $"Working:{work.ToString()} {p_w.ToString()}  Coding and Programming:{code.ToString()} {p_c.ToString()}  Studying/Learning:{study.ToString()} {p_s.ToString()}  " +
        $"Social Media:{social.ToString()} {p_sm.ToString()}  Entertainment:{enter.ToString()} {p_e.ToString()}  Gaming:{game.ToString()} {p_g.ToString()}";
    }

    private void Send_Clicked(object sender,EventArgs e)
    {
        using (ServiceController serviceController = new ServiceController("Supervisor_Service"))
        {
            if (serviceController.Status != ServiceControllerStatus.Running)
            {
                DisplayAlert("Error", "Service has to be running first!", "OK");
            }
            else
            {
                using (NamedPipeClientStream pipeClient = new NamedPipeClientStream("myserver", "mypipe", PipeDirection.InOut))
                {
                    using (StreamWriter writer = new StreamWriter(pipeClient))
                    {
                        writer.WriteLine("Start_P_List");
                        writer.Flush();
                    }

                    using (StreamWriter writer = new StreamWriter(pipeClient))
                    {
                        foreach (AppItem app in AppList)
                        {
                            if (app.Name.Length > 0)
                            {
                                writer.WriteLine(app.Name);
                                writer.Flush();
                            }
                        }
                    }

                    using (StreamWriter writer = new StreamWriter(pipeClient))
                    {
                        writer.WriteLine("Stop_P_List");
                        writer.Flush();
                    }

                    using (StreamReader reader = new StreamReader(pipeClient))
                    {
                        if(reader.ReadLine() == "List_Received") { DisplayAlert("Message","Settings sent","OK"); }
                    }
                }
            }
        }
    }

    int mode;

    private void Picker_SelectedIndexChanged(object sender, EventArgs e)
    {
        Picker picker = (Picker)sender;
        string selectedOption = picker.SelectedItem as string;

        switch (selectedOption)
        {
            case "Today":
                mode = 1;
                break;
            case "Last Week":
                mode = 2;
                break;
            case "Last Month":
                mode = 3;
                break;
        }
    }    
}
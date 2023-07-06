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

public partial class ProductivityPage : ContentPage
{
    public ProductivityPage()
    {
        InitializeComponent();

      //  Check_Service();
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

    private void Statistic_Clicked(object sender, EventArgs e)
    {
        if(mode == 1)
        {

        }
        else if(mode == 2)
        {

        }
        else if(mode == 3)
        {

        }
        else
        {

        }
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
                        if(reader.ReadLine() == "End") { DisplayAlert("Message","","OK"); }
                    }
                }
            }
        }
    }

    private void Settings_Clicked(object sender, EventArgs e)
    {
        
    }

    int mode;

    private void Picker_SelectedIndexChanged(object sender, EventArgs e)
    {
        Picker picker = (Picker)sender;
        string selectedOption = picker.SelectedItem as string;

        // Perform an action based on the selected option
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


        /*

        public class File
        {
            public string file_name { get; set; }
            public string file_path { get; set; }
        };

        public ObservableCollection<File> MyList { get; set; }

        async void File_Pick()
        {
            var folderPickerResult = await FolderPicker.PickAsync(default);
            if (folderPickerResult.IsSuccessful)
            {
                await Toast.Make($"Folder picked: Name - {folderPickerResult.Folder.Name}, Path - {folderPickerResult.Folder.Path}", ToastDuration.Long).Show(default);
                File file = new File();
                file.file_name = folderPickerResult.Folder.Name;
                file.file_path = folderPickerResult.Folder.Path;
                MyList.Add(file);
                this.BindingContext = this;
            }
            else
            {
                await Toast.Make($"Folder is not picked, {folderPickerResult.Exception.Message}").Show(default);
            }
        }*/



    
}
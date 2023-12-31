namespace Supervisor;

using System;
using System.IO;
using System.IO.Pipes;
using System.Diagnostics;
using Microsoft.Maui.Controls;
using System.Collections.ObjectModel;
using System.Net.Mail;

public partial class SupervisePage : ContentPage
{
	public SupervisePage()
	{
		InitializeComponent();
	}

    public ObservableCollection<AppF> AppFound { get; set; } = new ObservableCollection<AppF>();

    int _id = 0;

    public class AppF
    {
        public int id { get; set; }
        public string mwt { get; set; }
        public bool ch { get; set; }
    }

    public void Add(string s)
    {
        _id++;
        Device.InvokeOnMainThreadAsync(() =>
        {
            AppFound.Add(new AppF { id = _id, mwt = s , ch = false});
        });
    }

    bool set = false;

    List<string> unallowed = new List<string>();

    public void Pipe_S()
    {
        List.IsEnabled = false;
        using (NamedPipeClientStream pipeClient = new NamedPipeClientStream("myserver", "mypipe", PipeDirection.InOut))
        {
            pipeClient.Connect();
            using (StreamWriter writer = new StreamWriter(pipeClient))
            {
                writer.WriteLine("StartSendingData");
                writer.Flush();
            }
                using (StreamReader reader = new StreamReader(pipeClient))
                {
                    while(reader.ReadLine() != null) if(!unallowed.Contains(reader.ReadLine())) unallowed.Add(reader.ReadLine());
                    foreach (string s in unallowed)
                    {
                        AppF app = AppFound.FirstOrDefault(x => x.mwt == s);
                       if((s.Length > 0)&&(app == null)) { Add(s); }
                    }
                }

            using (StreamWriter writer = new StreamWriter(pipeClient))
            {
                writer.WriteLine("StopSendingData");
                writer.Flush();
            }
        }
        List.IsEnabled = true;
    }

    private void CheckBox_CheckedChanged(object sender, CheckedChangedEventArgs e)
    {
        if (sender is CheckBox checkBox && checkBox.BindingContext is AppF item)
        {
            item.ch = e.Value;
        }
    }

    private void List_Clicked(object sender, EventArgs e)
    {
        Pipe_S();
    }

    private void Entry_Completed(object sender, EventArgs e)
    {
        if ((en.Text.Contains(':') || (en.Text.Contains('/')))) { en.Text = ""; en.Placeholder = "Invalid name"; }
    }

    List<string> titles = new List<string>();
    string target,email,pass,start,stop;

    public void Send_Service()
    {
        using (NamedPipeClientStream pipeClient = new NamedPipeClientStream("myserver", "mypipe", PipeDirection.InOut))
        {
            using (StreamWriter writer = new StreamWriter(pipeClient))
            {
                writer.WriteLine("Title_IN");
                writer.Flush();

                foreach (string s in titles)
                {
                    writer.WriteLine(s);
                    writer.Flush();
                }
                writer.WriteLine("Title_OUT");
                writer.Flush();

                writer.WriteLine(target);
                writer.Flush();
                writer.WriteLine(pass);
                writer.Flush();
                writer.WriteLine(email);
                writer.Flush();
                writer.WriteLine(start);
                writer.Flush();
                writer.WriteLine(stop);
                writer.Flush();
            }
            using (StreamReader reader = new StreamReader(pipeClient))
            {
                if (reader.ReadLine() == "DataReceived")
                {
                    DisplayAlert("Message", "Data received by server!", "OK");
                }
            }
        }
    }

    private void Stop_Clicked(object sender, EventArgs e)
    {
        using (NamedPipeClientStream pipeClient = new NamedPipeClientStream("myserver", "mypipe", PipeDirection.InOut))
        {
            using (StreamReader reader = new StreamReader(pipeClient))
            {
                if(reader.ReadLine() == "Supervising")
                    using (StreamWriter writer = new StreamWriter(pipeClient))
                    { writer.WriteLine("Stop_supervise"); writer.Flush(); }
            }
        }
    }

    private void Set_Clicked(object sender, EventArgs e)
    {
        if ((P1.Text == P2.Text)&&(P1.Text.Length > 0))
        {
            if (en.Text.Length > 0)
            {
                if (Email() == true)
                {
                    email = Email_entry.Text;
                    if (_start.Time <= _stop.Time)
                    {
                        foreach (AppF app in AppFound)
                        {
                            if (app.ch == true)
                            {
                                titles.Add(app.mwt);
                            }
                        }
                        target = en.Text;
                        pass = P1.Text;
                        start = _start.ToString();
                        stop = _stop.ToString();
                        Send_Service();
                    }
                    else { DisplayAlert("Error", "Invalid stop time!", "OK"); }
                }
                else DisplayAlert("Error", "Invalid email address!", "OK");
            }
            else { DisplayAlert("Error", "Invalid target name!", "OK"); }
        }
        else
        {
            DisplayAlert("Error", "Password confirmation is not the same as password or invalid password!", "OK");
        }
    }

    public bool Email()
    {
        bool exists = true;
        try
        {
            var emailAddress = new MailAddress(email);
        }
        catch
        { exists = false; }
        return exists;
    }

    private void Apply_Clicked(object sender, EventArgs e)
    {
        en.Text = Preferences.Default.Get("target_name",":");
        P1.Text = Preferences.Default.Get("password","");
        Email_entry.Text = Preferences.Default.Get("address", "3mail");
        _start.Time = TimeSpan.Parse(Preferences.Default.Get("start_time", ""));
        _stop.Time = TimeSpan.Parse(Preferences.Default.Get("stop_time", ""));
    }

    private void Settings_Clicked(object sender, EventArgs e)
    {

        if ((P1.Text == P2.Text)&&(en.Text.Length > 0))
        {
            if (en.Text.Length > 0)
            {
                if (Email() == true)
                    if (_start.Time <= _stop.Time)
                    {
                        // Preferences.Default.Clear();
                        Preferences.Default.Set("target_name", en.Text);
                        Preferences.Default.Set("password", P1.Text);
                        Preferences.Default.Set("address",email);
                        Preferences.Default.Set("start_time", _start.ToString());
                        Preferences.Default.Set("stop_time", _stop.ToString());
                    }
                    else { DisplayAlert("Error", "Invalid stop time!", "OK"); }
                else DisplayAlert("Error","Invalid email address!","OK");
            }
            else { DisplayAlert("Error", "Invalid target name!", "OK"); }
        }
        else
        {
            DisplayAlert("Error", "Password confirmation is not the same as password!", "OK");
        }
    }
}
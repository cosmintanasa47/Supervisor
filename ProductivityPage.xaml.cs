using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Maui;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Internals;
using Microsoft.Maui.Controls.Xaml;
using Microsoft.Maui.Graphics;
using Microsoft.Maui.Storage;
using CommunityToolkit.Maui.Alerts;
using CommunityToolkit.Maui.Storage;
using static System.Net.Mime.MediaTypeNames;

namespace Supervisor
{
    // Token: 0x02000044 RID: 68
    //[XamlFilePath("ProductivityPage.xaml")]
    public partial class ProductivityPage : ContentPage
    {
        // Token: 0x060000B4 RID: 180 RVA: 0x000041F0 File Offset: 0x000023F0
        public ProductivityPage()
        {
            this.InitializeComponent();
            this.Check_Service();
            if (!string.IsNullOrEmpty(Preferences.Default.Get<string>("game", "", null)))
            {
                this.game_ing.Time = TimeSpan.Parse(Preferences.Default.Get<string>("game", "", null));
                this.enter_tain.Time = TimeSpan.Parse(Preferences.Default.Get<string>("enter", "", null));
                this.social_med.Time = TimeSpan.Parse(Preferences.Default.Get<string>("social", "", null));
            }
        }

        // Token: 0x060000B5 RID: 181 RVA: 0x000042C6 File Offset: 0x000024C6
        public void Check_Service()
        {
            if (Process.GetProcessesByName("Work_serv").Length == 0)
            {
                this.Service_State.Text = "Service is Stopped";
                return;
            }
            this.Service_State.Text = "Service is Running";
        }

        // Token: 0x060000B6 RID: 182 RVA: 0x000042F8 File Offset: 0x000024F8
        private void Restrict_Clicked(object sender, EventArgs e)
        {
            CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
            this.PickFolder(cancellationTokenSource.Token);
        }

        // Token: 0x060000B7 RID: 183 RVA: 0x00004318 File Offset: 0x00002518
        private async Task PickFolder(CancellationToken cancellationToken)
        {
            var result = await FolderPicker.Default.PickAsync(cancellationToken);
            string path = null;
            if (result.IsSuccessful)
            {
                id++;
                await base.DisplayAlert("Info", "Folder selected", "OK");
                path = result.Folder.Path;
            }
            else
            {
                await base.DisplayAlert("Info", $"The folder was not picked with error: {result.Exception.Message}", "OK");
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

        // Token: 0x17000007 RID: 7
        // (get) Token: 0x060000B8 RID: 184 RVA: 0x00004363 File Offset: 0x00002563
        // (set) Token: 0x060000B9 RID: 185 RVA: 0x0000436B File Offset: 0x0000256B
        public ObservableCollection<ProductivityPage.AppItem> AppList { get; set; } = new ObservableCollection<ProductivityPage.AppItem>();

        // Token: 0x060000BA RID: 186 RVA: 0x00004374 File Offset: 0x00002574
        private void Delete_Clicked(object sender, EventArgs e)
        {
            MenuItem menuItem = sender as MenuItem;
            ProductivityPage.AppItem process = menuItem.CommandParameter as ProductivityPage.AppItem;
            if (this.AppList.FirstOrDefault((ProductivityPage.AppItem x) => x.Id == process.Id) != null)
            {
                Device.InvokeOnMainThreadAsync(delegate ()
                {
                    this.AppList.Remove(process);
                });
            }
        }

        // Token: 0x060000BB RID: 187 RVA: 0x000043D4 File Offset: 0x000025D4
        private async void Service_Clicked(object sender, EventArgs e)
        {
            /*ProductivityPage.<Service_Clicked>d__13 <Service_Clicked>d__;
			<Service_Clicked>d__.<>t__builder = AsyncVoidMethodBuilder.Create();
			<Service_Clicked>d__.<>4__this = this;
			<Service_Clicked>d__.<>1__state = -1;
			<Service_Clicked>d__.<>t__builder.Start<ProductivityPage.<Service_Clicked>d__13>(ref <Service_Clicked>d__);*/

            try
            {
                // await base.DisplayAlert("Info", "A intra in try", "OK");

                if (Process.GetProcessesByName("Work_serv").Length == 0 && Service_State.Text == "Service is Stopped")
                {
                    // await base.DisplayAlert("Info", "Vrea sa dea start", "OK");
                    Process.Start(path_to_work_serv);
                    //await base.DisplayAlert("Info", "Teoretic a dat start", "OK");
                }
                else
                {
                    foreach (Process process in Process.GetProcessesByName("Work_serv"))
                    {
                        // await base.DisplayAlert("Info", "Vrea sa dea kill", "OK");
                        process.Kill();
                        // await base.DisplayAlert("Info", "Teoretic a dat kill", "OK");
                        Service_State.Text = "Service is Stopped";
                    }
                }
            }
            catch (Exception ex) { await base.DisplayAlert("ERROR", $"Error : {ex.Message}", "OK"); }
            Check_Service();
        }

        // Token: 0x060000BC RID: 188 RVA: 0x0000440C File Offset: 0x0000260C
        public void Add_Stat(string name, string time)
        {
            TimeSpan timeSpan = TimeSpan.Parse(time);
            ProductivityPage.Stat stat = this.Stats.FirstOrDefault((ProductivityPage.Stat x) => x.name == name);
            if (stat == null)
            {
                this.Stats.Add(new ProductivityPage.Stat
                {
                    name = name,
                    time = timeSpan
                });
                return;
            }
            stat.time += timeSpan;
        }

        // Token: 0x060000BD RID: 189 RVA: 0x00004480 File Offset: 0x00002680
        public void Clear_timespan()
        {
            this.total = TimeSpan.Zero;
            this.work = TimeSpan.Zero;
            this.code = TimeSpan.Zero;
            this.study = TimeSpan.Zero;
            this.enter = TimeSpan.Zero;
            this.social = TimeSpan.Zero;
            this.game = TimeSpan.Zero;
        }

        // Token: 0x060000BE RID: 190 RVA: 0x000044DC File Offset: 0x000026DC
        private void Statistic_Clicked(object sender, EventArgs e)
        {
            this.Stats.Clear();
            this.Clear_timespan();
            Process[] processesByName = Process.GetProcessesByName("Work_serv");
            string[] array = null;
            string text2 = path_to_work_serv;
            string text = path_to_days;

                text2 = path_to_work_serv;
                text = path_to_days;
                
                if (Directory.Exists(text))
                {
                    array = Directory.GetFiles(text, "*.txt");
                }

            if (this.mode == 1)
            {
                if (array.Length != 0 && text.Length > 0 && text2.Length > 0)
                {
                    if (!array.Contains(Path.Combine(text, DateTime.Now.ToString("ddMMyyyy") + ".txt")))
                    {
                        base.DisplayAlert("Error", "Text file for today wasn't found!", "OK");
                        return;
                    }
                    int i = 0;
                    string[] array2 = File.ReadAllLines(Path.Combine(text, DateTime.Now.ToString("ddMMyyyy") + ".txt"));
                    while (i < array2.Length)
                    {
                        this.Add_Stat(array2[i], array2[i + 2]);
                        i += 3;
                    }
                    if (this.Stats.Count > 0)
                    {
                        foreach (ProductivityPage.Stat stat in this.Stats)
                        {
                            ML_Model.ModelOutput modelOutput = ML_Model.Predict(new ML_Model.ModelInput
                            {
                                Col1 = stat.name
                            });
                            if (modelOutput.PredictedLabel.ToString() == "Working")
                            {
                                this.work += stat.time;
                            }
                            else if (modelOutput.PredictedLabel.ToString() == "Coding and Programming")
                            {
                                this.code += stat.time;
                            }
                            else if (modelOutput.PredictedLabel.ToString() == "Studying/Learning")
                            {
                                this.study += stat.time;
                            }
                            else if (modelOutput.PredictedLabel.ToString() == "Entertainment")
                            {
                                this.enter += stat.time;
                            }
                            else if (modelOutput.PredictedLabel.ToString() == "Social Media")
                            {
                                this.social += stat.time;
                            }
                            else if (modelOutput.PredictedLabel.ToString() == "Gaming")
                            {
                                this.game += stat.time;
                            }
                        }
                        this.total = this.work + this.code + this.study + this.enter + this.social + this.game;
                        this.Percentage();
                        return;
                    }
                }
            }
            else if (this.mode == 2)
            {
                List<string> list = new List<string>();
                DateTime now = DateTime.Now;
                DateTime t = now.AddDays(-6.0);
                while (t <= now)
                {
                    string path = t.ToString("ddMMyyyy") + ".txt";
                    string text3 = Path.Combine(text, path);
                    if (File.Exists(text3))
                    {
                        list.Add(text3);
                    }
                    t = t.AddDays(1.0);
                }
                foreach (string path2 in list)
                {
                    using (StreamReader streamReader = new StreamReader(path2))
                    {
                        while (streamReader.ReadLine() != null)
                        {
                            string name = streamReader.ReadLine();
                            streamReader.ReadLine();
                            string time = streamReader.ReadLine();
                            this.Add_Stat(name, time);
                        }
                    }
                }
                if (this.Stats.Count > 0)
                {
                    foreach (ProductivityPage.Stat stat2 in this.Stats)
                    {
                        ML_Model.ModelOutput modelOutput2 = ML_Model.Predict(new ML_Model.ModelInput
                        {
                            Col1 = stat2.name
                        });
                        if (modelOutput2.ToString() == "Working")
                        {
                            this.work += stat2.time;
                        }
                        else if (modelOutput2.ToString() == "Coding and Programming")
                        {
                            this.code += stat2.time;
                        }
                        else if (modelOutput2.ToString() == "Studying/Learning")
                        {
                            this.study += stat2.time;
                        }
                        else if (modelOutput2.ToString() == "Entertainment")
                        {
                            this.enter += stat2.time;
                        }
                        else if (modelOutput2.ToString() == "Social Media")
                        {
                            this.social += stat2.time;
                        }
                        else if (modelOutput2.ToString() == "Gaming")
                        {
                            this.game += stat2.time;
                        }
                    }
                    this.total = this.work + this.code + this.study + this.enter + this.social + this.game;
                    return;
                }
            }
            else if (this.mode == 3)
            {
                string[] array3 = array;
                for (int j = 0; j < array3.Length; j++)
                {
                    using (StreamReader streamReader2 = new StreamReader(array3[j]))
                    {
                        while (streamReader2.ReadLine() != null)
                        {
                            string name2 = streamReader2.ReadLine();
                            streamReader2.ReadLine();
                            string time2 = streamReader2.ReadLine();
                            this.Add_Stat(name2, time2);
                        }
                    }
                }
                if (this.Stats.Count > 0)
                {
                    foreach (ProductivityPage.Stat stat3 in this.Stats)
                    {
                        ML_Model.ModelOutput modelOutput3 = ML_Model.Predict(new ML_Model.ModelInput
                        {
                            Col1 = stat3.name
                        });
                        if (modelOutput3.ToString() == "Working")
                        {
                            this.work += stat3.time;
                        }
                        else if (modelOutput3.ToString() == "Coding and Programming")
                        {
                            this.code += stat3.time;
                        }
                        else if (modelOutput3.ToString() == "Studying/Learning")
                        {
                            this.study += stat3.time;
                        }
                        else if (modelOutput3.ToString() == "Entertainment")
                        {
                            this.enter += stat3.time;
                        }
                        else if (modelOutput3.ToString() == "Social Media")
                        {
                            this.social += stat3.time;
                        }
                        else if (modelOutput3.ToString() == "Gaming")
                        {
                            this.game += stat3.time;
                        }
                    }
                    this.total = this.work + this.code + this.study + this.enter + this.social + this.game;
                    return;
                }
            }
            else
            {
                base.DisplayAlert("Error", "Select a time period first", "OK");
            }
        }



        // Token: 0x060000BF RID: 191 RVA: 0x00004D78 File Offset: 0x00002F78
        private void Setting_Clicked(object sender, EventArgs e)
        {
            Preferences.Default.Set<string>("game", this.game_ing.Time.ToString("hh\\:mm"), null);
            Preferences.Default.Set<string>("enter", this.enter_tain.Time.ToString("hh\\:mm"), null);
            Preferences.Default.Set<string>("social", this.social_med.Time.ToString("hh\\:mm"), null);
            base.DisplayAlert("Info", "Settings saved", "OK");
        }

        // Token: 0x060000C0 RID: 192 RVA: 0x00004E14 File Offset: 0x00003014
        public void Percentage()
        {
            double num = this.work.TotalMinutes / this.total.TotalMinutes * 100.0;
            double num2 = this.code.TotalMinutes / this.total.TotalMinutes * 100.0;
            double num3 = this.study.TotalMinutes / this.total.TotalMinutes * 100.0;
            double num4 = this.enter.TotalMinutes / this.total.TotalMinutes * 100.0;
            double num5 = this.social.TotalMinutes / this.total.TotalMinutes * 100.0;
            double num6 = this.game.TotalMinutes / this.total.TotalMinutes * 100.0;
            Label label = this.st;
            DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(67, 6);
            defaultInterpolatedStringHandler.AppendLiteral("Working:");
            defaultInterpolatedStringHandler.AppendFormatted(this.work.ToString());
            defaultInterpolatedStringHandler.AppendLiteral(" (");
            defaultInterpolatedStringHandler.AppendFormatted(num.ToString("F2"));
            defaultInterpolatedStringHandler.AppendLiteral("%) \nCoding and Programming:");
            defaultInterpolatedStringHandler.AppendFormatted(this.code.ToString());
            defaultInterpolatedStringHandler.AppendLiteral(" (");
            defaultInterpolatedStringHandler.AppendFormatted(num2.ToString("F2"));
            defaultInterpolatedStringHandler.AppendLiteral("%) \nStudying/Learning:");
            defaultInterpolatedStringHandler.AppendFormatted(this.study.ToString());
            defaultInterpolatedStringHandler.AppendLiteral(" (");
            defaultInterpolatedStringHandler.AppendFormatted(num3.ToString("F2"));
            defaultInterpolatedStringHandler.AppendLiteral("%)  ");
            string str = defaultInterpolatedStringHandler.ToStringAndClear();
            defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(53, 6);
            defaultInterpolatedStringHandler.AppendLiteral("\nSocial Media:");
            defaultInterpolatedStringHandler.AppendFormatted(this.social.ToString());
            defaultInterpolatedStringHandler.AppendLiteral(" (");
            defaultInterpolatedStringHandler.AppendFormatted(num5.ToString("F2"));
            defaultInterpolatedStringHandler.AppendLiteral("%)  \nEntertainment:");
            defaultInterpolatedStringHandler.AppendFormatted(this.enter.ToString());
            defaultInterpolatedStringHandler.AppendLiteral(" (");
            defaultInterpolatedStringHandler.AppendFormatted(num4.ToString("F2"));
            defaultInterpolatedStringHandler.AppendLiteral("%)  \nGaming:");
            defaultInterpolatedStringHandler.AppendFormatted(this.game.ToString());
            defaultInterpolatedStringHandler.AppendLiteral(" (");
            defaultInterpolatedStringHandler.AppendFormatted(num6.ToString("F2"));
            defaultInterpolatedStringHandler.AppendLiteral("%)");
            label.Text = str + defaultInterpolatedStringHandler.ToStringAndClear();
        }

        // Token: 0x060000C1 RID: 193 RVA: 0x000050CC File Offset: 0x000032CC
        public void Send_()
        {
            try
            {
                if (Process.GetProcessesByName("Work_serv").Length == 0)
                {
                    base.DisplayAlert("Error", "Service has to be running first!", "OK");
                }
                else
                {
                    using (NamedPipeClientStream namedPipeClientStream = new NamedPipeClientStream(".", "mypipe", PipeDirection.Out))
                    {
                        namedPipeClientStream.Connect();
                        ProductivityPage.StreamString streamString = new ProductivityPage.StreamString(namedPipeClientStream);
                        string text = null;
                        text += "Start_P_List\n";
                        foreach (ProductivityPage.AppItem appItem in this.AppList)
                        {
                            if (appItem.Name.Length > 0)
                            {
                                text = text + appItem.Name + "\n";
                            }
                        }
                        text += "Stop_P_List";
                        streamString.WriteString(text);
                        namedPipeClientStream.Close();
                        Thread.Sleep(1000);
                    }
                }
            }
            catch (Exception ex)
            {
                base.DisplayAlert("Error", ex.Message, "OK");
            }
        }

        // Token: 0x060000C2 RID: 194 RVA: 0x000051F0 File Offset: 0x000033F0
        private void Send_Clicked(object sender, EventArgs e)
        {
            new Thread(new ThreadStart(this.Send_)).Start();
        }

        // Token: 0x060000C3 RID: 195 RVA: 0x00005208 File Offset: 0x00003408
        private void Picker_SelectedIndexChanged(object sender, EventArgs e)
        {
            string a = ((Picker)sender).SelectedItem as string;
            if (a == "Today")
            {
                this.mode = 1;
                return;
            }
            if (a == "Last Week")
            {
                this.mode = 2;
                return;
            }
            if (!(a == "Last Month"))
            {
                return;
            }
            this.mode = 3;
        }

        // Token: 0x060000C4 RID: 196 RVA: 0x00005268 File Offset: 0x00003468
        public byte[] Encrypt(string simple_text, byte[] Key, byte[] IV)
        {
            byte[] result;
            using (Aes aes = Aes.Create())
            {
                ICryptoTransform transform = aes.CreateEncryptor(Key, IV);
                using (MemoryStream memoryStream = new MemoryStream())
                {
                    using (CryptoStream cryptoStream = new CryptoStream(memoryStream, transform, CryptoStreamMode.Write))
                    {
                        using (StreamWriter streamWriter = new StreamWriter(cryptoStream))
                        {
                            streamWriter.Write(simple_text);
                        }
                        result = memoryStream.ToArray();
                    }
                }
            }
            return result;
        }

        // Token: 0x060000C5 RID: 197 RVA: 0x00005314 File Offset: 0x00003514
        public string Decrypt(byte[] encrypted_text, byte[] Key, byte[] IV)
        {
            string result = null;
            using (Aes aes = Aes.Create())
            {
                ICryptoTransform transform = aes.CreateDecryptor(Key, IV);
                using (MemoryStream memoryStream = new MemoryStream(encrypted_text))
                {
                    using (CryptoStream cryptoStream = new CryptoStream(memoryStream, transform, CryptoStreamMode.Read))
                    {
                        using (StreamReader streamReader = new StreamReader(cryptoStream))
                        {
                            result = streamReader.ReadToEnd();
                        }
                    }
                }
            }
            return result;
        }

        // Token: 0x060000C6 RID: 198 RVA: 0x000053B8 File Offset: 0x000035B8
        /*[GeneratedCode("Microsoft.Maui.Controls.SourceGen", "1.0.0.0")]
		[MemberNotNull("Restrict")]
		[MemberNotNull("AppListView")]
		[MemberNotNull("game_ing")]
		[MemberNotNull("enter_tain")]
		[MemberNotNull("social_med")]
		[MemberNotNull("Setting")]
		[MemberNotNull("Send")]
		[MemberNotNull("Service")]
		[MemberNotNull("Service_State")]
		[MemberNotNull("Statistic")]
		[MemberNotNull("st")]
		private void InitializeComponent()
		{
			Label label = new Label();
			Label label2 = new Label();
			Button button = new Button();
			BindingExtension bindingExtension = new BindingExtension();
			DataTemplate dataTemplate = new DataTemplate();
			ListView listView = new ListView();
			Label label3 = new Label();
			Label label4 = new Label();
			TimePicker timePicker = new TimePicker();
			HorizontalStackLayout horizontalStackLayout = new HorizontalStackLayout();
			Label label5 = new Label();
			TimePicker timePicker2 = new TimePicker();
			HorizontalStackLayout horizontalStackLayout2 = new HorizontalStackLayout();
			Label label6 = new Label();
			TimePicker timePicker3 = new TimePicker();
			HorizontalStackLayout horizontalStackLayout3 = new HorizontalStackLayout();
			VerticalStackLayout verticalStackLayout = new VerticalStackLayout();
			Button button2 = new Button();
			Button button3 = new Button();
			Button button4 = new Button();
			Label label7 = new Label();
			VerticalStackLayout verticalStackLayout2 = new VerticalStackLayout();
			HorizontalStackLayout horizontalStackLayout4 = new HorizontalStackLayout();
			Type typeFromHandle = typeof(string);
			string text = "Today";
			string text2 = "Last Week";
			string text3 = "Last Month";
			ArrayExtension arrayExtension;
			(arrayExtension = new ArrayExtension()).Type = typeFromHandle;
			arrayExtension.Items.Add(text);
			arrayExtension.Items.Add(text2);
			arrayExtension.Items.Add(text3);
			string[] value = new string[]
			{
				text,
				text2,
				text3
			};
			Picker picker = new Picker();
			Button button5 = new Button();
			StackLayout stackLayout = new StackLayout();
			Label label8 = new Label();
			VerticalStackLayout verticalStackLayout3 = new VerticalStackLayout();
			ScrollView scrollView = new ScrollView();
			NameScope nameScope = NameScope.GetNameScope(this) ?? new NameScope();
			NameScope.SetNameScope(this, nameScope);
			((INameScope)nameScope).RegisterName("Restrict", button);
			if (button.StyleId == null)
			{
				button.StyleId = "Restrict";
			}
			((INameScope)nameScope).RegisterName("AppListView", listView);
			if (listView.StyleId == null)
			{
				listView.StyleId = "AppListView";
			}
			((INameScope)nameScope).RegisterName("game_ing", timePicker);
			if (timePicker.StyleId == null)
			{
				timePicker.StyleId = "game_ing";
			}
			((INameScope)nameScope).RegisterName("enter_tain", timePicker2);
			if (timePicker2.StyleId == null)
			{
				timePicker2.StyleId = "enter_tain";
			}
			((INameScope)nameScope).RegisterName("social_med", timePicker3);
			if (timePicker3.StyleId == null)
			{
				timePicker3.StyleId = "social_med";
			}
			((INameScope)nameScope).RegisterName("Setting", button2);
			if (button2.StyleId == null)
			{
				button2.StyleId = "Setting";
			}
			((INameScope)nameScope).RegisterName("Send", button3);
			if (button3.StyleId == null)
			{
				button3.StyleId = "Send";
			}
			((INameScope)nameScope).RegisterName("Service", button4);
			if (button4.StyleId == null)
			{
				button4.StyleId = "Service";
			}
			((INameScope)nameScope).RegisterName("Service_State", label7);
			if (label7.StyleId == null)
			{
				label7.StyleId = "Service_State";
			}
			((INameScope)nameScope).RegisterName("Statistic", button5);
			if (button5.StyleId == null)
			{
				button5.StyleId = "Statistic";
			}
			((INameScope)nameScope).RegisterName("st", label8);
			if (label8.StyleId == null)
			{
				label8.StyleId = "st";
			}
			this.Restrict = button;
			this.AppListView = listView;
			this.game_ing = timePicker;
			this.enter_tain = timePicker2;
			this.social_med = timePicker3;
			this.Setting = button2;
			this.Send = button3;
			this.Service = button4;
			this.Service_State = label7;
			this.Statistic = button5;
			this.st = label8;
			this.SetValue(Page.TitleProperty, "Productivity");
			label.SetValue(Label.TextProperty, "Let's set you up for productivity!");
			label.SetValue(Label.TextColorProperty, Colors.Blue);
			label.SetValue(View.VerticalOptionsProperty, LayoutOptions.Start);
			label.SetValue(View.HorizontalOptionsProperty, LayoutOptions.Center);
			label.SetValue(Label.FontSizeProperty, Device.GetNamedSize(NamedSize.Header, label.GetType()));
			verticalStackLayout3.Children.Add(label);
			label2.SetValue(Label.TextProperty, "Restricted folder (process will be terminated)");
			label2.SetValue(Label.TextColorProperty, Colors.Blue);
			label2.SetValue(View.MarginProperty, new Thickness(15.0));
			label2.SetValue(View.VerticalOptionsProperty, LayoutOptions.Start);
			label2.SetValue(View.HorizontalOptionsProperty, LayoutOptions.Start);
			label2.SetValue(Label.FontSizeProperty, Device.GetNamedSize(NamedSize.Large, label2.GetType()));
			verticalStackLayout3.Children.Add(label2);
			button.SetValue(Button.TextProperty, "Add Restriction");
			button.SetValue(Button.TextColorProperty, Colors.Blue);
			button.SetValue(VisualElement.BackgroundColorProperty, Colors.White);
			button.SetValue(Button.BorderColorProperty, Colors.Black);
			button.SetValue(View.MarginProperty, new Thickness(15.0));
			button.SetValue(View.VerticalOptionsProperty, LayoutOptions.Start);
			button.SetValue(View.HorizontalOptionsProperty, LayoutOptions.Start);
			button.SetValue(Button.FontSizeProperty, Device.GetNamedSize(NamedSize.Body, button.GetType()));
			button.Clicked += this.Restrict_Clicked;
			verticalStackLayout3.Children.Add(button);
			bindingExtension.Path = "AppList";
			BindingBase binding = ((IMarkupExtension<BindingBase>)bindingExtension).ProvideValue(null);
			listView.SetBinding(ItemsView<Cell>.ItemsSourceProperty, binding);
			listView.SetValue(View.MarginProperty, new Thickness(15.0));
			listView.SetValue(View.VerticalOptionsProperty, LayoutOptions.Start);
			listView.SetValue(View.HorizontalOptionsProperty, LayoutOptions.Start);
			ElementTemplate elementTemplate = dataTemplate;
			ProductivityPage.<InitializeComponent>_anonXamlCDataTemplate_0 <InitializeComponent>_anonXamlCDataTemplate_ = new ProductivityPage.<InitializeComponent>_anonXamlCDataTemplate_0();
			object[] array = new object[0 + 5];
			array[0] = dataTemplate;
			array[1] = listView;
			array[2] = verticalStackLayout3;
			array[3] = scrollView;
			array[4] = this;
			<InitializeComponent>_anonXamlCDataTemplate_.parentValues = array;
			<InitializeComponent>_anonXamlCDataTemplate_.root = this;
			elementTemplate.LoadTemplate = new Func<object>(<InitializeComponent>_anonXamlCDataTemplate_.LoadDataTemplate);
			listView.SetValue(ItemsView<Cell>.ItemTemplateProperty, dataTemplate);
			verticalStackLayout3.Children.Add(listView);
			label3.SetValue(Label.TextProperty, "Daily Maximum Time Spent on Unallowed Type Of Processes");
			label3.SetValue(View.MarginProperty, new Thickness(15.0));
			label3.SetValue(Label.TextColorProperty, Colors.Blue);
			label3.SetValue(View.VerticalOptionsProperty, LayoutOptions.Start);
			label3.SetValue(View.HorizontalOptionsProperty, LayoutOptions.Start);
			label3.SetValue(Label.FontSizeProperty, Device.GetNamedSize(NamedSize.Large, label3.GetType()));
			verticalStackLayout3.Children.Add(label3);
			horizontalStackLayout.SetValue(StackBase.SpacingProperty, 10.0);
			horizontalStackLayout.SetValue(View.MarginProperty, new Thickness(20.0));
			horizontalStackLayout.SetValue(View.VerticalOptionsProperty, LayoutOptions.Start);
			horizontalStackLayout.SetValue(View.HorizontalOptionsProperty, LayoutOptions.Start);
			label4.SetValue(Label.TextProperty, "Set Daily Gaming Time Limit:  ");
			label4.SetValue(Label.TextColorProperty, Colors.Blue);
			label4.SetValue(View.VerticalOptionsProperty, LayoutOptions.Center);
			horizontalStackLayout.Children.Add(label4);
			timePicker.SetValue(VisualElement.VisualProperty, new VisualTypeConverter().ConvertFromInvariantString("Default"));
			timePicker.SetValue(TimePicker.TextColorProperty, Colors.Blue);
			timePicker.SetValue(TimePicker.FormatProperty, "HH:mm");
			horizontalStackLayout.Children.Add(timePicker);
			verticalStackLayout.Children.Add(horizontalStackLayout);
			horizontalStackLayout2.SetValue(StackBase.SpacingProperty, 10.0);
			horizontalStackLayout2.SetValue(View.MarginProperty, new Thickness(20.0));
			horizontalStackLayout2.SetValue(View.VerticalOptionsProperty, LayoutOptions.Start);
			horizontalStackLayout2.SetValue(View.HorizontalOptionsProperty, LayoutOptions.Start);
			label5.SetValue(Label.TextProperty, "Set Daily Entertainment Time Limit:  ");
			label5.SetValue(Label.TextColorProperty, Colors.Blue);
			label5.SetValue(View.VerticalOptionsProperty, LayoutOptions.Center);
			horizontalStackLayout2.Children.Add(label5);
			timePicker2.SetValue(VisualElement.VisualProperty, new VisualTypeConverter().ConvertFromInvariantString("Default"));
			timePicker2.SetValue(TimePicker.TextColorProperty, Colors.Blue);
			timePicker2.SetValue(TimePicker.FormatProperty, "HH:mm");
			horizontalStackLayout2.Children.Add(timePicker2);
			verticalStackLayout.Children.Add(horizontalStackLayout2);
			horizontalStackLayout3.SetValue(StackBase.SpacingProperty, 10.0);
			horizontalStackLayout3.SetValue(View.MarginProperty, new Thickness(20.0));
			horizontalStackLayout3.SetValue(View.VerticalOptionsProperty, LayoutOptions.Start);
			horizontalStackLayout3.SetValue(View.HorizontalOptionsProperty, LayoutOptions.Start);
			label6.SetValue(Label.TextProperty, "Set Daily Social Media Time Limit:  ");
			label6.SetValue(Label.TextColorProperty, Colors.Blue);
			label6.SetValue(View.VerticalOptionsProperty, LayoutOptions.Center);
			horizontalStackLayout3.Children.Add(label6);
			timePicker3.SetValue(VisualElement.VisualProperty, new VisualTypeConverter().ConvertFromInvariantString("Default"));
			timePicker3.SetValue(TimePicker.TextColorProperty, Colors.Blue);
			timePicker3.SetValue(TimePicker.FormatProperty, "HH:mm");
			horizontalStackLayout3.Children.Add(timePicker3);
			verticalStackLayout.Children.Add(horizontalStackLayout3);
			verticalStackLayout3.Children.Add(verticalStackLayout);
			horizontalStackLayout4.SetValue(View.MarginProperty, new Thickness(20.0));
			horizontalStackLayout4.SetValue(StackBase.SpacingProperty, 20.0);
			horizontalStackLayout4.SetValue(View.VerticalOptionsProperty, LayoutOptions.Start);
			horizontalStackLayout4.SetValue(View.HorizontalOptionsProperty, LayoutOptions.Start);
			button2.SetValue(Button.TextProperty, "Save Settings");
			button2.SetValue(VisualElement.BackgroundColorProperty, Colors.White);
			button2.SetValue(Button.TextColorProperty, Colors.Blue);
			button2.Clicked += this.Setting_Clicked;
			button2.SetValue(Button.BorderColorProperty, Colors.Black);
			horizontalStackLayout4.Children.Add(button2);
			button3.SetValue(Button.TextProperty, "Send List");
			button3.SetValue(VisualElement.BackgroundColorProperty, Colors.White);
			button3.SetValue(Button.TextColorProperty, Colors.Blue);
			button3.Clicked += this.Send_Clicked;
			button3.SetValue(Button.BorderColorProperty, Colors.Black);
			horizontalStackLayout4.Children.Add(button3);
			button4.SetValue(Button.TextProperty, "Start Service");
			button4.SetValue(VisualElement.BackgroundColorProperty, Colors.White);
			button4.SetValue(Button.TextColorProperty, Colors.Blue);
			button4.Clicked += this.Service_Clicked;
			button4.SetValue(Button.BorderColorProperty, Colors.Black);
			button4.SetValue(View.VerticalOptionsProperty, LayoutOptions.Start);
			button4.SetValue(View.HorizontalOptionsProperty, LayoutOptions.Start);
			verticalStackLayout2.Children.Add(button4);
			label7.SetValue(Label.TextProperty, "Service is Stopped");
			label7.SetValue(Label.TextColorProperty, Colors.Blue);
			verticalStackLayout2.Children.Add(label7);
			horizontalStackLayout4.Children.Add(verticalStackLayout2);
			verticalStackLayout3.Children.Add(horizontalStackLayout4);
			stackLayout.SetValue(View.MarginProperty, new Thickness(20.0));
			stackLayout.SetValue(StackLayout.OrientationProperty, StackOrientation.Horizontal);
			stackLayout.SetValue(View.VerticalOptionsProperty, LayoutOptions.Start);
			picker.SetValue(Picker.TitleProperty, "Select Date Range");
			picker.SelectedIndexChanged += this.Picker_SelectedIndexChanged;
			picker.SetValue(Picker.ItemsSourceProperty, value);
			stackLayout.Children.Add(picker);
			button5.SetValue(Button.TextProperty, "See statistic");
			button5.SetValue(VisualElement.BackgroundColorProperty, Colors.White);
			button5.SetValue(View.MarginProperty, new Thickness(10.0));
			button5.SetValue(Button.TextColorProperty, Colors.Blue);
			button5.SetValue(Button.BorderColorProperty, Colors.Black);
			button5.Clicked += this.Statistic_Clicked;
			button5.SetValue(View.VerticalOptionsProperty, LayoutOptions.Center);
			stackLayout.Children.Add(button5);
			verticalStackLayout3.Children.Add(stackLayout);
			label8.SetValue(Label.TextProperty, "");
			label8.SetValue(Label.TextColorProperty, Colors.Blue);
			verticalStackLayout3.Children.Add(label8);
			scrollView.Content = verticalStackLayout3;
			this.SetValue(ContentPage.ContentProperty, scrollView);
		}
		*/
        // Token: 0x04000041 RID: 65
        private string path_to_work_serv = "C:\\Users\\STATION_27\\source\\repos\\Work_serv\\Work_serv.exe";

        // Token: 0x04000042 RID: 66
        private string path_to_days = "C:\\Users\\STATION_27\\source\\repos\\Work_serv\\Days";

        // Token: 0x04000043 RID: 67
        private int id;

        // Token: 0x04000045 RID: 69
        private List<ProductivityPage.Stat> Stats = new List<ProductivityPage.Stat>();

        // Token: 0x04000046 RID: 70
        private TimeSpan total;

        // Token: 0x04000047 RID: 71
        private TimeSpan work;

        // Token: 0x04000048 RID: 72
        private TimeSpan code;

        // Token: 0x04000049 RID: 73
        private TimeSpan study;

        // Token: 0x0400004A RID: 74
        private TimeSpan enter;

        // Token: 0x0400004B RID: 75
        private TimeSpan social;

        // Token: 0x0400004C RID: 76
        private TimeSpan game;

        // Token: 0x0400004D RID: 77
        private int mode;

        // Token: 0x0400004E RID: 78
        /*[GeneratedCode("Microsoft.Maui.Controls.SourceGen", "1.0.0.0")]
		private Button Restrict;

		// Token: 0x0400004F RID: 79
		[GeneratedCode("Microsoft.Maui.Controls.SourceGen", "1.0.0.0")]
		private ListView AppListView;

		// Token: 0x04000050 RID: 80
		[GeneratedCode("Microsoft.Maui.Controls.SourceGen", "1.0.0.0")]
		private TimePicker game_ing;

		// Token: 0x04000051 RID: 81
		[GeneratedCode("Microsoft.Maui.Controls.SourceGen", "1.0.0.0")]
		private TimePicker enter_tain;

		// Token: 0x04000052 RID: 82
		[GeneratedCode("Microsoft.Maui.Controls.SourceGen", "1.0.0.0")]
		private TimePicker social_med;

		// Token: 0x04000053 RID: 83
		[GeneratedCode("Microsoft.Maui.Controls.SourceGen", "1.0.0.0")]
		private Button Setting;

		// Token: 0x04000054 RID: 84
		[GeneratedCode("Microsoft.Maui.Controls.SourceGen", "1.0.0.0")]
		private Button Send;

		// Token: 0x04000055 RID: 85
		[GeneratedCode("Microsoft.Maui.Controls.SourceGen", "1.0.0.0")]
		private Button Service;

		// Token: 0x04000056 RID: 86
		[GeneratedCode("Microsoft.Maui.Controls.SourceGen", "1.0.0.0")]
		private Label Service_State;

		// Token: 0x04000057 RID: 87
		[GeneratedCode("Microsoft.Maui.Controls.SourceGen", "1.0.0.0")]
		private Button Statistic;

		// Token: 0x04000058 RID: 88
		[GeneratedCode("Microsoft.Maui.Controls.SourceGen", "1.0.0.0")]
		private Label st;

		// Token: 0x02000045 RID: 69*/
        public class AppItem
        {
            // Token: 0x17000008 RID: 8
            // (get) Token: 0x060000C7 RID: 199 RVA: 0x000060E0 File Offset: 0x000042E0
            // (set) Token: 0x060000C8 RID: 200 RVA: 0x000060E8 File Offset: 0x000042E8
            public int Id { get; set; }

            // Token: 0x17000009 RID: 9
            // (get) Token: 0x060000C9 RID: 201 RVA: 0x000060F1 File Offset: 0x000042F1
            // (set) Token: 0x060000CA RID: 202 RVA: 0x000060F9 File Offset: 0x000042F9
            public string Name { get; set; }

            // Token: 0x1700000A RID: 10
            // (get) Token: 0x060000CB RID: 203 RVA: 0x00006102 File Offset: 0x00004302
            // (set) Token: 0x060000CC RID: 204 RVA: 0x0000610A File Offset: 0x0000430A
            public string Path { get; set; }
        }

        // Token: 0x02000046 RID: 70
        public class Stat
        {
            // Token: 0x1700000B RID: 11
            // (get) Token: 0x060000CE RID: 206 RVA: 0x00006113 File Offset: 0x00004313
            // (set) Token: 0x060000CF RID: 207 RVA: 0x0000611B File Offset: 0x0000431B
            public string name { get; set; }

            // Token: 0x1700000C RID: 12
            // (get) Token: 0x060000D0 RID: 208 RVA: 0x00006124 File Offset: 0x00004324
            // (set) Token: 0x060000D1 RID: 209 RVA: 0x0000612C File Offset: 0x0000432C
            public TimeSpan time { get; set; }
        }

        // Token: 0x02000047 RID: 71
        public class StreamString
        {
            // Token: 0x060000D3 RID: 211 RVA: 0x00006135 File Offset: 0x00004335
            public StreamString(Stream ioStream)
            {
                this.ioStream = ioStream;
                this.streamEncoding = new UnicodeEncoding();
            }

            // Token: 0x060000D4 RID: 212 RVA: 0x00006150 File Offset: 0x00004350
            public string ReadString()
            {
                int num = this.ioStream.ReadByte() * 256;
                num += this.ioStream.ReadByte();
                byte[] array = new byte[num];
                this.ioStream.Read(array, 0, num);
                return this.streamEncoding.GetString(array);
            }

            // Token: 0x060000D5 RID: 213 RVA: 0x000061A4 File Offset: 0x000043A4
            public int WriteString(string outString)
            {
                byte[] bytes = this.streamEncoding.GetBytes(outString);
                int num = bytes.Length;
                if (num > 65535)
                {
                    num = 65535;
                }
                this.ioStream.WriteByte((byte)(num / 256));
                this.ioStream.WriteByte((byte)(num & 255));
                this.ioStream.Write(bytes, 0, num);
                this.ioStream.Flush();
                return bytes.Length + 2;
            }

            // Token: 0x0400005E RID: 94
            private Stream ioStream;

            // Token: 0x0400005F RID: 95
            private UnicodeEncoding streamEncoding;
        }
        /*
		// Token: 0x0200004D RID: 77
		[CompilerGenerated]
		private sealed class <InitializeComponent>_anonXamlCDataTemplate_0
		{
			// Token: 0x060000E2 RID: 226 RVA: 0x000067E8 File Offset: 0x000049E8
			internal object LoadDataTemplate()
			{
				BindingExtension bindingExtension = new BindingExtension();
				MenuItem menuItem = new MenuItem();
				BindingExtension bindingExtension2 = new BindingExtension();
				Label label = new Label();
				BindingExtension bindingExtension3 = new BindingExtension();
				Label label2 = new Label();
				StackLayout stackLayout = new StackLayout();
				ViewCell viewCell = new ViewCell();
				NameScope value = new NameScope();
				NameScope.SetNameScope(viewCell, value);
				menuItem.SetValue(MenuItem.TextProperty, "Delete");
				menuItem.Clicked += this.root.Delete_Clicked;
				bindingExtension.Path = ".";
				BindingBase binding = ((IMarkupExtension<BindingBase>)bindingExtension).ProvideValue(null);
				menuItem.SetBinding(MenuItem.CommandParameterProperty, binding);
				viewCell.ContextActions.Add(menuItem);
				stackLayout.SetValue(View.MarginProperty, new Thickness(20.0));
				stackLayout.SetValue(StackLayout.OrientationProperty, StackOrientation.Vertical);
				stackLayout.SetValue(View.HorizontalOptionsProperty, LayoutOptions.Start);
				stackLayout.SetValue(View.VerticalOptionsProperty, LayoutOptions.Start);
				bindingExtension2.Path = "Name";
				BindingBase binding2 = ((IMarkupExtension<BindingBase>)bindingExtension2).ProvideValue(null);
				label.SetBinding(Label.TextProperty, binding2);
				label.SetValue(Label.FontSizeProperty, Device.GetNamedSize(NamedSize.Medium, label.GetType()));
				label.SetValue(Label.TextColorProperty, Colors.Blue);
				stackLayout.Children.Add(label);
				bindingExtension3.Path = "Path";
				BindingBase binding3 = ((IMarkupExtension<BindingBase>)bindingExtension3).ProvideValue(null);
				label2.SetBinding(Label.TextProperty, binding3);
				label2.SetValue(Label.FontSizeProperty, Device.GetNamedSize(NamedSize.Caption, label2.GetType()));
				label2.SetValue(Label.TextColorProperty, Colors.Blue);
				stackLayout.Children.Add(label2);
				viewCell.View = stackLayout;
				return viewCell;
			}

			// Token: 0x04000074 RID: 116
			internal object[] parentValues;

			// Token: 0x04000075 RID: 117
			internal ProductivityPage root;*/
    }
}


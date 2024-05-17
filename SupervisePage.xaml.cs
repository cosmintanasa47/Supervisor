using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Net.Mail;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using Microsoft.Maui;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Internals;
using Microsoft.Maui.Controls.Xaml;
using Microsoft.Maui.Graphics;
using Microsoft.Maui.Storage;

namespace Supervisor
{
    // Token: 0x0200004E RID: 78
    //[XamlFilePath("SupervisePage.xaml")]
    public partial class SupervisePage : ContentPage
    {
        // Token: 0x060000E3 RID: 227 RVA: 0x000069B4 File Offset: 0x00004BB4
        public SupervisePage()
        {
            this.InitializeComponent();
            string text = Environment.GetEnvironmentVariable("The_key", EnvironmentVariableTarget.Process);
            string text2 = Environment.GetEnvironmentVariable("The_iv", EnvironmentVariableTarget.Process);
            if (string.IsNullOrEmpty(text) || string.IsNullOrEmpty(text2))
            {
                using (Aes aes = Aes.Create())
                {
                    aes.KeySize = 256;
                    aes.GenerateKey();
                    aes.GenerateIV();
                    this.key = aes.Key;
                    this.iv = aes.IV;
                }
                text = Convert.ToBase64String(this.key);
                text2 = Convert.ToBase64String(this.iv);
                Environment.SetEnvironmentVariable("The_key", text, EnvironmentVariableTarget.Process);
                Environment.SetEnvironmentVariable("The_iv", text2, EnvironmentVariableTarget.Process);
                return;
            }
            this.key = Convert.FromBase64String(text);
            this.iv = Convert.FromBase64String(text2);
        }

        // Token: 0x1700000D RID: 13
        // (get) Token: 0x060000E4 RID: 228 RVA: 0x00006AB0 File Offset: 0x00004CB0
        // (set) Token: 0x060000E5 RID: 229 RVA: 0x00006AB8 File Offset: 0x00004CB8
        public ObservableCollection<SupervisePage.AppF> AppFound { get; set; } = new ObservableCollection<SupervisePage.AppF>();

        // Token: 0x060000E6 RID: 230 RVA: 0x00006AC4 File Offset: 0x00004CC4
        public void Add(string s)
        {
            try
            {
                this._id++;
                Device.InvokeOnMainThreadAsync(delegate ()
                {
                    this.AppFound.Add(new SupervisePage.AppF
                    {
                        id = this._id,
                        mwt = s
                    });
                });
            }
            catch (Exception ex)
            {
                base.DisplayAlert("Alert", ex.Message, "OK");
            }
        }

        // Token: 0x060000E7 RID: 231 RVA: 0x00006B34 File Offset: 0x00004D34
        private void CheckBox_CheckedChanged(object sender, CheckedChangedEventArgs e)
        {
            CheckBox checkBox = sender as CheckBox;
            if (checkBox != null)
            {
                SupervisePage.AppF appF = checkBox.BindingContext as SupervisePage.AppF;
                if (appF != null)
                {
                    appF.ch = e.Value;
                }
            }
        }

        // Token: 0x060000E8 RID: 232 RVA: 0x00006B68 File Offset: 0x00004D68
        private void List_Clicked(object sender, EventArgs e)
        {
            Process[] processes = Process.GetProcesses();
            for (int i = 0; i < processes.Length; i++)
            {
                Process process = processes[i];
                if (process.MainWindowTitle.Length > 0 && this.AppFound.FirstOrDefault((SupervisePage.AppF x) => x.mwt == process.MainWindowTitle) == null)
                {
                    this.Add(process.MainWindowTitle.ToString());
                }
            }
            this.AppListView.ItemsSource = this.AppFound;
            this.List.HorizontalOptions = LayoutOptions.Center;
        }

        // Token: 0x060000E9 RID: 233 RVA: 0x00006BFC File Offset: 0x00004DFC
        private void Entry_Completed(object sender, EventArgs e)
        {
            if (this.en.Text.Contains(':') || this.en.Text.Contains('/'))
            {
                this.en.Text = "";
                this.en.Placeholder = "Invalid name";
            }
        }

        // Token: 0x060000EA RID: 234 RVA: 0x00006C54 File Offset: 0x00004E54
        public void Send_Service()
        {
            try
            {
                using (NamedPipeClientStream namedPipeClientStream = new NamedPipeClientStream(".", "mypipe", PipeDirection.Out))
                {
                    namedPipeClientStream.Connect();
                    SupervisePage.StreamString streamString = new SupervisePage.StreamString(namedPipeClientStream);
                    string text = null;
                    text += "Title_IN\n";
                    foreach (string str in this.titles)
                    {
                        text = text + str + "\n";
                    }
                    text += "Title_OUT\n";
                    text = string.Concat(new string[]
                    {
                        text,
                        this.target,
                        "\n",
                        this.pass,
                        "\n",
                        this.email,
                        "\n",
                        this.start,
                        "\n",
                        this.stop
                    });
                    streamString.WriteString(text);
                    namedPipeClientStream.Close();
                    Thread.Sleep(1000);
                }
            }
            catch (Exception ex)
            {
                base.DisplayAlert("Error", ex.Message, "OK");
            }
        }

        // Token: 0x060000EB RID: 235 RVA: 0x00006DA0 File Offset: 0x00004FA0
        private void Set_Clicked(object sender, EventArgs e)
        {
            if (this.en.Text.Length <= 0)
            {
                base.DisplayAlert("Error", "Invalid target name!", "OK");
                return;
            }
            if (!this.Email())
            {
                base.DisplayAlert("Error", "Invalid email address!", "OK");
                return;
            }
            this.email = this.Email_entry.Text;
            if (this._start.Time <= this._stop.Time)
            {
                foreach (SupervisePage.AppF appF in this.AppFound)
                {
                    if (appF.ch)
                    {
                        this.titles.Add(appF.mwt);
                    }
                }
                this.target = this.en.Text;
                this.start = this._start.Time.ToString("hh\\:mm");
                this.stop = this._stop.Time.ToString("hh\\:mm");
                new Thread(new ThreadStart(this.Send_Service)).Start();
                return;
            }
            base.DisplayAlert("Error", "Invalid stop time!", "OK");
        }

        // Token: 0x060000EC RID: 236 RVA: 0x00006EFC File Offset: 0x000050FC
        public bool Email()
        {
            bool result = true;
            try
            {
                new MailAddress(this.Email_entry.Text);
            }
            catch
            {
                result = false;
            }
            return result;
        }

        // Token: 0x060000ED RID: 237 RVA: 0x00006F34 File Offset: 0x00005134
        private void Apply_Clicked(object sender, EventArgs e)
        {
            this.en.Text = Preferences.Default.Get<string>("target_name", ":", null);
            this.Email_entry.Text = Preferences.Default.Get<string>("address", "", null);
            this._start.Time = TimeSpan.Parse(Preferences.Default.Get<string>("start_time", "", null));
            this._stop.Time = TimeSpan.Parse(Preferences.Default.Get<string>("stop_time", "", null));
        }

        // Token: 0x060000EE RID: 238 RVA: 0x00006FCC File Offset: 0x000051CC
        private void Settings_Clicked(object sender, EventArgs e)
        {
            if (this.en.Text.Length <= 0)
            {
                base.DisplayAlert("Error", "Invalid target name!", "OK");
                return;
            }
            if (!this.Email())
            {
                base.DisplayAlert("Error", "Invalid email address!", "OK");
                return;
            }
            if (this._start.Time <= this._stop.Time)
            {
                Preferences.Default.Set<string>("target_name", this.en.Text, null);
                Preferences.Default.Set<string>("address", this.Email_entry.Text, null);
                Preferences.Default.Set<string>("start_time", this._start.Time.ToString("hh\\:mm"), null);
                Preferences.Default.Set<string>("stop_time", this._stop.Time.ToString("hh\\:mm"), null);
                base.DisplayAlert("Info", "Settings saved", "OK");
                return;
            }
            base.DisplayAlert("Error", "Invalid stop time!", "OK");
        }

        // Token: 0x060000EF RID: 239 RVA: 0x000070FC File Offset: 0x000052FC
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

        // Token: 0x060000F0 RID: 240 RVA: 0x000071A8 File Offset: 0x000053A8
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
        /*
                // Token: 0x060000F1 RID: 241 RVA: 0x0000724C File Offset: 0x0000544C
                [GeneratedCode("Microsoft.Maui.Controls.SourceGen", "1.0.0.0")]
                [MemberNotNull("lst")]
                [MemberNotNull("List")]
                [MemberNotNull("AppListView")]
                [MemberNotNull("l")]
                [MemberNotNull("en")]
                [MemberNotNull("Email_entry")]
                [MemberNotNull("_start")]
                [MemberNotNull("_stop")]
                [MemberNotNull("Set")]
                [MemberNotNull("Apply")]
                [MemberNotNull("Settings")]
                private void InitializeComponent()
                {
                    Label label = new Label();
                    ColumnDefinition columnDefinition = new ColumnDefinition();
                    ColumnDefinition columnDefinition2 = new ColumnDefinition();
                    Button button = new Button();
                    BindingExtension bindingExtension = new BindingExtension();
                    DataTemplate dataTemplate = new DataTemplate();
                    ListView listView = new ListView();
                    StackLayout stackLayout = new StackLayout();
                    Label label2 = new Label();
                    Entry entry = new Entry();
                    HorizontalStackLayout horizontalStackLayout = new HorizontalStackLayout();
                    Label label3 = new Label();
                    Entry entry2 = new Entry();
                    HorizontalStackLayout horizontalStackLayout2 = new HorizontalStackLayout();
                    Label label4 = new Label();
                    TimePicker timePicker = new TimePicker();
                    HorizontalStackLayout horizontalStackLayout3 = new HorizontalStackLayout();
                    Label label5 = new Label();
                    TimePicker timePicker2 = new TimePicker();
                    HorizontalStackLayout horizontalStackLayout4 = new HorizontalStackLayout();
                    VerticalStackLayout verticalStackLayout = new VerticalStackLayout();
                    Grid grid = new Grid();
                    Button button2 = new Button();
                    Button button3 = new Button();
                    Button button4 = new Button();
                    HorizontalStackLayout horizontalStackLayout5 = new HorizontalStackLayout();
                    VerticalStackLayout verticalStackLayout2 = new VerticalStackLayout();
                    ScrollView scrollView = new ScrollView();
                    NameScope nameScope = NameScope.GetNameScope(this) ?? new NameScope();
                    NameScope.SetNameScope(this, nameScope);
                    ((INameScope)nameScope).RegisterName("lst", stackLayout);
                    if (stackLayout.StyleId == null)
                    {
                        stackLayout.StyleId = "lst";
                    }
                    ((INameScope)nameScope).RegisterName("List", button);
                    if (button.StyleId == null)
                    {
                        button.StyleId = "List";
                    }
                    ((INameScope)nameScope).RegisterName("AppListView", listView);
                    if (listView.StyleId == null)
                    {
                        listView.StyleId = "AppListView";
                    }
                    ((INameScope)nameScope).RegisterName("l", label2);
                    if (label2.StyleId == null)
                    {
                        label2.StyleId = "l";
                    }
                    ((INameScope)nameScope).RegisterName("en", entry);
                    if (entry.StyleId == null)
                    {
                        entry.StyleId = "en";
                    }
                    ((INameScope)nameScope).RegisterName("Email_entry", entry2);
                    if (entry2.StyleId == null)
                    {
                        entry2.StyleId = "Email_entry";
                    }
                    ((INameScope)nameScope).RegisterName("_start", timePicker);
                    if (timePicker.StyleId == null)
                    {
                        timePicker.StyleId = "_start";
                    }
                    ((INameScope)nameScope).RegisterName("_stop", timePicker2);
                    if (timePicker2.StyleId == null)
                    {
                        timePicker2.StyleId = "_stop";
                    }
                    ((INameScope)nameScope).RegisterName("Set", button2);
                    if (button2.StyleId == null)
                    {
                        button2.StyleId = "Set";
                    }
                    ((INameScope)nameScope).RegisterName("Apply", button3);
                    if (button3.StyleId == null)
                    {
                        button3.StyleId = "Apply";
                    }
                    ((INameScope)nameScope).RegisterName("Settings", button4);
                    if (button4.StyleId == null)
                    {
                        button4.StyleId = "Settings";
                    }
                    this.lst = stackLayout;
                    this.List = button;
                    this.AppListView = listView;
                    this.l = label2;
                    this.en = entry;
                    this.Email_entry = entry2;
                    this._start = timePicker;
                    this._stop = timePicker2;
                    this.Set = button2;
                    this.Apply = button3;
                    this.Settings = button4;
                    this.SetValue(Page.TitleProperty, "Supervise");
                    label.SetValue(Label.TextProperty, "To supervise it's super wise!");
                    label.SetValue(Label.TextColorProperty, Colors.Blue);
                    label.SetValue(Label.FontSizeProperty, Device.GetNamedSize(NamedSize.Header, label.GetType()));
                    label.SetValue(View.VerticalOptionsProperty, LayoutOptions.Start);
                    label.SetValue(View.HorizontalOptionsProperty, LayoutOptions.Center);
                    verticalStackLayout2.Children.Add(label);
                    columnDefinition.SetValue(ColumnDefinition.WidthProperty, GridLength.Star);
                    grid.GetValue(Grid.ColumnDefinitionsProperty).Add(columnDefinition);
                    columnDefinition2.SetValue(ColumnDefinition.WidthProperty, GridLength.Star);
                    grid.GetValue(Grid.ColumnDefinitionsProperty).Add(columnDefinition2);
                    stackLayout.SetValue(Grid.ColumnProperty, 0);
                    stackLayout.SetValue(StackBase.SpacingProperty, 5.0);
                    stackLayout.SetValue(View.MarginProperty, new Thickness(10.0));
                    stackLayout.SetValue(View.HorizontalOptionsProperty, LayoutOptions.Center);
                    button.SetValue(Button.TextProperty, "Get process list");
                    button.SetValue(VisualElement.BackgroundColorProperty, Colors.White);
                    button.SetValue(Button.TextColorProperty, Colors.Blue);
                    button.Clicked += this.List_Clicked;
                    button.SetValue(Button.BorderColorProperty, Colors.Black);
                    stackLayout.Children.Add(button);
                    bindingExtension.Path = "AppFound";
                    BindingBase binding = ((IMarkupExtension<BindingBase>)bindingExtension).ProvideValue(null);
                    listView.SetBinding(ItemsView<Cell>.ItemsSourceProperty, binding);
                    ElementTemplate elementTemplate = dataTemplate;
                    SupervisePage.<InitializeComponent>_anonXamlCDataTemplate_1 <InitializeComponent>_anonXamlCDataTemplate_ = new SupervisePage.<InitializeComponent>_anonXamlCDataTemplate_1();
                    object[] array = new object[0 + 7];
                    array[0] = dataTemplate;
                    array[1] = listView;
                    array[2] = stackLayout;
                    array[3] = grid;
                    array[4] = verticalStackLayout2;
                    array[5] = scrollView;
                    array[6] = this;
                    <InitializeComponent>_anonXamlCDataTemplate_.parentValues = array;
                    <InitializeComponent>_anonXamlCDataTemplate_.root = this;
                    elementTemplate.LoadTemplate = new Func<object>(<InitializeComponent>_anonXamlCDataTemplate_.LoadDataTemplate);
                    listView.SetValue(ItemsView<Cell>.ItemTemplateProperty, dataTemplate);
                    stackLayout.Children.Add(listView);
                    grid.Children.Add(stackLayout);
                    verticalStackLayout.SetValue(Grid.ColumnProperty, 1);
                    verticalStackLayout.SetValue(StackBase.SpacingProperty, 5.0);
                    verticalStackLayout.SetValue(View.MarginProperty, new Thickness(10.0));
                    verticalStackLayout.SetValue(View.HorizontalOptionsProperty, LayoutOptions.Center);
                    horizontalStackLayout.SetValue(View.HorizontalOptionsProperty, LayoutOptions.FillAndExpand);
                    horizontalStackLayout.SetValue(View.VerticalOptionsProperty, LayoutOptions.Start);
                    label2.SetValue(Label.TextProperty, "Target name:");
                    label2.SetValue(Label.TextColorProperty, Colors.Blue);
                    label2.SetValue(View.HorizontalOptionsProperty, LayoutOptions.EndAndExpand);
                    label2.SetValue(View.VerticalOptionsProperty, LayoutOptions.Center);
                    horizontalStackLayout.Children.Add(label2);
                    entry.SetValue(View.HorizontalOptionsProperty, LayoutOptions.End);
                    entry.SetValue(View.VerticalOptionsProperty, LayoutOptions.Center);
                    entry.SetValue(Entry.PlaceholderProperty, "Enter target name...");
                    entry.SetValue(Entry.PlaceholderColorProperty, Colors.Blue);
                    entry.SetValue(Entry.TextColorProperty, Colors.Blue);
                    entry.SetValue(View.MarginProperty, new Thickness(10.0));
                    entry.SetValue(InputView.MaxLengthProperty, 20);
                    entry.Completed += this.Entry_Completed;
                    horizontalStackLayout.Children.Add(entry);
                    verticalStackLayout.Children.Add(horizontalStackLayout);
                    label3.SetValue(Label.TextProperty, "Email address:");
                    label3.SetValue(Label.TextColorProperty, Colors.Blue);
                    label3.SetValue(View.HorizontalOptionsProperty, LayoutOptions.EndAndExpand);
                    label3.SetValue(View.VerticalOptionsProperty, LayoutOptions.Center);
                    horizontalStackLayout2.Children.Add(label3);
                    entry2.SetValue(Entry.PlaceholderProperty, "Enter email address...");
                    entry2.SetValue(Entry.PlaceholderColorProperty, Colors.Blue);
                    entry2.SetValue(Entry.TextColorProperty, Colors.Blue);
                    entry2.SetValue(View.MarginProperty, new Thickness(20.0));
                    entry2.SetValue(View.HorizontalOptionsProperty, LayoutOptions.End);
                    entry2.SetValue(View.VerticalOptionsProperty, LayoutOptions.Center);
                    horizontalStackLayout2.Children.Add(entry2);
                    verticalStackLayout.Children.Add(horizontalStackLayout2);
                    label4.SetValue(Label.TextProperty, "Start time:");
                    label4.SetValue(Label.TextColorProperty, Colors.Blue);
                    horizontalStackLayout3.Children.Add(label4);
                    timePicker.SetValue(VisualElement.VisualProperty, new VisualTypeConverter().ConvertFromInvariantString("Default"));
                    timePicker.SetValue(TimePicker.TextColorProperty, Colors.Blue);
                    timePicker.SetValue(TimePicker.FormatProperty, "HH:mm");
                    horizontalStackLayout3.Children.Add(timePicker);
                    verticalStackLayout.Children.Add(horizontalStackLayout3);
                    label5.SetValue(Label.TextProperty, "Stop time:");
                    label5.SetValue(Label.TextColorProperty, Colors.Blue);
                    horizontalStackLayout4.Children.Add(label5);
                    timePicker2.SetValue(VisualElement.VisualProperty, new VisualTypeConverter().ConvertFromInvariantString("Default"));
                    timePicker2.SetValue(TimePicker.TextColorProperty, Colors.Blue);
                    timePicker2.SetValue(TimePicker.FormatProperty, "HH:mm");
                    horizontalStackLayout4.Children.Add(timePicker2);
                    verticalStackLayout.Children.Add(horizontalStackLayout4);
                    grid.Children.Add(verticalStackLayout);
                    verticalStackLayout2.Children.Add(grid);
                    horizontalStackLayout5.SetValue(View.MarginProperty, new Thickness(20.0));
                    horizontalStackLayout5.SetValue(StackBase.SpacingProperty, 20.0);
                    button2.SetValue(Button.TextProperty, "Set");
                    button2.SetValue(Button.TextColorProperty, Colors.Blue);
                    button2.SetValue(VisualElement.BackgroundColorProperty, Colors.White);
                    button2.SetValue(Button.BorderColorProperty, Colors.Black);
                    button2.Clicked += this.Set_Clicked;
                    horizontalStackLayout5.Children.Add(button2);
                    button3.SetValue(Button.TextProperty, "Apply settings");
                    button3.SetValue(Button.TextColorProperty, Colors.Blue);
                    button3.SetValue(VisualElement.BackgroundColorProperty, Colors.White);
                    button3.SetValue(Button.BorderColorProperty, Colors.Black);
                    button3.Clicked += this.Apply_Clicked;
                    horizontalStackLayout5.Children.Add(button3);
                    button4.SetValue(Button.TextProperty, "Save settings");
                    button4.SetValue(Button.TextColorProperty, Colors.Blue);
                    button4.SetValue(VisualElement.BackgroundColorProperty, Colors.White);
                    button4.SetValue(Button.BorderColorProperty, Colors.Black);
                    button4.Clicked += this.Settings_Clicked;
                    horizontalStackLayout5.Children.Add(button4);
                    verticalStackLayout2.Children.Add(horizontalStackLayout5);
                    scrollView.Content = verticalStackLayout2;
                    this.SetValue(ContentPage.ContentProperty, scrollView);
                }*/

        // Token: 0x04000076 RID: 118
        private byte[] key;

        // Token: 0x04000077 RID: 119
        private byte[] iv;

        // Token: 0x04000079 RID: 121
        private int _id;

        // Token: 0x0400007A RID: 122
        private bool set;

        // Token: 0x0400007B RID: 123
        private List<string> unallowed = new List<string>();

        // Token: 0x0400007C RID: 124
        private List<string> titles = new List<string>();

        // Token: 0x0400007D RID: 125
        private string target;

        // Token: 0x0400007E RID: 126
        private string email;

        // Token: 0x0400007F RID: 127
        private string pass;

        // Token: 0x04000080 RID: 128
        private string start;

        // Token: 0x04000081 RID: 129
        private string stop;

        // Token: 0x04000082 RID: 130
        /*[GeneratedCode("Microsoft.Maui.Controls.SourceGen", "1.0.0.0")]
		private StackLayout lst;

		// Token: 0x04000083 RID: 131
		[GeneratedCode("Microsoft.Maui.Controls.SourceGen", "1.0.0.0")]
		private Button List;

		// Token: 0x04000084 RID: 132
		[GeneratedCode("Microsoft.Maui.Controls.SourceGen", "1.0.0.0")]
		private ListView AppListView;

		// Token: 0x04000085 RID: 133
		[GeneratedCode("Microsoft.Maui.Controls.SourceGen", "1.0.0.0")]
		private Label l;

		// Token: 0x04000086 RID: 134
		[GeneratedCode("Microsoft.Maui.Controls.SourceGen", "1.0.0.0")]
		private Entry en;

		// Token: 0x04000087 RID: 135
		[GeneratedCode("Microsoft.Maui.Controls.SourceGen", "1.0.0.0")]
		private Entry Email_entry;

		// Token: 0x04000088 RID: 136
		[GeneratedCode("Microsoft.Maui.Controls.SourceGen", "1.0.0.0")]
		private TimePicker _start;

		// Token: 0x04000089 RID: 137
		[GeneratedCode("Microsoft.Maui.Controls.SourceGen", "1.0.0.0")]
		private TimePicker _stop;

		// Token: 0x0400008A RID: 138
		[GeneratedCode("Microsoft.Maui.Controls.SourceGen", "1.0.0.0")]
		private Button Set;

		// Token: 0x0400008B RID: 139
		[GeneratedCode("Microsoft.Maui.Controls.SourceGen", "1.0.0.0")]
		private Button Apply;

		// Token: 0x0400008C RID: 140
		[GeneratedCode("Microsoft.Maui.Controls.SourceGen", "1.0.0.0")]
		private Button Settings;
		*/
        // Token: 0x0200004F RID: 79
        public class AppF
        {
            // Token: 0x1700000E RID: 14
            // (get) Token: 0x060000F2 RID: 242 RVA: 0x00007CA6 File Offset: 0x00005EA6
            // (set) Token: 0x060000F3 RID: 243 RVA: 0x00007CAE File Offset: 0x00005EAE
            public int id { get; set; }

            // Token: 0x1700000F RID: 15
            // (get) Token: 0x060000F4 RID: 244 RVA: 0x00007CB7 File Offset: 0x00005EB7
            // (set) Token: 0x060000F5 RID: 245 RVA: 0x00007CBF File Offset: 0x00005EBF
            public string mwt { get; set; }

            // Token: 0x17000010 RID: 16
            // (get) Token: 0x060000F6 RID: 246 RVA: 0x00007CC8 File Offset: 0x00005EC8
            // (set) Token: 0x060000F7 RID: 247 RVA: 0x00007CD0 File Offset: 0x00005ED0
            public bool ch { get; set; }
        }

        // Token: 0x02000050 RID: 80
        public class StreamString
        {
            // Token: 0x060000F9 RID: 249 RVA: 0x00007CD9 File Offset: 0x00005ED9
            public StreamString(Stream ioStream)
            {
                this.ioStream = ioStream;
                this.streamEncoding = new UnicodeEncoding();
            }

            // Token: 0x060000FA RID: 250 RVA: 0x00007CF4 File Offset: 0x00005EF4
            public string ReadString()
            {
                int num = this.ioStream.ReadByte() * 256;
                num += this.ioStream.ReadByte();
                byte[] array = new byte[num];
                this.ioStream.Read(array, 0, num);
                return this.streamEncoding.GetString(array);
            }

            // Token: 0x060000FB RID: 251 RVA: 0x00007D48 File Offset: 0x00005F48
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

            // Token: 0x04000090 RID: 144
            private Stream ioStream;

            // Token: 0x04000091 RID: 145
            private UnicodeEncoding streamEncoding;
        }

        // Token: 0x02000053 RID: 83
        /*[CompilerGenerated]
		private sealed class <InitializeComponent>_anonXamlCDataTemplate_1
		{
			// Token: 0x06000101 RID: 257 RVA: 0x00007E18 File Offset: 0x00006018
			internal object LoadDataTemplate()
			{
				BindingExtension bindingExtension = new BindingExtension();
				Label label = new Label();
				BindingExtension bindingExtension2 = new BindingExtension();
				CheckBox checkBox = new CheckBox();
				StackLayout stackLayout = new StackLayout();
				ViewCell viewCell = new ViewCell();
				NameScope value = new NameScope();
				NameScope.SetNameScope(viewCell, value);
				bindingExtension.Path = "mwt";
				BindingBase binding = ((IMarkupExtension<BindingBase>)bindingExtension).ProvideValue(null);
				label.SetBinding(Label.TextProperty, binding);
				label.SetValue(Label.FontSizeProperty, Device.GetNamedSize(NamedSize.Medium, label.GetType()));
				label.SetValue(Label.TextColorProperty, Colors.Blue);
				stackLayout.Children.Add(label);
				bindingExtension2.Path = "ch";
				BindingBase binding2 = ((IMarkupExtension<BindingBase>)bindingExtension2).ProvideValue(null);
				checkBox.SetBinding(CheckBox.IsCheckedProperty, binding2);
				checkBox.CheckedChanged += this.root.CheckBox_CheckedChanged;
				stackLayout.Children.Add(checkBox);
				viewCell.View = stackLayout;
				return viewCell;
			}*/

        // Token: 0x04000095 RID: 149
        internal object[] parentValues;

        // Token: 0x04000096 RID: 150
        internal SupervisePage root;
    }
}




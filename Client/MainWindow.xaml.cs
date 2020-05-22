using CommonTypes;
using CommonTypes.ObjectStructureModel;
using Microsoft.Win32;
using Newtonsoft.Json;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Brushes = System.Windows.Media.Brushes;

namespace Client
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private static readonly int port = 447; // порт сервера
        private static readonly string address = "127.0.0.1"; // адрес сервера
        private byte[] token;
        private byte[] key;
        private string photoPath;
        private EditingWindowMode editingMode;
        private double scaleFactor = 1;
        private double totalScale = 1;
        private Floor floor = new Floor();
        private PointD CoordsCenter = new PointD(0, 0);
        private double transitionX = 0;
        private double transitionY = 0;
        private System.Windows.Point prewiewMousePosition;
        private int stroke = 50;
        private bool gridVisibility;

        private enum EditingWindowMode
        {
            Editing,
            AddNew
        }
        public string PhotoPath
        {
            get { return photoPath; }
            set
            {
                photoPath = value;
            }
        }

        private readonly ClientSSLCommunicationController communicationController = new ClientSSLCommunicationController();
        public EmployeeContainer EmployeeContainer { get; set; } = new EmployeeContainer();


        private ObservableCollection<EmployeeContainer> employees = new ObservableCollection<EmployeeContainer>();

        public ObservableCollection<EmployeeContainer> Employees
        {
            get { return employees; }
            set
            {
                if (value == employees)
                {
                    return;
                }

                employees = value;

            }
        }
        private ObservableCollection<StringContainer> professions = new ObservableCollection<StringContainer>();

        public ObservableCollection<StringContainer> Professions
        {
            get { return professions; }
            set
            {
                if (value == professions)
                {
                    return;
                }

                professions = value;
            }
        }
        public event PropertyChangedEventHandler PropertyChanged;
        protected void RaisePropertyChanged(string propertyName)
        {
            var handler = PropertyChanged;
            if (handler != null)
            {
                handler(this, new PropertyChangedEventArgs(propertyName));
            }
        }
        public void EmployeeSelected(object sender, EventArgs args)
        {
            RadioButton checkedRB = sender as RadioButton;
            MoreEmployeeInfo.DataContext = checkedRB.DataContext;
        }

        public void SaveEmployee(object sender, EventArgs args)
        {
            var birthday = new DateTime();
            var employment = new DateTime();

            if (IsEmployeeFormValid(out birthday, out employment))
            {
                switch (editingMode)
                {
                    case EditingWindowMode.AddNew:
                        communicationController.Send("AddEmployee", JsonConvert.SerializeObject(EmployeeContainer), token, key);
                        break;
                    case EditingWindowMode.Editing:
                        communicationController.Send("EditEmployee", JsonConvert.SerializeObject(EmployeeContainer), token, key);
                        break;
                }
                EditingVindow.Visibility = Visibility.Hidden;
                ClearEmployeeWindow();
            }

        }
        public void EditEmployee(object sender, EventArgs args)
        {
            Profession.ItemsSource = professions;
            Profession.DisplayMemberPath = "ContainedString";
            editingMode = EditingWindowMode.Editing;
            TextOfEditingWindow.Text = "Редактирование";
            var data = (EmployeeContainer)(sender as Control).DataContext;
            EmployeeContainer.CopyAllFields((EmployeeContainer)(sender as Control).DataContext);
            AccessInput.Text = data.AccessLevel.ToString();
            PasswordInput.Password = data.Password;
            BirthdayInput.Text = data.Birthday;
            EmploymentInput.Text = data.DateOfEmployment;
            Profession.SelectedItem = professions.FirstOrDefault(x => x.ContainedString == EmployeeContainer.profession);
            EditingVindow.Visibility = Visibility.Visible;
        }
        private void ClearEmployeeWindow()
        {
            EmployeeContainer.Clear();
            AccessInput.Text = "";
            PasswordInput.Password = "";
            BirthdayInput.Text = "";
            EmploymentInput.Text = "";
            Profession.SelectedIndex = -1;
        }
        private bool IsEmployeeFormValid(out DateTime birthday, out DateTime employment)
        {
            string errorString = "";
            int rez = 0;
            birthday = DateTime.MinValue;
            employment = DateTime.MinValue;
            if (EmployeeContainer.Photo == null)
            {
                errorString += "Фото не выбрано;\n";
            }
            if (!EmployeeContainer.IsValid())
            {
                errorString += "Пропущены обязатяельные поля;\n";
            }
            if (!(int.TryParse(AccessInput.Text, out rez) && rez >= 0 && rez <= 255))
            {
                errorString += "Уровень доступа должен быть числом в диапазоне [0;255];\n";
            }
            else
            {
                EmployeeContainer.AccessLevel = (byte)rez;
            }

            if (!(EmployeeContainer.Login.TrimEnd() == "" ^ PasswordInput.Password != ""))
            {
                errorString += "Логин и пароль должны либо оба присутствовать, либо оба отсутствовать;\n";
            }
            else
            {
                EmployeeContainer.Password = PasswordInput.Password;
            }

            if (!(DateTime.TryParse(BirthdayInput.Text, new CultureInfo("de-DE"), DateTimeStyles.None, out birthday)
                && DateTime.TryParse(EmploymentInput.Text, new CultureInfo("de-DE"), DateTimeStyles.None, out employment)))
            {
                errorString += "Введите время в формате dd.mm.yyyy;\n";
            }
            else
            {
                EmployeeContainer.Birthday = birthday.ToShortDateString();
                EmployeeContainer.DateOfEmployment = employment.ToShortDateString();
            }
            if (Profession.SelectedIndex == -1)
            {
                errorString += "Профессия не выбрана;\n";
            }
            else
            {
                EmployeeContainer.Profession = ((StringContainer)Profession.SelectedItem).ContainedString;
            }

            if (errorString != "")
            {
                MessageBox.Show(errorString);
                return false;
            }
            return true;
        }
        public void AddEmployee(object sender, EventArgs args)
        {
            editingMode = EditingWindowMode.AddNew;
            TextOfEditingWindow.Text = "Добавление";
            EditingVindow.Visibility = Visibility.Visible;
            Profession.ItemsSource = professions;
            Profession.DisplayMemberPath = "ContainedString";
            ClearEmployeeWindow();

        }

        private void Navigated(object sender, NavigationEventArgs e)
        {
            SetSilent((WebBrowser)sender, true);
        }





        public static void SetSilent(WebBrowser browser, bool silent)
        {
            if (browser == null)
            {
                throw new ArgumentNullException("browser");
            }

            IOleServiceProvider sp = browser.Document as IOleServiceProvider;
            if (sp != null)
            {
                Guid IID_IWebBrowserApp = new Guid("0002DF05-0000-0000-C000-000000000046");
                Guid IID_IWebBrowser2 = new Guid("D30C1661-CDAF-11d0-8A3E-00C04FC9E26E");

                object webBrowser;
                sp.QueryService(ref IID_IWebBrowserApp, ref IID_IWebBrowser2, out webBrowser);
                if (webBrowser != null)
                {
                    webBrowser.GetType().InvokeMember("Silent", BindingFlags.Instance | BindingFlags.Public | BindingFlags.PutDispProperty, null, webBrowser, new object[] { silent });
                }
            }
        }


        [ComImport, Guid("6D5140C1-7436-11CE-8034-00AA006009FA"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        public interface IOleServiceProvider
        {
            [PreserveSig]
            public int QueryService([In] ref Guid guidService, [In] ref Guid riid, [MarshalAs(UnmanagedType.IDispatch)] out object ppvObject);
        }
        public void CloseEditing(object sender, EventArgs args)
        {
            EditingVindow.Visibility = Visibility.Hidden;
            ClearEmployeeWindow();
        }

        public void DeleteEmployee(object sender, EventArgs args)
        {
            var data = ((EmployeeContainer)(sender as Control).DataContext).id;
            communicationController.Send("DeleteEmployee", data.ToString(), token, key);
        }
        public MainWindow()
        {
            InitializeComponent();
            floor.rooms.Add(new Room(new PointD[] { new PointD(50, 50), new PointD(200, 50), new PointD(200, 100), new PointD(100, 100), new PointD(100, 200), new PointD(50, 200) }));
            floor.rooms.Add(new Room(new PointD[] { new PointD(500, 50), new PointD(400, 150), new PointD(600, 150) }));
            floor.Doors.Add(new Floor.Door(new PointD(60, 50), new PointD(100, 50)));
            floor.Cameras.Add(new Camera(new PointD(100, 150), "https://www.youtube.com/embed/HpZAez2oYsA?controls=0&showinfo=0&rel=1&mute=1"));
            Draw(floor);
            this.DataContext = this;
            MouseWheel += ChangeScaleIndex;
            MouseMove += MainWindow_MouseMove;
            SizeChanged += (x, y) => { Draw(floor); };
            using (RegistryKey Key = Registry.CurrentUser.CreateSubKey(@"SOFTWARE\Microsoft\Internet Explorer\Main\FeatureControl\FEATURE_BROWSER_EMULATION", RegistryKeyPermissionCheck.ReadWriteSubTree))
            {
                if (Key.GetValue(System.Diagnostics.Process.GetCurrentProcess().ProcessName + ".exe") == null)
                {
                    Key.SetValue(System.Diagnostics.Process.GetCurrentProcess().ProcessName + ".exe", 11001, RegistryValueKind.DWord);
                }
            }
        }


        private void MainWindow_MouseMove(object sender, System.Windows.Input.MouseEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                var currentPos = e.GetPosition(DrawingField);
                transitionX += (currentPos.X - prewiewMousePosition.X);
                transitionY += (currentPos.Y - prewiewMousePosition.Y);
                prewiewMousePosition = currentPos;
                scaleFactor = 1;
                Draw(floor);
            }
            else
            {
                prewiewMousePosition = e.GetPosition(DrawingField);
            }
        }

        private void ChangeScaleIndex(object sender, System.Windows.Input.MouseWheelEventArgs e)
        {

            if (e.Delta < 0)
            {

                scaleFactor = 0.9;
                if (totalScale*2 * scaleFactor > 0.8)
                {
                    totalScale *= scaleFactor;
                    Draw(floor);
                }

            }
            else if (e.Delta > 0)
            {
                scaleFactor = 1.1;
                if (totalScale*2 * scaleFactor < 8)
                {
                    totalScale *= scaleFactor;
                    Draw(floor);
                }
            }


        }


        public void FirstRBChecked(object sender, EventArgs args)
        {
            if (AdministratorRBContainer.ItemContainerGenerator.Items.Count == 1)
            {
                (sender as RadioButton).IsChecked = true;
            }
        }
        public void Draw(Floor floor)
        {
            ChangePointCoords(CoordsCenter);
            DrawingField.Children.Clear();
            Ruler.Children.Clear();
            if(gridVisibility)
            DrawGrid(stroke, "#7FFF7F50");
            for (int i = 0; i < floor.rooms.Count; i++)
            {
                GetRoomLine(floor.rooms[i]);
            }
            for (int i = 0; i < floor.Doors.Count; i++)
            {
                GetDoorView(floor.Doors[i]);
            }
            for (int i = 0; i < floor.Cameras.Count; i++)
            {
                GetCameraView(floor.Cameras[i]);
            }
            transitionX = 0;
            transitionY = 0;
        }
        public void DrawGrid(int division,string color)
        {
            var brush = new SolidColorBrush((Color)ColorConverter.ConvertFromString(color));
            double x1 = (CoordsCenter.X % (division * totalScale));

            for (int x=0; x <=(DrawingField.ActualWidth/(division * totalScale))+1; x++)
            {
                if (x1 + x * division * totalScale >= 0 && x1 + x * division * totalScale <= DrawingField.ActualWidth)
                {
                    TextBlock text = new TextBlock();
                    text.Text = (((int)((-CoordsCenter.X + x1 + x * division*totalScale)/totalScale )/10) / 10.0).ToString();
                    Canvas.SetLeft(text, x1 + x * division * totalScale);
                    text.Foreground = brush;
                    Ruler.Children.Add(text);
                    Line line = new Line();
                    line.StrokeThickness = 0.5;
                    line.Stroke = brush;
                    line.Y1 = 0;
                    line.Y2 = DrawingField.ActualHeight;
                    line.X1 = x1 + x * division * totalScale;
                    line.X2 = x1 + x * division * totalScale;
                    DrawingField.Children.Add(line);
                }
            }
            double y1 =  (CoordsCenter.Y % (division * totalScale));
            for (int y =0; y <= (DrawingField.ActualHeight / (division * totalScale))+1; y++)
            {
              if (y1 + y * division * totalScale >= 0 && y1 + y * division * totalScale <= DrawingField.ActualHeight)
                 {
                     TextBlock text = new TextBlock();
                     text.Text = (((int)((-CoordsCenter.Y + y1 + y * division*totalScale)/totalScale )/10) / 10.0).ToString();
                     Canvas.SetLeft(text, -20);
                     Canvas.SetTop(text, y1 + y * division * totalScale);
                     text.Foreground = brush;
                     Ruler.Children.Add(text);
                     Line line = new Line();
                     line.StrokeThickness = 0.5;
                     line.Stroke = brush;
                     line.Y1 = y1 + y * division * totalScale;
                     line.Y2 = y1 + y * division * totalScale;
                     line.X1 = 0;
                     line.X2 = DrawingField.ActualWidth;
                     DrawingField.Children.Add(line);
                 }
            }
        }

        public void GetRoomLine(Room room)
        {

            for (int j = 0; j < room.Points.Count; j++)
            {
                ChangePointCoords(room.Points[j]);
            }
            for (int j = 0; j < room.Points.Count; j++)
            {
                Line line = new Line();
                line.Stroke = Brushes.Lime;
                line.StrokeThickness = totalScale*2;
                var point1 = room.Points[j];
                var point2 = room.Points[j == room.Points.Count - 1 ? 0 : j + 1];
                line.X1 = point1.X;
                line.X2 = point2.X;
                line.Y1 = point1.Y;
                line.Y2 = point2.Y;
                DrawingField.Children.Add(line);
            }
          

        }
   
        public void GetCameraView(Camera camera)
        {
            ChangePointCoords(camera.Point);
            var container = new Canvas();
            if (camera.Point.X - 8 * totalScale*2 >= 0 && camera.Point.X - 8 * totalScale*2<= DrawingField.ActualWidth &&
                camera.Point.Y - 8 * totalScale*2>= 0 && camera.Point.Y - 8 * totalScale*2<= DrawingField.ActualHeight)
            {
                Canvas.SetLeft(container, camera.Point.X - 4 * totalScale*2);
                Canvas.SetTop(container, camera.Point.Y - 4 * totalScale*2);

                var circle = new Ellipse();
                circle.Width = totalScale*2 * 8;
                circle.Height = totalScale*2 * 8;
                circle.Fill = Brushes.Blue;
                container.Children.Add(circle);
                circle.MouseLeftButtonUp += (x, y) =>
                {
                    if (container.Children.Count == 2)
                    {
                        container.Children.RemoveAt(1);
                    }
                    else
                    {
                        var browser = new WebBrowser();
                        Canvas.SetLeft(browser, totalScale*2 * 10);
                        Canvas.SetTop(browser, -totalScale*2 * 10);
                        browser.Width = 350;
                        browser.Height = 200;
                        browser.Navigate(camera.Stream);
                        container.Children.Add(browser);
                    }
                };
                DrawingField.Children.Add(container);
            }
        }

        public void ChangePointCoords(PointD point)
        {
            point.X -= prewiewMousePosition.X;
            point.X *= scaleFactor;
            point.X += prewiewMousePosition.X + transitionX;

            point.Y -= prewiewMousePosition.Y;
            point.Y *= scaleFactor;
            point.Y += prewiewMousePosition.Y + transitionY;
        }
        public void GetDoorView(Floor.Door door)
        {


            ChangePointCoords(door.point1);
            ChangePointCoords(door.point2);


            Line line = new Line();
            line.Stroke = Brushes.Red;
            line.StrokeThickness = totalScale*2 * 2.5;
            line.X1 = door.point1.X;
            line.X2 = door.point2.X;
            line.Y1 = door.point1.Y;
            line.Y2 = door.point2.Y;
            DrawingField.Children.Add(line);

        }
        public PointD RotateCoordinateSystem(PointD point, double fi)
        {
            return new PointD((float)(point.X * Math.Cos(fi) + point.Y * Math.Sin(fi)), (float)(-point.X * Math.Sin(fi) + point.Y * Math.Cos(fi)));
        }

        public PointD MinLineCoordinates(PointD point1, PointD point2)
        {
            return new PointD((point1.X < point2.X ? point1.X : point2.X), (point1.Y < point2.Y ? point1.Y : point2.Y));
        }

        public void DisplayAdministratorsEmployees(object sender, EventArgs args)
        {
            DrawingField.Visibility = Visibility.Hidden;
            EmployeeInfo.Visibility = Visibility.Visible;
            if (EditingInterface != null)
                EditingInterface.Visibility = Visibility.Hidden;
        }
        public void DisplayAdministratorsObjects(object sender, EventArgs args)
        {
            DrawingField.Visibility = Visibility.Visible;
            EmployeeInfo.Visibility = Visibility.Hidden;
            if(EditingInterface!=null)
            EditingInterface.Visibility = Visibility.Visible;

        }
        public void Send(object sender, EventArgs args)
        {
            string login = null, password = null;
            Dispatcher.Invoke(() =>
            {
                login = Login.Text;
                password = Password.Password;
            });
            communicationController.TryConnect(IPAddress.Parse(address), port, login, password, this);
        }

        public bool DisplayRoleInterface(string answerFromServer)
        {
            bool access = false;
            string[] args = answerFromServer.Split('.');
            if (args.Length != 4)
            {
                if (args.Length == 1)
                {
                    if (args[0] == "DENY")
                    {
                        Dispatcher.Invoke(() => message.Text = "Неверный логин или пароль");
                    }
                }
                else
                {
                    Dispatcher.Invoke(() => message.Text = "Ошибка");
                }
            }
            else if (args[0].Equals("OK"))
            {
                token = Convert.FromBase64String(args[2]);
                key = Convert.FromBase64String(args[3]);
                switch (args[1])
                {
                    case "Administrator":
                        Dispatcher.Invoke(() =>
                        {
                            access = true;
                            communicationController.Send("FirstData", "NoData", token, key);
                            LoginForm.Visibility = Visibility.Hidden;
                            DisplayAdministratorsObjects(null, null);
                        });
                        break;
                }


            }
            else
            {
                Dispatcher.Invoke(() => message.Text = "Ошибка");
            }
            return access;
        }

        private void ChoosePhoto(object sender, RoutedEventArgs e)
        {
            OpenFileDialog OPF = new OpenFileDialog();
            OPF.Filter = "PNG file|*.png";
            if (OPF.ShowDialog() == true)
            {
                Task.Run(() =>
                {
                    EmployeeContainer.Photo = File.ReadAllBytes(OPF.FileName);
                });
                PhotoPath = OPF.SafeFileName;
            }
        }

        private void SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

            switch (((ComboBox)sender).SelectedIndex)
            {
                case 0:
                    stroke = 50;
                    break;
                case 1:
                    stroke = 100;
                    break;
                case 2:
                    stroke = 500;
                    break;
                case 3:
                    stroke = 1000;
                    break;
            }
            Draw(floor);
        }
        private void GridVisible(object sender, RoutedEventArgs e)
        {
            gridVisibility = true;
            Draw(floor);
        }
        private void HideGrid(object sender, RoutedEventArgs e)
        {
            gridVisibility = false;
            Draw(floor);
        }
    }
}

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
using System.Runtime.Serialization.Formatters.Binary;
using Brushes = System.Windows.Media.Brushes;
using System.Windows.Controls.Primitives;

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
        private string role;
        private string photoPath;
        private EditingWindowMode editingMode;
        private double scaleFactor = 1;
        private double totalScale = 0.4;
        ObservableCollection<Floor> floors;
        
        public ObservableCollection<Floor> SeeFloors
        {
            get
            {
                if (floors == null)
                {
                    floors = new ObservableCollection<Floor>();
                    FloorSelection.ItemsSource = floors;
                }
                return floors;
            }
             set
            {
                if (value == floors)
                    return;
                floors = value;
            }
        }
        private int floorIndex = -1;
        private Floor DrawingFloor
        {
            get
            {
                return floorIndex<SeeFloors.Count&&floorIndex>=0?SeeFloors[floorIndex]:null;
            }
            set
            {
                
                SeeFloors[floorIndex] = value;
            }
        }
        private PointD CoordsCenter = new PointD(0, 0);
        private double transitionX = 0;
        private double transitionY = 0;
        private System.Windows.Point prewiewMousePosition;
        private int stroke = 50;
        private bool gridVisibility;

        public MainWindow()
        {
            this.DataContext = this;
            InitializeComponent();
            DivisionStep.SelectedIndex = 2;
            SelectedIndexToStroke(2);
            floors.CollectionChanged += HandleFloorsChange;
            using (RegistryKey Key = Registry.CurrentUser.CreateSubKey(@"SOFTWARE\Microsoft\Internet Explorer\Main\FeatureControl\FEATURE_BROWSER_EMULATION", RegistryKeyPermissionCheck.ReadWriteSubTree))
            {
                if (Key.GetValue(System.Diagnostics.Process.GetCurrentProcess().ProcessName + ".exe") == null)
                {
                    Key.SetValue(System.Diagnostics.Process.GetCurrentProcess().ProcessName + ".exe", 11001, RegistryValueKind.DWord);
                }
            }
          
        }
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
        public void HandleFloorsChange(object sender, EventArgs args)
        {
            communicationController.Send("ChangeFloors",JsonConvert.SerializeObject(floors) , token, key);
        }
        public void EmployeeSelected(object sender, EventArgs args)
        {
            RadioButton checkedRB = sender as RadioButton;
            MoreEmployeeInfo.DataContext = checkedRB.DataContext;
        }
        public void AddFloor(object sender, EventArgs args)
        {
            SomeEntityAdd(4);
        }
        public void RemoveFloor(object sender, EventArgs args)
        {
            if (floorIndex != -1)
                SeeFloors.RemoveAt(floorIndex);
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


        private void MainWindow_MouseMove(object sender, System.Windows.Input.MouseEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                var currentPos = e.GetPosition(DrawingField);
 
                transitionX += (currentPos.X - prewiewMousePosition.X);
                transitionY += (currentPos.Y - prewiewMousePosition.Y);
                if (transitionX < 0)
                {
                    if (-CoordsCenter.X + DrawingField.ActualWidth * totalScale > 810)
                        transitionX = 0;
                }
                else
                {
                    if (CoordsCenter.X < -810)
                        transitionX = 0;
                }
                if (transitionY > 0)
                {
                    if (-CoordsCenter.Y + DrawingField.ActualHeight * totalScale > 810)
                        transitionX = 0;
                }
                else
                {
                    if (-CoordsCenter.Y < -810)
                        transitionX = 0;
                }
                prewiewMousePosition = currentPos;
                scaleFactor = 1;
                Draw();
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
                if (totalScale <= 0.15)
                    scaleFactor = 0.98;
                else
                scaleFactor = 0.9;
                if (totalScale*2 * scaleFactor > 0.05)
                {
                    totalScale *= scaleFactor;
                    Draw();
                }

            }
            else if (e.Delta > 0)
            {
                scaleFactor = 1.1;
                if (totalScale*2 * scaleFactor < 4)
                {
                    totalScale *= scaleFactor;
                    Draw();
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
        public void FloorSelectionChanged(object sender, RoutedEventArgs args)
        {
            floorIndex = (sender as ComboBox).SelectedIndex;
            Draw();
            SomeCBUnselected(null, null);
        }
        public void Draw()
        {
            ChangePointCoords(CoordsCenter);
            DrawingField.Children.Clear();
            Ruler.Children.Clear();
            if (DrawingFloor != null)
            {
                if (gridVisibility)
                    if ( stroke * totalScale <= 250)
                    {
                        if (stroke * totalScale>= 25)
                        {
                            DrawGrid(stroke, "#7FFF7F50");
                        }
                        else
                        {
                            DivisionStep.SelectedIndex += 1;
                            return;
                        }
                    }
                    else
                    {
                        DivisionStep.SelectedIndex -= 1;
                        return;
                        
                    }
                for (int i = 0; i < DrawingFloor.rooms.Count; i++)
                {
                    GetRoomLine(SetRoomCoordinatesToCurrentView(DrawingFloor.rooms[i]));
                }
                for (int i = 0; i < DrawingFloor.Doors.Count; i++)
                {
                    GetDoorView(SetDoorCoordinatesToCurrentView(DrawingFloor.Doors[i]));
                }
                for (int i = 0; i < DrawingFloor.Cameras.Count; i++)
                {
                    GetCameraView(SetCameraCoordinatesToCurrentView(DrawingFloor.Cameras[i]));
                }
            }
            transitionX = 0;
            transitionY = 0;
        }
        public void DrawGrid(int division,string color)
        {

            var brush = new SolidColorBrush((Color)ColorConverter.ConvertFromString(color));
            double x1 = (CoordsCenter.X % (division * totalScale));
            
                    for (int x = 0; x <= (DrawingField.ActualWidth / (division * totalScale)) + 1; x++)
                    {
                        if (x1 + x * division * totalScale >= 0 && x1 + x * division * totalScale <= DrawingField.ActualWidth)
                        {
                            TextBlock text = new TextBlock();
                            double argument = (-CoordsCenter.X + x1 + x * division * totalScale);
                            text.Text = (((int)(argument / totalScale + (argument > 0 ? 0.01 : -0.01)) / 10) / 10.0).ToString();
                            Canvas.SetLeft(text, x1 + x * division * totalScale-10);
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
                    double y1 = (CoordsCenter.Y % (division * totalScale));
                    for (int y = 0; y <= (DrawingField.ActualHeight / (division * totalScale)) + 1; y++)
                    {
                        if (y1 + y * division * totalScale >= 0 && y1 + y * division * totalScale <= DrawingField.ActualHeight)
                        {
                            TextBlock text = new TextBlock();
                            double argument = -CoordsCenter.Y + y1 + y * division * totalScale;
                            text.Text = (((int)(argument / totalScale + (argument > 0 ? 0.01 : -0.01)) / 10) / 10.0).ToString();
                            Canvas.SetLeft(text, -20);
                            Canvas.SetTop(text, y1 + y * division * totalScale + 15);
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
            var container = new Canvas();
            if (camera.Point.X - 8 * totalScale*2 >= 0 && camera.Point.X - 8 * totalScale*2<= DrawingField.ActualWidth &&
                camera.Point.Y - 8 * totalScale*2>= 0 && camera.Point.Y - 8 * totalScale*2<= DrawingField.ActualHeight)
            {
                Canvas.SetLeft(container, camera.Point.X - 4 * totalScale*2);
                Canvas.SetTop(container, camera.Point.Y - 4 * totalScale*2);

                var circle = new Ellipse();
                circle.Width = totalScale*2 * 8;
                circle.Height = totalScale*2 * 8;
                circle.Fill = Brushes.Yellow;
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
                        browser.Navigated += Navigated;
                        try
                        {
                            browser.Navigate(camera.Stream);
                            container.Children.Add(browser);
                        }
                        catch(Exception e)
                        {
                            MessageBox.Show(e.Message);
                        }
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
            if(Ruler != null)
            Ruler.Visibility = Visibility.Hidden;
            MouseWheel -= ChangeScaleIndex;
            MouseMove -= MainWindow_MouseMove;
            SizeChanged -= (x, y) => { Draw(); };
            if (EditingInterface != null)
                EditingInterface.Visibility = Visibility.Hidden;
        }
        public void DisplayAdministratorsObjects(object sender, EventArgs args)
        {
            DrawingField.Visibility = Visibility.Visible;
            EmployeeInfo.Visibility = Visibility.Hidden;
            if (Ruler != null)
                Ruler.Visibility = Visibility.Visible;
            MouseWheel += ChangeScaleIndex;
            MouseMove += MainWindow_MouseMove;
            SizeChanged += (x, y) => { Draw(); };
            if (EditingInterface!=null)
            EditingInterface.Visibility = Visibility.Visible;

        }
        public void Send(object sender, EventArgs args)
        {
            TryEnter.IsEnabled = false;
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
                            AdministratorInterface.Visibility = Visibility.Visible;
                            DisplayAdministratorsObjects(null, null);
                            role = args[1];
                        });
                        break;
                    case "Guard":
                        Dispatcher.Invoke(() =>
                        {
                            access = true;
                            communicationController.Send("FirstData", "NoData", token, key);
                            LoginForm.Visibility = Visibility.Hidden;
                            AdministratorInterface.Visibility = Visibility.Visible;
                            DisplayAdministratorsObjects(null, null);
                            EditEmployeeButton.Visibility = Visibility.Collapsed;
                            DeleteEmployeeButton.Visibility = Visibility.Collapsed;
                            AddEmployeeButton.Visibility = Visibility.Collapsed;
                            AddFloorsButton.Visibility = Visibility.Collapsed;
                            RemoveFloorsButton.Visibility = Visibility.Collapsed;
                            role = args[1];
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

            SelectedIndexToStroke(((ComboBox)sender).SelectedIndex);
            Draw();
        }
        public void SelectedIndexToStroke(int index)
        {
            switch (index)
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
        }
        private void GridVisible(object sender, RoutedEventArgs e)
        {
            gridVisibility = true;
            Draw();
        }
        private void HideGrid(object sender, RoutedEventArgs e)
        {
            gridVisibility = false;
            Draw();
        }
        
        private void RoomsSelected(object sender, RoutedEventArgs e)
        {
            
            DoorsCB.IsChecked = false;
            CamerasCB.IsChecked = false;
            ObjectsSW.Visibility = Visibility.Visible;
            ObjectsContainer.Children.Clear();
            if (DrawingFloor == null)
                return;
            if (role == "Administrator")
            {
                var addButton = GetStandartButton();
                addButton.Content = "+";
                addButton.Click += (x, y) =>
                {
                    SomeEntityAdd(1);
                    AddContainer.Visibility = Visibility.Visible;
                };

                ObjectsContainer.Children.Add(addButton);
            }
            for (int i = 0; i < DrawingFloor.rooms.Count; i++)
            {
                TextBlock id = new TextBlock(), name = new TextBlock(), barycenter = new TextBlock();
                id.Foreground = Brushes.Lime;
                name.Foreground = Brushes.Lime;
                barycenter.Foreground = Brushes.Lime;
                id.Text = (i+1).ToString();
                name.Text = DrawingFloor.rooms[i].name;
                barycenter.Text = "Барицентер: " + DrawingFloor.rooms[i].GetBarycenter();
                id.VerticalAlignment = VerticalAlignment.Center;
                id.HorizontalAlignment = HorizontalAlignment.Left;

                Grid grid = new Grid();
                grid.Children.Add(id);
                if (role == "Administrator")
                {
                    var delButton = GetStandartButton();
                    delButton.Content = "x";
                    delButton.VerticalAlignment = VerticalAlignment.Center;
                    delButton.HorizontalAlignment = HorizontalAlignment.Right;
                    int buf = i;
                    delButton.Click += (x, y) =>
                    {

                        DrawingFloor.rooms.RemoveAt(buf);
                        RoomsSelected(null, null);
                        Draw();
                    };
                    grid.Children.Add(delButton);
                }

                
                

                StackPanel stackPanel = new StackPanel();
                stackPanel.Orientation = Orientation.Vertical;
                stackPanel.Children.Add(grid);
                stackPanel.Children.Add(name);
                stackPanel.Children.Add(barycenter);

                Border border = new Border();
                border.Margin = new Thickness(3);
                border.BorderThickness = new Thickness(1);
                border.CornerRadius = new CornerRadius(5);
                border.BorderBrush = Brushes.Lime;
                border.Padding = new Thickness(2);
                border.Child = stackPanel;

                ObjectsContainer.Children.Add(border);
            }
        }
        private void DoorsSelected(object sender, RoutedEventArgs e)
        {
 
            RoomsCB.IsChecked = false;
            CamerasCB.IsChecked = false;
            ObjectsSW.Visibility = Visibility.Visible;
            ObjectsContainer.Children.Clear();
            if (DrawingFloor == null)
                return;
            if (role == "Administrator")
            {
                var addButton = GetStandartButton();
                addButton.Content = "+";
                addButton.Click += (x, y) =>
                {
                    SomeEntityAdd(2);
                    AddContainer.Visibility = Visibility.Visible;
                };
                ObjectsContainer.Children.Add(addButton);
            }
            for (int i = 0; i < DrawingFloor.Doors.Count; i++)
            {
                TextBlock id = new TextBlock(), barycenter = new TextBlock();
                id.Foreground = Brushes.Lime;
                barycenter.Foreground = Brushes.Lime;
                id.Text = (i + 1).ToString();
                barycenter.Text = "Координаты: " + (DrawingFloor.Doors[i].point1+ DrawingFloor.Doors[i].point2)/2;

                Grid grid = new Grid();
                grid.Children.Add(id);

                if (role == "Administrator")
                {
                    var delButton = GetStandartButton();
                    delButton.Content = "x";
                    delButton.VerticalAlignment = VerticalAlignment.Center;
                    delButton.HorizontalAlignment = HorizontalAlignment.Right;
                    int buf = i;
                    delButton.Click += (x, y) =>
                    {

                        DrawingFloor.Doors.RemoveAt(buf);
                        DoorsSelected(null, null);
                        Draw();
                    };
                    grid.Children.Add(delButton);
                }




                    StackPanel stackPanel = new StackPanel();
                stackPanel.Orientation = Orientation.Vertical;
                stackPanel.Children.Add(grid);
                stackPanel.Children.Add(barycenter);

                Border border = new Border();
                border.Margin = new Thickness(3);
                border.BorderThickness = new Thickness(1);
                border.CornerRadius = new CornerRadius(5);
                border.BorderBrush = Brushes.Lime;
                border.Padding = new Thickness(2);
                border.Child = stackPanel;

                ObjectsContainer.Children.Add(border);
            }
        }
        private void SomeEntityAdd(int mode)
        {
            StackPanel stack = new StackPanel();
            stack.Width = 300;
            stack.Orientation = Orientation.Vertical;
            Button closeButton = GetStandartButton();
            closeButton.Click+=(x,y)=>
            {
                AddContainer.Visibility = Visibility.Collapsed;
            };
            closeButton.HorizontalAlignment = HorizontalAlignment.Right;
            closeButton.Content = "X";
            stack.Children.Add(closeButton);
            switch (mode)
            {
                case 1:
                    CreatingRoom(stack);
                    break;
                case 2:
                    CreatingDoor(stack);
                    break;
                case 3:
                    CreatingCamera(stack);
                    break;
                case 4:
                    CreatingFloor(stack);
                    break;
            }
            AddContainer.Child = stack;
            AddContainer.Visibility = Visibility.Visible;
        }
        public void CreatingFloor(StackPanel stack)
        {
            TextBlock name = new TextBlock();
            name.Foreground = Brushes.Lime;
            name.Text = "Название этажа:";
            TextBox nameTB = new TextBox();
            nameTB.MaxLength = 15;
            nameTB.Width = 280;
            nameTB.Margin = new Thickness(5);
            nameTB.Foreground = Brushes.Lime;
            nameTB.Background = Brushes.Black;
            nameTB.BorderBrush = Brushes.Lime;
            var save = GetStandartButton();
            save.Content = "Сохранить";
            save.Click += (x, y) =>
            {
                
                if (nameTB.Text.TrimEnd()!="")
                {
                    SeeFloors.Add(new Floor(nameTB.Text, HandleFloorsChange));
                    AddContainer.Visibility = Visibility.Collapsed;
                    Draw();
                }
                else MessageBox.Show("Название не введено");
            };

            stack.Children.Add(name);
            stack.Children.Add(nameTB);

            stack.Children.Add(save);
        }
        public void CreatingRoom(StackPanel stack)
        {

            TextBlock name = new TextBlock();
            name.Foreground = Brushes.Lime;
            name.Text = "Название комнаты:";
            TextBox nameTB = new TextBox();
            nameTB.MaxLength = 30;
            nameTB.Width = 280;
            nameTB.Foreground = Brushes.Lime;
            nameTB.Background = Brushes.Black;
            nameTB.BorderBrush = Brushes.Lime;
            TextBlock points = new TextBlock();
            points.Foreground = Brushes.Lime;
            points.Text = "Точки:";
            ScrollViewer scroll = new ScrollViewer();
            scroll.Background = Brushes.Black;
            scroll.Height = 100;
            StackPanel pointsContainer = new StackPanel();
            pointsContainer.Orientation = Orientation.Vertical;
            scroll.Content = pointsContainer;
            StackPanel buttonContainer = new StackPanel();
            var add = GetStandartButton();
            var rem = GetStandartButton();
            add.Content = "+";
            rem.Content = "-";
            add.Click += (x, y) =>
            {
                pointsContainer.Children.Add(GetPointForm(pointsContainer.Children.Count + 1));
            };
            rem.Click += (x, y) =>
            {
                if (pointsContainer.Children.Count != 0)
                    pointsContainer.Children.RemoveAt(pointsContainer.Children.Count - 1);
            };
            buttonContainer.Children.Add(add);
            buttonContainer.Children.Add(rem);
            var save = GetStandartButton();
            save.Content = "Сохранить";
            save.Click += (x, y) =>
            {
                var pointsToRoom = ParsePoints(pointsContainer);
                if (pointsToRoom != null)
                {
                    DrawingFloor.rooms.Add(new Room(pointsToRoom, nameTB.Text, HandleFloorsChange));
                    RoomsSelected(null, null);
                    AddContainer.Visibility = Visibility.Collapsed;
                    Draw();

                }
            };
            stack.Children.Add(name);
            stack.Children.Add(nameTB);
            stack.Children.Add(points);
            stack.Children.Add(scroll);
            stack.Children.Add(buttonContainer);
            stack.Children.Add(save);
        }
        public void CreatingDoor(StackPanel stack) {              
            var save = GetStandartButton();
            save.Content = "Сохранить";
            var form1 = GetPointForm(1);
            var form2 = GetPointForm(2);
            save.Click += (x, y) =>
            {
                int x1, x2, y1, y2;
                if (int.TryParse((form1.Children[2] as TextBox).Text, out x1)&& int.TryParse((form1.Children[4] as TextBox).Text, out y1)&& int.TryParse((form2.Children[2] as TextBox).Text, out x2) && int.TryParse((form2.Children[4] as TextBox).Text, out y2))
                {
                    DrawingFloor.Doors.Add(new Floor.Door(new PointD(x1,y1),new PointD(x2,y2)));
                    DoorsSelected(null, null);
                    AddContainer.Visibility = Visibility.Collapsed;
                    Draw();

                }
                else MessageBox.Show("Введены не корректные значения точек");
            };
            stack.Children.Add(form1);
            stack.Children.Add(form2);
            stack.Children.Add(save);
        }
        public void CreatingCamera(StackPanel stack)
        {
            TextBlock name = new TextBlock();
            name.Foreground = Brushes.Lime;
            name.Text = "Видео поток:";
            TextBox nameTB = new TextBox();
            nameTB.Width = 280;
            nameTB.Margin = new Thickness(5);
            nameTB.Foreground = Brushes.Lime;
            nameTB.Background = Brushes.Black;
            nameTB.BorderBrush = Brushes.Lime;
            var save = GetStandartButton();
            save.Content = "Сохранить";
            var form1 = GetPointForm(1);
            save.Click += (x, y) =>
            {
                int x1,  y1;
                if (int.TryParse((form1.Children[2] as TextBox).Text, out x1) && int.TryParse((form1.Children[4] as TextBox).Text, out y1))
                {
                    DrawingFloor.Cameras.Add(new Camera(new PointD(x1, y1),nameTB.Text));
                    CamerasSelected(null, null);
                    AddContainer.Visibility = Visibility.Collapsed;
                    Draw();

                }
                else MessageBox.Show("Введены не корректные значения точек");
            };

            stack.Children.Add(name);
            stack.Children.Add(nameTB);
            stack.Children.Add(form1);
            stack.Children.Add(save);
        }
        private StackPanel GetPointForm(int index)
{
    var pointSP = new StackPanel();
    pointSP.Orientation = Orientation.Horizontal;
    TextBlock X = new TextBlock(), Y = new TextBlock(), number = new TextBlock();
    X.Foreground = Brushes.Lime;
    Y.Foreground = Brushes.Lime;
    X.Text = "X: ";
    Y.Text = "  Y: ";
    number.Foreground = Brushes.Lime;
    number.Text = (index).ToString() + ". ";
    TextBox xTB = new TextBox(), yTB = new TextBox();
    xTB.Width = 70;
    xTB.Foreground = Brushes.Lime;
    xTB.Background = Brushes.Black;
    xTB.BorderBrush = Brushes.Lime;
    yTB.Width = 70;
    yTB.Foreground = Brushes.Lime;
    yTB.Background = Brushes.Black;
    yTB.BorderBrush = Brushes.Lime;
    pointSP.Children.Add(number);
    pointSP.Children.Add(X);
    pointSP.Children.Add(xTB);
    pointSP.Children.Add(Y);
    pointSP.Children.Add(yTB);
    return pointSP;
}
        private Room SetRoomCoordinatesToCurrentView(Room room)
        {
            PointD[] points = new PointD[room.Points.Count];
            for(int i = 0; i < room.Points.Count; i++)
            {
               points[i]= room.Points[i] * totalScale +CoordsCenter;
            }
            return new Room(points,"", null);
        }
        private Floor.Door SetDoorCoordinatesToCurrentView(Floor.Door door)
        {
            return new Floor.Door(door.point1 * totalScale + CoordsCenter, door.point2 * totalScale + CoordsCenter);
        }
        private Camera SetCameraCoordinatesToCurrentView(Camera camera)
        {
            return new Camera(camera.Point * totalScale + CoordsCenter,camera.Stream);
        }
        private PointD[] ParsePoints(StackPanel pointsContainer)
        {
            var points = new PointD[pointsContainer.Children.Count];
            for(int i=0;i< pointsContainer.Children.Count; i++)
            {
               TextBox X=(TextBox) (pointsContainer.Children[i] as StackPanel).Children[2];
               TextBox Y=(TextBox) (pointsContainer.Children[i] as StackPanel).Children[4];
                int x, y;
                if(int.TryParse(X.Text,out x)&& int.TryParse(Y.Text, out y))
                {
                    points[i] = new PointD(x, y);
                }
                else
                {
                    MessageBox.Show("Введены не корректные значения точек");
                    points = null;
                    break;
                }
            }
            return points;
        }
        private Button GetStandartButton()
        {
            Button button = new Button();
            button.Foreground = Brushes.Lime;
            button.Background = Brushes.Black;
            button.BorderBrush = Brushes.Lime;
            button.Margin = new Thickness(3);
            return button;
        }
        private void CamerasSelected(object sender, RoutedEventArgs e)
        {
            RoomsCB.IsChecked = false;
            DoorsCB.IsChecked = false;
            ObjectsSW.Visibility = Visibility.Visible;
            ObjectsContainer.Children.Clear();
            if (DrawingFloor == null)
                return;

            if (role == "Administrator")
            {
                var addButton = GetStandartButton();
                addButton.Content = "+";
                addButton.Click += (x, y) =>
                {
                    SomeEntityAdd(3);
                    AddContainer.Visibility = Visibility.Visible;
                };
                ObjectsContainer.Children.Add(addButton);
            }
            for (int i = 0; i < DrawingFloor.Cameras.Count; i++)
            {
                TextBlock id = new TextBlock(), barycenter = new TextBlock();
                id.Foreground = Brushes.Lime;
                barycenter.Foreground = Brushes.Lime;
                id.Text = (i + 1).ToString();
                barycenter.Text = "Координаты: " + DrawingFloor.Cameras[i].Point;


                Grid grid = new Grid();
                grid.Children.Add(id);
                if (role == "Administrator")
                {
                    var delButton = GetStandartButton();
                    delButton.Content = "x";
                    delButton.VerticalAlignment = VerticalAlignment.Center;
                    delButton.HorizontalAlignment = HorizontalAlignment.Right;
                    int buf = i;
                    delButton.Click += (x, y) =>
                    {

                        DrawingFloor.Cameras.RemoveAt(buf);
                        CamerasSelected(null, null);
                        Draw();
                    };

                    grid.Children.Add(delButton);
                }
                StackPanel stackPanel = new StackPanel();
                stackPanel.Orientation = Orientation.Vertical;
                stackPanel.Children.Add(grid);
                stackPanel.Children.Add(barycenter);

                Border border = new Border();
                border.Margin = new Thickness(3);
                border.BorderThickness = new Thickness(1);
                border.CornerRadius = new CornerRadius(5);
                border.BorderBrush = Brushes.Lime;
                border.Padding = new Thickness(2);
                border.Child = stackPanel;

                ObjectsContainer.Children.Add(border);
            }
        }
        private void SomeCBUnselected(object sender, RoutedEventArgs e)
        {
            RoomsCB.IsChecked = false;
            DoorsCB.IsChecked = false;
            CamerasCB.IsChecked = false;
            ObjectsSW.Visibility=Visibility.Collapsed;
            ObjectsContainer.Children.Clear();
        }
    }
}

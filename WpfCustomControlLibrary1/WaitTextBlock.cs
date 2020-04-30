using System.Threading;
using System.Windows;
using System.Windows.Controls;

namespace WpfCustomControlLibrary1
{
    /// <summary>
    /// Follow steps 1a or 1b and then 2 to use this custom control in a XAML file.
    ///
    /// Step 1a) Using this custom control in a XAML file that exists in the current project.
    /// Add this XmlNamespace attribute to the root element of the markup file where it is 
    /// to be used:
    ///
    ///     xmlns:MyNamespace="clr-namespace:WpfCustomControlLibrary1"
    ///
    ///
    /// Step 1b) Using this custom control in a XAML file that exists in a different project.
    /// Add this XmlNamespace attribute to the root element of the markup file where it is 
    /// to be used:
    ///
    ///     xmlns:MyNamespace="clr-namespace:WpfCustomControlLibrary1;assembly=WpfCustomControlLibrary1"
    ///
    /// You will also need to add a project reference from the project where the XAML file lives
    /// to this project and Rebuild to avoid compilation errors:
    ///
    ///     Right click on the target project in the Solution Explorer and
    ///     "Add Reference"->"Projects"->[Select this project]
    ///
    ///
    /// Step 2)
    /// Go ahead and use your control in the XAML file.
    ///
    ///     <MyNamespace:CustomControl1/>
    ///
    /// </summary>
    public class WaitTextBlock : TextBlock
    {
        private Timer timer;
        private int dots = 0;
        private string TextContent = "";
        static WaitTextBlock()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(WaitTextBlock), new FrameworkPropertyMetadata(typeof(WaitTextBlock)));
        }

        public WaitTextBlock()
        {
            Loaded += (x, y) => { TextContent += Text; Start(); };

        }

        public void Start()
        {
            TimerCallback tm = new TimerCallback(ChangeState);

            timer = new Timer(tm, null, 0, 500);
        }
        public void Stop()
        {
            timer.Dispose();
            timer = null;
        }
        private void ChangeState(object obj)
        {
            Dispatcher.Invoke(() =>
            {
                if (dots < 3)
                {
                    dots++;
                    Text += " .";
                }
                else
                {
                    dots = 0;
                    Text = "";
                    Text += TextContent;

                }
            });
        }
    }
}

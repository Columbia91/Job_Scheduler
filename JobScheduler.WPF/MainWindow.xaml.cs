using Microsoft.Win32.TaskScheduler;
using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;

namespace JobScheduler.WPF
{
    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private string period;
        private string operation;

        private DateTime date;
        private DateTime time;
        private DateTime dateTime;

        private string source = "";
        private string destination = "";
        private string subject = "";
        private string letter = "";

        private string arguments = "";
        private string appName = "";

        private System.Windows.Forms.NotifyIcon myNotifyIcon;
        public MainWindow()
        {
            InitializeComponent();

            myNotifyIcon = new System.Windows.Forms.NotifyIcon();
            myNotifyIcon.Icon = new Icon(Directory.GetCurrentDirectory() + @"\Resources\w512h51213846930623_256px.ico");
            myNotifyIcon.Text = "Job Scheduler";
            myNotifyIcon.MouseDoubleClick += new System.Windows.Forms.MouseEventHandler(MyNotifyIconMouseDoubleClick);
        }

        #region Появление иконки в трее при сворачивании
        private void Window_StateChanged(object sender, EventArgs e)
        {
            if (this.WindowState == WindowState.Minimized)
            {
                this.ShowInTaskbar = false;
                myNotifyIcon.BalloonTipTitle = "Minimize Sucessful";
                myNotifyIcon.BalloonTipText = "Minimized the app ";
                myNotifyIcon.ShowBalloonTip(400);
                myNotifyIcon.Visible = true;
            }
            else if (this.WindowState == WindowState.Normal)
            {
                myNotifyIcon.Visible = false;
                this.ShowInTaskbar = true;
            }
        }
        private void MyNotifyIconMouseDoubleClick(object sender, System.Windows.Forms.MouseEventArgs e)
        {
            this.WindowState = WindowState.Normal;
        }
        #endregion

        #region Создание задачи
        private void CreateButtonClick(object sender, RoutedEventArgs e)
        {
            using (TaskService task = new TaskService())
            {
                TaskDefinition definition = task.NewTask();

                if (SettingCalendar(definition) && SettingValuesOfTask())
                {
                    definition.Actions.Add(new ExecAction(AppDomain.CurrentDomain.BaseDirectory + appName,
                    arguments, null));

                    int numb = new Random().Next(int.MaxValue);
                    string taskName = appName + "_" + numb.ToString();
                    task.RootFolder.RegisterTaskDefinition(taskName, definition);

                    MessageBox.Show("Ваше задание добавлено в планировщик заданий Windows", "Информация", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
        }
        #endregion

        #region Установка типа задачи и ввод необходимых данных
        private bool SettingValuesOfTask()
        {
            if (operation == "Скачать файл"
                && !string.IsNullOrEmpty(urlTextBox.Text) && !string.IsNullOrWhiteSpace(urlTextBox.Text)
                && !string.IsNullOrEmpty(savePathTextBox.Text) && !string.IsNullOrWhiteSpace(savePathTextBox.Text))
            {
                appName = "Downloader.exe";
                source = urlTextBox.Text.Trim();
                destination = savePathTextBox.Text.Trim();

                if (CheckingPaths(source, destination))
                    arguments = source + " " + destination;
                else
                    return false;
            }
            else if (operation == "Переместить каталог"
                && !string.IsNullOrEmpty(initialPathTextBox.Text) && !string.IsNullOrWhiteSpace(initialPathTextBox.Text)
                && !string.IsNullOrEmpty(newPathTextBox.Text) && !string.IsNullOrWhiteSpace(newPathTextBox.Text))
            {
                appName = "MovingDirectory.exe";
                source = initialPathTextBox.Text;
                destination = newPathTextBox.Text;

                if (CheckingPaths(source, destination))
                    arguments = source + " " + destination;
                else
                    return false;
            }
            else if (operation == "Отправить mail"
                && !string.IsNullOrEmpty(recipientTextBox.Text) && !string.IsNullOrWhiteSpace(recipientTextBox.Text))
            {
                appName = "MailSender.exe";
                destination = recipientTextBox.Text.Trim();

                var textRange = new TextRange(letterRichBox.Document.ContentStart, letterRichBox.Document.ContentEnd);
                letter = textRange.Text.Trim();

                if (string.IsNullOrEmpty(subjectTextBox.Text) || string.IsNullOrWhiteSpace(subjectTextBox.Text))
                    subject = "Без темы" + " end_of_subject";
                else
                    subject = subjectTextBox.Text.Trim() + " end_of_subject";

                if (CheckingPaths(source, destination))
                    arguments = destination + " " + subject + "  " + letter;
                else
                    return false;
            }
            else
            {
                MessageBox.Show("Не все поля заполнены", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
            return true;
        }

        private bool CheckingPaths(string source, string destination)
        {
            if (source.Contains(" ") || destination.Contains(" "))
            {
                MessageBox.Show("Один из полей содержит не допустимые символы", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
            else if (destination[destination.Length - 1] == '\\' || destination[destination.Length - 1] == '/')
                destination.TrimEnd(destination[destination.Length - 1]);
            return true;
        }
        #endregion

        #region Установка расписания
        private bool SettingCalendar(TaskDefinition definition)
        {
            dateTime = new DateTime(date.Year, date.Month, date.Day, time.Hour, time.Minute, 0);
            
            if(dateTime > DateTime.Now)
            {
                definition.RegistrationInfo.Description = operation;
                if (period == "Однократно")
                {
                    definition.Triggers.Add(new TimeTrigger(dateTime));
                }
                else if (period == "Ежедневно")
                {
                    var dailyTrigger = new DailyTrigger();
                    dailyTrigger.StartBoundary = dateTime;
                    definition.Triggers.Add(dailyTrigger);
                }
                else if (period == "Еженедельно")
                {
                    var weeklyTrigger = new WeeklyTrigger();
                    weeklyTrigger.StartBoundary = dateTime;
                    int index = (int)dateTime.DayOfWeek;
                    weeklyTrigger.DaysOfWeek = (DaysOfTheWeek)Enum.GetValues(typeof(DaysOfTheWeek)).GetValue(index);
                    definition.Triggers.Add(weeklyTrigger);
                }
                else if (period == "Ежемесячно")
                {
                    var monthlyTrigger = new MonthlyTrigger();
                    monthlyTrigger.StartBoundary = dateTime;
                    if (dateTime.Day > 30)
                    {
                        monthlyTrigger.DaysOfMonth = new int[] { dateTime.Day };
                        monthlyTrigger.RunOnLastDayOfMonth = true;
                    }
                    else
                        monthlyTrigger.DaysOfMonth = new int[] { dateTime.Day };
                    definition.Triggers.Add(monthlyTrigger);
                }
            }
            else
            {
                MessageBox.Show("Не указано правильное расписание", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
            return true;
        }
        #endregion

        #region Выбор даты и времени
        private void DatePickerSelectedDateChanged(object sender, SelectionChangedEventArgs e)
        {
            date = (DateTime)datePicker.SelectedDate;
        }
        
        private void TimePickerValueChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            time = (DateTime)timePicker.Value;
        }
        #endregion

        #region Выбор типа задачи и периодичности
        private void OperationComboBoxSelected(object sender, RoutedEventArgs e)
        {
            ComboBox comboBox = (ComboBox)sender;
            var selectedItem = (TextBlock)comboBox.SelectedItem;
            operation = selectedItem.Text;
            if (operation == "Скачать файл")
            {
                downloadPanel.Visibility = Visibility.Visible;
                folderPanel.Visibility = Visibility.Collapsed;
                mailPanel.Visibility = Visibility.Collapsed;
            }
            else if (operation == "Переместить каталог")
            {
                downloadPanel.Visibility = Visibility.Collapsed;
                folderPanel.Visibility = Visibility.Visible;
                mailPanel.Visibility = Visibility.Collapsed;
            }
            else
            {
                downloadPanel.Visibility = Visibility.Collapsed;
                folderPanel.Visibility = Visibility.Collapsed;
                mailPanel.Visibility = Visibility.Visible;
            }
        }
        private void PeriodComboBoxSelected(object sender, RoutedEventArgs e)
        {
            ComboBox comboBox = (ComboBox)sender;
            var selectedItem = (TextBlock)comboBox.SelectedItem;
            period = selectedItem.Text;
        }
        #endregion
    }
}

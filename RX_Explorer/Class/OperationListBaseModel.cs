﻿using System;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Windows.UI.Xaml;

namespace RX_Explorer.Class
{
    public abstract class OperationListBaseModel : INotifyPropertyChanged
    {
        public abstract string OperationKindText { get; }

        public string[] FromPath { get; }

        public string ToPath { get; }

        public virtual string FromPathText
        {
            get
            {
                if (FromPath.Length > 5)
                {
                    return $"{Globalization.GetString("TaskList_From_Label")}: {Environment.NewLine}{string.Join(Environment.NewLine, FromPath.Take(5))}{Environment.NewLine}({FromPath.Length - 5} {Globalization.GetString("TaskList_More_Items")})...";
                }
                else
                {
                    return $"{Globalization.GetString("TaskList_From_Label")}: {Environment.NewLine}{string.Join(Environment.NewLine, FromPath)}";
                }
            }
        }

        public virtual string ToPathText
        {
            get
            {
                return $"{Globalization.GetString("TaskList_To_Label")}: {Environment.NewLine}{ToPath}";
            }
        }

        public int Progress { get; private set; }

        public string ProgressSpeed { get; private set; }

        public string RemainingTime { get; private set; }

        public string ActionButton1Content { get; private set; }

        public string ActionButton2Content { get; private set; }

        public string ActionButton3Content { get; private set; }

        public string StatusText
        {
            get
            {
                switch (Status)
                {
                    case OperationStatus.Waiting:
                        {
                            return $"{Globalization.GetString("TaskList_Task_Status_Waiting")}...";
                        }
                    case OperationStatus.Preparing:
                        {
                            return $"{Globalization.GetString("TaskList_Task_Status_Preparing")}...";
                        }
                    case OperationStatus.Processing:
                        {
                            return $"{Globalization.GetString("TaskList_Task_Status_Processing")}...";
                        }
                    case OperationStatus.NeedAttention:
                        {
                            return $"{Globalization.GetString("TaskList_Task_Status_NeedAttention")}: {Message}";
                        }
                    case OperationStatus.Error:
                        {
                            return $"{Globalization.GetString("TaskList_Task_Status_Error")}: {Message}";
                        }
                    case OperationStatus.Completed:
                        {
                            return Globalization.GetString("TaskList_Task_Status_Completed");
                        }
                    case OperationStatus.Cancelled:
                        {
                            return Globalization.GetString("TaskList_Task_Status_Cancelled");
                        }
                    default:
                        {
                            return string.Empty;
                        }
                }
            }
        }

        public bool ProgressIndeterminate { get; private set; }

        public bool ProgressError { get; private set; }

        public bool ProgressPause { get; private set; }

        public Visibility RemoveButtonVisibility { get; private set; } = Visibility.Collapsed;

        public Visibility CancelButtonVisibility { get; private set; } = Visibility.Collapsed;

        public Visibility SpeedAndTimeVisibility { get; private set; } = Visibility.Collapsed;

        public Visibility ActionButton1Visibility { get; private set; } = Visibility.Visible;

        public Visibility ActionButton2Visibility { get; private set; } = Visibility.Visible;

        public Visibility ActionButton3Visibility { get; private set; } = Visibility.Visible;

        public Visibility ActionButtonAreaVisibility { get; private set; } = Visibility.Collapsed;

        private OperationStatus status;
        public OperationStatus Status
        {
            get
            {
                return status;
            }
            private set
            {
                status = value;

                switch (status)
                {
                    case OperationStatus.Waiting:
                        {
                            ProgressIndeterminate = true;
                            RemoveButtonVisibility = Visibility.Collapsed;
                            CancelButtonVisibility = Visibility.Visible;
                            SpeedAndTimeVisibility = Visibility.Collapsed;
                            ActionButtonAreaVisibility = Visibility.Collapsed;
                            break;
                        }
                    case OperationStatus.Preparing:
                        {
                            ProgressIndeterminate = true;
                            RemoveButtonVisibility = Visibility.Collapsed;
                            CancelButtonVisibility = Visibility.Collapsed;
                            SpeedAndTimeVisibility = Visibility.Collapsed;
                            ActionButtonAreaVisibility = Visibility.Collapsed;
                            break;
                        }
                    case OperationStatus.Processing:
                        {
                            ProgressIndeterminate = false;
                            ProgressPause = false;
                            ProgressError = false;
                            RemoveButtonVisibility = Visibility.Collapsed;
                            CancelButtonVisibility = Visibility.Collapsed;
                            SpeedAndTimeVisibility = Visibility.Visible;
                            ActionButtonAreaVisibility = Visibility.Collapsed;
                            break;
                        }
                    case OperationStatus.NeedAttention:
                        {
                            ProgressIndeterminate = true;
                            ProgressPause = true;
                            ActionButton1Content = Globalization.GetString("NameCollision_Override");
                            ActionButton2Content = Globalization.GetString("NameCollision_Rename");
                            ActionButton3Content = Globalization.GetString("NameCollision_MoreOption");
                            RemoveButtonVisibility = Visibility.Collapsed;
                            CancelButtonVisibility = Visibility.Visible;
                            SpeedAndTimeVisibility = Visibility.Collapsed;
                            ActionButtonAreaVisibility = Visibility.Visible;
                            break;
                        }
                    case OperationStatus.Error:
                        {
                            ProgressIndeterminate = true;
                            ProgressError = true;
                            RemoveButtonVisibility = Visibility.Visible;
                            CancelButtonVisibility = Visibility.Collapsed;
                            SpeedAndTimeVisibility = Visibility.Collapsed;
                            ActionButtonAreaVisibility = Visibility.Collapsed;
                            OnErrorHappended?.Invoke(this, null);
                            break;
                        }
                    case OperationStatus.Cancelled:
                        {
                            ProgressIndeterminate = true;
                            ProgressPause = true;
                            RemoveButtonVisibility = Visibility.Visible;
                            CancelButtonVisibility = Visibility.Collapsed;
                            SpeedAndTimeVisibility = Visibility.Collapsed;
                            ActionButtonAreaVisibility = Visibility.Collapsed;
                            OnCancelled?.Invoke(this, null);
                            break;
                        }
                    case OperationStatus.Completed:
                        {
                            RemoveButtonVisibility = Visibility.Visible;
                            CancelButtonVisibility = Visibility.Collapsed;
                            SpeedAndTimeVisibility = Visibility.Collapsed;
                            ActionButtonAreaVisibility = Visibility.Collapsed;
                            ProgressIndeterminate = false;
                            ProgressPause = false;
                            ProgressError = false;
                            OnCompleted?.Invoke(this, null);
                            break;
                        }
                }

                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ActionButton1Content)));
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ActionButton2Content)));
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ActionButton3Content)));
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ActionButton1Visibility)));
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ActionButton2Visibility)));
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ActionButton3Visibility)));
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ActionButtonAreaVisibility)));
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(CancelButtonVisibility)));
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(RemoveButtonVisibility)));
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(SpeedAndTimeVisibility)));
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ProgressIndeterminate)));
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ProgressError)));
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ProgressPause)));
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(StatusText)));
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private string Message;
        private short ActionButtonIndex = -1;

        private event EventHandler OnCompleted;
        private event EventHandler OnErrorHappended;
        private event EventHandler OnCancelled;

        private ProgressCalculator Calculator;

        public async Task PrepareSizeDataAsync()
        {
            ulong TotalSize = 0;

            foreach (string Path in FromPath)
            {
                switch (await FileSystemStorageItemBase.OpenAsync(Path))
                {
                    case FileSystemStorageFolder Folder:
                        {
                            TotalSize += await Folder.GetFolderSizeAsync();
                            break;
                        }
                    case FileSystemStorageFile File:
                        {
                            TotalSize += File.SizeRaw;
                            break;
                        }
                }
            }

            Calculator = new ProgressCalculator(TotalSize);
        }

        public void UpdateProgress(int NewProgress)
        {
            Progress = Math.Min(Math.Max(0, NewProgress), 100);

            if (Calculator != null)
            {
                Calculator.SetProgressValue(NewProgress);
                ProgressSpeed = Calculator.GetSpeed();
                RemainingTime = Calculator.GetRemainingTime().ConvertTimsSpanToString();
            }

            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Progress)));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ProgressSpeed)));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(RemainingTime)));
        }

        public void UpdateStatus(OperationStatus Status, string Message = null)
        {
            this.Message = Message;
            this.Status = Status;
        }

        public void ActionButton1(object sender, RoutedEventArgs args)
        {
            ActionButtonIndex = 0;
        }

        public void ActionButton2(object sender, RoutedEventArgs args)
        {
            ActionButtonIndex = 1;
        }

        public void ActionButton3(object sender, RoutedEventArgs args)
        {
            ActionButtonIndex = 2;
        }

        public short WaitForButtonAction()
        {
            try
            {
                while (ActionButtonIndex < 0 && Status != OperationStatus.Cancelled)
                {
                    Thread.Sleep(500);
                }

                return ActionButtonIndex;
            }
            finally
            {
                ActionButtonIndex = -1;
            }
        }

        public OperationListBaseModel(string[] FromPath, string ToPath, EventHandler OnCompleted, EventHandler OnErrorHappended, EventHandler OnCancelled)
        {
            Status = OperationStatus.Waiting;
            ProgressIndeterminate = true;

            this.FromPath = FromPath;
            this.ToPath = ToPath;
            this.OnCompleted = OnCompleted;
            this.OnErrorHappended = OnErrorHappended;
            this.OnCancelled = OnCancelled;
        }
    }
}

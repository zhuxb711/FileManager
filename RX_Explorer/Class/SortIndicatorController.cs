﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace RX_Explorer.Class
{
    public sealed class SortIndicatorController : INotifyPropertyChanged
    {
        public Visibility NameIndicatorVisibility { get; private set; }

        public Visibility ModifiedTimeIndicatorVisibility { get; private set; }

        public Visibility TypeIndicatorVisibility { get; private set; }

        public Visibility SizeIndicatorVisibility { get; private set; }

        public Visibility PathIndicatorVisibility { get; private set; }

        public FontIcon NameIndicatorIcon { get; private set; }

        public FontIcon ModifiedTimeIndicatorIcon { get; private set; }

        public FontIcon TypeIndicatorIcon { get; private set; }

        public FontIcon SizeIndicatorIcon { get; private set; }

        public FontIcon PathIndicatorIcon { get; private set; }

        private const string UpArrowIcon = "\uF0AD";

        private const string DownArrowIcon = "\uF0AE";

        public event PropertyChangedEventHandler PropertyChanged;

        public void SetIndicatorStatus(SortTarget Target, SortDirection Direction)
        {
            switch (Target)
            {
                case SortTarget.Name:
                    {
                        NameIndicatorIcon = new FontIcon { Glyph = Direction == SortDirection.Ascending ? UpArrowIcon : DownArrowIcon };

                        NameIndicatorVisibility = Visibility.Visible;
                        ModifiedTimeIndicatorVisibility = Visibility.Collapsed;
                        TypeIndicatorVisibility = Visibility.Collapsed;
                        SizeIndicatorVisibility = Visibility.Collapsed;
                        PathIndicatorVisibility = Visibility.Collapsed;

                        break;
                    }
                case SortTarget.ModifiedTime:
                    {
                        ModifiedTimeIndicatorIcon = new FontIcon { Glyph = Direction == SortDirection.Ascending ? UpArrowIcon : DownArrowIcon };

                        NameIndicatorVisibility = Visibility.Collapsed;
                        ModifiedTimeIndicatorVisibility = Visibility.Visible;
                        TypeIndicatorVisibility = Visibility.Collapsed;
                        SizeIndicatorVisibility = Visibility.Collapsed;
                        PathIndicatorVisibility = Visibility.Collapsed;

                        break;
                    }
                case SortTarget.Type:
                    {
                        TypeIndicatorIcon = new FontIcon { Glyph = Direction == SortDirection.Ascending ? UpArrowIcon : DownArrowIcon };

                        NameIndicatorVisibility = Visibility.Collapsed;
                        ModifiedTimeIndicatorVisibility = Visibility.Collapsed;
                        TypeIndicatorVisibility = Visibility.Visible;
                        SizeIndicatorVisibility = Visibility.Collapsed;
                        PathIndicatorVisibility = Visibility.Collapsed;

                        break;
                    }
                case SortTarget.Size:
                    {
                        SizeIndicatorIcon = new FontIcon { Glyph = Direction == SortDirection.Ascending ? UpArrowIcon : DownArrowIcon };

                        NameIndicatorVisibility = Visibility.Collapsed;
                        ModifiedTimeIndicatorVisibility = Visibility.Collapsed;
                        TypeIndicatorVisibility = Visibility.Collapsed;
                        SizeIndicatorVisibility = Visibility.Visible;
                        PathIndicatorVisibility = Visibility.Collapsed;

                        break;
                    }
                case SortTarget.Path:
                    {
                        PathIndicatorIcon = new FontIcon { Glyph = Direction == SortDirection.Ascending ? UpArrowIcon : DownArrowIcon };

                        NameIndicatorVisibility = Visibility.Collapsed;
                        ModifiedTimeIndicatorVisibility = Visibility.Collapsed;
                        TypeIndicatorVisibility = Visibility.Collapsed;
                        SizeIndicatorVisibility = Visibility.Collapsed;
                        PathIndicatorVisibility = Visibility.Visible;

                        break;
                    }
                default:
                    {
                        throw new NotSupportedException("SortTarget is not supported");
                    }
            }

            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(NameIndicatorIcon)));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ModifiedTimeIndicatorIcon)));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(TypeIndicatorIcon)));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(SizeIndicatorIcon)));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(PathIndicatorIcon)));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(NameIndicatorVisibility)));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ModifiedTimeIndicatorVisibility)));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(TypeIndicatorVisibility)));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(SizeIndicatorVisibility)));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(PathIndicatorVisibility)));
        }

        public SortIndicatorController()
        {
            SetIndicatorStatus(SortTarget.Name, SortDirection.Ascending);
        }
    }
}

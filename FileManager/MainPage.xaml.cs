﻿using AnimationEffectProvider;
using Microsoft.Toolkit.Uwp.Notifications;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Background;
using Windows.ApplicationModel.Core;
using Windows.Foundation;
using Windows.Security.Cryptography;
using Windows.Security.Cryptography.Core;
using Windows.Services.Store;
using Windows.Storage;
using Windows.Storage.Streams;
using Windows.System;
using Windows.System.Profile;
using Windows.UI.Core.Preview;
using Windows.UI.Notifications;
using Windows.UI.Shell;
using Windows.UI.StartScreen;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media.Animation;
using Windows.UI.Xaml.Navigation;

namespace FileManager
{
    public sealed partial class MainPage : Page, INotifyPropertyChanged
    {
        public static MainPage ThisPage { get; private set; }

        private Dictionary<Type, string> PageDictionary;

        public bool IsUSBActivate { get; set; } = false;

        public string ActivateUSBDevicePath { get; private set; }

        private EntranceAnimationEffect EntranceEffectProvider;

        public bool IsAnyTaskRunning { get; set; }

        public GridLength LeftSideLength
        {
            get
            {
                if (ApplicationData.Current.LocalSettings.Values["IsLeftAreaOpen"] is bool Enable)
                {
                    return Enable ? new GridLength(2.5, GridUnitType.Star) : new GridLength(0);
                }
                else
                {
                    ApplicationData.Current.LocalSettings.Values["IsLeftAreaOpen"] = true;

                    return new GridLength(2.5, GridUnitType.Star);
                }
            }
            set
            {
                if (value.Value == 0)
                {
                    ApplicationData.Current.LocalSettings.Values["IsLeftAreaOpen"] = false;
                }
                else
                {
                    ApplicationData.Current.LocalSettings.Values["IsLeftAreaOpen"] = true;
                }

                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(LeftSideLength)));
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public MainPage()
        {
            InitializeComponent();
            ThisPage = this;
            Window.Current.SetTitleBar(TitleBar);
            Loaded += MainPage_Loaded;
            Application.Current.EnteredBackground += Current_EnteredBackground;
            Application.Current.LeavingBackground += Current_LeavingBackground;
            SystemNavigationManagerPreview.GetForCurrentView().CloseRequested += MainPage_CloseRequested;

            try
            {
                ToastNotificationManager.History.Clear();
            }
            catch (Exception)
            {

            }
#if DEBUG
            AppName.Text += " (Debug 模式)";
#endif
        }

        private void Current_LeavingBackground(object sender, LeavingBackgroundEventArgs e)
        {
            ToastNotificationManager.History.Remove("EnterBackgroundTips");
        }

        private void Current_EnteredBackground(object sender, EnteredBackgroundEventArgs e)
        {
            if (IsAnyTaskRunning || GeneralTransformer.IsAnyTransformTaskRunning)
            {
                ToastNotificationManager.History.Remove("EnterBackgroundTips");

                if (Globalization.Language == LanguageEnum.Chinese)
                {
                    var Content = new ToastContent()
                    {
                        Scenario = ToastScenario.Alarm,
                        Launch = "EnterBackgroundTips",
                        Visual = new ToastVisual()
                        {
                            BindingGeneric = new ToastBindingGeneric()
                            {
                                Children =
                                {
                                    new AdaptiveText()
                                    {
                                        Text = "请不要最小化RX文件管理器"
                                    },

                                    new AdaptiveText()
                                    {
                                        Text = "Windows策略可能会终止RX的运行"
                                    },

                                    new AdaptiveText()
                                    {
                                        Text="点击以重新激活"
                                    }
                                }
                            }
                        },
                    };
                    ToastNotificationManager.CreateToastNotifier().Show(new ToastNotification(Content.GetXml()) { Tag = "EnterBackgroundTips", Priority = ToastNotificationPriority.High });
                }
                else
                {
                    var Content = new ToastContent()
                    {
                        Scenario = ToastScenario.Alarm,
                        Launch = "EnterBackgroundTips",
                        Visual = new ToastVisual()
                        {
                            BindingGeneric = new ToastBindingGeneric()
                            {
                                Children =
                                {
                                    new AdaptiveText()
                                    {
                                        Text = "Please do not minimize RX-Explorer"
                                    },

                                    new AdaptiveText()
                                    {
                                        Text = "Windows policy may terminate RX-Explorer"
                                    },

                                    new AdaptiveText()
                                    {
                                        Text="Click to activate"
                                    }
                                }
                            }
                        },
                    };
                    ToastNotificationManager.CreateToastNotifier().Show(new ToastNotification(Content.GetXml()) { Tag = "EnterBackgroundTips" });
                }
            }
        }

        private async void MainPage_CloseRequested(object sender, SystemNavigationCloseRequestedPreviewEventArgs e)
        {
            Deferral Deferral = e.GetDeferral();

            if (IsAnyTaskRunning || GeneralTransformer.IsAnyTransformTaskRunning)
            {
                if (Globalization.Language == LanguageEnum.Chinese)
                {
                    QueueContentDialog Dialog = new QueueContentDialog
                    {
                        Title = "警告",
                        Content = "RX文件管理器正在运行一些任务，此时关闭可能导致数据不正确\r\r是否继续?",
                        PrimaryButtonText = "立即关闭",
                        CloseButtonText = "等待完成"
                    };

                    if ((await Dialog.ShowAsync().ConfigureAwait(true)) != ContentDialogResult.Primary)
                    {
                        e.Handled = true;
                    }
                    else
                    {
                        IsAnyTaskRunning = false;
                        GeneralTransformer.IsAnyTransformTaskRunning = false;
                        ToastNotificationManager.History.Clear();
                    }
                }
                else
                {
                    QueueContentDialog Dialog = new QueueContentDialog
                    {
                        Title = "Warning",
                        Content = "The RX Explorer is running some tasks, closing at this time may cause data errors\r\rDo you want to continue?",
                        PrimaryButtonText = "Right now",
                        CloseButtonText = "Later"
                    };

                    if ((await Dialog.ShowAsync().ConfigureAwait(true)) != ContentDialogResult.Primary)
                    {
                        e.Handled = true;
                    }
                    else
                    {
                        IsAnyTaskRunning = false;
                        GeneralTransformer.IsAnyTransformTaskRunning = false;
                        ToastNotificationManager.History.Clear();
                    }
                }
            }

            Deferral.Complete();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            if (e.Parameter is Tuple<string, Rect> Parameter)
            {
                string[] Paras = Parameter.Item1.Split("||");
                if (Paras[0] == "USBActivate")
                {
                    IsUSBActivate = true;
                    ActivateUSBDevicePath = Paras[1];
                }

                if (Win10VersionChecker.Windows10_1903)
                {
                    EntranceEffectProvider = new EntranceAnimationEffect(this, Nav, Parameter.Item2);
                    EntranceEffectProvider.PrepareEntranceEffect();
                }
            }
            else if (e.Parameter is Rect SplashRect)
            {
                if (Win10VersionChecker.Windows10_1903)
                {
                    EntranceEffectProvider = new EntranceAnimationEffect(this, Nav, SplashRect);
                    EntranceEffectProvider.PrepareEntranceEffect();
                }
            }
        }

        private async void MainPage_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                if (ApplicationData.Current.LocalSettings.Values["IsDoubleClickEnable"] is bool IsDoubleClick)
                {
                    SettingControl.IsDoubleClickEnable = IsDoubleClick;
                }
                else
                {
                    ApplicationData.Current.LocalSettings.Values["IsDoubleClickEnable"] = true;
                }

                if (Globalization.Language == LanguageEnum.Chinese)
                {
                    PageDictionary = new Dictionary<Type, string>()
                    {
                        {typeof(TabViewContainer),"这台电脑" },
                        {typeof(FileControl),"这台电脑" },
                        {typeof(SecureArea),"安全域" }
                    };
                }
                else
                {
                    PageDictionary = new Dictionary<Type, string>()
                    {
                        {typeof(TabViewContainer),"ThisPC" },
                        {typeof(FileControl),"ThisPC" },
                        {typeof(SecureArea),"Security Area" }
                    };
                }

                if (Win10VersionChecker.Windows10_1903)
                {
                    EntranceEffectProvider.StartEntranceEffect();
                }

                Nav.Navigate(typeof(TabViewContainer));

                var PictureUri = await SQLite.Current.GetBackgroundPictureAsync().ConfigureAwait(true);
                var FileList = await (await ApplicationData.Current.LocalFolder.CreateFolderAsync("CustomImageFolder", CreationCollisionOption.OpenIfExists)).GetFilesAsync();
                foreach (var ToDeletePicture in FileList.Where((File) => PictureUri.All((ImageUri) => ImageUri.ToString().Replace("ms-appdata:///local/CustomImageFolder/", string.Empty) != File.Name)))
                {
                    await ToDeletePicture.DeleteAsync(StorageDeleteOption.PermanentDelete);
                }

                await GetUserInfoAsync().ConfigureAwait(true);

                await ShowReleaseLogDialogAsync().ConfigureAwait(true);

#if !DEBUG
                await RegisterBackgroundTaskAsync().ConfigureAwait(true);
#endif

                await DonateDeveloperAsync().ConfigureAwait(true);

                await Task.Delay(10000).ConfigureAwait(true);

                await PinApplicationToTaskBarAsync().ConfigureAwait(true);

            }
            catch (Exception ex)
            {
                ExceptionTracer.RequestBlueScreen(ex);
            }
        }

        private async Task ShowReleaseLogDialogAsync()
        {
            if (Microsoft.Toolkit.Uwp.Helpers.SystemInformation.IsAppUpdated || Microsoft.Toolkit.Uwp.Helpers.SystemInformation.IsFirstRun)
            {
                WhatIsNew Dialog = new WhatIsNew();
                _ = await Dialog.ShowAsync().ConfigureAwait(true);
            }
        }

        private async Task GetUserInfoAsync()
        {
            if ((await User.FindAllAsync()).Where(p => p.AuthenticationStatus == UserAuthenticationStatus.LocallyAuthenticated && p.Type == UserType.LocalUser).FirstOrDefault() is User CurrentUser)
            {
                string UserName = (await CurrentUser.GetPropertyAsync(KnownUserProperties.FirstName))?.ToString();
                string UserID = (await CurrentUser.GetPropertyAsync(KnownUserProperties.AccountName))?.ToString();
                if (string.IsNullOrEmpty(UserID))
                {
                    var Token = HardwareIdentification.GetPackageSpecificToken(null);
                    HashAlgorithmProvider md5 = HashAlgorithmProvider.OpenAlgorithm(HashAlgorithmNames.Md5);
                    IBuffer hashedData = md5.HashData(Token.Id);
                    UserID = CryptographicBuffer.EncodeToHexString(hashedData).ToUpper();
                }

                if (string.IsNullOrEmpty(UserName))
                {
                    UserName = UserID.Substring(0, 10);
                }

                ApplicationData.Current.LocalSettings.Values["SystemUserName"] = UserName;
                ApplicationData.Current.LocalSettings.Values["SystemUserID"] = UserID;
            }
            else
            {
                var Token = HardwareIdentification.GetPackageSpecificToken(null);
                HashAlgorithmProvider md5 = HashAlgorithmProvider.OpenAlgorithm(HashAlgorithmNames.Md5);
                IBuffer hashedData = md5.HashData(Token.Id);
                string UserID = CryptographicBuffer.EncodeToHexString(hashedData).ToUpper();
                string UserName = UserID.Substring(0, 10);

                ApplicationData.Current.LocalSettings.Values["SystemUserName"] = UserName;
                ApplicationData.Current.LocalSettings.Values["SystemUserID"] = UserID;
            }
        }

        private async Task RegisterBackgroundTaskAsync()
        {
            switch (await BackgroundExecutionManager.RequestAccessAsync())
            {
                case BackgroundAccessStatus.AllowedSubjectToSystemPolicy:
                case BackgroundAccessStatus.AlwaysAllowed:
                    {
                        if (BackgroundTaskRegistration.AllTasks.Select((item) => item.Value).FirstOrDefault((task) => task.Name == "UpdateTask") is IBackgroundTaskRegistration Registration)
                        {
                            Registration.Unregister(true);
                        }

                        SystemTrigger Trigger = new SystemTrigger(SystemTriggerType.SessionConnected, false);
                        BackgroundTaskBuilder Builder = new BackgroundTaskBuilder
                        {
                            Name = "UpdateTask",
                            IsNetworkRequested = true,
                            TaskEntryPoint = "UpdateCheckBackgroundTask.UpdateCheck"
                        };
                        Builder.SetTrigger(Trigger);
                        Builder.AddCondition(new SystemCondition(SystemConditionType.InternetAvailable));
                        Builder.AddCondition(new SystemCondition(SystemConditionType.UserPresent));
                        Builder.AddCondition(new SystemCondition(SystemConditionType.FreeNetworkAvailable));
                        Builder.Register();

                        break;
                    }
                default:
                    {
                        if (!ApplicationData.Current.LocalSettings.Values.ContainsKey("DisableBackgroundTaskTips"))
                        {
                            if (Globalization.Language == LanguageEnum.Chinese)
                            {
                                QueueContentDialog Dialog = new QueueContentDialog
                                {
                                    Title = "提示",
                                    Content = "后台任务被禁用，RX将无法在更新发布时及时通知您\r\r请手动开启后台任务权限",
                                    PrimaryButtonText = "现在开启",
                                    SecondaryButtonText = "稍后提醒",
                                    CloseButtonText = "不再提醒"
                                };
                                switch (await Dialog.ShowAsync().ConfigureAwait(true))
                                {
                                    case ContentDialogResult.Primary:
                                        {
                                            _ = await Launcher.LaunchUriAsync(new Uri("ms-settings:privacy-backgroundapps"));
                                            break;
                                        }
                                    case ContentDialogResult.Secondary:
                                        {
                                            break;
                                        }
                                    default:
                                        {
                                            ApplicationData.Current.LocalSettings.Values["DisableBackgroundTaskTips"] = true;
                                            break;
                                        }
                                }
                            }
                            else
                            {
                                QueueContentDialog Dialog = new QueueContentDialog
                                {
                                    Title = "Tips",
                                    Content = "Background tasks are disabled, RX will not be able to notify you in time when the update is released \r \rPlease manually enable background task permissions",
                                    PrimaryButtonText = "Authorize now",
                                    SecondaryButtonText = "Remind later",
                                    CloseButtonText = "Never remind"
                                };
                                switch (await Dialog.ShowAsync().ConfigureAwait(true))
                                {
                                    case ContentDialogResult.Primary:
                                        {
                                            _ = await Launcher.LaunchUriAsync(new Uri("ms-settings:privacy-backgroundapps"));
                                            break;
                                        }
                                    case ContentDialogResult.Secondary:
                                        {
                                            break;
                                        }
                                    default:
                                        {
                                            ApplicationData.Current.LocalSettings.Values["DisableBackgroundTaskTips"] = true;
                                            break;
                                        }
                                }
                            }
                        }
                        break;
                    }
            }
        }

        private void Nav_Navigated(object sender, NavigationEventArgs e)
        {
            if (NavView.MenuItems.Select((Item) => Item as NavigationViewItem).FirstOrDefault((Item) => Item.Content.ToString() == PageDictionary[e.SourcePageType]) is NavigationViewItem Item)
            {
                Item.IsSelected = true;
            }
        }

        private async Task PinApplicationToTaskBarAsync()
        {
            if (ApplicationData.Current.LocalSettings.Values.ContainsKey("IsPinToTaskBar"))
            {
                if (!ApplicationData.Current.RoamingSettings.Values.ContainsKey("IsRated"))
                {
                    await RequestRateApplication().ConfigureAwait(false);
                }
            }
            else
            {
                ApplicationData.Current.LocalSettings.Values["IsPinToTaskBar"] = true;

                TaskbarManager BarManager = TaskbarManager.GetDefault();
                StartScreenManager ScreenManager = StartScreenManager.GetDefault();

                bool PinStartScreen = false, PinTaskBar = false;

                AppListEntry Entry = (await Package.Current.GetAppListEntriesAsync())[0];
                if (ScreenManager.SupportsAppListEntry(Entry) && !await ScreenManager.ContainsAppListEntryAsync(Entry))
                {
                    PinStartScreen = true;
                }
                if (BarManager.IsPinningAllowed && !await BarManager.IsCurrentAppPinnedAsync())
                {
                    PinTaskBar = true;
                }

                if (PinStartScreen && PinTaskBar)
                {
                    PinTip.ActionButtonClick += async (s, e) =>
                    {
                        s.IsOpen = false;
                        _ = await BarManager.RequestPinCurrentAppAsync();
                        _ = await ScreenManager.RequestAddAppListEntryAsync(Entry);
                    };
                }
                else if (PinStartScreen && !PinTaskBar)
                {
                    PinTip.ActionButtonClick += async (s, e) =>
                    {
                        s.IsOpen = false;
                        _ = await ScreenManager.RequestAddAppListEntryAsync(Entry);
                    };
                }
                else if (!PinStartScreen && PinTaskBar)
                {
                    PinTip.ActionButtonClick += async (s, e) =>
                    {
                        s.IsOpen = false;
                        _ = await BarManager.RequestPinCurrentAppAsync();
                    };
                }
                else
                {
                    PinTip.ActionButtonClick += (s, e) =>
                    {
                        s.IsOpen = false;
                    };
                }

                PinTip.Closed += async (s, e) =>
                {
                    s.IsOpen = false;
                    await RequestRateApplication().ConfigureAwait(true);
                };

                PinTip.Subtitle = Globalization.Language == LanguageEnum.Chinese
                    ? "将RX文件管理器固定在和开始屏幕任务栏，启动更快更方便哦！\r\r★固定至开始菜单\r\r★固定至任务栏"
                    : "Pin the RX FileManager to StartScreen and TaskBar ！\r\r★Pin to StartScreen\r\r★Pin to TaskBar";
                PinTip.IsOpen = true;
            }
        }

        private async Task RequestRateApplication()
        {
            await Task.Delay(60000).ConfigureAwait(true);

            RateTip.ActionButtonClick += async (s, e) =>
            {
                s.IsOpen = false;
                await Launcher.LaunchUriAsync(new Uri("ms-windows-store://review/?productid=9N88QBQKF2RS"));
                ApplicationData.Current.RoamingSettings.Values["IsRated"] = true;
            };
            RateTip.IsOpen = true;
        }

        private async Task DonateDeveloperAsync()
        {
            if (ApplicationData.Current.LocalSettings.Values["IsDonated"] is bool Donated)
            {
                if (Donated)
                {
                    StoreProductQueryResult PurchasedProductResult = await StoreContext.GetDefault().GetUserCollectionAsync(new string[] { "Durable" });
                    if (PurchasedProductResult.ExtendedError == null && PurchasedProductResult.Products.Count > 0)
                    {
                        return;
                    }

                    await Task.Delay(30000).ConfigureAwait(true);
                    DonateTip.ActionButtonClick += async (s, e) =>
                    {
                        s.IsOpen = false;

                        if (Globalization.Language == LanguageEnum.Chinese)
                        {
                            StoreContext Store = StoreContext.GetDefault();
                            StoreProductQueryResult StoreProductResult = await Store.GetAssociatedStoreProductsAsync(new string[] { "Durable" });
                            if (StoreProductResult.ExtendedError == null)
                            {
                                StoreProduct Product = StoreProductResult.Products.Values.FirstOrDefault();
                                if (Product != null)
                                {
                                    switch ((await Store.RequestPurchaseAsync(Product.StoreId)).Status)
                                    {
                                        case StorePurchaseStatus.Succeeded:
                                            {
                                                QueueContentDialog QueueContenDialog = new QueueContentDialog
                                                {
                                                    Title = "感谢",
                                                    Content = "感谢您的支持，我们将努力将RX做得越来越好q(≧▽≦q)\r\r" +
                                                               "RX文件管理器的诞生，是为了填补UWP文件管理器缺位的空白\r" +
                                                               "它并非是一个盈利项目，因此下载和使用都是免费的，并且不含有广告\r" +
                                                               "RX的目标是打造一个免费且功能全面文件管理器\r" +
                                                               "RX文件管理器是我利用业余时间开发的项目\r" +
                                                               "希望大家能够喜欢\r\r" +
                                                               "Ruofan,\r敬上",
                                                    CloseButtonText = "朕知道了"
                                                };
                                                _ = await QueueContenDialog.ShowAsync().ConfigureAwait(true);
                                                break;
                                            }
                                        case StorePurchaseStatus.AlreadyPurchased:
                                            {
                                                QueueContentDialog QueueContenDialog = new QueueContentDialog
                                                {
                                                    Title = "再次感谢",
                                                    Content = "您已为RX支持过一次了，您的心意开发者已心领\r\r" +
                                                              "RX的初衷并非是赚钱，因此不可重复支持哦\r\r" +
                                                              "您可以向周围的人宣传一下RX，也是对RX的最好的支持哦（*＾-＾*）\r\r" +
                                                              "Ruofan,\r敬上",
                                                    CloseButtonText = "朕知道了"
                                                };
                                                _ = await QueueContenDialog.ShowAsync().ConfigureAwait(true);
                                                break;
                                            }
                                        case StorePurchaseStatus.NotPurchased:
                                            {
                                                QueueContentDialog QueueContenDialog = new QueueContentDialog
                                                {
                                                    Title = "感谢",
                                                    Content = "无论支持与否，RX始终如一\r\r" +
                                                              "即使您最终决定放弃支持本项目，依然十分感谢您能够点进来看一看\r\r" +
                                                              "Ruofan,\r敬上",
                                                    CloseButtonText = "朕知道了"
                                                };
                                                _ = await QueueContenDialog.ShowAsync().ConfigureAwait(true);
                                                break;
                                            }
                                        default:
                                            {
                                                QueueContentDialog QueueContenDialog = new QueueContentDialog
                                                {
                                                    Title = "抱歉",
                                                    Content = "由于Microsoft Store或网络原因，无法打开支持页面，请稍后再试",
                                                    CloseButtonText = "朕知道了"
                                                };
                                                _ = await QueueContenDialog.ShowAsync().ConfigureAwait(true);
                                                break;
                                            }
                                    }
                                }
                            }
                            else
                            {
                                QueueContentDialog QueueContenDialog = new QueueContentDialog
                                {
                                    Title = "抱歉",
                                    Content = "由于Microsoft Store或网络原因，无法打开支持页面，请稍后再试",
                                    CloseButtonText = "朕知道了"
                                };
                                _ = await QueueContenDialog.ShowAsync().ConfigureAwait(true);
                            }
                        }
                        else
                        {
                            StoreContext Store = StoreContext.GetDefault();
                            StoreProductQueryResult StoreProductResult = await Store.GetAssociatedStoreProductsAsync(new string[] { "Durable" });
                            if (StoreProductResult.ExtendedError == null)
                            {
                                StoreProduct Product = StoreProductResult.Products.Values.FirstOrDefault();
                                if (Product != null)
                                {
                                    switch ((await Store.RequestPurchaseAsync(Product.StoreId)).Status)
                                    {
                                        case StorePurchaseStatus.Succeeded:
                                            {
                                                QueueContentDialog QueueContenDialog = new QueueContentDialog
                                                {
                                                    Title = "Appreciation",
                                                    Content = "Thank you for your support, we will work hard to make RX better and better q(≧▽≦q)\r\r" +
                                                              "The RX file manager was born to fill the gaps in the UWP file manager\r" +
                                                              "This is not a profitable project, so downloading and using are free and do not include ads\r" +
                                                              "RX's goal is to create a free and full-featured file manager\r" +
                                                              "RX File Manager is a project I developed in my spare time\r" +
                                                              "I hope everyone likes\r\r" +
                                                              "Sincerely,\rRuofan",
                                                    CloseButtonText = "Got it"
                                                };
                                                _ = await QueueContenDialog.ShowAsync().ConfigureAwait(true);
                                                break;
                                            }
                                        case StorePurchaseStatus.AlreadyPurchased:
                                            {
                                                QueueContentDialog QueueContenDialog = new QueueContentDialog
                                                {
                                                    Title = "Thanks again",
                                                    Content = "You have already supported RX once, thank you very much\r\r" +
                                                              "The original intention of RX is not to make money, so you can't repeat purchase it.\r\r" +
                                                              "You can advertise the RX to the people around you, and it is also the best support for RX（*＾-＾*）\r\r" +
                                                              "Sincerely,\rRuofan",
                                                    CloseButtonText = "Got it"
                                                };
                                                _ = await QueueContenDialog.ShowAsync().ConfigureAwait(true);
                                                break;
                                            }
                                        case StorePurchaseStatus.NotPurchased:
                                            {
                                                QueueContentDialog QueueContenDialog = new QueueContentDialog
                                                {
                                                    Title = "Appreciation",
                                                    Content = "Whether supported or not, RX is always the same\r\r" +
                                                              "Even if you finally decide to give up supporting the project, thank you very much for being able to click to see it\r\r" +
                                                              "Sincerely,\rRuofan",
                                                    CloseButtonText = "Got it"
                                                };
                                                _ = await QueueContenDialog.ShowAsync().ConfigureAwait(true);
                                                break;
                                            }
                                        default:
                                            {
                                                QueueContentDialog QueueContenDialog = new QueueContentDialog
                                                {
                                                    Title = "Sorry",
                                                    Content = "Unable to open support page due to Microsoft Store or network, please try again later",
                                                    CloseButtonText = "Got it"
                                                };
                                                _ = await QueueContenDialog.ShowAsync().ConfigureAwait(true);
                                                break;
                                            }
                                    }
                                }
                            }
                            else
                            {
                                QueueContentDialog QueueContenDialog = new QueueContentDialog
                                {
                                    Title = "Sorry",
                                    Content = "Unable to open support page due to Microsoft Store or network, please try again later",
                                    CloseButtonText = "Got it"
                                };
                                _ = await QueueContenDialog.ShowAsync().ConfigureAwait(true);
                            }
                        }
                    };

                    if (Globalization.Language == LanguageEnum.Chinese)
                    {
                        DonateTip.Subtitle = "开发者开发RX文件管理器花费了大量精力\r" +
                                             "🎉您可以自愿为开发者贡献一点小零花钱🎉\r\r" +
                                             "若您不愿意，则可以点击\"跪安\"以取消\r" +
                                             "若您愿意支持开发者，则可以点击\"准奏\"\r\r" +
                                             "Tips: 支持的小伙伴可以解锁独有文件保险柜功能：“安全域”";
                    }
                    else
                    {
                        DonateTip.Subtitle = "It takes a lot of effort for developers to develop RX file manager\r" +
                                             "🎉You can volunteer to contribute a little pocket money to developers.🎉\r\r" +
                                             "If you don't want to, you can click \"Later\" to cancel\r" +
                                             "if you want to donate, you can click \"Donate\" to support developer\r\r" +
                                             "Tips: Donator can unlock the unique file safe feature: \"Security Area\"";
                    }

                    DonateTip.IsOpen = true;
                    ApplicationData.Current.LocalSettings.Values["IsDonated"] = false;
                }
            }
            else
            {
                ApplicationData.Current.LocalSettings.Values["IsDonated"] = true;
            }
        }

        private async void NavView_ItemInvoked(NavigationView sender, NavigationViewItemInvokedEventArgs args)
        {
            try
            {
                if (args.IsSettingsInvoked)
                {
                    NavView.IsBackEnabled = false;

                    _ = FindName(nameof(SettingControl));

                    await SettingControl.Show().ConfigureAwait(true);
                }
                else
                {
                    if (SettingControl != null)
                    {
                        await SettingControl.Hide().ConfigureAwait(true);
                    }

                    switch (args.InvokedItem.ToString())
                    {
                        case "这台电脑":
                        case "ThisPC":
                            {
                                NavView.IsBackEnabled = (TabViewContainer.CurrentPageNav?.CanGoBack).GetValueOrDefault();
                                Nav.Navigate(typeof(TabViewContainer), null, new SlideNavigationTransitionInfo() { Effect = SlideNavigationTransitionEffect.FromLeft });
                                break;
                            }
                        case "安全域":
                        case "Security Area":
                            {
                                NavView.IsBackEnabled = false;
                                Nav.Navigate(typeof(SecureArea), null, new SlideNavigationTransitionInfo() { Effect = SlideNavigationTransitionEffect.FromRight });
                                break;
                            }
                    }
                }
            }
            catch (Exception ex)
            {
                ExceptionTracer.RequestBlueScreen(ex);
            }
        }

        private void Nav_Navigating(object sender, NavigatingCancelEventArgs e)
        {
            if (Nav.CurrentSourcePageType == e.SourcePageType)
            {
                e.Cancel = true;
            }
        }

        private void NavView_BackRequested(NavigationView sender, NavigationViewBackRequestedEventArgs args)
        {
            TabViewContainer.GoBack();
        }
    }
}

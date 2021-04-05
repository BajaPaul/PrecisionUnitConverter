using LibraryCoder.MainPageCommon;
using PrecisionUnitConverter.Pages;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

// Following Enum is generally unique for each App so place here.
/// <summary>
/// Enum used to reset App setup value via method AppReset().
/// </summary>
public enum EnumResetApp { DoNothing, ResetApp, ResetPurchaseHistory, ResetRateHistory, ShowDataStoreValues };

namespace PrecisionUnitConverter
{
    public sealed partial class MainPage : Page
    {
        // TODO: Update version number in next string before publishing application to Microsoft Store.
        /// <summary>
        /// String containing version of application as set in Package.appxmanifest file.
        /// </summary>
        public readonly string stringAppVersion = "2021.4.3";

        /// <summary>
        /// Pointer to MainPage. Other pages can use this pointer to access public variables and methods in MainPage.
        /// </summary>
        public static MainPage mainPagePointer;

        /// <summary>
        /// Location App uses to read or write various App settings. Save set value here for use in other pages as needed.
        /// </summary>
        public ApplicationDataContainer applicationDataContainer;

        // All data store 'ds' strings (keys) used by application declared here. These are (key, value) pairs. Each key has a matching value.
        
        /// <summary>
        /// Value is "BoolAppPurchased".
        /// </summary>
        public readonly string ds_BoolAppPurchased = "BoolAppPurchased";

        /// <summary>
        /// Value is "BoolAppRated".
        /// </summary>
        public readonly string ds_BoolAppRated = "BoolAppRated";

        /// <summary>
        /// Value is "IntAppRatedCounter".
        /// </summary>
        public readonly string ds_IntAppRatedCounter = "IntAppRatedCounter";

        /// <summary>
        /// True if application has been purchased, false otherwise.
        /// </summary>
        public bool boolAppPurchased = false;

        /// <summary>
        /// True if application has been rated, false otherwise.
        /// </summary>
        public bool boolAppRated = false;

        /// <summary>
        /// True if application purchase check has been competed, false otherwise.
        /// </summary>
        public bool boolPurchaseCheckCompleted = false;

        /// <summary>
        /// Save purchase check output string here for display on page Start if User comes back to page.
        /// </summary>
        public string stringPurchaseCheckOutput;

        /// <summary>
        /// String to append to conversion description indicating is base unit. Value="***"
        /// </summary>
        public readonly string stringBaseUnitIndicator = "***";

        /// <summary>
        /// Set public value of MainPage XAML ScrollViewerMP.
        /// </summary>
        public ScrollViewer mainPageScrollViewer;

        /// <summary>
        /// Set public value of MainPage XAML ButBack.
        /// </summary>
        public Button mainPageButBack;

        /// <summary>
        /// Set public value of MainPage XAML ButAbout.
        /// </summary>
        public Button mainPageButAbout;

        public MainPage()
        {
            InitializeComponent();
            mainPagePointer = this;     // Set pointer to this page at this location since required by various pages, methods, and libraries.
        }

        /*** Public Methods ****************************************************************************************************/

        /// <summary>
        /// Navigate to page Conversions.
        /// </summary>
        public void NavigateToPageConversions()
        {
            FrameMP.Navigate(typeof(Conversions));  // Navigate to page Conversions.
            FrameMP.BackStack.Clear();              // Clear page navigation history.
        }

        /// <summary>
        /// Open Windows 10 Store App so User can rate and review this App.
        /// </summary>
        /// <returns></returns>
        public async Task<bool> RateAppInW10StoreAsync()
        {
            if (await LibMPC.ShowRatingReviewDialogAsync())
            {
                boolAppRated = true;
                applicationDataContainer.Values[ds_BoolAppRated] = true;        // Write setting to data store. 
                applicationDataContainer.Values.Remove(ds_IntAppRatedCounter);  // Remove ds_IntAppRatedCounter since no longer used.
                return true;
            }
            return false;
        }

        /*** Private Methods ***************************************************************************************************/

        /// <summary>
        /// Reset App to various states using parameter enumResetApp.
        /// </summary>
        /// <param name="enumResetApp">Enum used to reset App setup values.</param>
        public void AppReset(EnumResetApp enumResetApp)
        {
            switch (enumResetApp)
            {
                case EnumResetApp.DoNothing:                // Do nothing. Most common so exit quick.
                    break;
                case EnumResetApp.ResetApp:                 // Clear all data store settings.
                    applicationDataContainer.Values.Clear();
                    break;
                case EnumResetApp.ResetPurchaseHistory:     // Clear App purchase history.
                    applicationDataContainer.Values.Remove(ds_BoolAppPurchased);
                    boolAppPurchased = false;
                    break;
                case EnumResetApp.ResetRateHistory:         // Clear App rate history.
                    applicationDataContainer.Values.Remove(ds_BoolAppRated);
                    boolAppRated = false;
                    break;
                case EnumResetApp.ShowDataStoreValues:         // Show data store values via Debug.
                    LibMPC.ListDataStoreItems(applicationDataContainer);
                    break;
                default:    // Throw exception so error can be discovered and corrected.
                    throw new NotSupportedException($"MainPage.AppReset(): enumResetApp={enumResetApp} not found in switch statement.");
            }
        }

        /// <summary>
        /// Back-a-page navigation event handler. Invoked when software or hardware back button is selected, 
        /// or Windows key + Backspace is entered, or say, "Hey Cortana, go back".
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void BackRequestedPage(object sender, BackRequestedEventArgs e)
        {
            _ = sender;     // Discard unused parameter.
            // If event has not already been handled then navigate back to previous page.
            // Next if statement required to prevent App from ending abruptly on a back event.
            if (FrameMP.CanGoBack && e.Handled == false)
            {
                e.Handled = true;
                PageGoBack();
            }
        }

        /// <summary>
        /// Navigate back a page and then set button visibilities accordingly.
        /// </summary>
        private void PageGoBack()
        {
            if (FrameMP.CanGoBack)
                FrameMP.GoBack();
        }

        /*** Page Events *******************************************************************************************************/

        /// <summary>
        /// Size of all MainPage buttons to same size.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            _ = sender;     // Discard unused parameter.
            _ = e;          // Discard unused parameter.
            // Set MainPage public values XAML variables so can be called from library LibMPC.
            mainPageScrollViewer = ScrollViewerMP;
            mainPageButBack = ButBack;
            mainPageButAbout = ButAbout;
            // Back-a-page navigation event handler. Invoked when software or hardware back button is selected, 
            // or Windows key + Backspace is entered, or say, "Hey Cortana, go back".
            SystemNavigationManager.GetForCurrentView().BackRequested += BackRequestedPage;
            // Get App data store location app uses to write or read various settings to or from files.
            // https://msdn.microsoft.com/windows/uwp/app-settings/store-and-retrieve-app-data#local-app-data
            applicationDataContainer = ApplicationData.Current.LocalSettings;
            LibMPC.CustomizeAppTitleBar();
            List<Button> listButtonsThisPage = new List<Button>()
            {
                ButBack,
                ButAbout
            };
            LibMPC.SizePageButtons(listButtonsThisPage);
            // Comment out next two lines before store publish.
            // StorageFolder storageFolderApp = ApplicationData.Current.LocalFolder;
            // Debug.WriteLine($"storageFolderApp.Path={storageFolderApp.Path}");

            // TODO: set next line to EnumResetApp.DoNothing before store publish.
            AppReset(EnumResetApp.DoNothing);   // Reset App to various states using parameter enumResetApp.

            // Get data store values for next two items and set to true or false.
            boolAppPurchased = LibMPC.DataStoreStringToBool(applicationDataContainer, ds_BoolAppPurchased);
            boolAppRated = LibMPC.DataStoreStringToBool(applicationDataContainer, ds_BoolAppRated);
            // AppReset(EnumResetApp.ShowDataStoreValues);     // TODO: Comment out this line before store publish. Show data store values.
            LibMPC.ScrollViewerOff(ScrollViewerMP);         // Turn mainScroller off for now.  Individual pages will set it as required.
            NavigateToPageConversions();
        }

        /// <summary>
        /// Navigate to page 'About'.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ButAbout_Click(object sender, RoutedEventArgs e)
        {
            _ = sender;     // Discard unused parameter.
            _ = e;          // Discard unused parameter.
            FrameMP.Navigate(typeof(About));
        }

        /// <summary>
        /// Navigate back to previous page when back button in title bar is selected.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ButBack_Click(object sender, RoutedEventArgs e)
        {
            _ = sender;     // Discard unused parameter.
            _ = e;          // Discard unused parameter.
            PageGoBack();
        }
    }
}

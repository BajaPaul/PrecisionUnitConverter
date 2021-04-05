using LibraryCoder.MainPageCommon;
using LibraryCoder.UnitConversions;
using System;
using System.Collections.Generic;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace PrecisionUnitConverter.Pages
{
    public sealed partial class About : Page
    {
        /// <summary>
        /// Pointer to MainPage used to call public methods or variables in MainPage.
        /// </summary>
        private readonly MainPage mainPage = MainPage.mainPagePointer;

        public About()
        {
            InitializeComponent();
        }

        /*** Private Methods ***************************************************************************************************/

        /// <summary>
        /// Calculate number of ConversionTypes and total number of unit conversions available in App.
        /// </summary>
        private string CountConversions()
        {
            LibUC.NumberOfConversions(out int numConversionTypes, out int numUnitConversions);
            return $"  Application conversion types = {numConversionTypes}.  Application unit conversions = {numUnitConversions}.";
        }

        /*** Page Events *******************************************************************************************************/

        /// <summary>
        /// Initialize settings for this page and set visibility of title bar items.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            _ = sender;     // Discard unused parameter.
            _ = e;          // Discard unused parameter.
            // Hide XAML layout rectangles by setting to same color as RelativePanel Background;
            RectLayoutCenter.Fill = Rpanel.Background;
            RectLayoutLeft.Fill = Rpanel.Background;
            RectLayoutRight.Fill = Rpanel.Background;
            // Set visibility of titlebar items.
            LibMPC.ButtonVisibility(mainPage.mainPageButAbout, false);
            LibMPC.ButtonVisibility(mainPage.mainPageButBack, true);
            // Set XAML item colors and other settings.
            LibMPC.OutputMsgSuccess(TblkPageTitle, "Application information");
            LibMPC.OutputMsgError(TblkAboutBaseUnit, $"Input option followed by {mainPage.stringBaseUnitIndicator} is base used for conversion.{Environment.NewLine}Input is converted to base.  Base is then converted to output.");
            LibMPC.OutputMsgNormal(TblkAboutProgrammer, $"Installed version is {mainPage.stringAppVersion}{Environment.NewLine}Contact developer using following Email link if you encounter any dead links or other issues with application.{Environment.NewLine}{CountConversions()}");
            LibMPC.OutputMsgSuccess(TblkAboutPayment, "Unlimited trial period for evaluation.  Buy application once and use on all your Windows 10 devices.  Please rate and review application.");
            LibMPC.OutputMsgBright(TblkAboutApp, "Application converts input value to related output value.  Sample conversion is Length.  1 foot converts exactly to 0.3048 meters.  Application uses Decimal Types which provide about twice the precision of Double Types.  Disadvantage of using Decimal Types versus Double Types is they are unable to handle extremely large or extremely small numbers that Double Types can process.  For general everyday use this limitation is not an issue.  As with any computer floating-point type calculation, Decimal Types are subject to small rounding errors in various situations.  Many of these errors are obvious and require 'Smart Rounding' and/or truncation of various ‘Trash Digits' to return mathematically exact value.  There is no simple solution that works for every case.  Simply rounding of a Decimal Type to a Double Type will correct many of these small rounding errors but potentially could lose many good significant digits of precision.  This is an option with this application and is handy for copy-paste operations into other applications that will not accept the precision of Decimal Types.  By default, application expends considerable effort to do 'Smart Rounding' to return mathematically exact result to as many significant digits as possible.  'Smart Rounding' will return the original unrounded result if it cannot complete successfully.  User can toggle output format from 'None', to 'Separator', to 'Scientific', to 'Double', and to 'Double x 10ⁿ' formats.  The desired formatted result can then be copied to allow pasting into other applications.");
            LibMPC.OutputMsgBright(TblkAboutUnits, "Throughout history, many methods of measurements have been used.  This application converts values between three dominant systems referred to as SI, US, and IMP.  The avoirdupois system of units became popular in the 13th century.  England, along with many other countries, used this system for trade.  The United States of America (USA), as a former colony of England, also used the avoirdupois system.  In 1824, the United Kingdom, formerly England, revised its system of measurements to the Imperial System which this application refers to as 'IMP'.  The USA chose not to adopt the Imperial System and retained the avoirdupois system.  In time, the avoirdupois system used by the USA became known as the US Customary Systems of Measurement, which this application refers to as 'US'.  The International System of Units 'SI', also known as the Metric System, came about around 1960.  Since then, 'SI' has become the standard system of measurement used by most of the world.  Unfortunately, around 1980, the USA abandoned their transition to 'SI' units and now uses 'US' or 'SI' units where applicable.");
            LibMPC.OutputMsgError(TblkAboutDisclaim, "DISCLAIMER: User assumes all risk of using calculated values from application.  Developer has done considerable research and testing to validate results are correct.  Many relevant links are provided to User to further research and verify results calculated by application.");
            LibMPC.OutputMsgNormal(TblkAboutLinks, "Explore following links for more information about various measurement systems and related conversions.");
            List<Button> listButtonsThisPage = new List<Button>()
            {
                ButEmail,
                ButRateApp,
                ButConversionOfUnits,
                ButInternationalSystemOfUnits,
                ButUnitedStatesCustomaryUnits,
                ButImperialUnits,
                ButAvoirdupois,
                ButMetricPrefixes,
                ButMetricationUnitedStates,
                ButAppReset
            };
            LibMPC.SizePageButtons(listButtonsThisPage);
            ButAppReset.Foreground = LibMPC.colorError;
            LibMPC.ButtonEmailXboxDisable(ButEmail);
            // Setup scrolling for this page.
            LibMPC.ScrollViewerOn(mainPage.mainPageScrollViewer, horz: ScrollMode.Disabled, vert: ScrollMode.Auto, horzVis: ScrollBarVisibility.Disabled, vertVis: ScrollBarVisibility.Auto, zoom: ZoomMode.Disabled);
            ButEmail.Focus(FocusState.Programmatic);    // Set focus to first button on page.
        }

        /// <summary>
        /// Invoked when user clicks a button requesting link to more information.
        /// </summary>
        /// <param name="sender">A button with a Tag that contains hyperlink string.</param>
        /// <param name="e"></param>
        private async void ButHyperlink_Click(object sender, RoutedEventArgs e)
        {
            _ = e;          // Discard unused parameter.
            await LibMPC.ButtonHyperlinkLaunchAsync((Button)sender);
        }

        /// <summary>
        /// Show User MS Store App rating popup box. Popup box will lock all access to App until closed.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void ButRateApp_Click(object sender, RoutedEventArgs e)
        {
            _ = sender;     // Discard unused parameter.
            _ = e;          // Discard unused parameter.
            await mainPage.RateAppInW10StoreAsync();
        }

        /// <summary>
        /// Revert application to default settings..
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void ButAppReset_Click(object sender, RoutedEventArgs e)
        {
            _ = sender;     // Discard unused parameter.
            _ = e;          // Discard unused parameter.
            if (await LibMPC.ShowPopupBoxAsync("Reset application?", "Click 'Yes' to reset application to default settings.", "Yes", "No"))
            {
                mainPage.AppReset(EnumResetApp.ResetApp);
                Application.Current.Exit();     // Exit App to complete reset.
            }
        }

    }
}

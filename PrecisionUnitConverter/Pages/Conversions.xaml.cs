using LibraryCoder.MainPageCommon;
using LibraryCoder.Numerics;
using LibraryCoder.UnitConversions;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;

namespace PrecisionUnitConverter.Pages
{
    /// <summary>
    /// Class used to set and get conversion values entered by User.
    /// </summary>
    public class ConvertType
    {
        // Class is read/write since contains { get; set; }
        /// <summary>
        /// Enumeration of available conversion types. Samples: EnumConversionsType.Length, EnumConversionsType.Area, EnumConversionsType.Speed...
        /// </summary>
        public EnumConversionsType EnumConversionsType { get; set; }
        /// <summary>
        /// Conversion type description. Sample: "inch". Show conversion DisplayAttribute if exist, otherwise show string value of EnumConversionsType.
        /// </summary>
        public string StringConversionsType { get; set; }
        /// <summary>
        /// Decimal input value.
        /// </summary>
        public decimal DecimalValueInput { get; set; }
        /// <summary>
        /// EnumConversions input. Sample: EnumConversionsLength.inch. Use Enum base class since enum value changes with each EnumConversionsType.
        /// </summary>
        public Enum EnumConversionsInput { get; set; }
        /// <summary>
        /// EnumConversions output. Sample: EnumConversionsLength.foot. Use Enum base class since enum value changes with each EnumConversionsType.
        /// </summary>
        public Enum EnumConversionsOutput { get; set; }
        /// <summary>
        /// Conversion type link to website that provides information about conversion. Default is Empty String. Sample: "https://en.wikipedia.org/wiki/Inch"
        /// </summary>
        public string StringConversionsTypeLink { get; set; }

        // Constructor must have same name as it's class.
        public ConvertType(EnumConversionsType _EnumConversionsType, string _StringConversionsType, decimal _DecimalValueInput, Enum _EnumConversionsInput, Enum _EnumConversionsOutput, string _StringConversionsTypeLink)
        {
            EnumConversionsType = _EnumConversionsType;
            StringConversionsType = _StringConversionsType;
            DecimalValueInput = _DecimalValueInput;
            EnumConversionsInput = _EnumConversionsInput;
            EnumConversionsOutput = _EnumConversionsOutput;
            StringConversionsTypeLink = _StringConversionsTypeLink;
        }

        public override string ToString()
        {
            return $"EnumConversionType={EnumConversionsType}, StringConversionsType={StringConversionsType}, DecimalValueInput={DecimalValueInput}, EnumConversionsInput={EnumConversionsInput}, EnumConversionsOutput={EnumConversionsOutput}, StringConversionsTypeLink={StringConversionsTypeLink}";
        }
    }

    public sealed partial class Conversions : Page
    {
        /// <summary>
        /// Pointer to MainPage used to call public methods or variables in MainPage.
        /// </summary>
        private readonly MainPage mainPage = MainPage.mainPagePointer;

        // Data store 'ds' strings (keys). These are (key, value) pairs. Each key has a matching value.
        /// <summary>
        /// Save last used EnumConversionsType to this key in app data store.  Samples: Angle, Mass, Speed.
        /// </summary>
        private readonly string ds_ConversionTypeLast = "ConversionTypeLast";

        /// <summary>
        /// Input unit stringPostfix added to end of keys saved in app data store. Samples: Length_UnitIn, Angle_UnitIn, Mass_UnitIn.
        /// Current value is "_UnitIn".
        /// </summary>
        private const string stringPostfixUnitIn = "_UnitIn";

        /// <summary>
        /// Output unit stringPostfix added to end of keys saved in app data store. Samples: Length_UnitOut, Angle_UnitOut, Mass_UnitOut.
        /// Current value is "_UnitOut".
        /// </summary>
        private const string stringPostfixUnitOut = "_UnitOut";

        /// <summary>
        /// Input value stringPostfix added to end of keys saved in app data store. Samples: Length_ValueIn, Angle_ValueIn, Mass_ValueIn.
        /// Current value is "_ValueIn".
        /// </summary>
        private const string stringPostfixValueIn = "_ValueIn";

        /// <summary>
        /// Enum indicating which global variable and data store value to update.
        /// </summary>
        private enum EnumChangeType { Setup, DecimalValueInput, EnumConversionsInput, EnumConversionsOutput };

        /// <summary>
        /// Enum that sets output format using these options.
        /// </summary>
        private enum EnumOutputFormat { None, Separator, Scientific, Double, DoubleX10 };

        /// <summary>
        /// Contains a ConvertType item foreach LibUC.ConversionsType item. User input changes are saved here for later retrieval.
        /// This list is built when program starts by ReadDataStoreValues() from values in ConversionType enumeration.
        /// </summary>
        private readonly List<ConvertType> listConvertType = new List<ConvertType>();

        /// <summary>
        /// Get or set if EnumConversionsType changes.
        /// </summary>
        private ConvertType convertType;

        /// <summary>
        /// This list becomes ItemsSource for CboxConvertType ComboBox to show values in EnumConversionsType.
        /// List does not change after initial build.
        /// </summary>
        private readonly List<string> listStringItemsSourceConvertTypes = new List<string>();

        /// <summary>
        /// This list becomes ItemsSource for ComboBoxes CboxConvertInput and CboxConvertOutput to show conversion options.
        /// List will change with each ConversionsType change selected by user.
        /// </summary>
        private readonly List<string> listStringItemsSourceConvertValues = new List<string>();

        /// <summary>
        /// List to copy currently selected conversion list too. This is a common list used in various methods.
        /// </summary>
        private List<LibUCBase> listLibUCBase = new List<LibUCBase>();

        /// <summary>
        /// Modified and sorted list that includes 'symbol' concatenation to end of 'convert' string.  Need separate list since will be called 
        /// mutiple times and need to have original listLibUCBase available to derive values from again.  This list used to lookup
        /// needed conversion information from changes to ComboBoxes CboxConvertInput and CboxConvertOutput.
        /// </summary>
        private readonly List<LibUCBaseRW> listLibUCBaseRWSymbol = new List<LibUCBaseRW>();

        // Current selected input and output conversions.  Used to retrieve values of current conversion in LibUC.
        private LibUCBaseRW libUCBaseRWInput;
        private LibUCBaseRW libUCBaseRWOutput;

        /// <summary>
        /// Conversion input value entered by User in TboxInput TextBox.
        /// </summary>
        private decimal decimalValueInput;

        /// <summary>
        /// Conversion output value for output and later use.
        /// </summary>
        private decimal decimalValueOutput;

        /// <summary>
        /// If true then skip updates to list values first time after a list change to prevent exception while loading.
        /// </summary>
        private bool boolConversionListUpdate;

        /// <summary>
        /// True if overflow error occurs during conversion, false otherwise.
        /// </summary>
        private bool boolErrorOverflow;

        /// <summary>
        /// If true then selectively round value in TboxOutput to correct display of floating-point inaccuracies after a conversion.
        /// If true, ButRoundingToggle Content="Output Rounding On", otherwise Content="Output Rounding Off".
        /// </summary>
        private bool boolButRoundingToggle;

        // Enumeration used to toggle outputed conversion appearance.
        private EnumOutputFormat enumOutputFormat;

        /// <summary>
        /// Value of prefix on output format button. Current value is "Output Format".
        /// </summary>
        private const string stringButFormatToggleContent = "Output Format";

        /// <summary>
        /// Default output string replacing XAML Text in TblkConvertOutput. Values swapped with rounding selection. Current value is "Output".
        /// </summary>
        private const string stringTblkConvertOutputDefault = "Output";

        /// <summary>
        /// Rounded output string replacing XAML Text in TblkConvertOutput. Values swapped with rounding selection. Current value is "Output Rounded".
        /// </summary>
        private const string stringTblkConvertOutputRounded = "Output Rounded";

        /// <summary>
        /// Show User ButRateApp button if this number of page loads since last reset.  Current value is 8.
        /// </summary>
        private readonly int intShowButRateApp = 8;

        public Conversions()
        {
            InitializeComponent();
        }

        /*** Private Methods ***************************************************************************************************/

        /// <summary>
        /// Get purchase status of application. Method controls visibility/Enable of PBarStatus, TblkPurchaseApp, and ButPurchaseApp.
        /// </summary>
        private async Task AppPurchaseCheck()
        {
            if (mainPage.boolAppPurchased)
            {
                // App has been purchased so hide following values and return.
                PBarStatus.Visibility = Visibility.Collapsed;
                TblkPurchaseApp.Visibility = Visibility.Collapsed;
                LibMPC.ButtonVisibility(ButPurchaseApp, false);
            }
            else
            {
                if (mainPage.boolPurchaseCheckCompleted)
                {
                    // App has not been purchased but purchase check done so show previous message. This occurs if User returning from another page.
                    PBarStatus.Visibility = Visibility.Collapsed;
                    LibMPC.OutputMsgError(TblkPurchaseApp, mainPage.stringPurchaseCheckOutput);
                    TblkPurchaseApp.Visibility = Visibility.Visible;
                    LibMPC.ButtonVisibility(ButPurchaseApp, true);
                }
                else
                {
                    // App has not been purchased so do purchase check.
                    LibMPC.OutputMsgBright(TblkPurchaseApp, "Application purchase check in progress...");
                    PBarStatus.Foreground = LibMPC.colorError;          // Set color PBarStatus from default.
                    PBarStatus.Visibility = Visibility.Visible;
                    PBarStatus.IsIndeterminate = true;
                    EnablePageItems(false);
                    mainPage.boolAppPurchased = await LibMPC.AppPurchaseStatusAsync(mainPage.applicationDataContainer, mainPage.ds_BoolAppPurchased);
                    if (mainPage.boolAppPurchased)
                    {
                        LibMPC.OutputMsgSuccess(TblkPurchaseApp, LibMPC.stringAppPurchaseResult);
                        LibMPC.ButtonVisibility(ButPurchaseApp, false);
                    }
                    else
                    {
                        LibMPC.OutputMsgError(TblkPurchaseApp, LibMPC.stringAppPurchaseResult);
                        LibMPC.ButtonVisibility(ButPurchaseApp, true);
                    }
                    PBarStatus.IsIndeterminate = false;
                    PBarStatus.Visibility = Visibility.Collapsed;
                    mainPage.boolPurchaseCheckCompleted = true;
                    mainPage.stringPurchaseCheckOutput = TblkPurchaseApp.Text;
                    EnablePageItems(true);
                }
            }
        }

        /// <summary>
        /// Attempt to buy application. Method controls visibility/Enable of PBarStatus, TblkPurchaseApp, and ButPurchaseApp.
        /// </summary>
        private async Task AppPurchaseBuy()
        {
            LibMPC.OutputMsgBright(TblkPurchaseApp, "Attempting to purchase application...");
            EnablePageItems(false);
            PBarStatus.Foreground = LibMPC.colorError;          // Set color PBarStatus from default.
            PBarStatus.Visibility = Visibility.Visible;
            PBarStatus.IsIndeterminate = true;
            mainPage.boolAppPurchased = await LibMPC.AppPurchaseBuyAsync(mainPage.applicationDataContainer, mainPage.ds_BoolAppPurchased);
            if (mainPage.boolAppPurchased)
            {
                // App purchased.
                LibMPC.OutputMsgSuccess(TblkPurchaseApp, LibMPC.stringAppPurchaseResult);
                LibMPC.ButtonVisibility(ButPurchaseApp, false);
            }
            else
            {
                // App not purchased.
                LibMPC.OutputMsgError(TblkPurchaseApp, LibMPC.stringAppPurchaseResult);
                LibMPC.ButtonVisibility(ButPurchaseApp, true);
            }
            PBarStatus.IsIndeterminate = false;
            PBarStatus.Visibility = Visibility.Collapsed;
            EnablePageItems(true);
        }

        /// <summary>
        /// If application has not been rated then show ButRateApp occasionally.
        /// </summary>
        private void AppRatedCheck()
        {
            if (!mainPage.boolAppRated)
            {
                if (mainPage.applicationDataContainer.Values.ContainsKey(mainPage.ds_IntAppRatedCounter))
                {
                    int intAppRatedCounter = (int)mainPage.applicationDataContainer.Values[mainPage.ds_IntAppRatedCounter];
                    intAppRatedCounter++;
                    if (intAppRatedCounter >= intShowButRateApp)
                    {
                        // Make ButRateApp visible.
                        if (mainPage.boolAppPurchased)
                            ButRateApp.Margin = new Thickness(4, 16, 4, 16);    // Change margin from (4, 0, 4 ,16). Order is left, top, right, bottom.
                        mainPage.applicationDataContainer.Values[mainPage.ds_IntAppRatedCounter] = 0;     // Reset data store setting to 0.
                        ButRateApp.Foreground = LibMPC.colorSuccess;
                        LibMPC.ButtonVisibility(ButRateApp, true);
                    }
                    else
                        mainPage.applicationDataContainer.Values[mainPage.ds_IntAppRatedCounter] = intAppRatedCounter;     // Update data store setting to intAppRatedCounter.
                }
                else
                    mainPage.applicationDataContainer.Values[mainPage.ds_IntAppRatedCounter] = 1;     // Initialize data store setting to 1.
            }
        }

        /// <summary>
        /// Enable items on page if boolEnableItems is true, otherwise disable items on page.
        /// </summary>
        /// <param name="boolEnableItems">If true then enable page items, otherwise disable.</param>
        private void EnablePageItems(bool boolEnableItems)
        {
            LibMPC.ButtonIsEnabled(mainPage.mainPageButAbout, boolEnableItems);
            CboxConvertType.IsEnabled = boolEnableItems;
            CboxConvertInput.IsEnabled = boolEnableItems;
            CboxConvertOutput.IsEnabled = boolEnableItems;
            TboxInput.IsEnabled = boolEnableItems;
            TboxOutput.IsEnabled = boolEnableItems;
            LibMPC.ButtonIsEnabled(ButInvertUnits, boolEnableItems);
            LibMPC.ButtonIsEnabled(ButRoundingToggle, boolEnableItems);
            LibMPC.ButtonIsEnabled(ButFormatToggle, boolEnableItems);
            LibMPC.ButtonIsEnabled(ButAboutConversion, boolEnableItems);
            LibMPC.ButtonIsEnabled(ButAboutOutputUnit, boolEnableItems);
            LibMPC.ButtonIsEnabled(ButPurchaseApp, boolEnableItems);
            LibMPC.ButtonIsEnabled(ButRateApp, boolEnableItems);
        }

        /// <summary>
        /// Read saved app values from data store. If a value does not exist, then create it.
        /// </summary>
        /// <param name="enumConversionsType">Start app with this EnumConversionsType if data store setting ds_ConversionTypeLast does not exist.</param>
        /// <returns></returns>
        private EnumConversionsType ReadDataStoreValues(EnumConversionsType enumConversionsType)
        {
            // Create list of postfixes values.
            List<string> listStringPostfixes = new List<string> { stringPostfixValueIn, stringPostfixUnitIn, stringPostfixUnitOut };
            // Copy EnumConversionsType values to array.
            Array arrayConversionsType = Enum.GetValues(typeof(EnumConversionsType));
            string stringConversionsTypeLink;
            decimal decimalValueInput = 1m;
            Enum enumConversionsInput = null;
            Enum enumConversionsOutput = null;
            // Following foreach loop gets or sets a data store value for every conversion.
            foreach (EnumConversionsType enumConversionsTypeFound in arrayConversionsType)
            {
                // Get list of EnumConversions from conversionsTypeFound.
                List<LibUCBase> listLibUCBaseFound = LibUC.GetListOfConversions(enumConversionsTypeFound);
                string stringConversionsType;
                string stringKeyValue;
                string stringStorevalue;
                foreach (string stringPostfix in listStringPostfixes)
                {
                    stringKeyValue = $"{enumConversionsTypeFound}{stringPostfix}";
                    if (mainPage.applicationDataContainer.Values.ContainsKey(stringKeyValue))
                    {
                        // Found matching setting in App data store, so read it in.
                        stringStorevalue = mainPage.applicationDataContainer.Values[stringKeyValue].ToString();
                        // Debug.WriteLine($"ReadDataStoreValues(): Found setting stringKeyValue={stringKeyValue}, stringStorevalue={stringStorevalue}");
                        bool boolMatchFound;
                        switch (stringPostfix)
                        {
                            case stringPostfixValueIn:
                                decimalValueInput = Convert.ToDecimal(stringStorevalue);
                                break;
                            case stringPostfixUnitIn:
                                boolMatchFound = false;
                                foreach (LibUCBase libUCBaseIn in listLibUCBaseFound)
                                {
                                    if (stringStorevalue.Equals(libUCBaseIn.EnumConversions.ToString()))
                                    {
                                        boolMatchFound = true;
                                        enumConversionsInput = libUCBaseIn.EnumConversions;
                                        // Debug.WriteLine($"ReadDataStoreValues(): enumConversionsInput={enumConversionsInput}");
                                        break;
                                    }
                                }
                                if (!boolMatchFound)
                                {
                                    // Throw exception so error can be discovered and corrected.
                                    throw new ArgumentOutOfRangeException($"Conversions.ReadDataStoreValues(): stringPostfixUnitIn={stringPostfixUnitIn} not found in listLibUCBaseFound.");
                                }
                                break;
                            case stringPostfixUnitOut:
                                boolMatchFound = false;
                                foreach (LibUCBase libUCBaseOut in listLibUCBaseFound)
                                {
                                    if (stringStorevalue.Equals(libUCBaseOut.EnumConversions.ToString()))
                                    {
                                        boolMatchFound = true;
                                        enumConversionsOutput = libUCBaseOut.EnumConversions;
                                        // Debug.WriteLine($"ReadDataStoreValues(): enumConversionsOutput={enumConversionsOutput}");
                                        break;
                                    }
                                }
                                if (!boolMatchFound)
                                {
                                    // Throw exception so error can be discovered and corrected.
                                    throw new ArgumentOutOfRangeException($"Conversions.ReadDataStoreValues(): stringPostfixUnitOut={stringPostfixUnitOut} not found in listLibUCBaseFound.");
                                }
                                break;
                            default:    // Throw exception so error can be discovered and corrected.
                                throw new NotSupportedException($"Conversions.ReadDataStoreValues(): stringPostfix={stringPostfix} not found in switch statement.");
                        }
                    }
                    else    // Did not find key in App data store so create it.
                    {
                        switch (stringPostfix)
                        {
                            case stringPostfixValueIn:
                                mainPage.applicationDataContainer.Values[stringKeyValue] = "1";     // Write default value of "1" to data store.
                                decimalValueInput = 1m;
                                break;
                            case stringPostfixUnitIn:
                                // Get base unit from conversionsTypeFound and then return corresponding EnumConversions.
                                // Sample: conversionsTypeFound=Area returns Area base unit Enum value EnumConversionsArea.meter_squared
                                enumConversionsInput = LibUC.GetConversionsTypeBaseUnit(enumConversionsTypeFound).EnumConversions;
                                // Save EnumConversions value to data store.
                                mainPage.applicationDataContainer.Values[stringKeyValue] = enumConversionsInput.ToString();
                                break;
                            case stringPostfixUnitOut:
                                enumConversionsOutput = LibUC.GetConversionsTypeBaseUnit(enumConversionsTypeFound).EnumConversions;
                                mainPage.applicationDataContainer.Values[stringKeyValue] = enumConversionsOutput.ToString();
                                break;
                            default:    // Throw exception so error can be discovered and corrected.
                                throw new NotSupportedException($"Conversions.ReadDataStoreValues(): stringPostfix={stringPostfix} case not found in switch statement.");
                        }
                    }
                }
                // Use DisplayAttribute if exist, otherwise use enumeration value.  Sample: [Display(Description = "Solid Angle")] SolidAngle returns "Solid Angle".
                stringConversionsType = LibUC.GetEnumString(enumConversionsTypeFound);
                // Build ItemsSource list for CboxConvertType ComboBox.
                listStringItemsSourceConvertTypes.Add(stringConversionsType);
                stringConversionsTypeLink = LibUC.GetConversionsTypeLink(enumConversionsTypeFound);    // Get ConversionType hyperlink.
                // Build list listConvertType.  Save user input value changes to this list so they can be retrieved when needed.
                listConvertType.Add(new ConvertType(enumConversionsTypeFound, stringConversionsType, decimalValueInput, enumConversionsInput, enumConversionsOutput, stringConversionsTypeLink));
            }
            object objectStoreValue;
            // Read or create ds_ConversionTypeLast setting.
            if (mainPage.applicationDataContainer.Values.ContainsKey(ds_ConversionTypeLast))
            {
                // Found setting so read it in.
                objectStoreValue = mainPage.applicationDataContainer.Values[ds_ConversionTypeLast];
            }
            else    // Did not find setting so create it.
            {
                objectStoreValue = enumConversionsType.ToString();
                mainPage.applicationDataContainer.Values[ds_ConversionTypeLast] = enumConversionsType.ToString();;
            }
            return LibUC.GetEnumConversionsTypeFromString(objectStoreValue.ToString());
        }

        /// <summary>
        /// After a ConversionsType change, retrieve saved values from convertType, which points to single ConvertType 
        /// in listConvertType. Then use the values to setup ComboBox menus and initialize them for use.
        /// </summary>
        private void SetupConversion()
        {
            listStringItemsSourceConvertValues.Clear();           // Clear List of existing items.
            listStringItemsSourceConvertValues.TrimExcess();      // Set list size to zero.
            listLibUCBaseRWSymbol.Clear();
            listLibUCBaseRWSymbol.TrimExcess();
            bool boolBaseUnit = true;   // If true, then add mainPage.stringBaseUnitIndicator to end of StringDescription to indicate unit is the base unit. First item in listLibUCBase is the base unit!
            foreach (LibUCBase libUCBase in listLibUCBase)
            {
                // Debug.WriteLine($"SetupConversion(): libUCBase={libUCBase}");
                // Copy LibUCBase readonly items to LibUCBaseRW equivalents.
                LibUCBaseRW libUCBaseRW = new LibUCBaseRW(libUCBase.EnumConversions, libUCBase.StringDescription, libUCBase.StringSymbol, libUCBase.DecimalBase, libUCBase.StringHyperlink);
                // Now update StringDescription to include symbol and base unit indicator if applicable.
                if (!string.IsNullOrEmpty(libUCBaseRW.StringSymbol))
                    {
                    if (boolBaseUnit)
                    {
                        // Add symbol and base unit indicator to end of description.
                        libUCBaseRW.StringDescription = $"{libUCBaseRW.StringDescription} ({libUCBaseRW.StringSymbol}) {mainPage.stringBaseUnitIndicator}";
                    }
                    else
                    {
                        // Add symbol to end of description.
                        libUCBaseRW.StringDescription = $"{libUCBaseRW.StringDescription} ({libUCBaseRW.StringSymbol})";
                    }
                }
                else
                {
                    if (boolBaseUnit)
                    {
                        // Add base unit indicator to end of description.
                        libUCBaseRW.StringDescription = $"{libUCBaseRW.StringDescription} {mainPage.stringBaseUnitIndicator}";
                    }
                }
                // Debug.WriteLine($"SetupConversion(): libUCBaseRW.StringDescription={libUCBaseRW.StringDescription}");
                listStringItemsSourceConvertValues.Add(libUCBaseRW.StringDescription);   // Becomes ItemsSource for CboxConvertInput and CboxConvertOutput ComboBoxes.
                listLibUCBaseRWSymbol.Add(libUCBaseRW);   // List to compare user selections from CboxConvertInput and CboxConvertOutput ComboBoxes too.
                boolBaseUnit = false;
            }
            listStringItemsSourceConvertValues.Sort();     // Sort the ItemsSource list alphabetically.
            listLibUCBaseRWSymbol.Sort(delegate (LibUCBaseRW x, LibUCBaseRW y) { return x.StringDescription.CompareTo(y.StringDescription); });  // Sort list using StringDescription field.
            int intIndexInput = 0;     // Initialize list index of input after sorted.
            int intIndexOutput = 0;
            foreach (LibUCBaseRW libUCBaseRWIn in listLibUCBaseRWSymbol)    // Search new list for matching input EnumConversions.
            {
                // Debug.WriteLine($"SetupConversion(): convertType.EnumConversionsInput={convertType.EnumConversionsInput}, libUCBaseRWIn.EnumConversions={libUCBaseRWIn.EnumConversions}");
                if (convertType.EnumConversionsInput.Equals(libUCBaseRWIn.EnumConversions))  // Retrieve saved SaveEnumInput.
                {
                    libUCBaseRWInput = libUCBaseRWIn;        // Set to match input selection then exit.
                    intIndexInput = listLibUCBaseRWSymbol.IndexOf(libUCBaseRWIn);
                    break;
                }
            }
            foreach (LibUCBaseRW libUCBaseRWOut in listLibUCBaseRWSymbol)      // Search new list for matching output EnumConversions.
            {
                // Debug.WriteLine($"SetupConversion(): convertType.EnumConversionsOutput={convertType.EnumConversionsOutput}, libUCBaseRWOut.EnumConversions={libUCBaseRWOut.EnumConversions}");
                if (convertType.EnumConversionsOutput.Equals(libUCBaseRWOut.EnumConversions)) // Retrieve saved SaveEnumOutput.
                {
                    libUCBaseRWOutput = libUCBaseRWOut;        // Set to match output selection then exit.
                    intIndexOutput = listLibUCBaseRWSymbol.IndexOf(libUCBaseRWOut);
                    break;
                }
            }
            // Debug.WriteLine($"SetupConversion(): intIndexInput={intIndexInput}, Input Menu={listLibUCBaseRWSymbol[intIndexInput].StringDescription}, intIndexOutput={intIndexOutput}, Output Menu={listLibUCBaseRWSymbol[intIndexOutput].StringDescription}");
            SetButtonTagToHyperlink(ButAboutOutputUnit, libUCBaseRWOutput.StringHyperlink);     //  If available, show hyperlink button to more information about output unit type.
            // Load new list listStringItemsSourceConvertValues. Skip updates on list change in CboxConvertInput_SelectionChanged() and CboxConvertOutput_SelectionChanged().
            boolConversionListUpdate = true;
            CboxConvertInput.ItemsSource = null;
            CboxConvertInput.ItemsSource = listStringItemsSourceConvertValues;     // Update Input list of conversions.  Need to set to null first, or values in ComboBox will not update until selected.
            CboxConvertInput.SelectedIndex = intIndexInput;     // This causes a SelectionChanged event in CboxConvertInput_SelectionChanged() method.
            CboxConvertOutput.ItemsSource = null;
            CboxConvertOutput.ItemsSource = listStringItemsSourceConvertValues;
            CboxConvertOutput.SelectedIndex = intIndexOutput;
            boolConversionListUpdate = false;
            decimalValueInput = convertType.DecimalValueInput;  // Retrieve saved DecimalValueInput.
            TboxInput.Text = decimalValueInput.ToString(LibNum.fpNumericFormatNone);
            ConvertValues(EnumChangeType.Setup);    // Complete conversion using decimalValueInput, libUCBaseRWInput, and libUCBaseRWOutput values.
        }

        /// <summary>
        /// Handler for overflow exceptions. If parameter is true then show overflow error messages, otherwise reset values to default.
        /// </summary>
        /// <param name="boolOverflowOccurred">True if overflow occurred, false otherwise.</param>
        private void OverflowError(bool boolOverflowOccurred)
        {
            // Default values in this method that do not change are set in IntializePage().
            if (boolOverflowOccurred)
            {
                boolErrorOverflow = true;
                TboxOutput.Foreground = LibMPC.colorError;      // Set color to error color.
                TboxOutput.Text = "Overflow Error";
                BdrError.Visibility = Visibility.Visible;
                TblkError.Visibility = Visibility.Visible;
            }
            else
            {
                boolErrorOverflow = false;
                TboxOutput.Foreground = LibMPC.colorBright;     // Reset color to default.
                BdrError.Visibility = Visibility.Collapsed;       // Make error TextBlock Border hidden.
                TblkError.Visibility = Visibility.Collapsed;      // Make error TextBlock hidden.
            }
        }

        /// <summary>
        /// Complete the conversion, format result, and update changed data store value.
        /// </summary>
        /// <param name="enumChangeType">Enum indicating which global variable and data store value to update.</param>
        private void ConvertValues(EnumChangeType enumChangeType)
        {
            OverflowError(false);
            try
            {
                // Debug.WriteLine($"ConvertValues(): decimalValueInput={decimalValueInput}, libUCBaseRWInput.EnumConversions={libUCBaseRWInput.EnumConversions}, libUCBaseRWOutput.EnumConversions={libUCBaseRWOutput.EnumConversions}");
                decimalValueOutput = LibUC.ConvertValue(decimalValueInput, libUCBaseRWInput.EnumConversions, libUCBaseRWOutput.EnumConversions);
                // Debug.WriteLine($"ConvertValues(): decimalValueOutput={decimalValueOutput}");
            }
            catch (OverflowException)
            {
                // Debug.WriteLine($"ConvertValues(): Caught overflow exception.");
                OverflowError(true);
                throw;
            }
            ShowConversion();
            UpdateValues(enumChangeType);
        }

        /// <summary>
        /// Update global variables and data store values depending on value of parameter enumChangeType.
        /// This saves current values so if User comes back to this conversionType, current values can be restored.
        /// </summary>
        /// <param name="enumChangeType">Enum indicating which global variable and data store value to update.</param>
        private void UpdateValues(EnumChangeType enumChangeType)
        {
            string stringDataStore;
            switch (enumChangeType)
            {
                case EnumChangeType.Setup:
                    // Do nothing here since on ConversionType change, saved values in convertType are retrieved and used.
                    // Nothing changes so updates to convertType.DecimalValueInput, convertType.EnumConversionsInput, convertType.EnumConversionsOutput
                    // and corresponding data store values not needed.
                    break;
                case EnumChangeType.DecimalValueInput:
                    convertType.DecimalValueInput = decimalValueInput;    // Update convertType.DecimalValueInput.
                    stringDataStore = $"{convertType.EnumConversionsType}{stringPostfixValueIn}";
                    // Debug.WriteLine($"UpdateValues(): enumChangeType={enumChangeType}, stringDataStore={stringDataStore}, convertType.DecimalValueInput={convertType.DecimalValueInput}");
                    mainPage.applicationDataContainer.Values[stringDataStore] = convertType.DecimalValueInput.ToString();
                    break;
                case EnumChangeType.EnumConversionsInput:
                    convertType.EnumConversionsInput = libUCBaseRWInput.EnumConversions;    // Update convertType.EnumConversionsInput.
                    stringDataStore = $"{convertType.EnumConversionsType}{stringPostfixUnitIn}";
                    // Debug.WriteLine($"UpdateValues(): enumChangeType={enumChangeType}, stringDataStore={stringDataStore}, convertType.EnumConversionsInput={convertType.EnumConversionsInput}");
                    mainPage.applicationDataContainer.Values[stringDataStore] = convertType.EnumConversionsInput.ToString();
                    break;
                case EnumChangeType.EnumConversionsOutput:
                    convertType.EnumConversionsOutput = libUCBaseRWOutput.EnumConversions;  // Update convertType.EnumConversionsOutput.
                    stringDataStore = $"{convertType.EnumConversionsType}{stringPostfixUnitOut}";
                    // Debug.WriteLine($"UpdateValues(): enumChangeType={enumChangeType}, stringDataStore={stringDataStore}, convertType.EnumConversionsOutput={convertType.EnumConversionsOutput}");
                    mainPage.applicationDataContainer.Values[stringDataStore] = convertType.EnumConversionsOutput.ToString();
                    break;
                default:    // Throw exception so error can be discovered and corrected.
                    throw new NotSupportedException($"Conversions.UpdateValues((): enumChangeType={enumChangeType} not found in switch statement.");
            }
        }

        // More on string formatting: https://msdn.microsoft.com/en-us/library/26etazsy(v=vs.110).aspx
        /// <summary>
        /// Show calculated conversion in TboxOutput TextBlock either formatted or raw depending on outputFormatted value.
        /// </summary>
        private void ShowConversion()
        {
            // This method called from mutiple locations so need to check boolErrorOverflow value before proceeding.
            if (boolErrorOverflow)
            {
                OverflowError(true);
                // Debug.WriteLine($"ShowConversion(): boolErrorOverflow={boolErrorOverflow} so skipped rest of method");
                return;
            }
            decimal decimalValueRounded;
            switch (enumOutputFormat)
            {
                case EnumOutputFormat.None:
                    // Display raw ouput with no formatting.
                    if (boolButRoundingToggle)   // Attempt to round decimalValueOutput if rounding button is On.
                    {
                        decimalValueRounded = LibNum.RoundOutput(decimalValueOutput);
                        TboxOutput.Text = decimalValueRounded.ToString(LibNum.fpNumericFormatNone);
                        TblkConvertOutputUpdateText(decimalValueRounded);     // Format TextBlock above output TextBox.
                    }
                    else
                    {
                        TboxOutput.Text = decimalValueOutput.ToString(LibNum.fpNumericFormatNone);
                    }
                    break;
                case EnumOutputFormat.Separator:
                    // Display Separator ouput.
                    if (boolButRoundingToggle)   // Attempt to round decimalValueOutput if rounding button is On.
                    {
                        decimalValueRounded = LibNum.RoundOutput(decimalValueOutput);
                        TboxOutput.Text = decimalValueRounded.ToString(LibNum.fpNumericFormatSeparator);
                        TblkConvertOutputUpdateText(decimalValueRounded);     // Format TextBlock above output TextBox.
                    }
                    else
                    {
                        TboxOutput.Text = decimalValueOutput.ToString(LibNum.fpNumericFormatSeparator);
                    }
                    break;
                case EnumOutputFormat.Scientific:
                    // Display Scientific ouput.
                    if (boolButRoundingToggle)   // Attempt to round decimalValueOutput if rounding button is On.
                    {
                        decimalValueRounded = LibNum.RoundOutput(decimalValueOutput);
                        TboxOutput.Text = decimalValueRounded.ToString(LibNum.fpNumericFormatScientific);
                        TblkConvertOutputUpdateText(decimalValueRounded);     // Format TextBlock above output TextBox.
                    }
                    else
                    {
                        TboxOutput.Text = decimalValueOutput.ToString(LibNum.fpNumericFormatScientific);
                    }
                    break;
                case EnumOutputFormat.Double:
                    // Display Double output.  Can be used to copy and paste into other applications that do not support decimals.
                    // Do not need to use RoundOutput() here since cast-to-double already does it.
                    TboxOutput.Text = Convert.ToDouble(decimalValueOutput).ToString(LibNum.fpNumericFormatNone);
                    TblkConvertOutputUpdateText(decimalValueOutput);    // Format TextBlock above output TextBox.
                    break;
                case EnumOutputFormat.DoubleX10:
                    // Display FormatAsPowerOfTen() output.  Convert value to double and format as 1.23456x10² versus 1.12346E+2.
                    // Do not need to use RoundOutput() here since cast-to-double already does it.
                    TboxOutput.Text = LibNum.FormatAsPowerOfTen((double)decimalValueOutput, 15, EnumRemoveTrailingZeros.Yes);
                    TblkConvertOutputUpdateText(decimalValueOutput);    // Format TextBlock above output TextBox.
                    break;
                default:    // Throw exception so error can be discovered and corrected.
                    throw new NotSupportedException($"Conversions.ShowConversion(): enumOutputFormat={enumOutputFormat} not found in switch statement.");
            }
        }

        /// <summary>
        /// Format TblkConvertOutput to show text and color changes when decimalValueOutput is rounded or not.
        /// </summary>
        /// <param name="outputValueCompare">Value to compare to decimalValueOutput. If same then no rounding, otherwise value was rounded.</param>
        private void TblkConvertOutputUpdateText(decimal outputValueCompare)
        {
            if (decimalValueOutput.CompareTo(outputValueCompare) == 0)     // Rounding was not done since compare values are equal.
            {
                LibMPC.OutputMsgNormal(TblkConvertOutput, stringTblkConvertOutputDefault);
                // Debug.WriteLine($"TblkConvertOutputUpdateText(): {decimalValueOutput}=={outputValueCompare}, Rounding was not done since compare values are equal");
            }
            else    // Rounding was done since compare values not equal.
            {
                LibMPC.OutputMsgSuccess(TblkConvertOutput, stringTblkConvertOutputRounded);
                // Debug.WriteLine($"TblkConvertOutputUpdateText(): {decimalValueOutput}!={outputValueCompare}, Rounding was done since compare values not equal");
            }
        }

        /// <summary>
        /// Set button tag to string containing hyperlink. Enable button if hyperlink string not empty, otherwise disable button.
        /// Presently used to enable or disable buttons ButAboutConversion and ButAboutOutputUnit.
        /// </summary>
        /// <param name="button">Button that opens hyperlink in web browser.</param>
        /// <param name="stringHyperlink">Hyperlink string to assign button tag.</param>
        private void SetButtonTagToHyperlink(Button button, string stringHyperlink)
        {
            if (string.IsNullOrEmpty(stringHyperlink))
            {
                button.Tag = string.Empty;
                LibMPC.ButtonIsEnabled(button, false);
            }
            else
            {
                button.Tag = stringHyperlink;
                LibMPC.ButtonIsEnabled(button, true);
            }
        }

        /*** Page Events *******************************************************************************************************/

        /// <summary>
        /// Initialize settings for this page and set visibility of title bar items.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void Page_Loaded(object sender, RoutedEventArgs e)
        {
            _ = sender;     // Discard unused parameter.
            _ = e;          // Discard unused parameter.
            // Hide XAML layout rectangles by setting to same color as RelativePanel Background;
            RectLayoutCenter.Fill = Rpanel.Background;
            RectLayoutLeft.Fill = Rpanel.Background;
            RectLayoutRight.Fill = Rpanel.Background;
            // Set visibility of titlebar items.
            LibMPC.ButtonVisibility(mainPage.mainPageButBack, false);
            LibMPC.ButtonVisibility(mainPage.mainPageButAbout, true);
            LibMPC.ButtonVisibility(ButPurchaseApp, false);
            LibMPC.ButtonVisibility(ButRateApp, false);
            // Note: TextBox and button sizes for this page set via XAML code using static resources.
            // Set colors of XAML items.
            TblkPageTitle.Foreground = LibMPC.colorSuccess;
            TblkConvertInput.Foreground = LibMPC.colorNormal;
            TblkConvertOutput.Foreground = LibMPC.colorNormal;
            // Set default overflow error values here.
            BdrError.BorderBrush = LibMPC.colorError;
            BdrError.Background = TboxOutput.Background;    // Set border background to match background of other XAML items on page.
            TblkError.Foreground = LibMPC.colorError;
            TblkError.Text = "Converted value exceeds decimal limit";
            OverflowError(false);
            // Read saved app values from data store.  If value does not exist, then create it.
            // Set EnumConversionsType.Speed for App first-run.
            // Also builds lists listStringItemsSourceConvertTypes and listConvertType.
            int initialConversionType = (int)ReadDataStoreValues(EnumConversionsType.Speed);
            // Change Text to defaults set above.
            TblkConvertOutput.Text = stringTblkConvertOutputDefault;
            ButFormatToggle.Content = $"{stringButFormatToggleContent} {EnumOutputFormat.None}";   // Change content of output format button.
            CboxConvertType.ItemsSource = listStringItemsSourceConvertTypes;    // Built by ReadDataStoreValues().
            convertType = listConvertType[initialConversionType];               // Built by ReadDataStoreValues().
            CboxConvertType.SelectedIndex = initialConversionType;              // This causes SelectionChanged event which initializes everything.
            // TODO: Comment out next line before publishing App.
            // LibUC.TestMethodsMain();    // Sample method that tests methods in LibUC are working properly.
            await AppPurchaseCheck();
            AppRatedCheck();
            // Setup mainScroller to handle scrolling for this page.
            LibMPC.ScrollViewerOn(mainPage.mainPageScrollViewer, horz: ScrollMode.Disabled, vert: ScrollMode.Auto, horzVis: ScrollBarVisibility.Disabled, vertVis: ScrollBarVisibility.Auto, zoom: ZoomMode.Disabled);
            CboxConvertType.Focus(FocusState.Programmatic);
        }

        /// <summary>
        /// User selected different conversion type from CboxConvertType so search list for match and then set variables accordingly.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void CboxConvertType_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            _ = e;          // Discard unused parameter.
            boolButRoundingToggle = false;   // Turn button back on with type changes. Set to false since next line will toggle value to true.
            ButRoundingToggle_Click(null, null);
            string stringSelection = ((ComboBox)sender).SelectedItem.ToString();    // Get selected item from ComboBox.
            bool boolSelectionFound = false;
            foreach (ConvertType convertTypeItem in listConvertType)
            {
                // Debug.WriteLine($"CboxConvertType_SelectionChanged(): Searching for match, stringSelection={stringSelection}, convertTypeItem.StringConversionsType={convertTypeItem.StringConversionsType}");
                if (stringSelection.Equals(convertTypeItem.StringConversionsType))
                {
                    convertType = convertTypeItem;     // Match found so update convertType to value User selected from ComboBox.
                    SetButtonTagToHyperlink(ButAboutConversion, convertType.StringConversionsTypeLink);   // If available, show hyperlink button to more information about 'ConversionsType'.
                    mainPage.applicationDataContainer.Values[ds_ConversionTypeLast] = convertType.EnumConversionsType.ToString();  // Save new 'ConversionType' selection to data store.
                    boolSelectionFound = true;
                    // Debug.WriteLine($"CboxConvertType_SelectionChanged(): Match found, convertType={convertType}, boolSelectionFound={boolSelectionFound}");
                    break;
                }
            }
            if (!boolSelectionFound)
                throw new ArgumentOutOfRangeException($"Conversions.CboxConvertType_SelectionChanged(): stringSelection={stringSelection} not found in listConvertType.");
            listLibUCBase = LibUC.GetListOfConversions(convertType.EnumConversionsType);     // Get new list of conversions to match 'ConversionType' selection.
            SetupConversion();
        }

        /// <summary>
        /// User selected different input conversion from CboxConvertInput so search list for match and then set variables accordingly.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void CboxConvertInput_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            _ = e;          // Discard unused parameter.
            if (!boolConversionListUpdate)         // Skip updates on list changes or an exception will be thrown.
            {
                string stringSelection = ((ComboBox)sender).SelectedItem.ToString();    // Get selected item from ComboBox.
                bool boolSelectionFound = false;
                foreach (LibUCBaseRW libUCBaseRW in listLibUCBaseRWSymbol)
                {
                    // Debug.WriteLine($"CboxConvertInput_SelectionChanged(): Searching for match, stringSelection={stringSelection}, libUCBaseRW.StringDescription={libUCBaseRW.StringDescription}");
                    if (stringSelection.Equals(libUCBaseRW.StringDescription))  // Found match.
                    {
                        libUCBaseRWInput = libUCBaseRW;         // Match found so update libUCBaseRWInput to value User selected from ComboBox.
                        ConvertValues(EnumChangeType.EnumConversionsInput);   // Input type changed, complete conversion and update saved values.
                        boolSelectionFound = true;
                        // Debug.WriteLine($"CboxConvertInput_SelectionChanged(): Match found, libUCBaseRWInput={libUCBaseRWInput}, boolSelectionFound={boolSelectionFound}");
                        break;
                    }
                }
                if (!boolSelectionFound)
                    throw new ArgumentOutOfRangeException($"Conversions.CboxConvertInput_SelectionChanged(): stringSelection={stringSelection} not found in listLibUCBaseRWSymbol.");
            }
        }

        /// <summary>
        /// User selected different input conversion from CboxConvertOutput so search list for match and then set variables accordingly.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void CboxConvertOutput_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            _ = e;          // Discard unused parameter.
            if (!boolConversionListUpdate)         // Skip updates on list changes or exception is thrown.
            {
                string stringSelection = ((ComboBox)sender).SelectedItem.ToString();    // Get selected item from ComboBox.
                bool boolSelectionFound = false;
                foreach (LibUCBaseRW libUCBaseRW in listLibUCBaseRWSymbol)
                {
                    // Debug.WriteLine($"CboxConvertOutput_SelectionChanged(): Searching for match, stringSelection={stringSelection}, libUCBaseRW.StringDescription={libUCBaseRW.StringDescription}");
                    if (stringSelection.Equals(libUCBaseRW.StringDescription))  // Found match.
                    {
                        libUCBaseRWOutput = libUCBaseRW;   // Match found so update libUCBaseRWOutput to value User selected from ComboBox.
                        SetButtonTagToHyperlink(ButAboutOutputUnit, libUCBaseRWOutput.StringHyperlink);     // If available, show hyperlink button to more information about output unit type.
                        ConvertValues(EnumChangeType.EnumConversionsOutput);      // Output type changed, complete conversion and update saved values.
                        boolSelectionFound = true;
                        // Debug.WriteLine($"CboxConvertOutput_SelectionChanged(): Match found, libUCBaseRWOutput={libUCBaseRWOutput}, boolSelectionFound={boolSelectionFound}");
                        break;
                    }
                }
                if (!boolSelectionFound)
                    throw new ArgumentOutOfRangeException($"Conversions.CboxConvertOutput_SelectionChanged(): stringSelection={stringSelection} not found in listLibUCBaseRWSymbol.");
            }
        }

        /// <summary>
        /// Invoked when user presses the 'Output Toggle' button.  Toggle the output conversion between various formats.
        /// Order is None, Separator, Scientific, Double, DoubleX10, then back to None.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ButOutputToggle_Click(object sender, RoutedEventArgs e)
        {
            _ = sender;     // Discard unused parameter.
            _ = e;          // Discard unused parameter.
            switch (enumOutputFormat)
            {
                case EnumOutputFormat.None:
                    enumOutputFormat = EnumOutputFormat.Separator;
                    ButFormatToggle.Content = $"{stringButFormatToggleContent} {EnumOutputFormat.Separator}";       // Must match line above
                    break;
                case EnumOutputFormat.Separator:
                    enumOutputFormat = EnumOutputFormat.Scientific;
                    ButFormatToggle.Content = $"{stringButFormatToggleContent} {EnumOutputFormat.Scientific}";      // Must match line above
                    break;
                case EnumOutputFormat.Scientific:
                    enumOutputFormat = EnumOutputFormat.Double;
                    ButFormatToggle.Content = $"{stringButFormatToggleContent} {EnumOutputFormat.Double}";          // Must match line above
                    break;
                case EnumOutputFormat.Double:
                    enumOutputFormat = EnumOutputFormat.DoubleX10;
                    // "Double × 10ⁿ", Special case, display this instead of enumeration text.
                    ButFormatToggle.Content = $"{stringButFormatToggleContent} Double {LibNum.expCharTimes} 10{LibNum.expCharN}";
                    break;
                case EnumOutputFormat.DoubleX10:
                    enumOutputFormat = EnumOutputFormat.None;
                    ButFormatToggle.Content = $"{stringButFormatToggleContent} {EnumOutputFormat.None}";            // Must match line above
                    break;
                default:    // Throw exception so error can be discovered and corrected.
                    throw new NotSupportedException($"Conversions.ButOutputToggle_Click(): enumOutputFormat={enumOutputFormat} not found in switch statement.");
            }
            ShowConversion();   // Update the output shown to user.
        }

        /// <summary>
        /// Invoked when user clicks the 'Invert Units' button.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ButInvertUnits_Click(object sender, RoutedEventArgs e)
        {
            _ = sender;     // Discard unused parameter.
            _ = e;          // Discard unused parameter.
            // Note: decimalValueInput does not change when inverting units.
            LibUCBaseRW libUCBaseRWInvert = libUCBaseRWInput;    // Invert current conversions.
            libUCBaseRWInput = libUCBaseRWOutput;
            libUCBaseRWOutput = libUCBaseRWInvert;
            UpdateValues(EnumChangeType.EnumConversionsInput);
            UpdateValues(EnumChangeType.EnumConversionsOutput);
            SetupConversion();             // Setup menus again using inverted values.
        }

        /// <summary>
        /// Invoked when user clicks 'ButAboutConversion' or 'ButAboutOutputUnit' requesting link to more information.
        /// </summary>
        /// <param name="sender">A button with a Tag that contains hyperlink string.</param>
        /// <param name="e"></param>
        private async void ButHyperlink_Click(object sender, RoutedEventArgs e)
        {
            _ = e;          // Discard unused parameter.
            await LibMPC.ButtonHyperlinkLaunchAsync((Button)sender);
        }

        /// <summary>
        /// Invoked when user clicks 'Output Rounding' button.  Toggle selective output rounding on and off.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ButRoundingToggle_Click(object sender, RoutedEventArgs e)
        {
            _ = sender;     // Discard unused parameter.
            _ = e;          // Discard unused parameter.
            if (boolButRoundingToggle)
            {
                boolButRoundingToggle = false;
                ButRoundingToggle.Content = "Output Rounding Off";
                TblkConvertOutputUpdateText(decimalValueOutput);      // Format TextBlock above output TextBox.
            }
            else
            {
                boolButRoundingToggle = true;
                ButRoundingToggle.Content = "Output Rounding On";
            }
            // Rounding is not done when enumOutputFormat equals EnumOutputFormat.Double or EnumOutputFormat.DoubleX10 since cast-to-double already does it.
            if (enumOutputFormat.Equals(EnumOutputFormat.None) || enumOutputFormat.Equals(EnumOutputFormat.Separator) || enumOutputFormat.Equals(EnumOutputFormat.Scientific))
                ShowConversion();   // Update TboxOutput.Text to show default output or rounded output as set above.
        }

        // Next three TboxInput events check that character entered by User is valid entry and responds accordingly.

        /// <summary>
        /// Do conversion when User presses Enter key while in TboxInput.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void TboxInput_KeyDown(object sender, KeyRoutedEventArgs e)
        {
            _ = sender;     // Discard unused parameter.
            if (e.Key == Windows.System.VirtualKey.Enter)           // Check if 'Enter' key was pressed.  Ignore everything else.
            {
                decimalValueInput = LibNum.TextBoxGetDecimal(TboxInput, EnumTextBoxUpdate.Yes);  // Get the input from the TextBox and convert to matching numeric.
                ConvertValues(EnumChangeType.DecimalValueInput);               // Use the numeric to complete the conversion.
                // Debug.WriteLine($"TboxInput_KeyDown(): TboxInput: Enter key entered so did conversion.");
            }
        }

        /// <summary>
        /// Do conversion when TboxInput focus changes.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void TboxInput_LostFocus(object sender, RoutedEventArgs e)
        {
            _ = sender;     // Discard unused parameter.
            _ = e;          // Discard unused parameter.
            decimalValueInput = LibNum.TextBoxGetDecimal(TboxInput, EnumTextBoxUpdate.Yes);      // Get the input from the TextBox and convert to matching numeric.
            ConvertValues(EnumChangeType.DecimalValueInput);                   // Use the numeric to complete the conversion.
            // Debug.WriteLine($"TboxInput_LostFocus(): TboxInput: Lost focus so did conversion.");
        }

        /// <summary>
        /// On TboxInput_TextChanged() event, verify character entered still results in a valid numeric, otherwise discard last character entered.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void TboxInput_TextChanged(object sender, TextChangedEventArgs e)
        {
            _ = sender;     // Discard unused parameter.
            _ = e;          // Discard unused parameter.
            // Verify character entered still results in a valid numeric, otherwise discard last character entered.
            LibNum.NumericTextBoxTextChanged(TboxInput, EnumNumericType._decimal);
            // Debug.WriteLine($"TboxInput_TextChanged(): TboxInput: Text changed, verify input string is still valid numeric.");
        }

        /// <summary>
        /// On KeyDown event equal 'ENTER' key, toggle text selection on/off to make right-click copy easy.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void TboxOutput_KeyDown(object sender, KeyRoutedEventArgs e)    // (object sender, KeyRoutedEventArgs e)
        {
            _ = sender;     // Discard unused parameter.
            if (e.Key == Windows.System.VirtualKey.Enter)           // Check if 'Enter' key was pressed.  Ignore everything else.
            {
                e.Handled = true;
                if (TboxOutput.SelectionLength > 0)
                    TboxOutput.SelectionLength = 0;     // Unselect text.
                else
                    TboxOutput.SelectAll();             // Select all text to make right-click copy easy.
                // Debug.WriteLine($"TboxOutput_KeyDown(): TboxOutput.SelectionLength={TboxOutput.SelectionLength}");
            }
        }

        /// <summary>
        /// Purchase application button. Button visible if application has not been purchased, collapsed otherwise.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void ButPurchaseApp_Click(object sender, RoutedEventArgs e)
        {
            _ = sender;     // Discard unused parameter.
            _ = e;          // Discard unused parameter.
            await AppPurchaseBuy();
        }

        /// <summary>
        /// Invoked when user clicks ButRateApp. MS Store popup box will lock out all access to App.
        /// Goal is to get more App ratings in Microsoft Store without hassling User too much.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void ButRateApp_Click(object sender, RoutedEventArgs e)
        {
            _ = sender;     // Discard unused parameter.
            _ = e;          // Discard unused parameter.
            if (await mainPage.RateAppInW10StoreAsync())
                LibMPC.ButtonVisibility(ButRateApp, false);
        }

    }
}

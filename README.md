PrecisionUnitConverter:

This C# UWP App converts values of one unit to another related unit.  For example, a length conversion: 10 meters converts to 32.808398950131233595800524934 feet.
What is unique about this App is it uses Decimal Types for all calculations.  This results in much more precise conversions.  Decimal Types have 28-29 significant digits of precision where Double Types have 15 to 16 significant digits of precision.
As with any floating-point calculation, Decimal Types are subject to small calculation errors in certain situations.  Many of these errors are obvious and require smart rounding and/or truncation of one to six digits to get an exact value of the remaining digits.  There is no simple solution for every case.  Simply rounding a Decimal type to a Double Type will correct many of these small calculation errors but can lose many good digits of precision.  This is an option with this app and is handy for copy-paste operations into other apps that do not accept the precision of a Decimal Type.

This App requires library files LibMainPageCommon.cs, LibNumerics.cs, and LibUnitConversios.cs located in Library-Files-for-other-Repository-Apps to be linked under folder named 'Libraries' to compile.

I no longer intend to support this App so placing code on GitHub so others can use if useful.

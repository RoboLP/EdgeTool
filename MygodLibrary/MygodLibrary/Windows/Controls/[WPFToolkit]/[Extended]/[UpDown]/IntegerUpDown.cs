/************************************************************************

   Extended WPF Toolkit

   Copyright (C) 2010-2012 Xceed Software Inc.

   This program is provided to you under the terms of the Microsoft Public
   License (Ms-PL) as published at http://wpftoolkit.codeplex.com/license 

   This program can be provided to you by Xceed Software Inc. under a
   proprietary commercial license agreement for use in non-Open Source
   projects. The commercial version of Extended WPF Toolkit also includes
   priority technical support, commercial updates, and many additional 
   useful WPF controls if you license Xceed Business Suite for WPF.

   Visit http://xceed.com and follow @datagrid on Twitter.

  **********************************************************************/

using System;
using System.Windows;

namespace Mygod.Windows.Controls
{
    public class IntegerUpDown : NumericUpDown<long?>
    {
        #region Constructors

        static IntegerUpDown()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(IntegerUpDown), new FrameworkPropertyMetadata(typeof(IntegerUpDown)));
            DefaultValueProperty.OverrideMetadata(typeof(IntegerUpDown), new FrameworkPropertyMetadata(0L));
            IncrementProperty.OverrideMetadata(typeof(IntegerUpDown), new FrameworkPropertyMetadata(1L));
            MaximumProperty.OverrideMetadata(typeof(IntegerUpDown), new FrameworkPropertyMetadata(long.MaxValue));
            MinimumProperty.OverrideMetadata(typeof(IntegerUpDown), new FrameworkPropertyMetadata(long.MinValue));
        }

        #endregion //Constructors

        #region Base Class Overrides

        protected override long? CoerceValue(long? value)
        {
            if (value < Minimum)
                return Minimum;
            return value > Maximum ? Maximum : value;
        }

        protected override void OnIncrement()
        {
            if (Value.HasValue)
                Value += Increment;
            else
                Value = DefaultValue;
        }

        protected override void OnDecrement()
        {
            if (Value.HasValue)
                Value -= Increment;
            else
                Value = DefaultValue;
        }

        protected override long? ConvertTextToValue(string text)
        {
            long? result;

            if (string.IsNullOrEmpty(text))
                return null;

            try
            {
                //don't know why someone would format an integer as %, but just in case they do.
                result = FormatString.Contains("P") ? decimal.ToInt64(ParsePercent(text, CultureInfo)) : ParseLong(text, CultureInfo);
                result = CoerceValue(result);
            }
            catch
            {
                Text = ConvertValueToText();
                return Value;
            }

            return result;
        }

        protected override string ConvertValueToText()
        {
            if (Value == null)
                return string.Empty;

            return Value.Value.ToString(FormatString, CultureInfo);
        }

        protected override void SetValidSpinDirection()
        {
            var validDirections = ValidSpinDirections.None;

            if (Value < Maximum || !Value.HasValue)
                validDirections = validDirections | ValidSpinDirections.Increase;

            if (Value > Minimum || !Value.HasValue)
                validDirections = validDirections | ValidSpinDirections.Decrease;

            if (Spinner != null)
                Spinner.ValidSpinDirection = validDirections;
        }

        protected override void ValidateValue(long? value)
        {
            if (value < Minimum)
                Value = Minimum;
            else if (value > Maximum)
                Value = Maximum;
        }

        #endregion //Base Class Overrides
    }
}
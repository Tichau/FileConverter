// <copyright file="ConversionJob_Office.cs" company="AAllard">License: http://www.gnu.org/licenses/gpl.html GPL version 3.</copyright>

namespace FileConverter.ConversionJobs
{
    using System;
    using System.Globalization;
    using System.Reflection;

    using FileConverter.Diagnostics;

    public abstract class ConversionJob_Office : ConversionJob
    {
        private const int MsoAutomationSecurityForceDisable = 3;

        protected ConversionJob_Office() : base()
        {
        }

        protected ConversionJob_Office(ConversionPreset conversionPreset, string inputFilePath) : base(conversionPreset, inputFilePath)
        {
        }

        public enum ApplicationName
        {
            None,

            Word,
            Excel,
            PowerPoint
        }

        protected abstract ApplicationName Application
        {
            get;
        }

        protected override bool IsCancelable() => false;

        protected void HardenOfficeApplicationInstance(object officeApplication)
        {
            if (officeApplication == null)
            {
                return;
            }

            this.TrySetOfficeApplicationProperty(officeApplication, "AutomationSecurity", MsoAutomationSecurityForceDisable);
            this.TrySetOfficeApplicationProperty(officeApplication, "EnableEvents", false);
            this.TrySetOfficeApplicationProperty(officeApplication, "DisplayAlerts", 0);
            this.TrySetOfficeApplicationProperty(officeApplication, "AskToUpdateLinks", false);
        }

        protected override void Initialize()
        {
            base.Initialize();

            if (!Helpers.IsMicrosoftOfficeApplicationAvailable(this.Application))
            {
                switch (this.Application)
                {
                    case ApplicationName.Word:
                        this.ConversionFailed(Properties.Resources.ErrorMicrosoftWordIsNotAvailable);
                        return;

                    case ApplicationName.PowerPoint:
                        this.ConversionFailed(Properties.Resources.ErrorMicrosoftPowerPointIsNotAvailable);
                        return;

                    case ApplicationName.Excel:
                        this.ConversionFailed(Properties.Resources.ErrorMicrosoftExcelIsNotAvailable);
                        return;

                    default:
                        this.ConversionFailed(Properties.Resources.ErrorMicrosoftOfficeIsNotAvailable);
                        return;
                }
            }
        }

        protected override void OnConversionFailed()
        {
            base.OnConversionFailed();

            this.ReleaseOfficeApplicationInstanceIfNeeded();
        }

        protected abstract void InitializeOfficeApplicationInstanceIfNecessary();

        protected abstract void ReleaseOfficeApplicationInstanceIfNeeded();

        private void TrySetOfficeApplicationProperty(object officeApplication, string propertyName, object value)
        {
            try
            {
                PropertyInfo property = officeApplication.GetType().GetProperty(propertyName);
                if (property == null || !property.CanWrite)
                {
                    return;
                }

                object typedValue = this.ConvertValue(value, property.PropertyType);
                property.SetValue(officeApplication, typedValue, null);
            }
            catch (Exception exception)
            {
                Debug.Log($"Could not set Office automation property {propertyName}: {exception.Message}");
            }
        }

        private object ConvertValue(object value, Type propertyType)
        {
            if (propertyType.IsEnum)
            {
                return Enum.ToObject(propertyType, value);
            }

            return System.Convert.ChangeType(value, propertyType, CultureInfo.InvariantCulture);
        }
    }
}

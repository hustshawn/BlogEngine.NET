using System;
using NUglify;
using NUglify.JavaScript;

namespace BlogEngine.Core.Web.Scripting
{
    /// <summary>
    /// Helper class for performing minification of Javascript.
    /// Uses NUglify (replacement for AjaxMin).
    /// </summary>
    public sealed class JavascriptMinifier
    {
        /// <summary>
        /// Creates a new JavascriptMinifier instance.
        /// </summary>
        public JavascriptMinifier()
        {
            this.RemoveWhitespace = true;
            this.PreserveFunctionNames = true;
            this.VariableMinification = VariableMinification.None;
        }

        #region "Methods"

        /// <summary>
        /// Builds the required CodeSettings class needed for NUglify.
        /// </summary>
        private CodeSettings CreateCodeSettings()
        {
            var codeSettings = new CodeSettings();
            codeSettings.MinifyCode = false;
            codeSettings.OutputMode = (this.RemoveWhitespace ? OutputMode.SingleLine : OutputMode.MultipleLines);

            codeSettings.MinifyCode = this.ShouldMinifyCode;
            if (this.ShouldMinifyCode)
            {
                switch (this.VariableMinification)
                {
                    case VariableMinification.None:
                        codeSettings.LocalRenaming = LocalRenaming.KeepAll;
                        break;

                    case VariableMinification.LocalVariablesOnly:
                        codeSettings.LocalRenaming = LocalRenaming.KeepLocalizationVars;
                        break;

                    case VariableMinification.LocalVariablesAndFunctionArguments:
                        codeSettings.LocalRenaming = LocalRenaming.CrunchAll;
                        break;
                }
                codeSettings.EvalTreatment = EvalTreatment.MakeAllSafe;
                codeSettings.PreserveFunctionNames = this.PreserveFunctionNames;
            }
            return codeSettings;
        }

        /// <summary>
        /// Gets the minified version of the passed in script.
        /// </summary>
        public string Minify(string script)
        {
            if (this.ShouldMinify)
            {
                if (String.IsNullOrEmpty(script))
                {
                    return string.Empty;
                }
                else
                {
                    var result = Uglify.Js(script, this.CreateCodeSettings());
                    return result.Code ?? script;
                }
            }

            return script;
        }

        #endregion

        #region "Properties"

        /// <summary>
        /// Gets or sets variable minification level.
        /// </summary>
        public VariableMinification VariableMinification { get; set; }

        /// <summary>
        /// Gets or sets whether function names should be preserved.
        /// </summary>
        public bool PreserveFunctionNames { get; set; }

        /// <summary>
        /// Gets or sets whether whitespace should be removed.
        /// </summary>
        public bool RemoveWhitespace { get; set; }

        private bool ShouldMinifyCode
        {
            get
            {
                return ((!PreserveFunctionNames) || (this.VariableMinification != VariableMinification.None));
            }
        }

        private bool ShouldMinify
        {
            get
            {
                return ((this.RemoveWhitespace) || (this.ShouldMinifyCode));
            }
        }

        #endregion
    }

    /// <summary>
    /// Represents the way variables should be minified.
    /// </summary>
    public enum VariableMinification
    {
        /// <summary>No minification.</summary>
        None = 0,
        /// <summary>Only local variables.</summary>
        LocalVariablesOnly = 1,
        /// <summary>Local variables and function arguments.</summary>
        LocalVariablesAndFunctionArguments = 2
    }
}

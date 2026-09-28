// SimpleCaptcha created by Aaron Stannard (http://www.aaronstannard.com)
// Migrated to ASP.NET Core - removed WebControl, uses service pattern

namespace App_Code.Controls
{
    using System;
    using BlogEngine.Core;
    using BlogEngine.Core.Web.Extensions;

    /// <summary>
    /// SimpleCaptcha validation service - converted from WebControl to a service class.
    /// </summary>
    public class SimpleCaptchaControl
    {
        private const string SimpleCaptchaAnswerField = "simpleCaptchaValue";

        private string errorMessage;
        private bool valid;
        private string simpleCaptchaAnswer;
        private string simpleCaptchaLabel;
        private bool skipSimpleCaptcha = true;

        public string ErrorMessage
        {
            get => this.errorMessage ?? "Incorrect captcha answer.";
            set => this.errorMessage = value;
        }

        public bool IsValid
        {
            get => this.valid;
            set { }
        }

        public bool SimpleCaptchaEnabled
        {
            get
            {
                var captchaExtension = ExtensionManager.GetExtension("SimpleCaptcha");
                return captchaExtension.Enabled;
            }
        }

        public bool SimpleCaptchaNecessary
        {
            get
            {
                var settings = ExtensionManager.GetSettings("SimpleCaptcha");
                return !Security.IsAuthenticated ||
                       Convert.ToBoolean(settings.GetSingleValue("ShowForAuthenticatedUsers"));
            }
        }

        public string CaptchaLabel => simpleCaptchaLabel;

        public int TabIndex { get; set; }

        /// <summary>
        /// Initializes the control settings from ExtensionManager.
        /// Call this instead of the old OnInit lifecycle event.
        /// </summary>
        public void Initialize()
        {
            var settings = ExtensionManager.GetSettings("SimpleCaptcha");
            if (settings != null)
            {
                this.simpleCaptchaAnswer = settings.GetSingleValue("CaptchaAnswer");
                this.simpleCaptchaLabel = settings.GetSingleValue("CaptchaLabel");

                if (this.SimpleCaptchaEnabled && this.SimpleCaptchaNecessary)
                {
                    this.skipSimpleCaptcha = false;
                }

                if (String.IsNullOrEmpty(this.simpleCaptchaAnswer) || String.IsNullOrEmpty(this.simpleCaptchaLabel))
                {
                    throw new ApplicationException(
                        "SimpleCaptcha needs to be configured with an appropriate captcha label and a captcha value.");
                }
            }
        }

        public void Validate(string simpleCaptchaChallenge)
        {
            this.valid = this.skipSimpleCaptcha || this.simpleCaptchaAnswer.Equals(simpleCaptchaChallenge);
        }

        public void Validate()
        {
            // Without HttpContext form data, validate with empty string
            this.Validate(string.Empty);
        }

        /// <summary>
        /// Renders the SimpleCaptcha HTML for embedding in a Blazor component or view.
        /// </summary>
        public string RenderHtml()
        {
            if (this.skipSimpleCaptcha)
            {
                return string.Empty;
            }

            var sb = new System.Text.StringBuilder();
            sb.Append("<p>");
            sb.Append($"<label for=\"{SimpleCaptchaAnswerField}\" style=\"margin-right: 5px;\">");
            sb.Append(this.simpleCaptchaLabel);
            sb.Append("</label>");
            sb.Append($"<input id=\"{SimpleCaptchaAnswerField}\" name=\"{SimpleCaptchaAnswerField}\" type=\"text\" tabindex=\"{this.TabIndex}\" maxlength=\"{SimpleCaptcha.MaxCaptchaLength}\" value=\"\" />");
            sb.Append($"<span id=\"spnSimpleCaptchaIncorrect\" style=\"display:none;color:Red;\">");
            sb.Append(this.ErrorMessage);
            sb.Append("</span>");
            sb.Append("</p>");

            return sb.ToString();
        }
    }
}

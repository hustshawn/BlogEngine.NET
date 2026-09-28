// Copyright (c) 2007 Adrian Godong, Ben Maurer
// Adapted for blogengine by Filip Stanek ( http://www.bloodforge.com )
// Migrated to ASP.NET Core - removed WebControl, uses service pattern

namespace App_Code.Controls
{
    using System;
    using Microsoft.Extensions.Caching.Memory;

    using BlogEngine.Core;
    using BlogEngine.Core.Web.Extensions;

    using Recaptcha;

    /// <summary>
    /// The recaptcha control - converted from WebControl to a service class.
    /// Validates reCAPTCHA responses and manages logging.
    /// </summary>
    public class RecaptchaControl
    {
        private const string RecaptchaChallengeField = "recaptcha_challenge_field";
        private const string RecaptchaResponseField = "recaptcha_response_field";

        private string errorMessage;
        private string privateKey;
        private string publicKey;
        private RecaptchaResponse recaptchaResponse;
        private bool skipRecaptcha = true;

        public string ErrorMessage
        {
            get => this.errorMessage ?? "The verification words are incorrect.";
            set => this.errorMessage = value;
        }

        public bool IsValid
        {
            get => this.recaptchaResponse != null && this.recaptchaResponse.IsValid;
            set { }
        }

        public int MaxLogEntries
        {
            get
            {
                var settings = ExtensionManager.GetSettings("Recaptcha");
                return settings != null ? Convert.ToInt32(settings.GetSingleValue("MaxLogEntries")) : 0;
            }
        }

        public bool RecaptchaEnabled
        {
            get
            {
                var captchaExtension = ExtensionManager.GetExtension("Recaptcha");
                return captchaExtension.Enabled;
            }
        }

        public bool RecaptchaLoggingEnabled
        {
            get
            {
                ExtensionManager.GetSettings("Recaptcha");
                return this.MaxLogEntries > 0;
            }
        }

        public bool RecaptchaNecessary
        {
            get
            {
                var settings = ExtensionManager.GetSettings("Recaptcha");
                return !Security.IsAuthenticated ||
                       Convert.ToBoolean(settings.GetSingleValue("ShowForAuthenticatedUsers"));
            }
        }

        public string Theme { get; set; }
        public string Language { get; set; }
        public string UserUniqueIdentifier { get; set; }
        public int TabIndex { get; set; }

        internal DateTime PageLoadTime
        {
            get
            {
                return Blog.CurrentInstance.Cache[$"{UserUniqueIdentifier}PageLoadTime"] != null
                           ? Convert.ToDateTime(
                               Blog.CurrentInstance.Cache[$"{UserUniqueIdentifier}PageLoadTime"])
                           : DateTime.Now;
            }
            set => Blog.CurrentInstance.Cache[$"{UserUniqueIdentifier}PageLoadTime"] = value;
        }

        internal ushort RecaptchaAttempts
        {
            get
            {
                return Blog.CurrentInstance.Cache[$"{UserUniqueIdentifier}RecaptchaAttempts"] != null
                           ? Convert.ToUInt16(
                               Blog.CurrentInstance.Cache[$"{UserUniqueIdentifier}RecaptchaAttempts"])
                           : (ushort)0;
            }
            set => Blog.CurrentInstance.Cache[$"{UserUniqueIdentifier}RecaptchaAttempts"] = value;
        }

        internal string RecaptchaChallengeValue
        {
            get
            {
                return Blog.CurrentInstance.Cache[$"{UserUniqueIdentifier}RecaptchaChallengeValue"] != null
                        ? Convert.ToString(
                            Blog.CurrentInstance.Cache[$"{UserUniqueIdentifier}RecaptchaChallengeValue"])
                        : string.Empty;
            }
            set => Blog.CurrentInstance.Cache[$"{UserUniqueIdentifier}RecaptchaChallengeValue"] = value;
        }

        internal DateTime RecaptchaRenderTime
        {
            get
            {
                return Blog.CurrentInstance.Cache[$"{UserUniqueIdentifier}RecaptchaRenderTime"] != null
                           ? Convert.ToDateTime(
                               Blog.CurrentInstance.Cache[$"{UserUniqueIdentifier}RecaptchaRenderTime"])
                           : DateTime.Now;
            }
            set => Blog.CurrentInstance.Cache[$"{UserUniqueIdentifier}RecaptchaRenderTime"] = value;
        }

        internal string RecaptchaResponseValue
        {
            get
            {
                return Blog.CurrentInstance.Cache[$"{UserUniqueIdentifier}RecaptchaResponseValue"] != null
                        ? Convert.ToString(
                            Blog.CurrentInstance.Cache[$"{UserUniqueIdentifier}RecaptchaResponseValue"])
                        : string.Empty;
            }
            set => Blog.CurrentInstance.Cache[$"{UserUniqueIdentifier}RecaptchaResponseValue"] = value;
        }

        public string PublicKey => publicKey;

        /// <summary>
        /// Initializes the control settings from ExtensionManager.
        /// Call this instead of the old OnInit lifecycle event.
        /// </summary>
        public void Initialize()
        {
            var settings = ExtensionManager.GetSettings("Recaptcha");
            this.publicKey = settings.GetSingleValue("PublicKey");
            this.privateKey = settings.GetSingleValue("PrivateKey");

            if (String.IsNullOrEmpty(this.Theme))
            {
                this.Theme = settings.GetSingleValue("Theme");
            }

            if (String.IsNullOrEmpty(this.Language))
            {
                this.Language = settings.GetSingleValue("Language");
            }

            if (this.RecaptchaEnabled && this.RecaptchaNecessary)
            {
                this.skipRecaptcha = false;
            }

            if (String.IsNullOrEmpty(this.publicKey) || String.IsNullOrEmpty(this.privateKey))
            {
                throw new ApplicationException("reCAPTCHA needs to be configured with a public & private key.");
            }
        }

        public void UpdateLog(Comment comment)
        {
            if (!this.RecaptchaLoggingEnabled || this.skipRecaptcha)
            {
                return;
            }

            var log = RecaptchaLogger.ReadLogItems();

            var logItem = new RecaptchaLogItem
                {
                    Response = this.RecaptchaResponseValue, 
                    Challenge = this.RecaptchaChallengeValue, 
                    CommentId = comment.Id, 
                    Enabled = this.RecaptchaEnabled, 
                    Necessary = this.RecaptchaNecessary, 
                    NumberOfAttempts = this.RecaptchaAttempts, 
                    TimeToComment = DateTime.Now.Subtract(this.PageLoadTime).TotalSeconds, 
                    TimeToSolveCapcha = DateTime.Now.Subtract(this.RecaptchaRenderTime).TotalSeconds
                };
            log.Add(logItem);

            if (log.Count > this.MaxLogEntries)
            {
                log.RemoveRange(0, log.Count - this.MaxLogEntries);
            }

            RecaptchaLogger.SaveLogItems(log);

            this.RecaptchaAttempts = 0;
            this.PageLoadTime = DateTime.Now;
            Blog.CurrentInstance.Cache.Remove($"{UserUniqueIdentifier}RecaptchaChallengeValue");
            Blog.CurrentInstance.Cache.Remove($"{UserUniqueIdentifier}RecaptchaResponseValue");
        }

        public bool ValidateAsync(string response, string challenge)
        {
            if (this.RecaptchaLoggingEnabled)
            {
                this.RecaptchaAttempts++;
            }

            this.RecaptchaResponseValue = response;
            this.RecaptchaChallengeValue = challenge;
            this.Validate();
            return this.IsValid;
        }

        /// <summary>
        /// Validates with pre-set challenge and response values.
        /// </summary>
        public void Validate()
        {
            if (this.skipRecaptcha)
            {
                this.recaptchaResponse = RecaptchaResponse.Valid;
            }
            else
            {
                var validator = new RecaptchaValidator
                    {
                       PrivateKey = this.privateKey, RemoteIP = Utils.GetClientIP() 
                    };
                validator.Challenge = this.RecaptchaChallengeValue;
                validator.Response = this.RecaptchaResponseValue;

                this.recaptchaResponse = validator.Validate();
            }
        }

        /// <summary>
        /// Validates with explicit challenge and response from form data.
        /// </summary>
        public void Validate(string challengeValue, string responseValue)
        {
            if (!string.IsNullOrEmpty(challengeValue))
            {
                this.RecaptchaChallengeValue = challengeValue;
            }
            if (!string.IsNullOrEmpty(responseValue))
            {
                this.RecaptchaResponseValue = responseValue;
            }
            this.Validate();
        }

        /// <summary>
        /// Renders the reCAPTCHA HTML for embedding in a Blazor component or view.
        /// </summary>
        public string RenderHtml()
        {
            if (this.skipRecaptcha)
            {
                return string.Empty;
            }

            var sb = new System.Text.StringBuilder();
            sb.AppendLine($"<script type=\"text/javascript\" src=\"http://www.google.com/recaptcha/api/js/recaptcha_ajax.js\"></script>");
            sb.AppendLine("<span id=\"spnCaptchaIncorrect\" style=\"color:Red;display:none;\">The captcha text was not valid. Please try again.</span>");
            sb.AppendLine("<div id=\"recaptcha_placeholder\"></div>");
            sb.AppendLine("<script type=\"text/javascript\">");
            sb.AppendLine("function showRecaptcha() {");
            sb.AppendLine($"Recaptcha.create('{this.publicKey}', 'recaptcha_placeholder', {{");
            sb.AppendLine($"theme: '{this.Theme}',");
            sb.AppendLine($"lang: '{this.Language}',");
            sb.AppendLine($"tabindex: {this.TabIndex}");
            sb.AppendLine("})");
            sb.AppendLine("}");
            sb.AppendLine("var rc_oldonload = window.onload;");
            sb.AppendLine("if (typeof window.onload != 'function') {");
            sb.AppendLine("window.onload = showRecaptcha;");
            sb.AppendLine("}");
            sb.AppendLine("else {");
            sb.AppendLine("window.onload = function() {");
            sb.AppendLine("rc_oldonload();");
            sb.AppendLine("showRecaptcha();");
            sb.AppendLine("}");
            sb.AppendLine("}");
            sb.AppendLine("</script>");

            return sb.ToString();
        }
    }
}

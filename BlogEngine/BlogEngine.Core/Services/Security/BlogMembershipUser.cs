namespace BlogEngine.Core
{
    using System;

    /// <summary>
    /// Replacement for System.Web.Security.MembershipUser.
    /// A POCO representing a blog user with membership information.
    /// </summary>
    public class BlogMembershipUser
    {
        /// <summary>
        /// Gets or sets the provider name.
        /// </summary>
        public string ProviderName { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the username.
        /// </summary>
        public string UserName { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the provider user key.
        /// </summary>
        public object ProviderUserKey { get; set; }

        /// <summary>
        /// Gets or sets the email.
        /// </summary>
        public string Email { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the password question.
        /// </summary>
        public string PasswordQuestion { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the comment (used to store password hash in XML provider).
        /// </summary>
        public string Comment { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets whether user is approved.
        /// </summary>
        public bool IsApproved { get; set; } = true;

        /// <summary>
        /// Gets or sets whether user is locked out.
        /// </summary>
        public bool IsLockedOut { get; set; }

        /// <summary>
        /// Gets or sets the creation date.
        /// </summary>
        public DateTime CreationDate { get; set; } = DateTime.Now;

        /// <summary>
        /// Gets or sets the last login date.
        /// </summary>
        public DateTime LastLoginDate { get; set; } = DateTime.Now;

        /// <summary>
        /// Gets or sets the last activity date.
        /// </summary>
        public DateTime LastActivityDate { get; set; } = DateTime.Now;

        /// <summary>
        /// Gets or sets the last password changed date.
        /// </summary>
        public DateTime LastPasswordChangedDate { get; set; } = DateTime.Now;

        /// <summary>
        /// Gets or sets the last lockout date.
        /// </summary>
        public DateTime LastLockoutDate { get; set; } = new DateTime(1980, 1, 1);

        /// <summary>
        /// Creates a new BlogMembershipUser with default values.
        /// </summary>
        public BlogMembershipUser()
        {
        }

        /// <summary>
        /// Creates a new BlogMembershipUser with all properties.
        /// </summary>
        public BlogMembershipUser(
            string providerName,
            string userName,
            object providerUserKey,
            string email,
            string passwordQuestion,
            string comment,
            bool isApproved,
            bool isLockedOut,
            DateTime creationDate,
            DateTime lastLoginDate,
            DateTime lastActivityDate,
            DateTime lastPasswordChangedDate,
            DateTime lastLockoutDate)
        {
            ProviderName = providerName;
            UserName = userName;
            ProviderUserKey = providerUserKey;
            Email = email;
            PasswordQuestion = passwordQuestion;
            Comment = comment;
            IsApproved = isApproved;
            IsLockedOut = isLockedOut;
            CreationDate = creationDate;
            LastLoginDate = lastLoginDate;
            LastActivityDate = lastActivityDate;
            LastPasswordChangedDate = lastPasswordChangedDate;
            LastLockoutDate = lastLockoutDate;
        }
    }
}

#pragma warning disable 1591
namespace BlogEngine.Core.FileSystem
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using Microsoft.Data.SqlClient;

    /// <summary>
    /// Provides data access for the file store database tables, replacing
    /// the original Linq-to-SQL DataContext with raw ADO.NET via Microsoft.Data.SqlClient.
    /// </summary>
    public partial class FileStoreDb : IDisposable
    {
        private readonly string _connectionString;

        partial void OnCreated();

        public FileStoreDb(string connection)
        {
            _connectionString = connection;
            OnCreated();
        }

        /// <summary>
        /// Retrieves all FileStoreDirectory records.
        /// </summary>
        public List<FileStoreDirectory> GetDirectories()
        {
            var list = new List<FileStoreDirectory>();
            using var conn = new SqlConnection(_connectionString);
            conn.Open();
            using var cmd = new SqlCommand("SELECT Id, ParentID, BlogID, [Name], FullPath, CreateDate, LastAccess, LastModify FROM dbo.be_FileStoreDirectory", conn);
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                list.Add(new FileStoreDirectory
                {
                    Id = reader.GetGuid(0),
                    ParentID = reader.IsDBNull(1) ? (Guid?)null : reader.GetGuid(1),
                    BlogID = reader.GetGuid(2),
                    Name = reader.GetString(3),
                    FullPath = reader.GetString(4),
                    CreateDate = reader.GetDateTime(5),
                    LastAccess = reader.GetDateTime(6),
                    LastModify = reader.GetDateTime(7)
                });
            }
            return list;
        }

        /// <summary>
        /// Retrieves all FileStoreFile records.
        /// </summary>
        public List<FileStoreFile> GetFiles()
        {
            var list = new List<FileStoreFile>();
            using var conn = new SqlConnection(_connectionString);
            conn.Open();
            using var cmd = new SqlCommand("SELECT FileID, ParentDirectoryID, [Name], FullPath, Contents, Size, CreateDate, LastAccess, LastModify FROM dbo.be_FileStoreFiles", conn);
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                list.Add(new FileStoreFile
                {
                    FileID = reader.GetGuid(0),
                    ParentDirectoryID = reader.GetGuid(1),
                    Name = reader.GetString(2),
                    FullPath = reader.GetString(3),
                    Contents = (byte[])reader.GetValue(4),
                    Size = reader.GetInt32(5),
                    CreateDate = reader.GetDateTime(6),
                    LastAccess = reader.GetDateTime(7),
                    LastModify = reader.GetDateTime(8)
                });
            }
            return list;
        }

        /// <summary>
        /// Retrieves all FileStoreFileThumb records.
        /// </summary>
        public List<FileStoreFileThumb> GetFileThumbs()
        {
            var list = new List<FileStoreFileThumb>();
            using var conn = new SqlConnection(_connectionString);
            conn.Open();
            using var cmd = new SqlCommand("SELECT thumbnailId, FileId, size, contents FROM dbo.be_FileStoreFileThumbs", conn);
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                list.Add(new FileStoreFileThumb
                {
                    thumbnailId = reader.GetGuid(0),
                    FileId = reader.GetGuid(1),
                    size = reader.GetInt32(2),
                    contents = (byte[])reader.GetValue(3)
                });
            }
            return list;
        }

        /// <summary>
        /// Inserts a directory record.
        /// </summary>
        public void InsertDirectory(FileStoreDirectory dir)
        {
            using var conn = new SqlConnection(_connectionString);
            conn.Open();
            using var cmd = new SqlCommand(
                "INSERT INTO dbo.be_FileStoreDirectory (Id, ParentID, BlogID, [Name], FullPath, CreateDate, LastAccess, LastModify) VALUES (@Id, @ParentID, @BlogID, @Name, @FullPath, @CreateDate, @LastAccess, @LastModify)", conn);
            cmd.Parameters.AddWithValue("@Id", dir.Id);
            cmd.Parameters.AddWithValue("@ParentID", (object)dir.ParentID ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@BlogID", dir.BlogID);
            cmd.Parameters.AddWithValue("@Name", dir.Name);
            cmd.Parameters.AddWithValue("@FullPath", dir.FullPath);
            cmd.Parameters.AddWithValue("@CreateDate", dir.CreateDate);
            cmd.Parameters.AddWithValue("@LastAccess", dir.LastAccess);
            cmd.Parameters.AddWithValue("@LastModify", dir.LastModify);
            cmd.ExecuteNonQuery();
        }

        /// <summary>
        /// Updates a directory record.
        /// </summary>
        public void UpdateDirectory(FileStoreDirectory dir)
        {
            using var conn = new SqlConnection(_connectionString);
            conn.Open();
            using var cmd = new SqlCommand(
                "UPDATE dbo.be_FileStoreDirectory SET ParentID=@ParentID, BlogID=@BlogID, [Name]=@Name, FullPath=@FullPath, CreateDate=@CreateDate, LastAccess=@LastAccess, LastModify=@LastModify WHERE Id=@Id", conn);
            cmd.Parameters.AddWithValue("@Id", dir.Id);
            cmd.Parameters.AddWithValue("@ParentID", (object)dir.ParentID ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@BlogID", dir.BlogID);
            cmd.Parameters.AddWithValue("@Name", dir.Name);
            cmd.Parameters.AddWithValue("@FullPath", dir.FullPath);
            cmd.Parameters.AddWithValue("@CreateDate", dir.CreateDate);
            cmd.Parameters.AddWithValue("@LastAccess", dir.LastAccess);
            cmd.Parameters.AddWithValue("@LastModify", dir.LastModify);
            cmd.ExecuteNonQuery();
        }

        /// <summary>
        /// Deletes a directory record.
        /// </summary>
        public void DeleteDirectory(Guid id)
        {
            using var conn = new SqlConnection(_connectionString);
            conn.Open();
            using var cmd = new SqlCommand("DELETE FROM dbo.be_FileStoreDirectory WHERE Id=@Id", conn);
            cmd.Parameters.AddWithValue("@Id", id);
            cmd.ExecuteNonQuery();
        }

        /// <summary>
        /// Inserts a file record.
        /// </summary>
        public void InsertFile(FileStoreFile file)
        {
            using var conn = new SqlConnection(_connectionString);
            conn.Open();
            using var cmd = new SqlCommand(
                "INSERT INTO dbo.be_FileStoreFiles (FileID, ParentDirectoryID, [Name], FullPath, Contents, Size, CreateDate, LastAccess, LastModify) VALUES (@FileID, @ParentDirectoryID, @Name, @FullPath, @Contents, @Size, @CreateDate, @LastAccess, @LastModify)", conn);
            cmd.Parameters.AddWithValue("@FileID", file.FileID);
            cmd.Parameters.AddWithValue("@ParentDirectoryID", file.ParentDirectoryID);
            cmd.Parameters.AddWithValue("@Name", file.Name);
            cmd.Parameters.AddWithValue("@FullPath", file.FullPath);
            cmd.Parameters.AddWithValue("@Contents", file.Contents);
            cmd.Parameters.AddWithValue("@Size", file.Size);
            cmd.Parameters.AddWithValue("@CreateDate", file.CreateDate);
            cmd.Parameters.AddWithValue("@LastAccess", file.LastAccess);
            cmd.Parameters.AddWithValue("@LastModify", file.LastModify);
            cmd.ExecuteNonQuery();
        }

        /// <summary>
        /// Updates a file record.
        /// </summary>
        public void UpdateFile(FileStoreFile file)
        {
            using var conn = new SqlConnection(_connectionString);
            conn.Open();
            using var cmd = new SqlCommand(
                "UPDATE dbo.be_FileStoreFiles SET ParentDirectoryID=@ParentDirectoryID, [Name]=@Name, FullPath=@FullPath, Contents=@Contents, Size=@Size, CreateDate=@CreateDate, LastAccess=@LastAccess, LastModify=@LastModify WHERE FileID=@FileID", conn);
            cmd.Parameters.AddWithValue("@FileID", file.FileID);
            cmd.Parameters.AddWithValue("@ParentDirectoryID", file.ParentDirectoryID);
            cmd.Parameters.AddWithValue("@Name", file.Name);
            cmd.Parameters.AddWithValue("@FullPath", file.FullPath);
            cmd.Parameters.AddWithValue("@Contents", file.Contents);
            cmd.Parameters.AddWithValue("@Size", file.Size);
            cmd.Parameters.AddWithValue("@CreateDate", file.CreateDate);
            cmd.Parameters.AddWithValue("@LastAccess", file.LastAccess);
            cmd.Parameters.AddWithValue("@LastModify", file.LastModify);
            cmd.ExecuteNonQuery();
        }

        /// <summary>
        /// Deletes a file record.
        /// </summary>
        public void DeleteFile(Guid fileId)
        {
            using var conn = new SqlConnection(_connectionString);
            conn.Open();
            using var cmd = new SqlCommand("DELETE FROM dbo.be_FileStoreFiles WHERE FileID=@FileID", conn);
            cmd.Parameters.AddWithValue("@FileID", fileId);
            cmd.ExecuteNonQuery();
        }

        /// <summary>
        /// Inserts a file thumbnail record.
        /// </summary>
        public void InsertFileThumb(FileStoreFileThumb thumb)
        {
            using var conn = new SqlConnection(_connectionString);
            conn.Open();
            using var cmd = new SqlCommand(
                "INSERT INTO dbo.be_FileStoreFileThumbs (thumbnailId, FileId, size, contents) VALUES (@thumbnailId, @FileId, @size, @contents)", conn);
            cmd.Parameters.AddWithValue("@thumbnailId", thumb.thumbnailId);
            cmd.Parameters.AddWithValue("@FileId", thumb.FileId);
            cmd.Parameters.AddWithValue("@size", thumb.size);
            cmd.Parameters.AddWithValue("@contents", thumb.contents);
            cmd.ExecuteNonQuery();
        }

        /// <summary>
        /// Updates a file thumbnail record.
        /// </summary>
        public void UpdateFileThumb(FileStoreFileThumb thumb)
        {
            using var conn = new SqlConnection(_connectionString);
            conn.Open();
            using var cmd = new SqlCommand(
                "UPDATE dbo.be_FileStoreFileThumbs SET FileId=@FileId, size=@size, contents=@contents WHERE thumbnailId=@thumbnailId", conn);
            cmd.Parameters.AddWithValue("@thumbnailId", thumb.thumbnailId);
            cmd.Parameters.AddWithValue("@FileId", thumb.FileId);
            cmd.Parameters.AddWithValue("@size", thumb.size);
            cmd.Parameters.AddWithValue("@contents", thumb.contents);
            cmd.ExecuteNonQuery();
        }

        /// <summary>
        /// Deletes a file thumbnail record.
        /// </summary>
        public void DeleteFileThumb(Guid thumbnailId)
        {
            using var conn = new SqlConnection(_connectionString);
            conn.Open();
            using var cmd = new SqlCommand("DELETE FROM dbo.be_FileStoreFileThumbs WHERE thumbnailId=@thumbnailId", conn);
            cmd.Parameters.AddWithValue("@thumbnailId", thumbnailId);
            cmd.ExecuteNonQuery();
        }

        /// <summary>
        /// Submits all pending changes. Provided for API compatibility with the old DataContext pattern.
        /// In the raw ADO.NET implementation, each operation is immediately committed.
        /// </summary>
        public void SubmitChanges()
        {
            // No-op: each Insert/Update/Delete above executes immediately.
        }

        /// <summary>
        /// Provides query access to FileStoreDirectory records (backward compat property).
        /// </summary>
        public List<FileStoreDirectory> FileStoreDirectories => GetDirectories();

        /// <summary>
        /// Provides query access to FileStoreFile records (backward compat property).
        /// </summary>
        public List<FileStoreFile> FileStoreFiles => GetFiles();

        /// <summary>
        /// Provides query access to FileStoreFileThumb records (backward compat property).
        /// </summary>
        public List<FileStoreFileThumb> FileStoreFileThumbs => GetFileThumbs();

        public void Dispose()
        {
            // No persistent connection to dispose.
        }
    }

    /// <summary>
    /// Represents a row in the be_FileStoreDirectory table.
    /// </summary>
    public partial class FileStoreDirectory : INotifyPropertyChanging, INotifyPropertyChanged
    {
        private static PropertyChangingEventArgs emptyChangingEventArgs = new PropertyChangingEventArgs(string.Empty);

        public Guid Id { get; set; }
        public Guid? ParentID { get; set; }
        public Guid BlogID { get; set; }
        public string Name { get; set; }
        public string FullPath { get; set; }
        public DateTime CreateDate { get; set; }
        public DateTime LastAccess { get; set; }
        public DateTime LastModify { get; set; }

        /// <summary>
        /// Navigation property: files in this directory.
        /// Must be populated manually when needed.
        /// </summary>
        public List<FileStoreFile> FileStoreFiles { get; set; } = new List<FileStoreFile>();

        public event PropertyChangingEventHandler PropertyChanging;
        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void SendPropertyChanging()
        {
            PropertyChanging?.Invoke(this, emptyChangingEventArgs);
        }

        protected virtual void SendPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    /// <summary>
    /// Represents a row in the be_FileStoreFileThumbs table.
    /// </summary>
    public partial class FileStoreFileThumb : INotifyPropertyChanging, INotifyPropertyChanged
    {
        private static PropertyChangingEventArgs emptyChangingEventArgs = new PropertyChangingEventArgs(string.Empty);

        public Guid thumbnailId { get; set; }
        public Guid FileId { get; set; }
        public int size { get; set; }
        public byte[] contents { get; set; }

        /// <summary>
        /// Navigation property: the parent file.
        /// Must be populated manually when needed.
        /// </summary>
        public FileStoreFile FileStoreFile { get; set; }

        public event PropertyChangingEventHandler PropertyChanging;
        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void SendPropertyChanging()
        {
            PropertyChanging?.Invoke(this, emptyChangingEventArgs);
        }

        protected virtual void SendPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    /// <summary>
    /// Represents a row in the be_FileStoreFiles table.
    /// </summary>
    public partial class FileStoreFile : INotifyPropertyChanging, INotifyPropertyChanged
    {
        private static PropertyChangingEventArgs emptyChangingEventArgs = new PropertyChangingEventArgs(string.Empty);

        public Guid FileID { get; set; }
        public Guid ParentDirectoryID { get; set; }
        public string Name { get; set; }
        public string FullPath { get; set; }
        public byte[] Contents { get; set; }
        public int Size { get; set; }
        public DateTime CreateDate { get; set; }
        public DateTime LastAccess { get; set; }
        public DateTime LastModify { get; set; }

        /// <summary>
        /// Navigation property: thumbnails for this file.
        /// Must be populated manually when needed.
        /// </summary>
        public List<FileStoreFileThumb> FileStoreFileThumbs { get; set; } = new List<FileStoreFileThumb>();

        /// <summary>
        /// Navigation property: the parent directory.
        /// Must be populated manually when needed.
        /// </summary>
        public FileStoreDirectory FileStoreDirectory { get; set; }

        public event PropertyChangingEventHandler PropertyChanging;
        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void SendPropertyChanging()
        {
            PropertyChanging?.Invoke(this, emptyChangingEventArgs);
        }

        protected virtual void SendPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
#pragma warning restore 1591

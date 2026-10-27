using System;

namespace NNIdec.Core
{
    /// <summary>
    /// Hasil operasi standar untuk komunikasi PLC IDEC non-generik (misalnya Write).
    /// </summary>
    public class IdecResult
    {
        /// <summary>
        /// Menunjukkan apakah operasi berhasil dieksekusi.
        /// </summary>
        public bool IsSuccess { get; set; }

        /// <summary>
        /// Kode error jika operasi gagal. Bernilai 0 jika operasi berhasil.
        /// </summary>
        public int ErrorCode { get; set; }

        /// <summary>
        /// Pesan penjelasan dari hasil operasi atau pesan error.
        /// </summary>
        public string Message { get; set; } = string.Empty;

        /// <summary>
        /// Konstruktor default.
        /// </summary>
        public IdecResult()
        {
        }

        /// <summary>
        /// Konstruktor dengan status sukses dan pesan.
        /// </summary>
        public IdecResult(bool isSuccess, string message = "", int errorCode = 0)
        {
            IsSuccess = isSuccess;
            Message = message;
            ErrorCode = errorCode;
        }

        /// <summary>
        /// Membuat hasil operasi yang sukses.
        /// </summary>
        public static IdecResult Ok(string message = "Success")
        {
            IdecResult result = new IdecResult();
            result.IsSuccess = true;
            result.Message = message;
            result.ErrorCode = 0;
            return result;
        }

        /// <summary>
        /// Membuat hasil operasi yang gagal.
        /// </summary>
        public static IdecResult Fail(string message, int errorCode = -1)
        {
            IdecResult result = new IdecResult();
            result.IsSuccess = false;
            result.Message = message;
            result.ErrorCode = errorCode;
            return result;
        }

        public override string ToString()
        {
            if (IsSuccess)
            {
                return "[Success] " + Message;
            }
            else
            {
                return "[Failed] Code: " + ErrorCode + ", Message: " + Message;
            }
        }
    }

    /// <summary>
    /// Hasil operasi standar untuk komunikasi PLC IDEC bertipe data generik (misalnya Read).
    /// </summary>
    /// <typeparam name="T">Tipe data hasil pembacaan.</typeparam>
    public class IdecResult<T> : IdecResult
    {
        /// <summary>
        /// Data konten hasil pembacaan dari PLC.
        /// </summary>
        public T? Content { get; set; }

        /// <summary>
        /// Alias alternatif untuk Content agar lebih mudah diakses.
        /// </summary>
        public T? Data => Content;

        /// <summary>
        /// Konstruktor default.
        /// </summary>
        public IdecResult()
        {
        }

        /// <summary>
        /// Konstruktor dengan nilai data dan status sukses.
        /// </summary>
        public IdecResult(bool isSuccess, T? content, string message = "", int errorCode = 0)
            : base(isSuccess, message, errorCode)
        {
            Content = content;
        }

        /// <summary>
        /// Membuat hasil pembacaan yang sukses dengan membawa data.
        /// </summary>
        public static IdecResult<T> Ok(T content, string message = "Success")
        {
            IdecResult<T> result = new IdecResult<T>();
            result.IsSuccess = true;
            result.Content = content;
            result.Message = message;
            result.ErrorCode = 0;
            return result;
        }

        /// <summary>
        /// Membuat hasil pembacaan yang gagal.
        /// </summary>
        public static new IdecResult<T> Fail(string message, int errorCode = -1)
        {
            IdecResult<T> result = new IdecResult<T>();
            result.IsSuccess = false;
            result.Content = default;
            result.Message = message;
            result.ErrorCode = errorCode;
            return result;
        }

        public override string ToString()
        {
            if (IsSuccess)
            {
                string dataStr = "null";
                if (Content != null)
                {
                    dataStr = Content.ToString() ?? "null";
                }
                return "[Success] Content: " + dataStr;
            }
            else
            {
                return "[Failed] Code: " + ErrorCode + ", Message: " + Message;
            }
        }
    }
}
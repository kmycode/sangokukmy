using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SangokuKmy.Models.Common.Definitions
{
    /// <summary>
    /// アプリケーションの基本となる例外
    /// </summary>
    public class SangokuKmyException : Exception
    {
        /// <summary>
        /// ステータスコード
        /// </summary>
        public int StatusCode { get; }

        /// <summary>
        /// エラーコード
        /// </summary>
        public ErrorCode ErrorCode { get; }
        
        public SangokuKmyException(int status, ErrorCode code)
        {
            this.StatusCode = status;
            this.ErrorCode = code;
        }

        public SangokuKmyException(ErrorCode code) : this((int)((uint)code % 1000), code)
        {
        }
    }
}

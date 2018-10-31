using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SangokuKmy.Models.Common.Definitions
{
    /// <summary>
    /// アクセストークンのスコープ
    /// </summary>
    [Flags]
    public enum Scope : uint
    {
        All = 0b111_1111_1111_1111_1111_1111_1111_1111,
    }

    /// <summary>
    /// エラーコード
    /// </summary>
    public enum ErrorCode : uint
    {
        /// <summary>
        /// データベース接続のエラー
        /// </summary>
        DatabaseError = 1_503
    }
}
